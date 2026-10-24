using System.Text.RegularExpressions;
using LimitYT.Core.Blocking;
using LimitYT.Core.Config;
using LimitYT.Core.Rules;
using LimitYT.Core.Usage;

namespace LimitYT.Core.Tests;

public class TitleMatcherTests
{
    private static readonly TitleMatcher Matcher = Create();

    private static TitleMatcher Create()
    {
        var c = Defaults.Create();
        return new TitleMatcher(c.Services.Select(s => (s.Id, (IEnumerable<string>)s.TitlePatterns)), c.TitleExcludePatterns);
    }

    [Theory]
    [InlineData("Lofi hip hop radio - YouTube - Google Chrome", "youtube")]
    [InlineData("(3) Lofi hip hop radio - YouTube - Google Chrome", "youtube")]
    [InlineData("YouTube - Google Chrome", "youtube")]
    [InlineData("(12) YouTube - Mozilla Firefox", "youtube")]
    [InlineData("YouTube Music - Cốc Cốc", "youtube")]
    [InlineData("Bài hát - YouTube Music - Brave", "youtube")]
    [InlineData("Video - YouTube and 3 more pages - Personal - Microsoft​ Edge", "youtube")]
    [InlineData("Video - YouTube và 3 trang khác - Cá nhân - Microsoft​ Edge", "youtube")]
    [InlineData("YouTube", "youtube")]
    [InlineData("Video — YouTube — Mozilla Firefox", "youtube")]
    [InlineData("TikTok - Make Your Day - Google Chrome", "tiktok")]
    [InlineData("Nhảy vui #fyp | TikTok - Google Chrome", "tiktok")]
    [InlineData("Lofi radio - Invidious - Google Chrome", "youtube")]
    [InlineData("Lofi radio - Piped - Mozilla Firefox", "youtube")]
    [InlineData("Lofi radio - FreeTube", "youtube")]
    [InlineData("Invidious - Google Chrome", "youtube")]
    public void Matches(string title, string expected) => Assert.Equal(expected, Matcher.Match(title));

    [Theory]
    [InlineData("youtube - Tìm với Google - Google Chrome")]
    [InlineData("YouTube - Tìm với Google - Google Chrome")]
    [InlineData("YouTube - Google Search - Google Chrome")]
    [InlineData("How to make YouTube videos - Blog - Google Chrome")]
    [InlineData("Top 10 - YouTube Shorts tips - Google Chrome")]
    [InlineData("Gmail - Hộp thư đến - Google Chrome")]
    [InlineData("tiktok downloader - Google Chrome")]
    [InlineData("")]
    public void DoesNotMatch(string title) => Assert.Null(Matcher.Match(title));
}

public class BlockedProcessTests
{
    private static readonly List<Regex> Patterns = Defaults.Create().PcBlockedProcessPatterns
        .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToList();

    private static bool Blocked(string s) => Patterns.Any(r => r.IsMatch(s));

    [Theory]
    [InlineData("Opera Internet Browser")]
    [InlineData("Opera GX Internet Browser")]
    [InlineData("Tor Browser")]
    [InlineData("Vivaldi")]
    [InlineData("Chromium")]
    [InlineData("Yandex")]
    [InlineData("Avast Secure Browser")]
    [InlineData("Psiphon 3")]
    public void BlocksBrowsersWithoutPolicySupport(string product) => Assert.True(Blocked(product));

    [Theory]
    [InlineData("Google Chrome")]
    [InlineData("Microsoft Edge")]
    [InlineData("Brave Browser")]
    [InlineData("Cốc Cốc")]
    [InlineData("Firefox")]
    [InlineData("Microsoft® Windows® Operating System")]
    [InlineData("Visual Studio Code")]
    [InlineData("LimitYT.Agent")]
    [InlineData("Operation Center")]
    public void KeepsAllowedPrograms(string product) => Assert.False(Blocked(product));
}

