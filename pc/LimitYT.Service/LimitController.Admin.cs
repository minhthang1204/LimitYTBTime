using System.Globalization;
using System.Text;
using LimitYT.Core;
using LimitYT.Core.Config;
using LimitYT.Core.Ipc;
using LimitYT.Core.Usage;

namespace LimitYT.Service;

/// <summary>Lệnh quản trị từ LimitYT.Cli (docs/02-kien-truc.md §9). Pipe quản trị chỉ Administrators mở được.</summary>
public sealed partial class LimitController
{
    private static readonly string[] VietnameseDays = ["Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy"];

    public AdminReply Admin(IReadOnlyList<string> args)
    {
        lock (_gate)
        {
            try
            {
                return args.FirstOrDefault()?.ToLowerInvariant() switch
                {
                    "status" or null => Ok(Status()),
                    "set" => Set(args),
                    "window" => Window(args),
                    "holiday" => Holiday(args),
                    "add" => AddTime(args),
                    "unlock" => Unlock(args),
                    "history" => Ok(History(args.Count > 1 && int.TryParse(args[1], out var w) ? Math.Clamp(w, 1, HistoryWeeks) : 2)),
                    "config" => Ok($"File cấu hình: {AppPaths.ConfigFile}\nSửa xong file, Service tự nạp lại trong vài giây."),
                    _ => Fail($"Lệnh không hợp lệ: {args[0]}"),
                };
            }
            catch (FormatException e)
            {
                return Fail(e.Message);
            }
        }
    }

    private string Status()
    {
        var now = _time.GetUtcNow();
        var cal = _engine.Calendar;
        var today = cal.LocalDate(now);
        var weekStart = cal.WeekStartOf(today);
        var holiday = cal.IsHoliday(today);

        var sb = new StringBuilder();
        sb.AppendLine($"Hôm nay {DayLabel(today)} ({(holiday ? "ngày nghỉ" : "ngày đi học")}):");
        sb.AppendLine($"  đã dùng {DurationParser.Format(_ledger.NetUsedMs(today))} / {cal.DailyQuotaMinutes(today)} phút"
                      + $"  (PC {DurationParser.Format(_ledger.DeviceMs(Devices.Pc, today))}, TV {DurationParser.Format(_ledger.DeviceMs(Devices.Tv, today))})");
        var adjToday = _ledger.AdjustmentMs(today, today);
        if (adjToday != 0) sb.AppendLine($"  phụ huynh điều chỉnh: {(adjToday > 0 ? "+" : "-")}{DurationParser.Format(Math.Abs(adjToday))}");
        sb.AppendLine($"Tuần này (từ {weekStart:dd/MM}):");
        sb.AppendLine($"  đã dùng {DurationParser.Format(_ledger.NetUsedMs(weekStart, today))} / {DurationParser.Format(_config.WeeklyQuotaMinutes * 60_000L)}");

        sb.Append("Trạng thái: ");
        if (!_blocked)
            sb.AppendLine($"được phép xem, còn {DurationParser.Format((long)_decision.Left.TotalMilliseconds, roundUp: true)} liên tục");
        else
        {
            sb.Append($"ĐANG CHẶN — {_message}");
            if (_decision.NextChangeUtc is { } until) sb.Append($" (mở lại lúc {cal.ToLocal(until):HH:mm dd/MM})");
            sb.AppendLine();
        }
        if (_unlockedUntil is { } u && u > now) sb.AppendLine($"Mở khoá tạm thời tới {cal.ToLocal(u):HH:mm dd/MM}");

        sb.AppendLine(_userSession is uint s && _heartbeats.TryGetValue(s, out var hb) && now - hb.At <= HeartbeatFresh
            ? $"Agent: đang chạy (phiên {s}){(hb.Watching ? ", đang mở YouTube/TikTok" : "")}"
            : "Agent: không có kết nối");
        if (EnforcementError is { } err) sb.AppendLine($"LỖI: {err}");
        sb.Append($"Cấu hình version {_config.Version}: {AppPaths.ConfigFile}");
        return sb.ToString();
    }

