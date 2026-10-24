using LimitYT.Core.Blocking;

namespace LimitYT.Agent;

internal sealed record WindowHit(IntPtr Handle, string Title, string ServiceId);

/// <summary>Tìm các cửa sổ top-level (của mọi tiến trình) có tiêu đề khớp dịch vụ bị giới hạn.</summary>
internal sealed class WindowWatcher(ProcessInspector processes)
{
    public List<WindowHit> Scan(TitleMatcher matcher)
    {
        var hits = new List<WindowHit>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            var title = NativeMethods.GetTitle(hWnd);
            if (title.Length == 0) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (IsIgnored(processes.Get((int)pid))) return true;

            if (matcher.Match(title) is { } serviceId)
                hits.Add(new WindowHit(hWnd, title, serviceId));
            return true;
        }, IntPtr.Zero);
        return hits;
    }

    // Thư mục tên "YouTube" trong File Explorer không phải đang xem YouTube. So theo đường dẫn đầy đủ,
    // vì chỉ so tên thì một trình duyệt portable đổi tên thành explorer.exe sẽ lọt qua.
    private static bool IsIgnored(ProcessInfo p) =>
        p.Path.Equals(ProcessInspector.ExplorerPath, StringComparison.OrdinalIgnoreCase) ||
        p.Path.Equals(ProcessInspector.SelfPath, StringComparison.OrdinalIgnoreCase);
}
