namespace LimitYT.Core.Sync;

public sealed class SyncPacket
{
    public string DeviceId { get; set; } = "";
    public string SentAtUtc { get; set; } = "";
    public int ConfigVersion { get; set; }
    public string? ConfigJson { get; set; }
    public List<UsageItem> Usage { get; set; } = [];
    public List<AdjustmentItem> Adjustments { get; set; } = [];
}

public sealed class UsageItem
{
    public string Device { get; set; } = "";
    public string Date { get; set; } = "";
    public long Ms { get; set; }
}

public sealed class AdjustmentItem
{
    public string Id { get; set; } = "";
    public string Date { get; set; } = "";
    public long Ms { get; set; }
    public string? Reason { get; set; }
    public string CreatedAtUtc { get; set; } = "";
}
