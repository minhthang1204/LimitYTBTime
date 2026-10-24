using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LimitYT.Agent;

internal sealed record ProcessInfo(string Name, string Path, string Product, string Description);

/// <summary>
/// Thông tin tiến trình theo PID: đường dẫn đầy đủ + ProductName/FileDescription trong file exe.
/// Nhận diện theo các thông tin này thay vì tên exe, vì con có thể đổi tên exe (ví dụ chép trình duyệt thành explorer.exe).
/// </summary>
internal sealed class ProcessInspector
{
    private static readonly ProcessInfo Unknown = new("", "", "", "");
    private readonly Dictionary<int, ProcessInfo> _byPid = new();
    private readonly Dictionary<string, (string Product, string Description)> _versionByPath = new(StringComparer.OrdinalIgnoreCase);

    public static readonly string ExplorerPath =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

    public static readonly string SelfPath = Environment.ProcessPath ?? "";

    public ProcessInfo Get(int pid)
    {
        if (_byPid.TryGetValue(pid, out var info)) return info;
        try
        {
            using var p = Process.GetProcessById(pid);
            info = Describe(p);
        }
        catch (ArgumentException)
        {
            info = Unknown;
        }
        _byPid[pid] = info;
        return info;
    }

    /// <summary>Bỏ các PID không còn chạy (PID có thể bị dùng lại cho tiến trình khác).</summary>
    public void Retain(IReadOnlySet<int> alive)
    {
        foreach (var pid in _byPid.Keys.Where(p => !alive.Contains(p)).ToList()) _byPid.Remove(pid);
    }

    /// <summary>Tắt các tiến trình trong phiên này khớp một trong các mẫu. Trả về tên các chương trình đã tắt.</summary>
    public List<string> KillMatching(IReadOnlyCollection<Regex> patterns)
    {
        var killed = new List<string>();
        var session = Process.GetCurrentProcess().SessionId;
        var alive = new HashSet<int>();
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                alive.Add(p.Id);
                if (patterns.Count == 0 || p.SessionId != session) continue;
                var info = Get(p.Id);
                if (info.Path.Length == 0 || info.Path.Equals(SelfPath, StringComparison.OrdinalIgnoreCase)) continue;
                if (!patterns.Any(r => r.IsMatch(info.Name) || r.IsMatch(info.Product) || r.IsMatch(info.Description))) continue;
                try
                {
                    p.Kill(entireProcessTree: true);
                    killed.Add(info.Product.Length > 0 ? info.Product : info.Name);
                }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
            }
        }
        Retain(alive);
        return killed.Distinct().ToList();
    }

    private ProcessInfo Describe(Process p)
    {
        string path;
        try { path = p.MainModule?.FileName ?? ""; }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { path = ""; }

        var (product, description) = path.Length > 0 ? VersionOf(path) : ("", "");
        return new ProcessInfo(p.ProcessName, path, product, description);
    }

    private (string Product, string Description) VersionOf(string path)
    {
        if (_versionByPath.TryGetValue(path, out var cached)) return cached;
        try
        {
            var v = FileVersionInfo.GetVersionInfo(path);
            cached = ((v.ProductName ?? "").Trim(), (v.FileDescription ?? "").Trim());
        }
        catch (Exception e) when (e is FileNotFoundException or IOException)
        {
            cached = ("", "");
        }
        return _versionByPath[path] = cached;
    }
}
