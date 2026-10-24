using System.Globalization;
using System.Text.RegularExpressions;

namespace LimitYT.Core.Config;

public static class ConfigValidator
{
    public static List<string> Validate(LimitConfig c)
    {
        var errors = new List<string>();

        if (c.WeeklyQuotaMinutes < 0) errors.Add("Quota tuần không được âm.");
        if (c.DailyQuotaMinutes < 0) errors.Add("Quota ngày không được âm.");
        if (c.DailyQuotaMinutes > c.WeeklyQuotaMinutes) errors.Add("Quota ngày không được lớn hơn quota tuần.");
        if (c.HolidayDailyQuotaMinutes is < 0) errors.Add("Quota ngày nghỉ không được âm.");
        if (c.DailyQuotaMinutes > 24 * 60) errors.Add("Quota ngày không được quá 24 giờ.");

        try { LocalCalendar.FindTimeZone(c.TimeZone); }
        catch (Exception) { errors.Add($"Múi giờ không hợp lệ: {c.TimeZone}"); }

        try { DayNames.Parse(c.WeekStart); }
        catch (FormatException e) { errors.Add(e.Message); }

        for (var i = 0; i < c.BlockedWindows.Count; i++)
        {
            var w = c.BlockedWindows[i];
            if (w.Days.Count == 0) errors.Add($"Khung giờ #{i + 1}: chưa chọn ngày.");
            foreach (var d in w.Days)
            {
                try { DayNames.Parse(d); }
                catch (FormatException e) { errors.Add($"Khung giờ #{i + 1}: {e.Message}"); }
            }
            var fromOk = TimeOfDay.TryParse(w.From, out var from);
            var toOk = TimeOfDay.TryParse(w.To, out var to);
            if (!fromOk) errors.Add($"Khung giờ #{i + 1}: giờ bắt đầu '{w.From}' sai định dạng HH:mm.");
            if (!toOk) errors.Add($"Khung giờ #{i + 1}: giờ kết thúc '{w.To}' sai định dạng HH:mm.");
            if (fromOk && toOk && from == to) errors.Add($"Khung giờ #{i + 1}: giờ bắt đầu và kết thúc trùng nhau.");
        }

        foreach (var h in c.Holidays)
        {
            if (!TryParseDate(h.From, out var f) || !TryParseDate(h.To, out var t))
                errors.Add($"Ngày nghỉ '{h}': ngày sai định dạng yyyy-MM-dd.");
            else if (t < f)
                errors.Add($"Ngày nghỉ '{h}': ngày kết thúc trước ngày bắt đầu.");
        }

        foreach (var key in c.HolidayOverrides.Keys)
            if (!TryParseDate(key, out _)) errors.Add($"Ngày '{key}' sai định dạng yyyy-MM-dd.");

        foreach (var s in c.Services)
            foreach (var p in s.TitlePatterns)
                if (!IsValidRegex(p)) errors.Add($"Dịch vụ {s.Id}: regex tiêu đề không hợp lệ: {p}");
        foreach (var p in c.TitleExcludePatterns)
            if (!IsValidRegex(p)) errors.Add($"Regex loại trừ không hợp lệ: {p}");
        foreach (var p in c.PcBlockedProcessPatterns)
            if (!IsValidRegex(p)) errors.Add($"Regex chương trình bị chặn không hợp lệ: {p}");

        return errors;
    }

    public static bool TryParseDate(string s, out DateOnly date) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool IsValidRegex(string pattern)
    {
        try { _ = new Regex(pattern); return true; }
        catch (ArgumentException) { return false; }
    }
}

public static class TimeOfDay
{
    public static bool TryParse(string s, out TimeSpan value) =>
        TimeSpan.TryParseExact(s, @"hh\:mm", CultureInfo.InvariantCulture, out value) && value < TimeSpan.FromDays(1);

    public static TimeSpan Parse(string s) =>
        TryParse(s, out var v) ? v : throw new FormatException($"Giờ '{s}' sai định dạng HH:mm.");
}
