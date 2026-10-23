namespace LimitYT.Core.Usage;

public static class Devices
{
    public const string Pc = "pc";
    public const string Tv = "tv";
}

public sealed record UsageEntry(string Device, DateOnly Date, long Ms);

/// <summary>Điều chỉnh của phụ huynh. Ms dương = cộng thêm thời gian được xem; âm = trừ (phạt).</summary>
public sealed record Adjustment(string Id, DateOnly Date, long Ms, string? Reason, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Sổ ghi giờ đã dùng (docs/02-kien-truc.md §4.2). Mỗi thiết bị chỉ ghi dòng của chính nó;
/// gộp hai bản = lấy max theo (thiết bị, ngày) và hợp các adjustment theo Id, nên không bao giờ xung đột.
/// </summary>
public sealed class UsageLedger
{
    private readonly Dictionary<(string Device, DateOnly Date), long> _usage = new();
    private readonly Dictionary<string, Adjustment> _adjustments = new();

    public IEnumerable<UsageEntry> Usage => _usage.Select(kv => new UsageEntry(kv.Key.Device, kv.Key.Date, kv.Value));
    public IEnumerable<Adjustment> Adjustments => _adjustments.Values;

    public void Add(string device, DateOnly date, long ms)
    {
        if (ms <= 0) return;
        _usage[(device, date)] = DeviceMs(device, date) + ms;
    }

    /// <returns>true nếu sổ thay đổi.</returns>
    public bool Merge(UsageEntry entry)
    {
        var key = (entry.Device, entry.Date);
        if (_usage.TryGetValue(key, out var current) && current >= entry.Ms) return false;
        _usage[key] = entry.Ms;
        return true;
    }

    /// <returns>true nếu là adjustment mới.</returns>
    public bool Merge(Adjustment adjustment) => _adjustments.TryAdd(adjustment.Id, adjustment);

    public void Merge(UsageLedger other)
    {
        foreach (var e in other.Usage) Merge(e);
        foreach (var a in other.Adjustments) Merge(a);
    }

    public long DeviceMs(string device, DateOnly date) => _usage.GetValueOrDefault((device, date));

    public long DeviceMs(string device, DateOnly from, DateOnly to) =>
        _usage.Where(kv => kv.Key.Device == device && kv.Key.Date >= from && kv.Key.Date <= to).Sum(kv => kv.Value);

    public long TotalMs(DateOnly from, DateOnly to) =>
        _usage.Where(kv => kv.Key.Date >= from && kv.Key.Date <= to).Sum(kv => kv.Value);

    public long AdjustmentMs(DateOnly from, DateOnly to) =>
        _adjustments.Values.Where(a => a.Date >= from && a.Date <= to).Sum(a => a.Ms);

    /// <summary>Giờ đã dùng sau khi trừ adjustment. Có thể âm (khi phụ huynh cộng thêm giờ) — không kẹp ở đây.</summary>
    public long NetUsedMs(DateOnly from, DateOnly to) => TotalMs(from, to) - AdjustmentMs(from, to);

    public long NetUsedMs(DateOnly date) => NetUsedMs(date, date);

    public void PruneBefore(DateOnly date)
    {
        foreach (var key in _usage.Keys.Where(k => k.Date < date).ToList()) _usage.Remove(key);
        foreach (var id in _adjustments.Values.Where(a => a.Date < date).Select(a => a.Id).ToList()) _adjustments.Remove(id);
    }
}