public class HostsFileTests
{
    private const string Original = "# Copyright (c) Microsoft\r\n127.0.0.1 localhost\r\n";

    [Fact]
    public void ApplyThenRemoveRestoresOriginal()
    {
        var applied = HostsFile.Apply(Original, ["youtube.com", "www.youtube.com"]);
        Assert.Contains("0.0.0.0 youtube.com\r\n", applied);
        Assert.Contains(":: www.youtube.com\r\n", applied);
        Assert.StartsWith(Original.TrimEnd(), applied);
        Assert.Equal(Original, HostsFile.Remove(applied));
    }

    [Fact]
    public void ApplyIsIdempotent()
    {
        var once = HostsFile.Apply(Original, ["youtube.com"]);
        Assert.Equal(once, HostsFile.Apply(once, ["youtube.com"]));
    }

    [Fact]
    public void ApplyReplacesOldBlock()
    {
        var old = HostsFile.Apply(Original, ["a.com"]);
        var updated = HostsFile.Apply(old, ["b.com"]);
        Assert.DoesNotContain("a.com", updated);
        Assert.Contains("0.0.0.0 b.com", updated);
    }

    [Fact]
    public void RemoveKeepsLinesAfterBlock()
    {
        var withTail = HostsFile.Apply(Original, ["a.com"]) + "10.0.0.1 nas\r\n";
        Assert.Equal(Original + "10.0.0.1 nas\r\n", HostsFile.Remove(withTail));
    }

    [Fact]
    public void RemoveWithoutBlockIsNoop() => Assert.Equal(Original, HostsFile.Remove(Original));

    [Fact]
    public void ApplyToEmptyFile() => Assert.StartsWith(HostsFile.BeginMarker, HostsFile.Apply("", ["a.com"]));
}

public class UsageLedgerTests
{
    private static readonly DateOnly Day = new(2026, 10, 26);

    [Fact]
    public void MergeTakesMaxPerDeviceAndDate()
    {
        var a = new UsageLedger();
        a.Add("pc", Day, 1000);
        a.Add("tv", Day, 5000);
        var b = new UsageLedger();
        b.Add("pc", Day, 3000);
        b.Add("tv", Day, 2000);

        a.Merge(b);

        Assert.Equal(3000, a.DeviceMs("pc", Day));
        Assert.Equal(5000, a.DeviceMs("tv", Day));
    }

    [Fact]
    public void MergeIsCommutativeAndIdempotent()
    {
        var a = new UsageLedger();
        a.Add("pc", Day, 1000);
        a.Merge(new Adjustment("x", Day, 600_000, null, DateTimeOffset.UnixEpoch));
        var b = new UsageLedger();
        b.Add("tv", Day, 7000);
        b.Add("pc", Day, 500);

        var ab = new UsageLedger(); ab.Merge(a); ab.Merge(b); ab.Merge(b);
        var ba = new UsageLedger(); ba.Merge(b); ba.Merge(a);

        Assert.Equal(ab.NetUsedMs(Day), ba.NetUsedMs(Day));
        Assert.Equal(1000 + 7000 - 600_000, ab.NetUsedMs(Day));
    }

    [Fact]
    public void AdjustmentsUnionById()
    {
        var l = new UsageLedger();
        Assert.True(l.Merge(new Adjustment("x", Day, 60_000, null, DateTimeOffset.UnixEpoch)));
        Assert.False(l.Merge(new Adjustment("x", Day, 60_000, null, DateTimeOffset.UnixEpoch)));
        Assert.Equal(-60_000, l.NetUsedMs(Day));
    }
}

public class CalendarTests
{
    private static readonly LocalCalendar Calendar = new(Defaults.Create());

    [Fact]
    public void SplitAcrossMidnight()
    {
        var end = DateTimeOffset.Parse("2026-10-27T00:00:03+07:00");
        var parts = Calendar.SplitByLocalDate(end, 5000);
        Assert.Equal([(new DateOnly(2026, 10, 26), 2000L), (new DateOnly(2026, 10, 27), 3000L)], parts);
    }

