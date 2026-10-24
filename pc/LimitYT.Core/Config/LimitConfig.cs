using System.Text.Json;

namespace LimitYT.Core.Config;

/// <summary>Cấu hình do PC sở hữu (docs/02-kien-truc.md §4.1).</summary>
public sealed class LimitConfig
{
    public int Version { get; set; } = 1;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    public string WeekStart { get; set; } = "Mon";
    public int WeeklyQuotaMinutes { get; set; } = 420;
    public int DailyQuotaMinutes { get; set; } = 90;
    public int? HolidayDailyQuotaMinutes { get; set; }
    public List<BlockedWindow> BlockedWindows { get; set; } = [];
    public List<HolidayRange> Holidays { get; set; } = [];
    /// <summary>Bật/tắt tay theo ngày "yyyy-MM-dd": true = nghỉ, false = đi học.</summary>
    public Dictionary<string, bool> HolidayOverrides { get; set; } = new();
    public List<int> WarnAtMinutesLeft { get; set; } = [15, 5];
    public Dictionary<string, int> OfflineCapPercent { get; set; } = new();
    public List<LimitedService> Services { get; set; } = [];
    public List<string> TitleExcludePatterns { get; set; } = [];
    /// <summary>
    /// Trình duyệt/VPN không nhận policy (vượt được hosts): Agent tắt ngay khi thấy.
    /// Regex (không phân biệt hoa thường) khớp tên exe, ProductName hoặc FileDescription — đổi tên exe vẫn bị bắt.
    /// </summary>
    public List<string> PcBlockedProcessPatterns { get; set; } = [];
    /// <summary>ID tiện ích trình duyệt được phép. Mọi tiện ích khác bị chặn và bị tắt.</summary>
    public List<string> BrowserExtensionAllowlist { get; set; } = [];
    public TvConfig Tv { get; set; } = new();
    public string? AdminPinHash { get; set; }

    public IEnumerable<string> AllDomains => Services.SelectMany(s => s.Domains).Distinct(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> AllPcExecutables => Services.SelectMany(s => s.PcExecutables).Distinct(StringComparer.OrdinalIgnoreCase);

    public LimitConfig Clone() =>
        JsonSerializer.Deserialize<LimitConfig>(JsonSerializer.Serialize(this, ConfigJson.Options), ConfigJson.Options)!;
}

public sealed class BlockedWindow
{
    public List<string> Days { get; set; } = [];
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "00:00";
    /// <summary>Khung "giờ học": bị bỏ qua vào ngày nghỉ.</summary>
    public bool SchoolOnly { get; set; }

    public override string ToString() =>
        $"{DayNames.Format(Days)} {From}-{To}{(SchoolOnly ? " (giờ học)" : "")}";
}

public sealed class HolidayRange
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string? Note { get; set; }

    public override string ToString() => $"{From}..{To}{(string.IsNullOrEmpty(Note) ? "" : " " + Note)}";
}

public sealed class LimitedService
{
    public string Id { get; set; } = "";
    public List<string> Domains { get; set; } = [];
    /// <summary>Regex khớp tiêu đề cửa sổ trên PC (phân biệt hoa thường).</summary>
    public List<string> TitlePatterns { get; set; } = [];
    public List<string> TvPackages { get; set; } = [];
    public List<string> PcExecutables { get; set; } = [];
}

public sealed class TvConfig
{
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5555;
    public List<string> GuardedSettingsPackages { get; set; } = [];
    public List<string> NeverBlockPackages { get; set; } = [];
    public List<string> InstallerPackages { get; set; } = [];
    public List<string> CleanerPackages { get; set; } = [];
}

public static class ConfigJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
