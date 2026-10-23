using LimitYT.Core.Config;
using LimitYT.Core.Usage;

namespace LimitYT.Core.Rules;

public enum BlockReason
{
    None,
    BlockedWindow,
    DailyQuota,
    WeeklyQuota,
    OfflineCap,
}

/// <param name="Left">Thời gian còn được xem liên tục (khi Allowed).</param>
/// <param name="NextChangeUtc">Khi bị chặn: thời điểm hết chặn, null nếu không xác định.</param>
public sealed record Decision(bool Allowed, BlockReason Reason, TimeSpan Left, DateTimeOffset? NextChangeUtc, bool Unlocked = false)
{
    public static Decision Block(BlockReason reason, DateTimeOffset? until) => new(false, reason, TimeSpan.Zero, until);

    public string Describe() => Reason switch
    {
        BlockReason.BlockedWindow => "Đang trong giờ cấm",
        BlockReason.DailyQuota => "Đã hết giờ xem hôm nay",
        BlockReason.WeeklyQuota => "Đã hết giờ xem tuần này",
        BlockReason.OfflineCap => "Chưa đồng bộ được với TV, tạm hết giờ",
        _ => Unlocked ? "Phụ huynh đang mở khoá tạm thời" : "Được phép xem",
    };
}

/// <summary>Trạng thái khi không đồng bộ được với thiết bị kia (docs/01-phan-tich.md §8).</summary>
public sealed record OfflineState(long BudgetMs, long SelfWeekUsedAtStartMs);

/// <summary>Quy tắc chung cho PC và TV (docs/02-kien-truc.md §5). Phải cho cùng kết quả với bản Kotlin trên spec/rules-test-vectors.json.</summary>
public sealed class RulesEngine(LimitConfig config)
{
    private const long MinuteMs = 60_000;

    public LimitConfig Config { get; } = config;
    public LocalCalendar Calendar { get; } = new(config);

    public Decision Evaluate(
        DateTimeOffset now,
        UsageLedger ledger,
        string selfDevice,
        OfflineState? offline = null,
        DateTimeOffset? unlockedUntil = null)
    {
        if (unlockedUntil is { } until && until > now)
            return new Decision(true, BlockReason.None, until - now, null, Unlocked: true);

        var local = Calendar.ToLocal(now);
        if (Calendar.ActiveWindowEnd(local) is { } windowEnd)
            return Decision.Block(BlockReason.BlockedWindow, Calendar.ToUtc(windowEnd));

        var today = DateOnly.FromDateTime(local);
        var weekStart = Calendar.WeekStartOf(today);

        var weeklyLeft = Config.WeeklyQuotaMinutes * MinuteMs - ledger.NetUsedMs(weekStart, today);
        if (weeklyLeft <= 0)
            return Decision.Block(BlockReason.WeeklyQuota, Calendar.ToUtc(weekStart.AddDays(7).ToDateTime(TimeOnly.MinValue)));

        var dailyLeft = Calendar.DailyQuotaMinutes(today) * MinuteMs - ledger.NetUsedMs(today);
        if (dailyLeft <= 0)
            return Decision.Block(BlockReason.DailyQuota, Calendar.ToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue)));

        var left = Math.Min(weeklyLeft, dailyLeft);

        if (offline is not null)
        {
            var selfUsedSinceOffline = ledger.DeviceMs(selfDevice, weekStart, today) - offline.SelfWeekUsedAtStartMs;
            var offlineLeft = offline.BudgetMs - selfUsedSinceOffline;
            if (offlineLeft <= 0) return Decision.Block(BlockReason.OfflineCap, null);
            left = Math.Min(left, offlineLeft);
        }

        if (Calendar.NextWindowStart(local) is { } nextWindow)
            left = Math.Min(left, (long)(nextWindow - local).TotalMilliseconds);

        return new Decision(true, BlockReason.None, TimeSpan.FromMilliseconds(left), null);
    }
}
