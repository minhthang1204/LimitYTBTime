using System.Diagnostics;
using LimitYT.Core.Blocking;

namespace LimitYT.Agent;

internal sealed record WindowHit(IntPtr Handle, string Title, string ServiceId);

/// <summary>Tìm các cửa sổ top-level (của mọi tiến trình) có tiêu đề khớp dịch vụ bị giới hạn.</summary>
internal sealed class WindowWatcher
{
    // Thư mục tên "YouTube" trong File Explorer không phải đang xem YouTube.
    private static readonly HashSet<string> IgnoredProcesses = new(StringComparer.OrdinalIgnoreCase) { "explorer", "LimitYT.Agent" };

    private readonly Dictionary<uint, string> _processNames = new();

    public List<WindowHit> Scan(TitleMatcher matcher)
    {
        var hits = new List<WindowHit>();
        var seenPids = new HashSet<uint>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            var title = NativeMethods.GetTitle(hWnd);
            if (title.Length == 0) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            seenPids.Add(pid);
            if (IgnoredProcesses.Contains(ProcessName(pid))) return true;

            if (matcher.Match(title) is { } serviceId)
                hits.Add(new WindowHit(hWnd, title, serviceId));
            return true;
        }, IntPtr.Zero);

        foreach (var pid in _processNames.Keys.Where(p => !seenPids.Contains(p)).ToList())
            _processNames.Remove(pid);
        return hits;
    }

    private string ProcessName(uint pid)
    {
        if (_processNames.TryGetValue(pid, out var name)) return name;
        try
        {
            using var p = Process.GetProcessById((int)pid);
            name = p.ProcessName;
        }
        catch (ArgumentException)
        {
            name = "";
        }
        _processNames[pid] = name;
        return name;
    }
}
