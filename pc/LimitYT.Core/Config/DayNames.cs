namespace LimitYT.Core.Config;

/// <summary>Tên ngày trong cấu hình: "Mon".."Sun". Hỗ trợ khoảng "Mon-Fri" và danh sách "Mon,Wed".</summary>
public static class DayNames
{
    private static readonly string[] Names = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    public static DayOfWeek Parse(string name)
    {
        var i = Array.FindIndex(Names, n => n.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return i >= 0 ? (DayOfWeek)i : throw new FormatException($"Tên ngày không hợp lệ: '{name}' (dùng Mon..Sun)");
    }

    public static string Name(DayOfWeek d) => Names[(int)d];

    /// <summary>"Mon-Fri", "Sat,Sun", "all" → danh sách tên ngày.</summary>
    public static List<string> ParseSpec(string spec)
    {
        if (spec.Equals("all", StringComparison.OrdinalIgnoreCase) || spec.Equals("daily", StringComparison.OrdinalIgnoreCase))
            return [.. Defaults.AllDays];

        var result = new List<string>();
        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var range = part.Split('-');
            if (range.Length == 1)
            {
                result.Add(Name(Parse(range[0])));
                continue;
            }
            var from = (int)Parse(range[0]);
            var to = (int)Parse(range[1]);
            for (var d = from; ; d = (d + 1) % 7)
            {
                result.Add(Names[d]);
                if (d == to) break;
            }
        }
        return result.Distinct().ToList();
    }

    /// <summary>Gộp các ngày liên tiếp (theo thứ tự Mon..Sun): ["Mon".."Fri"] → "Mon-Fri".</summary>
    public static string Format(IEnumerable<string> days)
    {
        var order = days.Select(Parse).Select(d => ((int)d + 6) % 7).Distinct().Order().ToList();
        if (order.Count == 7) return "Hằng ngày";

        var parts = new List<string>();
        for (var i = 0; i < order.Count;)
        {
            var j = i;
            while (j + 1 < order.Count && order[j + 1] == order[j] + 1) j++;
            var first = Names[(order[i] + 1) % 7];
            var last = Names[(order[j] + 1) % 7];
            parts.Add(j - i >= 2 ? $"{first}-{last}" : j > i ? $"{first},{last}" : first);
            i = j + 1;
        }
        return string.Join(",", parts);
    }
}
