using Microsoft.Win32;

namespace LimitYT.Service.Enforcement;

/// <summary>
/// Chính sách trình duyệt qua registry (docs/02-kien-truc.md §6.2).
/// Policy dạng danh sách phải đặt tên giá trị liên tiếp "1".."n", nên mỗi lần ghi sẽ dựng lại cả danh sách:
/// giữ các mục không phải của LimitYT, rồi thêm các mục của LimitYT vào cuối.
/// </summary>
public sealed class BrowserPolicy(ILogger<BrowserPolicy> log)
{
    private static readonly string[] ChromiumRoots =
    [
        @"SOFTWARE\Policies\Google\Chrome",
        @"SOFTWARE\Policies\Microsoft\Edge",
        @"SOFTWARE\Policies\BraveSoftware\Brave",
        @"SOFTWARE\Policies\CocCoc\Browser",
    ];

    private const string FirefoxRoot = @"SOFTWARE\Policies\Mozilla\Firefox";

    private static RegistryKey Hklm => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

    /// <summary>Áp thường trực: tắt DNS-over-HTTPS và QUIC để trình duyệt không bỏ qua file hosts.</summary>
    public void ApplyBaseline()
    {
        if (AppPaths.DryRun) return;
        using var hklm = Hklm;
        foreach (var root in ChromiumRoots)
        {
            using var key = hklm.CreateSubKey(root);
            key.SetValue("DnsOverHttpsMode", "off", RegistryValueKind.String);
            key.SetValue("QuicAllowed", 0, RegistryValueKind.DWord);
        }
        using var doh = hklm.CreateSubKey(FirefoxRoot + @"\DNSOverHTTPS");
        doh.SetValue("Enabled", 0, RegistryValueKind.DWord);
        doh.SetValue("Locked", 1, RegistryValueKind.DWord);
        log.LogInformation("Đã áp policy tắt DoH/QUIC");
    }

    public void SetBlocklist(IReadOnlyCollection<string> domains, bool blocked)
    {
        if (AppPaths.DryRun)
        {
            log.LogInformation("[dry-run] URLBlocklist {State}", blocked ? "bật" : "tắt");
            return;
        }
        using var hklm = Hklm;
        foreach (var root in ChromiumRoots)
            RewriteList(hklm, root + @"\URLBlocklist", ChromiumEntries(domains), blocked);
        RewriteList(hklm, FirefoxRoot + @"\WebsiteFilter\Block", FirefoxEntries(domains), blocked);
    }

    /// <summary>Gỡ mọi thứ LimitYT đã ghi (dùng khi gỡ cài đặt).</summary>
    public void RemoveAll(IReadOnlyCollection<string> domains)
    {
        SetBlocklist(domains, blocked: false);
        using var hklm = Hklm;
        foreach (var root in ChromiumRoots)
        {
            using var key = hklm.OpenSubKey(root, writable: true);
            key?.DeleteValue("DnsOverHttpsMode", throwOnMissingValue: false);
            key?.DeleteValue("QuicAllowed", throwOnMissingValue: false);
        }
        hklm.DeleteSubKeyTree(FirefoxRoot + @"\DNSOverHTTPS", throwOnMissingSubKey: false);
    }

    private static IEnumerable<string> ChromiumEntries(IEnumerable<string> domains) => domains;

    private static IEnumerable<string> FirefoxEntries(IEnumerable<string> domains) =>
        domains.SelectMany(d => new[] { $"*://{d}/*", $"*://*.{d}/*" });

    private static void RewriteList(RegistryKey hklm, string path, IEnumerable<string> ours, bool includeOurs)
    {
        var ourSet = ours.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var key = includeOurs ? hklm.CreateSubKey(path) : hklm.OpenSubKey(path, writable: true);
        if (key is null) return;

        var existing = key.GetValueNames()
            .Select(n => (Name: n, Index: int.TryParse(n, out var i) ? i : int.MaxValue, Value: key.GetValue(n) as string))
            .OrderBy(x => x.Index)
            .ToList();
        var others = existing.Where(x => x.Value is not null && !ourSet.Contains(x.Value)).Select(x => x.Value!).ToList();
        var desired = includeOurs ? others.Concat(ourSet).ToList() : others;

        var current = existing.Select(x => x.Value).ToList();
        if (current.SequenceEqual(desired) && existing.Select(x => x.Name).SequenceEqual(Enumerable.Range(1, desired.Count).Select(i => i.ToString())))
            return;

        foreach (var (name, _, _) in existing) key.DeleteValue(name, throwOnMissingValue: false);
        for (var i = 0; i < desired.Count; i++)
            key.SetValue((i + 1).ToString(), desired[i], RegistryValueKind.String);
    }
}
