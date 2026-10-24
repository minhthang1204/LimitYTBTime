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

    /// <summary>
    /// Áp thường trực:
    /// - tắt DNS-over-HTTPS và QUIC để trình duyệt không bỏ qua file hosts;
    /// - tắt DevTools và chặn mọi tiện ích (trừ <paramref name="extensionAllowlist"/>), vì cả hai đều đổi được
    ///   tiêu đề tab YouTube thành tên khác (khi đó Agent không đếm giờ) hoặc thêm VPN/proxy;
    /// - tắt cửa sổ Tor của Brave.
    /// </summary>
    public void ApplyBaseline(IReadOnlyCollection<string> extensionAllowlist)
    {
        if (AppPaths.DryRun) return;
        using var hklm = Hklm;
        foreach (var root in ChromiumRoots)
        {
            using var key = hklm.CreateSubKey(root);
            key.SetValue("DnsOverHttpsMode", "off", RegistryValueKind.String);
            key.SetValue("QuicAllowed", 0, RegistryValueKind.DWord);
            key.SetValue("DeveloperToolsAvailability", 2, RegistryValueKind.DWord);
            key.SetValue("TorDisabled", 1, RegistryValueKind.DWord);
            RewriteList(hklm, root + @"\ExtensionInstallBlocklist", ["*"], includeOurs: true);
            RewriteList(hklm, root + @"\ExtensionInstallAllowlist", extensionAllowlist, includeOurs: true, replaceAll: true);
        }

        using var firefox = hklm.CreateSubKey(FirefoxRoot);
        firefox.SetValue("DisableDeveloperTools", 1, RegistryValueKind.DWord);
        firefox.SetValue("BlockAboutConfig", 1, RegistryValueKind.DWord);
        var settings = new Dictionary<string, object> { ["*"] = new Dictionary<string, string> { ["installation_mode"] = "blocked" } };
        foreach (var id in extensionAllowlist) settings[id] = new Dictionary<string, string> { ["installation_mode"] = "allowed" };
        firefox.SetValue("ExtensionSettings", new[] { System.Text.Json.JsonSerializer.Serialize(settings) }, RegistryValueKind.MultiString);

        using var doh = hklm.CreateSubKey(FirefoxRoot + @"\DNSOverHTTPS");
        doh.SetValue("Enabled", 0, RegistryValueKind.DWord);
        doh.SetValue("Locked", 1, RegistryValueKind.DWord);
        log.LogInformation("Đã áp policy: tắt DoH/QUIC/DevTools, chặn tiện ích (cho phép {Count})", extensionAllowlist.Count);
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
            using (var key = hklm.OpenSubKey(root, writable: true))
                foreach (var name in new[] { "DnsOverHttpsMode", "QuicAllowed", "DeveloperToolsAvailability", "TorDisabled" })
                    key?.DeleteValue(name, throwOnMissingValue: false);
            RewriteList(hklm, root + @"\ExtensionInstallBlocklist", ["*"], includeOurs: false);
            // Danh sách cho phép do LimitYT quản lý toàn bộ: gỡ cả khoá.
            hklm.DeleteSubKeyTree(root + @"\ExtensionInstallAllowlist", throwOnMissingSubKey: false);
        }
        using (var firefox = hklm.OpenSubKey(FirefoxRoot, writable: true))
            foreach (var name in new[] { "DisableDeveloperTools", "BlockAboutConfig", "ExtensionSettings" })
                firefox?.DeleteValue(name, throwOnMissingValue: false);
        hklm.DeleteSubKeyTree(FirefoxRoot + @"\DNSOverHTTPS", throwOnMissingSubKey: false);
    }

    private static IEnumerable<string> ChromiumEntries(IEnumerable<string> domains) => domains;

    private static IEnumerable<string> FirefoxEntries(IEnumerable<string> domains) =>
        domains.SelectMany(d => new[] { $"*://{d}/*", $"*://*.{d}/*" });

    /// <param name="replaceAll">true: bỏ cả các mục không phải của LimitYT (danh sách do LimitYT quản lý toàn bộ).</param>
    private static void RewriteList(RegistryKey hklm, string path, IEnumerable<string> ours, bool includeOurs, bool replaceAll = false)
    {
        var ourSet = ours.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var key = includeOurs ? hklm.CreateSubKey(path) : hklm.OpenSubKey(path, writable: true);
        if (key is null) return;

        var existing = key.GetValueNames()
            .Select(n => (Name: n, Index: int.TryParse(n, out var i) ? i : int.MaxValue, Value: key.GetValue(n) as string))
            .OrderBy(x => x.Index)
            .ToList();
        List<string> others = replaceAll ? []
            : existing.Where(x => x.Value is not null && !ourSet.Contains(x.Value)).Select(x => x.Value!).ToList();
        var desired = includeOurs ? others.Concat(ourSet).ToList() : others;

        var current = existing.Select(x => x.Value).ToList();
        if (current.SequenceEqual(desired) && existing.Select(x => x.Name).SequenceEqual(Enumerable.Range(1, desired.Count).Select(i => i.ToString())))
            return;

        foreach (var (name, _, _) in existing) key.DeleteValue(name, throwOnMissingValue: false);
        for (var i = 0; i < desired.Count; i++)
            key.SetValue((i + 1).ToString(), desired[i], RegistryValueKind.String);
    }
}
