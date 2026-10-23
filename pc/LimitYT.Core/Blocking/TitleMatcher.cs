using System.Text.RegularExpressions;

namespace LimitYT.Core.Blocking;

/// <summary>Nhận diện cửa sổ đang xem dịch vụ bị giới hạn qua tiêu đề (docs/02-kien-truc.md §6.3, case B1).</summary>
public sealed class TitleMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private readonly List<(string ServiceId, Regex Regex)> _patterns;
    private readonly List<Regex> _excludes;

    public TitleMatcher(IEnumerable<(string ServiceId, IEnumerable<string> Patterns)> services, IEnumerable<string> excludes)
    {
        _patterns = services
            .SelectMany(s => s.Patterns.Select(p => (s.ServiceId, new Regex(p, RegexOptions.CultureInvariant, RegexTimeout))))
            .ToList();
        _excludes = excludes.Select(p => new Regex(p, RegexOptions.CultureInvariant, RegexTimeout)).ToList();
    }

    /// <returns>Id dịch vụ khớp, hoặc null.</returns>
    public string? Match(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        try
        {
            if (_excludes.Any(r => r.IsMatch(title))) return null;
            foreach (var (serviceId, regex) in _patterns)
                if (regex.IsMatch(title)) return serviceId;
        }
        catch (RegexMatchTimeoutException)
        {
        }
        return null;
    }
}
