namespace LimitYT.Service;

public static class AppPaths
{
    /// <summary>Đặt LIMITYT_DATA_DIR để chạy thử với thư mục dữ liệu khác.</summary>
    public static readonly string DataDir = Environment.GetEnvironmentVariable("LIMITYT_DATA_DIR") is { Length: > 0 } dir
        ? dir
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LimitYT");

    public static string ConfigFile => Path.Combine(DataDir, "config.json");
    public static string DbFile => Path.Combine(DataDir, "data.db");
    public static string AgentExe => Path.Combine(AppContext.BaseDirectory, "LimitYT.Agent.exe");

    /// <summary>
    /// --dry-run: không sửa hosts/registry, chỉ ghi log; pipe quản trị mở cho mọi user đăng nhập.
    /// Chỉ dùng để chạy thử trên máy phát triển, không dùng khi cài thật.
    /// </summary>
    public static bool DryRun { get; set; }
}