    private string History(int weeks)
    {
        var cal = _engine.Calendar;
        var today = cal.LocalDate(_time.GetUtcNow());
        var sb = new StringBuilder();
        for (var w = weeks - 1; w >= 0; w--)
        {
            var start = cal.WeekStartOf(today).AddDays(-7 * w);
            var end = start.AddDays(6) < today ? start.AddDays(6) : today;
            sb.AppendLine($"Tuần {start:dd/MM}: {DurationParser.Format(_ledger.NetUsedMs(start, end))} / {DurationParser.Format(_config.WeeklyQuotaMinutes * 60_000L)}");
            for (var d = start; d <= end; d = d.AddDays(1))
                sb.AppendLine($"  {DayLabel(d),-16} {DurationParser.Format(_ledger.NetUsedMs(d)),-16}"
                              + $" PC {DurationParser.Format(_ledger.DeviceMs(Devices.Pc, d))}, TV {DurationParser.Format(_ledger.DeviceMs(Devices.Tv, d))}");
        }
        return sb.ToString().TrimEnd();
    }

    private AdminReply Set(IReadOnlyList<string> args)
    {
        if (args.Count < 3) return Fail("Cú pháp: set weekly|daily|holiday-daily <thời lượng>|off");
        var value = args[2];
        return args[1].ToLowerInvariant() switch
        {
            "weekly" => MutateConfig(c => c.WeeklyQuotaMinutes = Minutes(value), $"Quota tuần = {value}"),
            "daily" => MutateConfig(c => c.DailyQuotaMinutes = Minutes(value), $"Quota ngày = {value}"),
            "holiday-daily" => MutateConfig(
                c => c.HolidayDailyQuotaMinutes = value.Equals("off", StringComparison.OrdinalIgnoreCase) ? null : Minutes(value),
                $"Quota ngày nghỉ = {value}"),
            _ => Fail($"Không có thiết lập '{args[1]}'"),
        };
    }

    private AdminReply Window(IReadOnlyList<string> args)
    {
        switch (args.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "list" or null:
                return Ok(List("Khung giờ cấm", _config.BlockedWindows));
            case "add" when args.Count >= 4:
            {
                var range = args[3].Split('-');
                if (range.Length != 2) return Fail("Khung giờ phải có dạng HH:mm-HH:mm");
                var window = new BlockedWindow
                {
                    Days = DayNames.ParseSpec(args[2]),
                    From = range[0],
                    To = range[1],
                    SchoolOnly = args.Skip(4).Any(a => a.Equals("--school", StringComparison.OrdinalIgnoreCase)),
                };
                return MutateConfig(c => c.BlockedWindows.Add(window), $"Đã thêm khung giờ cấm: {window}");
            }
            case "remove" when args.Count >= 3 && int.TryParse(args[2], out var n):
                if (n < 1 || n > _config.BlockedWindows.Count) return Fail($"Không có khung giờ số {n}");
                var removed = _config.BlockedWindows[n - 1];
                return MutateConfig(c => c.BlockedWindows.RemoveAt(n - 1), $"Đã xoá khung giờ: {removed}");
            default:
                return Fail("Cú pháp: window list | window add <Mon-Fri> <HH:mm-HH:mm> [--school] | window remove <số>");
        }
    }