    [Fact]
    public void SplitWithinDay()
    {
        var parts = Calendar.SplitByLocalDate(DateTimeOffset.Parse("2026-10-26T12:00:00+07:00"), 5000);
        Assert.Equal([(new DateOnly(2026, 10, 26), 5000L)], parts);
    }

    [Theory]
    [InlineData("2026-10-26", "2026-10-26")]
    [InlineData("2026-11-01", "2026-10-26")]
    [InlineData("2026-11-02", "2026-11-02")]
    public void WeekStartsMonday(string date, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), Calendar.WeekStartOf(DateOnly.Parse(date)));
}

public class WarningTrackerTests
{
    private static Decision Allowed(double minutes) => new(true, BlockReason.None, TimeSpan.FromMinutes(minutes), null);
    private static readonly int[] Thresholds = [15, 5];

    [Fact]
    public void WarnsWhenCrossingThresholdsOnce()
    {
        var t = new WarningTracker();
        Assert.Null(t.Next(Allowed(30), true, Thresholds));
        Assert.Null(t.Next(Allowed(16), true, Thresholds));
        Assert.Equal(15, t.Next(Allowed(14.9), true, Thresholds));
        Assert.Null(t.Next(Allowed(14), true, Thresholds));
        Assert.Equal(5, t.Next(Allowed(4.5), true, Thresholds));
        Assert.Null(t.Next(Allowed(4), true, Thresholds));
    }

    [Fact]
    public void WarnsWhenStartingToWatchBelowThreshold()
    {
        var t = new WarningTracker();
        Assert.Null(t.Next(Allowed(10), false, Thresholds));
        Assert.Equal(10, t.Next(Allowed(10), true, Thresholds));
        Assert.Null(t.Next(Allowed(9.9), true, Thresholds));
    }
}

public class ParserTests
{
    [Theory]
    [InlineData("7h", 420)]
    [InlineData("90m", 90)]
    [InlineData("1h30m", 90)]
    [InlineData("45", 45)]
    [InlineData("-30m", -30)]
    [InlineData("+1h", 60)]
    public void Durations(string text, int minutes)
    {
        Assert.True(DurationParser.TryParseMinutes(text, out var m));
        Assert.Equal(minutes, m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("h")]
    [InlineData("abc")]
    public void InvalidDurations(string text) => Assert.False(DurationParser.TryParseMinutes(text, out _));

    [Theory]
    [InlineData("Mon-Fri", "Mon,Tue,Wed,Thu,Fri")]
    [InlineData("Sat,Sun", "Sat,Sun")]
    [InlineData("Fri-Mon", "Fri,Sat,Sun,Mon")]
    [InlineData("all", "Mon,Tue,Wed,Thu,Fri,Sat,Sun")]
    public void DaySpecs(string spec, string expected) => Assert.Equal(expected, string.Join(",", DayNames.ParseSpec(spec)));

    [Theory]
    [InlineData("Mon,Tue,Wed,Thu,Fri", "Mon-Fri")]
    [InlineData("Sat,Sun", "Sat,Sun")]
    [InlineData("Mon,Wed,Thu,Fri,Sun", "Mon,Wed-Fri,Sun")]
    [InlineData("Sun,Mon,Tue,Wed,Thu,Fri,Sat", "Hằng ngày")]
    public void DayFormat(string days, string expected) => Assert.Equal(expected, DayNames.Format(days.Split(',')));

    [Fact]
    public void DefaultConfigIsValid() => Assert.Empty(ConfigValidator.Validate(Defaults.Create()));

    [Fact]
    public void InvalidWindowTimesReportedOnce()
    {
        var c = Defaults.Create();
        c.BlockedWindows.Add(new BlockedWindow { Days = ["Mon"], From = "25:00", To = "26:00" });
        Assert.Equal(2, ConfigValidator.Validate(c).Count);
    }
}
