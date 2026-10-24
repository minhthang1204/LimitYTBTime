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
                    // Một số instance Invidious/Piped phổ biến (danh sách thay đổi thường xuyên; tiêu đề vẫn là lớp chính).
                    "yewtu.be", "inv.nadeko.net", "invidious.nerdvpn.de", "piped.video", "poketube.fun",
                ],
                TitlePatterns =
                [
                    @"(?<=\S\s)[-–—|]\sYouTube( Music)?" + SegmentEnd,
                    @"^(\(\d+\)\s)?YouTube( Music)?" + SegmentEnd,
                    // Giao diện YouTube thay thế: tiêu đề không có chữ "YouTube".
                    @"(?<=\S\s)[-–—|]\s(Invidious|Piped|FreeTube|Poke|Hyperpipe)" + SegmentEnd,
                    @"^(Invidious|Piped|FreeTube|Poke)" + SegmentEnd,
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
        // Chỉ cho dùng các trình duyệt nhận policy (Chrome, Edge, Brave, Cốc Cốc, Firefox).
        PcBlockedProcessPatterns =
        [
            @"^Opera\b", @"Tor Browser", @"^Vivaldi", @"^Chromium$", @"Yandex", @"UC ?Browser", @"Maxthon",
            @"Waterfox", @"LibreWolf", @"Floorp", @"Pale ?Moon", @"Basilisk", @"Thorium", @"Supermium", @"Iridium",
            @"Slimjet", @"Comodo Dragon", @"Secure Browser", @"CCleaner Browser", @"Epic Privacy", @"SRWare Iron",
            @"Cent ?Browser", @"Ungoogled",
            // VPN/proxy chạy được không cần quyền Admin
            @"Psiphon", @"Lantern", @"Ultrasurf",
        ],
        Tv = new TvConfig
        {
            GuardedSettingsPackages = ["com.android.tv.settings", "com.xiaomi.mitv.settings"],
            InstallerPackages = ["com.android.packageinstaller", "com.google.android.packageinstaller"],
        },
    };
}
