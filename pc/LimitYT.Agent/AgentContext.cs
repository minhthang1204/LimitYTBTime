using LimitYT.Core.Blocking;
using LimitYT.Core.Ipc;
using Microsoft.Win32;

namespace LimitYT.Agent;

/// <summary>
/// Agent chạy trong phiên người dùng (docs/02-kien-truc.md §6.3): mỗi 5 giây quét tiêu đề cửa sổ,
/// gửi heartbeat cho Service, đóng YouTube/TikTok khi bị chặn và hiện thời gian còn lại trên khay hệ thống.
/// Agent không có nút thoát; nếu bị tắt thì Service chặn YouTube và khởi động lại Agent.
/// </summary>
internal sealed class AgentContext : ApplicationContext
{
    private const string AppName = "LimitYT";

    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private readonly ServiceClient _client = new();
    private readonly WindowWatcher _watcher = new();
    private readonly TabCloser _closer = new();
    private readonly DisplayStateWatcher _display = new();

    private TitleMatcher? _matcher;
    private int _configVersion = -1;
    private bool _locked;
    private bool _busy;
    private bool? _wasBlocked;
    private AgentReply? _last;

    public AgentContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Xem thời gian còn lại", null, (_, _) => ShowStatus());
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = AppName,
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => ShowStatus();

        SystemEvents.SessionSwitch += OnSessionSwitch;
        _timer.Tick += async (_, _) => await TickAsync();
        _timer.Start();
        _ = TickAsync();
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) _locked = true;
        else if (e.Reason == SessionSwitchReason.SessionUnlock) _locked = false;
    }

    private async Task TickAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var hits = _matcher is null ? [] : _watcher.Scan(_matcher);
            var watching = hits.Count > 0 && !_locked && _display.DisplayOn;
            var reply = await _client.SendAsync(new AgentHeartbeat(watching, hits.FirstOrDefault()?.ServiceId));
            AgentLog.Write($"hits={hits.Count} watching={watching} locked={_locked} display={_display.DisplayOn} reply={(reply is null ? "null" : $"blocked={reply.Blocked} left={reply.LeftMinutes}")}");
            if (reply is null)
            {
                SetTrayText("không kết nối được dịch vụ");
                return;
            }
            _last = reply;

            if (reply.ConfigVersion != _configVersion)
            {
                _matcher = new TitleMatcher(reply.Patterns.Select(p => (p.ServiceId, (IEnumerable<string>)p.Patterns)), reply.TitleExcludes);
                _configVersion = reply.ConfigVersion;
                hits = _watcher.Scan(_matcher);
            }

            TabCloser.KillExecutables(reply.Executables);
            if (reply.Blocked && hits.Count > 0) _closer.Close(hits);

            if (reply.Blocked && _wasBlocked != true && hits.Count > 0)
                Balloon($"{reply.Message}. YouTube/TikTok đã bị đóng.", ToolTipIcon.Warning);
            else if (reply.WarnMinutes is int minutes)
                Balloon($"Còn {minutes} phút xem YouTube/TikTok.", ToolTipIcon.Info);
            _wasBlocked = reply.Blocked;

            SetTrayText(reply.Blocked ? $"đang chặn — {reply.Message}" : $"còn {reply.LeftMinutes} phút");
        }
        catch (Exception e)
        {
            // Lỗi bất ngờ không được làm sập Agent; Service sẽ fail-closed nếu heartbeat dừng.
            AgentLog.Write("Lỗi: " + e);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ShowStatus()
    {
        var text = _last is null ? "Chưa kết nối được dịch vụ LimitYT."
            : _last.Blocked ? _last.Message
            : $"Còn {_last.LeftMinutes} phút xem YouTube/TikTok.";
        Balloon(text, ToolTipIcon.Info);
    }

    private void Balloon(string text, ToolTipIcon icon) => _tray.ShowBalloonTip(5000, AppName, text, icon);

    private void SetTrayText(string text)
    {
        var full = $"{AppName}: {text}";
        _tray.Text = full.Length > 127 ? full[..127] : full;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _timer.Dispose();
            _tray.Dispose();
            _client.Dispose();
            _display.Dispose();
        }
        base.Dispose(disposing);
    }
}
