namespace LimitYT.Agent;

/// <summary>Log chẩn đoán, chỉ bật khi đặt biến môi trường LIMITYT_AGENT_LOG = đường dẫn file.</summary>
internal static class AgentLog
{
    private static readonly string? Path = Environment.GetEnvironmentVariable("LIMITYT_AGENT_LOG");

    public static void Write(string message)
    {
        if (string.IsNullOrEmpty(Path)) return;
        try { File.AppendAllText(Path, $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}"); }
        catch (IOException) { }
    }
}
