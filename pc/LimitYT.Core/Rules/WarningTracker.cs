namespace LimitYT.Core.Rules;

/// <summary>
/// Quyết định khi nào hiện cảnh báo "còn N phút" (F9, case A7):
/// khi thời gian còn lại đi qua một ngưỡng, hoặc khi bắt đầu xem mà đã ở dưới ngưỡng lớn nhất.
/// </summary>
public sealed class WarningTracker
{
    private int? _lastLeftMinutes;
    private bool _wasWatching;

    /// <returns>Số phút cần báo, hoặc null nếu không cần báo.</returns>
    public int? Next(Decision decision, bool watching, IReadOnlyCollection<int> thresholds)
    {
        if (!decision.Allowed || thresholds.Count == 0)
        {
            _lastLeftMinutes = null;
            _wasWatching = watching;
            return null;
        }

        var left = (int)Math.Ceiling(decision.Left.TotalMinutes);
        int? warn = null;

        if (watching && !_wasWatching && left <= thresholds.Max())
            warn = left;
        else if (_lastLeftMinutes is int previous)
        {
            var crossed = thresholds.Where(t => previous > t && left <= t).ToList();
            if (crossed.Count > 0) warn = crossed.Min();
        }

        _lastLeftMinutes = left;
        _wasWatching = watching;
        return warn;
    }
}