    private AdminReply Holiday(IReadOnlyList<string> args)
    {
        switch (args.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "list" or null:
                var overrides = _config.HolidayOverrides.OrderBy(kv => kv.Key)
                    .Select(kv => $"  {kv.Key}: {(kv.Value ? "nghỉ" : "đi học")} (bật tay)");
                return Ok(List("Lịch nghỉ", _config.Holidays) + (_config.HolidayOverrides.Count > 0 ? "\n" + string.Join("\n", overrides) : ""));
            case "add" when args.Count >= 3:
            {
                var parts = args[2].Split("..");
                var range = new HolidayRange
                {
                    From = parts[0],
                    To = parts.Length > 1 ? parts[1] : parts[0],
                    Note = args.Count > 3 ? string.Join(' ', args.Skip(3)) : null,
                };
                return MutateConfig(c => c.Holidays.Add(range), $"Đã thêm ngày nghỉ: {range}");
            }
            case "remove" when args.Count >= 3 && int.TryParse(args[2], out var n):
                if (n < 1 || n > _config.Holidays.Count) return Fail($"Không có mục số {n}");
                var removed = _config.Holidays[n - 1];
                return MutateConfig(c => c.Holidays.RemoveAt(n - 1), $"Đã xoá ngày nghỉ: {removed}");
            case "today" when args.Count >= 3:
            {
                var today = _engine.Calendar.LocalDate(_time.GetUtcNow()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return args[2].ToLowerInvariant() switch
                {
                    "on" => MutateConfig(c => c.HolidayOverrides[today] = true, $"Hôm nay ({today}) tính là ngày nghỉ"),
                    "off" => MutateConfig(c => c.HolidayOverrides[today] = false, $"Hôm nay ({today}) tính là ngày đi học"),
                    "auto" => MutateConfig(c => c.HolidayOverrides.Remove(today), $"Hôm nay ({today}) tính theo lịch nghỉ"),
                    _ => Fail("Cú pháp: holiday today on|off|auto"),
                };
            }
            default:
                return Fail("Cú pháp: holiday list | holiday add <yyyy-MM-dd>[..<yyyy-MM-dd>] [ghi chú] | holiday remove <số> | holiday today on|off|auto");
        }
    }

    private AdminReply AddTime(IReadOnlyList<string> args)
    {
        if (args.Count < 2) return Fail("Cú pháp: add <thời lượng> [lý do]   (ví dụ: add 30m, add -15m phạt)");
        var minutes = SignedMinutes(args[1]);
        var now = _time.GetUtcNow();
        var adjustment = new Adjustment(Guid.NewGuid().ToString("N"), _engine.Calendar.LocalDate(now), minutes * 60_000L,
            args.Count > 2 ? string.Join(' ', args.Skip(2)) : null, now);
        _ledger.Merge(adjustment);
        _store.SaveAdjustment(adjustment);
        _decision = _engine.Evaluate(now, _ledger, Devices.Pc, null, _unlockedUntil);
        return Ok($"Đã {(minutes >= 0 ? "cộng" : "trừ")} {Math.Abs(minutes)} phút cho hôm nay.");
    }

    private AdminReply Unlock(IReadOnlyList<string> args)
    {
        if (args.Count < 2) return Fail("Cú pháp: unlock <thời lượng> | unlock off");
        var now = _time.GetUtcNow();
        if (args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            _unlockedUntil = null;
            _store.SetValue(UnlockKey, null);
            return Ok("Đã huỷ mở khoá tạm thời.");
        }
        var minutes = Minutes(args[1]);
        _unlockedUntil = now.AddMinutes(minutes);
        _store.SetValue(UnlockKey, _unlockedUntil.Value.ToString("O", CultureInfo.InvariantCulture));
        return Ok($"Mở khoá tới {_engine.Calendar.ToLocal(_unlockedUntil.Value):HH:mm dd/MM}. Thời gian xem vẫn được tính.");
    }

    private AdminReply MutateConfig(Action<LimitConfig> change, string message)
    {
        var updated = _config.Clone();
        change(updated);
        var errors = ConfigValidator.Validate(updated);
        if (errors.Count > 0) return Fail(string.Join("\n", errors));
        _configStore.Save(updated);
        ApplyConfig(updated);
        return Ok(message);
    }

    private static int Minutes(string text) =>
        DurationParser.TryParseMinutes(text, out var m) && m >= 0 ? m : throw new FormatException($"Thời lượng không hợp lệ: '{text}' (ví dụ 7h, 90m, 1h30m)");

    private static int SignedMinutes(string text) =>
        DurationParser.TryParseMinutes(text, out var m) ? m : throw new FormatException($"Thời lượng không hợp lệ: '{text}'");

    private static string DayLabel(DateOnly d) => $"{VietnameseDays[(int)d.DayOfWeek]} {d:dd/MM}";

    private static string List<T>(string title, IReadOnlyList<T> items) =>
        items.Count == 0 ? $"{title}: (trống)" : $"{title}:\n" + string.Join("\n", items.Select((x, i) => $"  {i + 1}. {x}"));

    private static AdminReply Ok(string message) => new(true, message);

    private static AdminReply Fail(string message) => new(false, message);
}
