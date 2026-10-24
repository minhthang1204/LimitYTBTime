using System.Globalization;
using LimitYT.Core.Config;
using LimitYT.Core.Ipc;
using LimitYT.Core.Rules;
using LimitYT.Core.Usage;
using LimitYT.Service.Storage;

namespace LimitYT.Service;

public sealed record TickResult(
    bool Blocked, string Message, IReadOnlyCollection<string> Domains, bool AgentMissing, IReadOnlyCollection<string> ExtensionAllowlist);

/// <summary>
/// Trạng thái trung tâm của node PC: sổ ghi giờ, quyết định chặn, heartbeat của Agent.
/// Worker gọi Tick mỗi 5 giây; các pipe server gọi OnHeartbeat / Admin. Mọi truy cập đều qua _gate.
/// </summary>
public sealed partial class LimitController
{
    private static readonly TimeSpan HeartbeatFresh = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan AgentGrace = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PersistInterval = TimeSpan.FromSeconds(30);
    private const int HistoryWeeks = 8;
    private const string UnlockKey = "unlocked_until";

    private readonly object _gate = new();
    private readonly ConfigStore _configStore;
    private readonly SqliteStore _store;
    private readonly TimeProvider _time;
    private readonly ILogger<LimitController> _log;

    private LimitConfig _config;
    private RulesEngine _engine;
    private readonly UsageLedger _ledger;
    private readonly HashSet<DateOnly> _dirtyDates = [];
    private DateTimeOffset? _unlockedUntil;
    private Decision _decision = new(true, BlockReason.None, TimeSpan.Zero, null);
    private readonly WarningTracker _warnings = new();
    private int? _pendingWarning;
    private readonly Dictionary<uint, (DateTimeOffset At, bool Watching)> _heartbeats = new();
    private uint? _userSession;
    private DateTimeOffset _userSessionSince;
    private DateTimeOffset _lastPersist;
    private bool _blocked;
    private string _message = "";

    public LimitController(ConfigStore configStore, SqliteStore store, TimeProvider time, ILogger<LimitController> log)
    {
        _configStore = configStore;
        _store = store;
        _time = time;
        _log = log;

        _config = configStore.Load();
        _engine = new RulesEngine(_config);
        var now = time.GetUtcNow();
        _ledger = store.LoadLedger(_engine.Calendar.LocalDate(now).AddDays(-7 * HistoryWeeks));
        if (DateTimeOffset.TryParse(store.GetValue(UnlockKey), CultureInfo.InvariantCulture, DateTimeStyles.None, out var unlocked))
            _unlockedUntil = unlocked;
        _lastPersist = now;
    }

    public string? EnforcementError { get; set; }

    public TickResult Tick(long deltaMs, uint? userSession)
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();

            if (_configStore.TryReload(out var reloaded))
                ApplyConfig(reloaded);

            if (userSession != _userSession)
            {
                _userSession = userSession;
                _userSessionSince = now;
            }

            foreach (var stale in _heartbeats.Where(h => now - h.Value.At > TimeSpan.FromMinutes(1)).Select(h => h.Key).ToList())
                _heartbeats.Remove(stale);

            var watching = _heartbeats.Values.Any(h => h.Watching && now - h.At <= HeartbeatFresh);
            if (watching)
            {
                foreach (var (date, ms) in _engine.Calendar.SplitByLocalDate(now, deltaMs))
                {
                    _ledger.Add(Devices.Pc, date, ms);
                    _dirtyDates.Add(date);
                }
            }

            _decision = _engine.Evaluate(now, _ledger, Devices.Pc, offline: null, _unlockedUntil);
            if (_warnings.Next(_decision, watching, _config.WarnAtMinutesLeft) is int warn)
                _pendingWarning = warn;

            var agentMissing = false;
            if (_userSession is uint session)
            {
                var lastSeen = _heartbeats.TryGetValue(session, out var hb) ? hb.At : DateTimeOffset.MinValue;
                agentMissing = now - (lastSeen > _userSessionSince ? lastSeen : _userSessionSince) > AgentGrace;
            }

            _blocked = !_decision.Allowed || agentMissing;
            _message = _decision.Allowed && agentMissing
                ? "Chương trình LimitYT trên máy tính không chạy nên tạm chặn"
                : _decision.Describe();

            if (now - _lastPersist >= PersistInterval) PersistLocked(now);

            return new TickResult(_blocked, _message, _config.AllDomains.ToList(), agentMissing, _config.BrowserExtensionAllowlist);
        }
    }

    public AgentReply OnHeartbeat(uint sessionId, AgentHeartbeat heartbeat)
    {
        lock (_gate)
        {
            _heartbeats[sessionId] = (_time.GetUtcNow(), heartbeat.Watching);
            var warning = _pendingWarning;
            _pendingWarning = null;
            return new AgentReply(
                _blocked,
                _message,
                _decision.Allowed ? (int)Math.Ceiling(_decision.Left.TotalMinutes) : 0,
                warning,
                _config.Version,
                _config.Services.Select(s => new ServicePatterns(s.Id, s.TitlePatterns)).ToList(),
                _config.TitleExcludePatterns,
                _config.AllPcExecutables.ToList(),
                _config.PcBlockedProcessPatterns);
        }
    }

    public void Persist()
    {
        lock (_gate) PersistLocked(_time.GetUtcNow());
    }

    private void PersistLocked(DateTimeOffset now)
    {
        if (_dirtyDates.Count > 0)
        {
            _store.SaveUsage(_dirtyDates.Select(d => new UsageEntry(Devices.Pc, d, _ledger.DeviceMs(Devices.Pc, d))).ToList());
            _dirtyDates.Clear();
        }
        _lastPersist = now;
    }

    private void ApplyConfig(LimitConfig config)
    {
        _config = config;
        _engine = new RulesEngine(config);
    }
}
