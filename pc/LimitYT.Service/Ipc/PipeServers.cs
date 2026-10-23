using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using LimitYT.Core.Ipc;
using LimitYT.Service.Native;

namespace LimitYT.Service.Ipc;

/// <summary>Named pipe nhận từng dòng JSON và trả lời bằng một dòng JSON.</summary>
public abstract class PipeServer(string pipeName, ILogger logger) : BackgroundService
{
    protected abstract PipeSecurity CreateSecurity();

    protected abstract string Handle(NamedPipeServerStream pipe, string line);

    protected static PipeSecurity Security(params (WellKnownSidType Sid, PipeAccessRights Rights)[] allow)
    {
        var security = new PipeSecurity();
        foreach (var (sid, rights) in allow)
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(sid, null), rights, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        // Tài khoản chạy Service cần quyền CreateNewInstance để mở thêm instance cho kết nối tiếp theo.
        using var self = WindowsIdentity.GetCurrent();
        security.AddAccessRule(new PipeAccessRule(self.User!, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        return security;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, CreateSecurity());
                await server.WaitForConnectionAsync(ct);
                _ = ServeAsync(server, ct);
                server = null;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Lỗi pipe {Pipe}", pipeName);
                try { await Task.Delay(1000, ct); }
                catch (OperationCanceledException) { break; }
            }
            finally
            {
                server?.Dispose();
            }
        }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
                while (await reader.ReadLineAsync(ct) is { } line)
                {
                    string reply;
                    try
                    {
                        reply = Handle(pipe, line);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Lỗi xử lý yêu cầu trên pipe {Pipe}", pipeName);
                        reply = PipeJson.Serialize(new AdminReply(false, "Lỗi nội bộ: " + e.Message));
                    }
                    await writer.WriteLineAsync(reply.AsMemory(), ct);
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException)
            {
            }
        }
    }
}

public sealed class AgentPipeServer(LimitController controller, ILogger<AgentPipeServer> logger)
    : PipeServer(PipeNames.Agent, logger)
{
    protected override PipeSecurity CreateSecurity() =>
        Security((WellKnownSidType.AuthenticatedUserSid, PipeAccessRights.ReadWrite));

    protected override string Handle(NamedPipeServerStream pipe, string line)
    {
        var heartbeat = PipeJson.Deserialize<AgentHeartbeat>(line) ?? throw new InvalidDataException("Heartbeat rỗng");
        // Lấy session từ chính kết nối pipe thay vì tin số do Agent gửi.
        if (!NativeMethods.GetNamedPipeClientSessionId(pipe.SafePipeHandle, out var sessionId))
            throw new InvalidOperationException("Không xác định được phiên của Agent");
        return PipeJson.Serialize(controller.OnHeartbeat(sessionId, heartbeat));
    }
}

public sealed class AdminPipeServer(LimitController controller, ILogger<AdminPipeServer> logger)
    : PipeServer(PipeNames.Admin, logger)
{
    protected override PipeSecurity CreateSecurity() => AppPaths.DryRun
        ? Security((WellKnownSidType.AuthenticatedUserSid, PipeAccessRights.ReadWrite))
        : Security((WellKnownSidType.BuiltinAdministratorsSid, PipeAccessRights.ReadWrite));

    protected override string Handle(NamedPipeServerStream pipe, string line)
    {
        var request = PipeJson.Deserialize<AdminRequest>(line) ?? throw new InvalidDataException("Yêu cầu rỗng");
        return PipeJson.Serialize(controller.Admin(request.Args));
    }
}
