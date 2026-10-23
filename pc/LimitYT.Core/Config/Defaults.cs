namespace LimitYT.Core.Config;

public static class Defaults
{
    public static readonly List<string> AllDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
    public static readonly List<string> Weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri"];

    // Tên dịch vụ phải là một đoạn riêng của tiêu đề: "<video> - YouTube - <trình duyệt>" hoặc "YouTube - <trình duyệt>".
    // Edge thêm " and 3 more pages" / " và 3 trang khác" sau tiêu đề tab.
    private const string SegmentEnd = @"(?=$|\s[-–—|]\s|\s(and|và)\s\d)";

    public static LimitConfig Create() => new()
    {
        BlockedWindows =
        [
            new() { Days = [.. AllDays], From = "22:00", To = "06:00" },
            new() { Days = [.. Weekdays], From = "07:00", To = "11:00", SchoolOnly = true },
            new() { Days = [.. Weekdays], From = "13:00", To = "17:00", SchoolOnly = true },
        ],
        OfflineCapPercent = new() { ["pc"] = 50, ["tv"] = 100 },
        Services =
        [
            new()
            {
                Id = "youtube",
                Domains =
                [
                    "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be",
                    "youtube-nocookie.com", "www.youtube-nocookie.com", "youtubei.googleapis.com", "i.ytimg.com",
                ],
                TitlePatterns =
                [
                    @"(?<=\S\s)[-–—|]\sYouTube( Music)?" + SegmentEnd,
                    @"^(\(\d+\)\s)?YouTube( Music)?" + SegmentEnd,
                ],
                TvPackages =
                [
                    "com.google.android.youtube.tv", "com.google.android.youtube.tvkids",
                    "com.google.android.youtube.tvmusic", "com.liskovsoft.smarttubetv",
                    "com.liskovsoft.smarttubetv.beta", "com.teamsmart.videomanager.tv",
                ],
                PcExecutables = ["FreeTube.exe", "yt-dlp.exe"],
            },
            new()
            {
                Id = "tiktok",
                Domains =
                [
                    "tiktok.com", "www.tiktok.com", "m.tiktok.com", "vt.tiktok.com", "vm.tiktok.com",
                    "tiktokv.com", "www.tiktokv.com",
                ],
                TitlePatterns =
                [
                    @"(?<=\S\s)[-–—|]\sTikTok" + SegmentEnd,
                    @"^(\(\d+\)\s)?TikTok" + SegmentEnd,
                ],
                TvPackages = ["com.ss.android.ugc.trill", "com.zhiliaoapp.musically"],
                PcExecutables = ["TikTok.exe"],
            },
        ],
        TitleExcludePatterns =
        [
            @"Tìm với Google|Tìm trên Google|Tìm kiếm Google|Google Search|\s-\sBing\b|Cốc Cốc Search|DuckDuckGo",
        ],
        Tv = new TvConfig
        {
            GuardedSettingsPackages = ["com.android.tv.settings", "com.xiaomi.mitv.settings"],
            InstallerPackages = ["com.android.packageinstaller", "com.google.android.packageinstaller"],
        },
    };
}
