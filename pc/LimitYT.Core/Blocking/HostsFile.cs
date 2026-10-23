using System.Text;

namespace LimitYT.Core.Blocking;

/// <summary>Thêm/xoá khối chặn của LimitYT trong nội dung file hosts, giữ nguyên phần còn lại.</summary>
public static class HostsFile
{
    public const string BeginMarker = "# >>> LimitYT BEGIN (do not edit)";
    public const string EndMarker = "# <<< LimitYT END";

    public static string Apply(string content, IEnumerable<string> domains)
    {
        var newline = content.Contains("\r\n") || content.Length == 0 ? "\r\n" : "\n";
        var body = Remove(content).TrimEnd('\r', '\n');

        var sb = new StringBuilder(body);
        if (body.Length > 0) sb.Append(newline).Append(newline);
        sb.Append(BeginMarker).Append(newline);
        foreach (var d in domains.Select(d => d.Trim().ToLowerInvariant()).Where(d => d.Length > 0).Distinct())
        {
            sb.Append("0.0.0.0 ").Append(d).Append(newline);
            sb.Append(":: ").Append(d).Append(newline);
        }
        sb.Append(EndMarker).Append(newline);
        return sb.ToString();
    }

    public static string Remove(string content)
    {
        var begin = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        if (begin < 0) return content;
        var end = content.IndexOf(EndMarker, begin, StringComparison.Ordinal);
        end = end < 0 ? content.Length : end + EndMarker.Length;

        var before = content[..begin].TrimEnd('\r', '\n');
        var after = content[end..].TrimStart('\r', '\n');
        var newline = content.Contains("\r\n") ? "\r\n" : "\n";

        if (before.Length == 0) return after;
        return after.Length == 0 ? before + newline : before + newline + after;
    }
}
