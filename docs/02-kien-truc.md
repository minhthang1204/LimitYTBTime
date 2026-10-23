# 02 — Kiến trúc & thiết kế

## 1. Tổng quan

Hệ thống gồm **hai node ngang hàng**. Mỗi node tự đếm giờ và tự chặn, rồi đồng bộ với node kia qua LAN khi thấy nhau.

```
┌─────────────────── Máy tính Windows ───────────────────┐        ┌──────────── Xiaomi TV (Android TV) ────────────┐
│                                                         │        │                                                 │
│ ┌──────────────────┐  Named Pipe  ┌──────────────────┐  │  LAN   │ ┌─────────────────────────────────────────────┐ │
│ │ LimitYT.Agent    │◄────────────►│ LimitYT.Service  │◄─┼─HTTP──►│ │ LimitYT TV app                              │ │
│ │ (tray, user)     │              │ (LocalSystem)    │  │ +mDNS  │ │  - GuardAccessibilityService (đo + chặn)    │ │
│ │ - đọc tiêu đề    │              │ - RulesEngine    │  │        │ │  - RulesEngine (cùng test vectors với PC)   │ │
│ │ - đóng tab       │              │ - UsageStore     │  │        │ │  - UsageStore (SQLite/Room)                 │ │
│ │ - cảnh báo       │              │ - PcBlocker      │  │        │ │  - SyncServer / SyncClient                  │ │
│ └──────────────────┘              │ - SyncServer     │  │        │ │  - SelfHeal (bật lại Accessibility)         │ │
│                                   │ - TvWatchdog ────┼──┼─ADB───►│ │  - BlockScreen / PinScreen                  │ │
│                                   └──────────────────┘  │ (tuỳ   │ └─────────────────────────────────────────────┘ │
│  hosts, browser policy ◄── PcBlocker                    │ chọn)  │                                                 │
└─────────────────────────────────────────────────────────┘        └─────────────────────────────────────────────────┘
```

- **PC Service** là nơi phụ huynh quản trị (đổi cấu hình, cộng giờ) bằng mật khẩu.
- **TV app** chạy hoàn toàn độc lập khi PC tắt.
- **TvWatchdog** (khi PC bật và Network debugging trên TV còn bật): dùng ADB kiểm tra app TV còn cài, Accessibility còn bật, chưa bị force stop; nếu sai thì sửa lại. Đây là lớp bảo vệ thứ hai, không bắt buộc phải có.

## 2. Công nghệ

| Thành phần | Lựa chọn | Lý do |
|-----------|----------|-------|
| PC | **C# / .NET 10 (LTS)**: Worker Service + WinForms tray | Windows Service, P/Invoke Win32 thuận tiện; publish ra exe tự chứa (máy đích không cần cài .NET). .NET 8 hết hỗ trợ 11/2026 nên không dùng |
| TV | **Kotlin**, minSdk 28, targetSdk 35. Không dùng AndroidX/Compose: View dựng bằng code, `SQLiteOpenHelper`, `JobScheduler`. Thư viện ngoài duy nhất: `kotlinx-serialization-json` | APK nhỏ, ít phụ thuộc; Accessibility, NSD có sẵn trong Android |
| Lưu trữ | SQLite (`Microsoft.Data.Sqlite` / `SQLiteOpenHelper`) | |
| Đồng bộ | HTTP/JSON trên LAN, ký HMAC-SHA256 | Đơn giản, debug được bằng curl |
| Tìm thiết bị | mDNS `_limityt._tcp` (Android NSD; trên PC dùng `Makaretu.Dns.Multicast`) + IP cấu hình tay để dự phòng | |
| ADB | `adb.exe` (platform-tools) đóng gói kèm | Dùng cho cài đặt TV và cho TvWatchdog |

