using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LimitYT.Agent;

/// <summary>
/// Đóng YouTube/TikTok đang mở khi bị chặn (docs/02-kien-truc.md §6.3):
/// lần đầu gửi Ctrl+W để đóng tab; nếu sau 2 giây tiêu đề vẫn khớp thì đóng cả cửa sổ.
/// </summary>
internal sealed class TabCloser
{
    private static readonly TimeSpan Escalate = TimeSpan.FromSeconds(2);
    private readonly Dictionary<IntPtr, DateTime> _attempts = new();

    public void Close(IReadOnlyCollection<WindowHit> hits)
    {
        var now = DateTime.UtcNow;
        foreach (var handle in _attempts.Keys.Where(h => hits.All(x => x.Handle != h)).ToList())
            _attempts.Remove(handle);

        foreach (var hit in hits)
        {
            if (!_attempts.TryGetValue(hit.Handle, out var first))
            {
                _attempts[hit.Handle] = now;
                if (BringToFront(hit.Handle)) SendCtrlW();
                else NativeMethods.PostMessage(hit.Handle, NativeMethods.WmClose, IntPtr.Zero, IntPtr.Zero);
            }
            else if (now - first >= Escalate)
            {
                NativeMethods.PostMessage(hit.Handle, NativeMethods.WmClose, IntPtr.Zero, IntPtr.Zero);
                _attempts[hit.Handle] = now;
            }
        }
    }

    public static void KillExecutables(IEnumerable<string> executables)
    {
        foreach (var exe in executables)
        {
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)))
            {
                try { p.Kill(entireProcessTree: true); }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
                finally { p.Dispose(); }
            }
        }
    }

    private static bool BringToFront(IntPtr hWnd)
    {
        if (NativeMethods.IsIconic(hWnd)) NativeMethods.ShowWindow(hWnd, NativeMethods.SwRestore);
        // Nhấn-nhả Alt để Windows cho phép đổi cửa sổ foreground.
        SendKeys([NativeMethods.Key(NativeMethods.VkMenu, false), NativeMethods.Key(NativeMethods.VkMenu, true)]);
        NativeMethods.SetForegroundWindow(hWnd);
        Thread.Sleep(50);
        return NativeMethods.GetForegroundWindow() == hWnd;
    }

    private static void SendCtrlW() => SendKeys(
    [
        NativeMethods.Key(NativeMethods.VkControl, false),
        NativeMethods.Key(NativeMethods.VkW, false),
        NativeMethods.Key(NativeMethods.VkW, true),
        NativeMethods.Key(NativeMethods.VkControl, true),
    ]);

    private static void SendKeys(NativeMethods.Input[] inputs) =>
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>());
}
