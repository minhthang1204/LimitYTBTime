namespace LimitYT.Core.Config;

/// <summary>
/// Các phép tính lịch theo giờ địa phương: ngày nghỉ, đầu tuần, khung giờ cấm.
/// Mọi DateTime ở đây là giờ địa phương (Kind = Unspecified).
/// </summary>
public sealed class LocalCalendar
{
    private readonly LimitConfig _config;
    private readonly TimeZoneInfo _tz;
    private readonly DayOfWeek _weekStart;
    private readonly List<Window> _windows;
    private readonly List<(DateOnly From, DateOnly To)> _holidays;
    private readonly Dictionary<DateOnly, bool> _overrides;

    private sealed record Window(HashSet<DayOfWeek> Days, TimeSpan From, TimeSpan To, bool SchoolOnly);

    public LocalCalendar(LimitConfig config)
    {
        _config = config;
        _tz = FindTimeZone(config.TimeZone);
        _weekStart = DayNames.Parse(config.WeekStart);
        _windows = config.BlockedWindows
            .Select(w => new Window(w.Days.Select(DayNames.Parse).ToHashSet(), TimeOfDay.Parse(w.From), TimeOfDay.Parse(w.To), w.SchoolOnly))
            .ToList();
        _holidays = config.Holidays
            .Select(h => (DateOnly.ParseExact(h.From, "yyyy-MM-dd"), DateOnly.ParseExact(h.To, "yyyy-MM-dd")))
            .ToList();
        _overrides = config.HolidayOverrides.ToDictionary(kv => DateOnly.ParseExact(kv.Key, "yyyy-MM-dd"), kv => kv.Value);
    }

    public static TimeZoneInfo FindTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var winId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(winId);
        }
    }

    public DateTime ToLocal(DateTimeOffset instant) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(instant, _tz).DateTime, DateTimeKind.Unspecified);

    public DateTimeOffset ToUtc(DateTime local) =>
        new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _tz), TimeSpan.Zero);

    public DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(ToLocal(instant));

    public bool IsHoliday(DateOnly date) =>
        _overrides.TryGetValue(date, out var forced) ? forced : _holidays.Any(h => h.From <= date && date <= h.To);

    public DateOnly WeekStartOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek - (int)_weekStart + 7) % 7));

    public int DailyQuotaMinutes(DateOnly date) =>
        IsHoliday(date) && _config.HolidayDailyQuotaMinutes is int h ? h : _config.DailyQuotaMinutes;

    /// <summary>Nếu <paramref name="t"/> nằm trong khung giờ cấm, trả về thời điểm hết cấm (tính cả các khung nối tiếp nhau).</summary>
    public DateTime? ActiveWindowEnd(DateTime t)
    {
        DateTime? result = null;
        var probe = t;
        for (var guard = 0; guard < 32; guard++)
        {
            DateTime? end = null;
            foreach (var (start, e) in InstancesStartingOn(DateOnly.FromDateTime(probe).AddDays(-1), 2))
                if (start <= probe && probe < e && (end is null || e > end))
                    end = e;
            if (end is null) break;
            result = end;
            probe = end.Value;
        }
        return result;
    }

    /// <summary>Thời điểm bắt đầu khung giờ cấm tiếp theo sau <paramref name="t"/> (tìm trong 8 ngày tới).</summary>
    public DateTime? NextWindowStart(DateTime t)
    {
        DateTime? best = null;
        foreach (var (start, _) in InstancesStartingOn(DateOnly.FromDateTime(t), 9))
            if (start > t && (best is null || start < best))
                best = start;
        return best;
    }

    private IEnumerable<(DateTime Start, DateTime End)> InstancesStartingOn(DateOnly firstDay, int dayCount)
    {
        for (var i = 0; i < dayCount; i++)
        {
            var day = firstDay.AddDays(i);
            var midnight = day.ToDateTime(TimeOnly.MinValue);
            foreach (var w in _windows)
            {
                if (!w.Days.Contains(day.DayOfWeek)) continue;
                if (w.SchoolOnly && IsHoliday(day)) continue;
                var start = midnight + w.From;
                var end = w.To > w.From ? midnight + w.To : midnight.AddDays(1) + w.To;
                yield return (start, end);
            }
        }
    }

    /// <summary>Tách một khoảng dài <paramref name="ms"/> kết thúc lúc <paramref name="end"/> theo ngày địa phương.</summary>
    public List<(DateOnly Date, long Ms)> SplitByLocalDate(DateTimeOffset end, long ms)
    {
        var result = new List<(DateOnly, long)>();
        var localEnd = ToLocal(end);
        var cursor = localEnd.AddMilliseconds(-ms);
        while (cursor < localEnd)
        {
            var nextMidnight = cursor.Date.AddDays(1);
            var segmentEnd = nextMidnight < localEnd ? nextMidnight : localEnd;
            result.Add((DateOnly.FromDateTime(cursor), (long)(segmentEnd - cursor).TotalMilliseconds));
            cursor = segmentEnd;
        }
        return result;
    }
}