**Logic quy tắc được viết hai lần** (C# và Kotlin). Để hai bản không lệch nhau: thư mục `spec/rules-test-vectors.json` chứa các ca kiểm thử (cấu hình + thời điểm + số giờ đã dùng → kết quả mong đợi), và **cả hai** bộ test đều chạy cùng file này.

## 3. Cấu trúc thư mục

```
LimitYTBTime/
├─ docs/
├─ spec/
│  ├─ sync-protocol.md
│  └─ rules-test-vectors.json
├─ tools/dev-env.ps1           # đặt .NET SDK, Android SDK, Gradle, cache lên E:\DevTools (dùng chung)
├─ pc/                         # mọi thứ của máy tính Windows
│  ├─ LimitYT.Core/            # RulesEngine, LocalCalendar, UsageLedger (merge)
│  ├─ LimitYT.Service/         # Worker, SqliteStore, HostsBlocker, BrowserPolicy, PipeServer (+ Sync, TvWatchdog ở GĐ3)
│  ├─ LimitYT.Agent/           # WindowWatcher, TabCloser, tray, PipeClient
│  ├─ LimitYT.Cli/             # công cụ quản trị
│  ├─ LimitYT.Core.Tests/
│  ├─ install/                 # install-pc.ps1, uninstall-pc.ps1
│  └─ publish-pc.ps1           # → out\pc
└─ tv/                         # Android Gradle project (package vn.limityt.tv)
   ├─ app/src/main/java/vn/limityt/tv/
   │  ├─ rules/                # LimitConfig, LocalCalendar, UsageLedger, RulesEngine (port từ LimitYT.Core)
   │  ├─ guard/GuardAccessibilityService.kt, ScreenRules.kt, PinGuard.kt (PIN + ClockGuard)
   │  ├─ data/UsageStore.kt (SQLite), Guardian.kt (trạng thái dùng chung trong tiến trình)
   │  ├─ heal/SelfHealJob.kt (+ BootReceiver), AccessibilityToggle.kt
   │  ├─ setup/SetupReceiver.kt   # ghép cặp qua ADB
   │  ├─ ui/MainActivity, BlockActivity, PinActivity, AdminActivity
   │  └─ sync/                 # GĐ3: SyncServer, SyncClient, Discovery
   ├─ app/src/test/            # RulesVectorTest (spec/rules-test-vectors.json) + test luật màn hình, PIN, giờ
   ├─ install/                 # install-tv.ps1, uninstall-tv.ps1
   └─ build-tv.ps1             # → out\tv (APK + script + adb.exe)
```

## 4. Mô hình dữ liệu

### 4.1. Cấu hình (do PC sở hữu, có số phiên bản)

```json
{
  "version": 3,
  "timeZone": "Asia/Ho_Chi_Minh",
  "weekStart": "Monday",
  "weeklyQuotaMinutes": 420,
  "dailyQuotaMinutes": 90,
  "blockedWindows": [
    { "days": ["Mon","Tue","Wed","Thu","Fri","Sat","Sun"], "from": "22:00", "to": "06:00" },
    { "days": ["Mon","Tue","Wed","Thu","Fri"], "from": "07:00", "to": "11:00", "schoolOnly": true },
    { "days": ["Mon","Tue","Wed","Thu","Fri"], "from": "13:00", "to": "17:00", "schoolOnly": true }
  ],
  "holidays": [
    { "from": "2027-01-29", "to": "2027-02-14", "note": "Tết" },
    { "from": "2027-06-01", "to": "2027-08-31", "note": "Nghỉ hè" }
  ],
  "holidayOverrides": { "2026-10-26": true },      // bật/tắt tay theo ngày (true = nghỉ, false = đi học)
  "holidayDailyQuotaMinutes": null,                 // null = dùng dailyQuotaMinutes
  "services": [
    {
      "id": "youtube",
      "domains": ["youtube.com","www.youtube.com","m.youtube.com","music.youtube.com","youtu.be",
                  "youtube-nocookie.com","www.youtube-nocookie.com","youtubei.googleapis.com","i.ytimg.com"],
      "titlePatterns": ["(?i)(^|\\s[-–|]\\s)YouTube( Music)?(\\s[-–|]\\s[^-–|]+)?$"],
      "tvPackages": ["com.google.android.youtube.tv","com.google.android.youtube.tvkids",
                     "com.google.android.youtube.tvmusic","com.liskovsoft.smarttubetv",
                     "com.liskovsoft.smarttubetv.beta","com.teamsmart.videomanager.tv"],
      "pcExecutables": ["FreeTube.exe","yt-dlp.exe"]
    },
    {
      "id": "tiktok",
      "domains": ["tiktok.com","www.tiktok.com","m.tiktok.com","vt.tiktok.com","vm.tiktok.com",
                  "tiktokv.com","www.tiktokv.com"],
      "titlePatterns": ["(?i)(^|\\s[-–|]\\s)TikTok(\\s[-–|]\\s[^-–|]+)?$"],
      "tvPackages": ["com.ss.android.ugc.trill","com.zhiliaoapp.musically"],   // TV hiện không có app TikTok; giữ để chặn nếu bị sideload
      "pcExecutables": ["TikTok.exe"]
    }
  ],
  "warnAtMinutesLeft": [15, 5],
  "offlineCapPercent": { "pc": 50, "tv": 100 },
  "tv": {
    "guardedSettingsPackages": ["com.android.tv.settings","com.xiaomi.mitv.settings"],
    "neverBlockPackages": ["<TV Input / HDMI player — lấy ở Giai đoạn 0>", "<launcher>"],
    "installerPackages": ["com.android.packageinstaller","com.google.android.packageinstaller"]
  },
  "adminPinHash": "<PBKDF2>",
  "updatedAtUtc": "2026-10-23T12:00:00Z"
}
```

TV không có app TikTok và trình cài APK đã bị chặn, nên trên TV chỉ còn hai đường vào TikTok: qua trình duyệt (`isBrowserShowingLimitedService`) và qua APK bị sideload (đã có trong `tvPackages`). Domain TikTok trên PC cần kiểm chứng thực tế ở Giai đoạn 1. Chỉ cần chặn các domain trang chính và API là đủ; CDN video không cần chặn, lý do giống như với `googlevideo.com`.

Khung giờ có `to < from` (ví dụ 22:00–06:00) nghĩa là khung giờ vắt qua nửa đêm.

`neverBlockPackages` được kiểm tra **trước mọi luật khác** trong GuardAccessibilityService. Danh sách này gồm app phát nguồn vào HDMI (TV Input / Live TV của Xiaomi) và launcher. Nhờ vậy việc xem camera không bao giờ bị ảnh hưởng.

### 4.2. Sổ ghi giờ đã dùng (Usage Ledger) — không bao giờ xung đột khi đồng bộ

Mỗi node **chỉ ghi vào dòng của chính nó**:

```
usage[deviceId][localDate] = tổng số giây đã xem
```

- Gộp hai bản: với mỗi cặp `(deviceId, date)`, lấy **max**. Số giây của một thiết bị trong một ngày chỉ tăng, nên phép gộp này luôn đúng và không phụ thuộc thứ tự đồng bộ.
- **Điều chỉnh của phụ huynh** (cộng/trừ giờ) là các bản ghi có `id` duy nhất (UUID): `{id, date, seconds, reason}`. Khi gộp thì lấy hợp (union) theo `id`.
- **Cấu hình**: so sánh `(version, updatedAtUtc)`, bên lớn hơn thắng; nếu vẫn hoà thì **PC thắng**. Chỉ PC (hoặc TV sau khi nhập PIN) được tăng `version`.

```
dailyUsed(d)  = Σ_device usage[device][d] − Σ adjustments(d)
weeklyUsed(w) = Σ_{d ∈ w} dailyUsed(d)
```

### 4.3. Bảng SQLite (giống nhau ở hai bên)

```sql
CREATE TABLE usage (device_id TEXT, date TEXT, ms INTEGER, PRIMARY KEY (device_id, date));   -- mili-giây
CREATE TABLE adjustment (id TEXT PRIMARY KEY, date TEXT, ms INTEGER, reason TEXT, created_at TEXT);
CREATE TABLE peer (device_id TEXT PRIMARY KEY, last_sync_utc TEXT, last_address TEXT);
CREATE TABLE kv (key TEXT PRIMARY KEY, value TEXT);  -- config json, clock anchors...
```

## 5. RulesEngine

```
evaluate(now, config, ledger, lastPeerSyncToday) -> Decision {allowed, reason, minutesLeft, nextChangeAt}

  if unlockedUntil > now:                           return Allowed(...)   # unlock: bỏ mọi chặn, vẫn đếm giờ
  holiday = holidayOverrides[today] ?? today ∈ holidays
  if inBlockedWindow(now, skipSchoolOnly = holiday): return Blocked("Giờ cấm", until = end of window)
  dailyQuota = holiday ? (holidayDailyQuotaMinutes ?? dailyQuotaMinutes) : dailyQuotaMinutes
  dailyLeft  = dailyQuota  − dailyUsed(today)       # adjustment (add) đã nằm trong dailyUsed, nên tăng cả ngày lẫn tuần
  weeklyLeft = weeklyQuota − weeklyUsed(thisWeek)
  left = min(dailyLeft, weeklyLeft)
  if not lastPeerSyncToday:
      # ngân sách offline cố định theo phần còn lại tại thời điểm bắt đầu offline
      # (đầu ngày, hoặc lần đồng bộ cuối nếu trong ngày), không tính lại mỗi tick
      budget = leftAtOfflineStart × offlineCapPercent[self] / 100
      left = min(left, budget − selfUsedSinceOfflineStart)
  if left <= 0:                                     return Blocked(dailyLeft<=0 ? "Hết giờ hôm nay" : "Hết giờ tuần này")
  minutesLeft = min(left, minutes until next blocked window)
  return Allowed(minutesLeft)
```

Quy ước:

- Quota ngày không dùng hết **không** được cộng dồn sang hôm sau.
- Cấu hình thay đổi thì đánh giá lại ngay. Nếu quota mới thấp hơn số giờ đã dùng thì chặn ngay.
- Khi cộng giờ, `delta` vắt qua 00:00 (hoặc qua Thứ Hai 00:00) được tách tại mốc đó: phần trước ghi vào ngày cũ, phần sau ghi vào ngày mới.
- Số giờ đã dùng được kẹp ở mức ≥ 0 khi có adjustment âm.

`minutesLeft` dùng cho cảnh báo F9: khi qua ngưỡng 15 hoặc 5 phút thì hiện thông báo (mỗi ngưỡng một lần cho mỗi lần chuyển trạng thái).

## 6. Node PC

### 6.1. Vòng lặp Service (mỗi 5 giây)

```
tick():
  delta = stopwatch.Elapsed, giới hạn ≤ 10s   # máy sleep sẽ không bị cộng giờ
  if agent.watching and not sessionLocked and displayOn: ledger.add(self, today, delta)
  if startupSyncPending (≤ 30s sau khi khởi động / thức dậy): PcBlocker.block(); return
  decision = rules.evaluate(...)
  if not decision.allowed: PcBlocker.block(); agent.send(CloseYouTube, decision.reason)
  else:                    PcBlocker.unblock()          # idempotent
  warnings.check(decision)
```

Đồng bộ: mỗi **30 giây** (và ngay khi Service khởi động, khi máy thức dậy từ sleep, hoặc khi vừa tìm thấy TV) gọi `POST /sync` tới TV. TV bật gần như cả ngày, nên trường hợp thường gặp là đồng bộ được ngay.

Trạng thái khoá phiên và trạng thái màn hình do **Agent** theo dõi: `SystemEvents.SessionSwitch` và `RegisterPowerSettingNotification(GUID_SESSION_DISPLAY_STATUS)`. Agent báo `watching = false` khi phiên bị khoá hoặc màn hình tắt. Dùng GUID theo phiên thay cho `GUID_CONSOLE_DISPLAY_STATE`, vì Agent chạy trong phiên của người dùng. Số phiên của Agent được Service lấy từ chính kết nối pipe (`GetNamedPipeClientSessionId`), không tin con số do Agent gửi.

### 6.2. PcBlocker

- `block()`: chèn khối giữa hai dòng đánh dấu `# >>> LimitYT BEGIN` và `# <<< LimitYT END` vào `hosts` (ghi ra file tạm rồi thay thế, để không làm hỏng `hosts`). Sau đó `ipconfig /flushdns` và ghi `URLBlocklist` (Chrome/Edge/Brave/Cốc Cốc) và `WebsiteFilter` (Firefox).
- `unblock()`: xoá khối khỏi `hosts`, xoá `URLBlocklist` / `WebsiteFilter`.
- Lúc cài đặt: áp thường trực `DnsOverHttpsMode=off` và `QuicAllowed=0` (Chrome/Edge/Brave), và `DNSOverHTTPS {Enabled:false, Locked:true}` (Firefox). Policy Firefox được ghi vào **registry** `HKLM\SOFTWARE\Policies\Mozilla\Firefox` chứ không qua `policies.json`, để áp dụng được cả cho Firefox portable.
- Mỗi domain có hai dòng trong `hosts`: `0.0.0.0` và `::` (IPv6).

### 6.3. Agent

Mỗi 5 giây Agent duyệt các cửa sổ top-level của **mọi tiến trình** (không chỉ trình duyệt, để bắt được PWA và app Store). Nếu có cửa sổ nào có tiêu đề khớp một `titlePatterns` → `watching = true` (chỉ tính một lần dù có nhiều cửa sổ). Mẫu khớp **ở cuối tiêu đề** (ví dụ `<tên video> - YouTube - Google Chrome`), không khớp chuỗi con. Nhờ vậy trang `youtube - Tìm với Google` không bị đếm. Tiến trình có tên trong `pcExecutables` bị kill ngay khi phát hiện. Agent gửi heartbeat cho Service. Khi đang bị chặn mà vẫn thấy cửa sổ YouTube: đưa cửa sổ đó lên trước, gửi `Ctrl+W`; nếu sau 2 giây tiêu đề vẫn chứa YouTube thì gửi `WM_CLOSE`. Agent cũng hiện thời gian còn lại trên tray và hiện cảnh báo dạng toast.

Tài khoản Standard **có quyền** kill hoặc suspend Agent bằng Task Manager, vì Agent chạy dưới chính tài khoản đó. Vì vậy Service áp nguyên tắc **fail-closed**: nếu không nhận được heartbeat quá **15 giây** trong khi có người đăng nhập, Service sẽ:

1. `PcBlocker.block()` với lý do "Agent không hoạt động";
2. khởi động lại Agent trong phiên người dùng (`WTSQueryUserToken` + `CreateProcessAsUser`);
3. bỏ chặn khi heartbeat trở lại, nếu quy tắc cho phép.

Service được đăng ký thêm trong `HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Network` để vẫn chạy khi máy khởi động vào Safe Mode with Networking.

### 6.4. TvWatchdog (tuỳ chọn)

Mỗi 5 phút, nếu kết nối ADB tới TV được:

- `pm list packages` → app LimitYT còn cài không;
- `settings get secure enabled_accessibility_services` → còn chứa service không; nếu không thì `settings put` lại;
- `dumpsys activity services <app>` → service có đang chạy không.

Phát hiện bất thường thì sửa lại và ghi vào nhật ký để phụ huynh xem (`limityt log`).

## 7. Node TV

### 7.1. GuardAccessibilityService

Cấu hình service: `accessibilityEventTypes = typeWindowStateChanged|typeWindowContentChanged`, `canRetrieveWindowContent = true`, `notificationTimeout = 300`.

```
onAccessibilityEvent(e):
  pkg = e.packageName
  if pkg in neverBlockPackages: foregroundYouTube = false; return   # HDMI/camera, launcher
  if pinUnlocked(): return                    # phụ huynh đã mở khoá tạm thời (mặc định 10 phút)

  if pkg in services[*].tvPackages or isBrowserShowingLimitedService(e):
      foregroundYouTube = true
      if not rules.evaluate().allowed: kick(reason)
  else:
      foregroundYouTube = false

  if pkg in installerPackages: kick("Cần PIN để cài ứng dụng")
  if pkg in guardedSettingsPackages and screenMentions(e, ["LimitYT", "YouTube", "TikTok", "Accessibility",
                                                          "Trợ năng", "Date", "Ngày giờ", "Hỗ trợ tiếp cận",
                                                          "Developer options", "Tùy chọn nhà phát triển", "Build"]):
      kick("Cần PIN để mở mục này")
  if pkg in cleanerPackages: kick("Cần PIN")   # trình dọn dẹp/tăng tốc của Xiaomi — mặc định luôn chặn, xem 04 §C4

kick(reason):
  performGlobalAction(GLOBAL_ACTION_HOME)
  start BlockActivity(reason, minutesUntilAllowed)   # có nút "Nhập PIN"
```

Danh sách chuỗi nhận diện màn hình Settings phụ thuộc firmware và ngôn ngữ, nên được thu thập ở Giai đoạn 0 và lưu trong `ScreenRules`.

**PIN**: 6 số, lưu dạng PBKDF2. Sai 5 lần liên tiếp → khoá nhập PIN 15 phút, mỗi lần khoá sau gấp đôi lần trước. Mọi lần sai đều được ghi log và đồng bộ lên PC.

### 7.2. Đếm giờ

Một coroutine chạy trong service, mỗi 5 giây:

```
if foregroundYouTube and powerManager.isInteractive:
    ledger.add(self, today, delta)            # delta theo SystemClock.elapsedRealtime, giới hạn ≤ 10s
    if not rules.evaluate().allowed: kick(...)
```

Chỉ dựa vào sự kiện Accessibility thì có thể bỏ sót. Vì vậy mỗi tick cũng kiểm tra lại foreground bằng `rootInActiveWindow?.packageName`.

**Chạy dài (TV bật ~18 giờ/ngày)**:

- Giờ đã dùng được cộng dồn trong RAM và ghi xuống DB **mỗi 30 giây**, ngoài ra ghi ngay khi rời YouTube, khi màn hình tắt (`ACTION_SCREEN_OFF`), hoặc khi tới mốc chặn. Cách này tránh ghi flash quá dày. Mất điện đột ngột thì mất tối đa 30 giây.
- Khi TV vào chế độ chờ (`ACTION_SCREEN_OFF`): dừng tick, dừng đồng bộ chủ động, nhưng vẫn giữ SyncServer. Khi màn hình bật lại (`ACTION_SCREEN_ON`): đồng bộ ngay, đánh giá lại quy tắc, và kiểm tra app foreground. Nếu YouTube đã mở sẵn mà đang bị chặn thì kick ngay.
- Không giữ tham chiếu `AccessibilityNodeInfo` qua các sự kiện (tránh rò rỉ bộ nhớ). Ghi log nội bộ dạng vòng (ring buffer, tối đa ~1 MB).
- Cảnh báo 15 phút / 5 phút trên TV hiện bằng overlay nhỏ ở góc màn hình (`TYPE_ACCESSIBILITY_OVERLAY`), không chiếm focus và không dừng video.

### 7.3. Chống chỉnh giờ

Mỗi tick, app lưu cặp mốc `(elapsedRealtime, currentTimeMillis)`. Nếu `Δwall − Δelapsed` lệch quá 2 phút mà không có reboot, thì coi là **giờ bị chỉnh**. Khi đó app tạm khoá YouTube cho tới khi (a) đồng bộ được với PC và giờ hai bên khớp nhau trong khoảng 2 phút, hoặc (b) phụ huynh nhập PIN.

### 7.4. SelfHeal

- `BootReceiver` (`BOOT_COMPLETED`, `MY_PACKAGE_REPLACED`) và `SelfHealJob` (JobScheduler, mỗi 15 phút) kiểm tra Accessibility còn bật không. Nếu không, dùng `WRITE_SECURE_SETTINGS` để bật lại.
- Lưu ý: nếu app bị **force stop** thì cả hai cơ chế trên không chạy, cho tới khi TV khởi động lại hoặc PC Watchdog sửa. Vì vậy việc chặn trang Settings → Apps → LimitYT (mục 7.1) là lớp phòng thủ chính.
- **Fail-closed khi không có cấu hình**: lần chạy đầu chưa ghép cặp, hoặc dữ liệu bị xoá. App chặn mọi dịch vụ bị giới hạn và hiện "Cần kết nối lại với máy tính". Khi PC bật, TvWatchdog phát hiện TV chưa ghép cặp thì tự ghép cặp lại qua ADB. Sau lần đồng bộ đầu tiên, TV nhận lại cấu hình và số giờ của chính nó (bản sao trên PC).

### 7.5. Cài đặt TV — `tv/install/install-tv.ps1`

```
adb connect <IP_TV>:5555
adb install -r limityt-tv.apk
adb shell pm grant vn.limityt.tv android.permission.WRITE_SECURE_SETTINGS
adb shell appops set vn.limityt.tv SYSTEM_ALERT_WINDOW allow
adb shell am broadcast -f 0x20 -n vn.limityt.tv/.setup.SetupReceiver -a vn.limityt.tv.SETUP \
    --es pin <6 số> [--es config <config.json dạng base64>]
```

- `SetupReceiver` yêu cầu quyền `android.permission.DUMP`. Chỉ `adb shell` có quyền này, app thường không xin được, nên không app nào khác trên TV giả lệnh ghép cặp được.
- Ghép cặp xong, app tự bật Accessibility bằng `WRITE_SECURE_SETTINGS` (không cần `settings put` từ script).
- Chạy lại script để cài bản mới: PIN, cấu hình và số giờ đã xem được giữ. Không gửi `config` thì giữ cấu hình đang có. Ở Giai đoạn 3, `secret` đồng bộ sẽ đi cùng lệnh này.
- `adb shell dumpsys activity service vn.limityt.tv/.guard.GuardAccessibilityService` in trạng thái (dùng cho TvWatchdog).

### 7.6. Những điểm đã chốt khi code Giai đoạn 2

- **Mở khoá trên TV có hai loại**, đều chỉ có sau khi nhập PIN:
  - *Mở Cài đặt 10 phút* (`adminUntil`): được vào Settings, cài app, mở trình dọn dẹp. **Không** bỏ chặn YouTube.
  - *Bỏ mọi chặn 1 giờ* (`unlockedUntil`, giống `limityt unlock`): YouTube được mở kể cả trong giờ cấm, **vẫn đếm giờ**.
- **Chưa ghép cặp** (chưa có PIN): YouTube/TikTok bị chặn (C1), nhưng Settings **không** bị khoá, vì không có PIN để mở.
- **Chống chỉnh giờ**: nếu TV bật "giờ tự động" (mặc định) thì giờ chỉ đổi do đồng bộ mạng, nên không coi là bị chỉnh. Màn hình Ngày & giờ đã bị khoá bằng PIN, nên con không tắt được "giờ tự động".
- Các danh sách package có sẵn trong app (`ScreenRules`) được **hợp** với danh sách trong cấu hình từ PC. Cấu hình chỉ thêm được, không bỏ được mục có sẵn.
- App lạ có package hoặc tên kiểu `*youtube*`, `newpipe`, `...Tube`, `Tik Tok` được coi như YouTube/TikTok (C11).
- Trình duyệt: đọc chữ trên màn hình (thanh địa chỉ) và so với `services[*].domains`, khớp theo tên miền đầy đủ (`notyoutube.com` không khớp).
- Giai đoạn 2 chưa có đồng bộ, nên chưa dùng `offlineCapPercent` trên TV (mặc định 100% nên kết quả như nhau).

## 8. Giao thức đồng bộ (chi tiết trong `spec/sync-protocol.md`)

- Mỗi node chạy một HTTP server: PC trên cổng **47600**, TV trên cổng **47601**. Mỗi node quảng bá `_limityt._tcp` qua mDNS.
- `POST /sync`. Body gửi đi gồm: `{deviceId, sentAtUtc, configVersion, config?, usage[], adjustments[]}`. Bên nhận gộp dữ liệu (mục 4.2) và trả về bản đã gộp của mình theo cùng định dạng.
- Header `X-LimitYT-Seq: <số thứ tự>` và `X-LimitYT-Sig: HMAC-SHA256(secret, seq + body)`. Mỗi bên giữ một số thứ tự tăng dần theo từng peer. Bên nhận từ chối nếu chữ ký sai hoặc `seq` không lớn hơn số đã nhận lần trước (chống phát lại). Việc chống phát lại **không dựa vào giờ**: nếu dựa vào giờ thì một thiết bị bị chỉnh giờ sẽ không đồng bộ được, trong khi chính việc đồng bộ là cách để phát hiện chỉnh giờ (mục 7.3). Độ lệch giờ giữa hai bên được ghi nhận và báo, không làm đồng bộ thất bại.
- Windows Firewall: script cài thêm rule inbound cho cổng 47600, chỉ cho phép từ `LocalSubnet`, áp cho mọi profile mạng.
- `secret` được sinh trên PC lúc cài, rồi chuyển sang TV qua ADB lúc ghép cặp.
- Mỗi lần đồng bộ thành công, hai bên cập nhật `peer.last_sync_utc` (dùng cho `offlineCapPercent`) và gửi kèm giờ của mình để làm mốc kiểm tra chỉnh giờ (mục 7.3).

## 9. Quản trị

PC — `limityt.exe`. Lệnh chỉ chạy được khi mở terminal bằng **Run as administrator**, vì pipe quản trị chỉ cho nhóm Administrators kết nối. Không có mật khẩu riêng của app: con dùng tài khoản Standard nên không mở được pipe, và nếu có mật khẩu riêng thì cũng không thêm được lớp bảo vệ nào. Các lệnh có ★ thuộc Giai đoạn 3.

```
limityt status                       # đã dùng hôm nay / tuần này, theo từng thiết bị, trạng thái đồng bộ
limityt set weekly 7h | daily 90m
limityt window add Mon-Fri 07:00-11:00
limityt add 30m [--reason "thưởng"]  # cộng thêm giờ (tạo adjustment)
limityt unlock 1h                    # tạm bỏ mọi chặn (kể cả giờ cấm) trên cả hai thiết bị; vẫn đếm giờ
limityt holiday add 2027-06-01..2027-08-31 "Nghỉ hè"
limityt holiday today on|off         # bật/tắt tay "hôm nay nghỉ học"
limityt set pin                      # ★ đặt lại PIN TV (đồng bộ sang TV)
limityt tv install <IP>              # ★ gọi install-tv.ps1
limityt log
```

TV — nhập PIN ở BlockActivity để mở khoá tạm thời 10 phút (để vào Settings, cài app…) hoặc cộng giờ nhanh.

## 10. Kiểm thử

- `spec/rules-test-vectors.json`: các ca biên như khung giờ qua nửa đêm, mốc Thứ Hai 00:00, quota ngày hết trước quota tuần, offline cap, có adjustment âm. Cả **C#** và **Kotlin** đều chạy bộ vector này.
- Unit test hàm gộp ledger: gộp theo thứ tự bất kỳ, gộp lặp lại nhiều lần đều phải cho cùng kết quả.
- Integration: `HostsBlocker` trên file giả; `SyncServer` của hai bên gọi chéo nhau.
- Thủ công: checklist trong `03-ke-hoach.md`.
