namespace LimitYT.Agent;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Mỗi phiên đăng nhập chỉ có một Agent (Service có thể khởi động lại Agent khi đã có sẵn từ Run key).
        using var mutex = new Mutex(true, @"Local\LimitYT.Agent", out var isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new AgentContext());
    }
}
