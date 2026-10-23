using System.Runtime.InteropServices;

namespace LimitYT.Agent;

/// <summary>Theo dõi màn hình bật/tắt để không tính giờ khi màn hình đã tắt (docs/01-phan-tich.md §8).</summary>
internal sealed class DisplayStateWatcher : NativeWindow, IDisposable
{
    private readonly IntPtr _registration;

    public DisplayStateWatcher()
    {
        CreateHandle(new CreateParams());
        var guid = NativeMethods.SessionDisplayStatus;
        _registration = NativeMethods.RegisterPowerSettingNotification(Handle, ref guid, 0);
    }

    /// <summary>Chỉ dùng khi kiểm thử lúc màn hình đang tắt: LIMITYT_AGENT_IGNORE_DISPLAY=1.</summary>
    private static readonly bool IgnoreDisplay = Environment.GetEnvironmentVariable("LIMITYT_AGENT_IGNORE_DISPLAY") == "1";

    private bool _displayOn = true;

    public bool DisplayOn
    {
        get => IgnoreDisplay || _displayOn;
        private set => _displayOn = value;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmPowerBroadcast && m.WParam == NativeMethods.PbtPowerSettingChange && m.LParam != IntPtr.Zero)
        {
            // POWERBROADCAST_SETTING { GUID PowerSetting; DWORD DataLength; UCHAR Data[] }: 0 = tắt, 1 = bật, 2 = mờ.
            var setting = Marshal.PtrToStructure<Guid>(m.LParam);
            if (setting == NativeMethods.SessionDisplayStatus)
            {
                DisplayOn = Marshal.ReadInt32(m.LParam, 20) != 0;
                AgentLog.Write($"display state = {Marshal.ReadInt32(m.LParam, 20)}");
            }
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registration != IntPtr.Zero) NativeMethods.UnregisterPowerSettingNotification(_registration);
        DestroyHandle();
    }
}
