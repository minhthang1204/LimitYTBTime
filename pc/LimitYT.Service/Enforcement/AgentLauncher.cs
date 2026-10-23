using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using LimitYT.Service.Native;

namespace LimitYT.Service.Enforcement;

/// <summary>Khởi động lại Agent trong phiên người dùng khi Agent bị tắt (case B2).</summary>
public sealed class AgentLauncher(ILogger<AgentLauncher> log, TimeProvider time)
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;

    public void TryLaunch(uint sessionId)
    {
        var now = time.GetUtcNow();
        if (now - _lastAttempt < RetryInterval) return;
        _lastAttempt = now;

        var exe = AppPaths.AgentExe;
        if (!File.Exists(exe))
        {
            log.LogWarning("Không tìm thấy {Exe}", exe);
            return;
        }

        if (!NativeMethods.WTSQueryUserToken(sessionId, out var token))
        {
            log.LogWarning(new Win32Exception(Marshal.GetLastWin32Error()), "Không lấy được token của phiên {Session}", sessionId);
            return;
        }

        var environment = IntPtr.Zero;
        try
        {
            NativeMethods.CreateEnvironmentBlock(out environment, token, false);
            var startup = new NativeMethods.StartupInfo
            {
                cb = Marshal.SizeOf<NativeMethods.StartupInfo>(),
                lpDesktop = @"winsta0\default",
            };
            var commandLine = new StringBuilder($"\"{exe}\"");
            if (NativeMethods.CreateProcessAsUser(token, null, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                    NativeMethods.CreateUnicodeEnvironment, environment, Path.GetDirectoryName(exe), ref startup, out var info))
            {
                NativeMethods.CloseHandle(info.hThread);
                NativeMethods.CloseHandle(info.hProcess);
                log.LogInformation("Đã khởi động lại Agent trong phiên {Session}", sessionId);
            }
            else
            {
                log.LogWarning(new Win32Exception(Marshal.GetLastWin32Error()), "Không khởi động được Agent");
            }
        }
        finally
        {
            if (environment != IntPtr.Zero) NativeMethods.DestroyEnvironmentBlock(environment);
            NativeMethods.CloseHandle(token);
        }
    }
}
