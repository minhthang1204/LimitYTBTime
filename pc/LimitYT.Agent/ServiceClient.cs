using System.IO.Pipes;
using System.Text;
using LimitYT.Core.Ipc;

namespace LimitYT.Agent;

/// <summary>Giữ một kết nối pipe tới Service; tự kết nối lại khi lỗi.</summary>
internal sealed class ServiceClient : IDisposable
{
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public async Task<AgentReply?> SendAsync(AgentHeartbeat heartbeat)
    {
        try
        {
            if (_pipe is not { IsConnected: true })
            {
                Reset();
                _pipe = new NamedPipeClientStream(".", PipeNames.Agent, PipeDirection.InOut, PipeOptions.Asynchronous);
                await _pipe.ConnectAsync(2000);
                _reader = new StreamReader(_pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
                _writer = new StreamWriter(_pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _writer!.WriteLineAsync(PipeJson.Serialize(heartbeat).AsMemory(), timeout.Token);
            var line = await _reader!.ReadLineAsync(timeout.Token);
            return line is null ? null : PipeJson.Deserialize<AgentReply>(line);
        }
        catch (Exception e) when (e is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        {
            Reset();
            return null;
        }
    }

    public void Dispose() => Reset();

    private void Reset()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _pipe?.Dispose();
        _reader = null;
        _writer = null;
        _pipe = null;
    }
}
