using System.Text.RegularExpressions;

namespace LimitYT.Core;

/// <summary>"7h", "90m", "1h30m", "-30m", "45" (phút) → số phút.</summary>
public static partial class DurationParser
{
    [GeneratedRegex(@"^(?<sign>[-+])?(?:(?<h>\d+)h)?(?:(?<m>\d+)m?)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();

    public static bool TryParseMinutes(string text, out int minutes)
    {
        minutes = 0;
        var m = Pattern().Match(text.Trim());
        if (!m.Success || (!m.Groups["h"].Success && !m.Groups["m"].Success)) return false;
        var total = (m.Groups["h"].Success ? int.Parse(m.Groups["h"].Value) * 60 : 0)
                    + (m.Groups["m"].Success ? int.Parse(m.Groups["m"].Value) : 0);
        minutes = m.Groups["sign"].Value == "-" ? -total : total;
        return true;
    }

    /// <param name="roundUp">true cho thời gian còn lại ("còn 30 phút" thay vì "còn 29 phút" khi còn 29 phút 50 giây).</param>
    public static string Format(long ms, bool roundUp = false)
    {
        var minutes = Math.Max(0, ms) / 60_000.0;
        var totalMinutes = (long)(roundUp ? Math.Ceiling(minutes) : Math.Floor(minutes));
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return h > 0 ? $"{h} giờ {m:00} phút" : $"{m} phút";
    }
}
