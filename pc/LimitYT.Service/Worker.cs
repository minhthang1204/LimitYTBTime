using System.Diagnostics;
using LimitYT.Service.Enforcement;
using LimitYT.Service.Native;

namespace LimitYT.Service;

/// <summary>Vòng lặp chính (docs/02-kien-truc.md §6.1).</summary>
public sealed class Worker(
    LimitController controller,
    HostsBlocker hosts,
    BrowserPolicy policy,
    AgentLauncher launcher,
    ILogger<Worker> log) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PolicyRefresh = TimeSpan.FromMinutes(1);
    /// <summary>Máy sleep / service bị treo không được tính thành thời gian xem.</summary>
    private const long MaxDeltaMs = 10_000;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        string? appliedAllowlist = null;
        bool? policyBlocked = null;
        var lastPolicyWrite = DateTime.MinValue;

        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                var delta = Math.Min(stopwatch.ElapsedMilliseconds, MaxDeltaMs);
                stopwatch.Restart();

                var session = NativeMethods.GetActiveUserSession();
                var result = controller.Tick(delta, session);

                controller.EnforcementError = hosts.Ensure(result.Domains, result.Blocked);

                // Áp lại policy thường trực khi khởi động và khi phụ huynh đổi danh sách tiện ích được phép.
                var allowlist = string.Join("\n", result.ExtensionAllowlist);
                if (allowlist != appliedAllowlist)
                {
                    Try(() => policy.ApplyBaseline(result.ExtensionAllowlist), "áp policy trình duyệt");
                    appliedAllowlist = allowlist;
                }

                if (policyBlocked != result.Blocked || DateTime.UtcNow - lastPolicyWrite > PolicyRefresh)
                {
                    Try(() => policy.SetBlocklist(result.Domains, result.Blocked), "ghi URLBlocklist");
                    policyBlocked = result.Blocked;
                    lastPolicyWrite = DateTime.UtcNow;
                }

                if (result.AgentMissing && session is uint s)
                    launcher.TryLaunch(s);
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            controller.Persist();
        }
    }

    private void Try(Action action, string what)
    {
        try { action(); }
        catch (Exception e) { log.LogError(e, "Lỗi khi {What}", what); }
    }
}
