# 03 — Kế hoạch triển khai

## Giai đoạn 0 — Khảo sát TV thật (trước khi code TV)

1. TV: *Settings → Device Preferences → About* → bấm 7 lần vào *Build* để bật Developer options. Vào *Developer options* → bật **USB debugging**; nếu có thì bật cả **Network debugging**.
2. PC (đã cài platform-tools):
   ```
   adb connect <IP_TV>:5555                       # chọn "Always allow" trên TV
   adb shell getprop ro.build.version.release     # phiên bản Android
   adb shell getprop ro.product.model
   adb shell pm list packages | findstr -i "youtube settings installer browser"
   adb shell settings get secure enabled_accessibility_services
   ```
3. Mở lần lượt các màn hình cần chặn (Settings → Apps → YouTube, Accessibility, Date & time, trình cài APK), rồi với mỗi màn hình chạy:
   ```
   adb shell dumpsys window | findstr mCurrentFocus
   adb shell uiautomator dump /sdcard/ui.xml && adb pull /sdcard/ui.xml
   ```
   Lưu các file `ui.xml` vào `spec/tv-screens/`. Đây là dữ liệu để viết `ScreenRules`.
4. Kiểm tra Network debugging có còn bật sau khi tắt TV bằng remote, và sau khi rút điện.
5. **Nguồn vào HDMI**: chuyển sang HDMI (nguồn camera) và chạy `adb shell dumpsys window | findstr mCurrentFocus` để lấy package của TV Input. Làm tương tự với **bảng chọn nguồn vào** (Inputs) và với launcher. Các package này sẽ vào `neverBlockPackages`. Dump thêm `ui.xml` của bảng Inputs để chắc chắn nó không khớp các từ khoá chặn Settings.
6. Đặt **IP cố định** cho TV (DHCP reservation trên router).
7. Tìm **app dọn dẹp / tăng tốc** của Xiaomi: mở từng app/mục có tên kiểu "Dọn dẹp", "Tăng tốc", "Quản lý TV", "Cleaner", "Boost" và chạy `adb shell dumpsys window | findstr mCurrentFocus` để lấy package. Danh sách này đưa vào `cleanerPackages`. Chưa cần biết nó có buộc dừng được app hay không (xem case C4).

Đã xác nhận: TV không có app TikTok, chỉ có 1 hồ sơ người dùng, không có chế độ chỉ phát tiếng.

## Giai đoạn 1 — Lõi + PC

- [x] `spec/rules-test-vectors.json` (27 ca)
- [x] `LimitYT.Core`: `RulesEngine`, `LocalCalendar`, `UsageLedger`, `TitleMatcher`, `HostsFile`, `WarningTracker` + test (82 test)
- [x] `SqliteStore`, `ConfigStore` (tự nạp lại khi sửa file)
- [x] `HostsBlocker`, `BrowserPolicy` (Chrome, Edge, Brave, Cốc Cốc, Firefox)
- [x] Agent: `WindowWatcher`, `TabCloser`, tray, cảnh báo, khoá phiên / màn hình tắt
- [x] IPC Named Pipe; fail-closed khi mất Agent; Service tự khởi động lại Agent
- [x] CLI: `status`, `history`, `set`, `window`, `holiday`, `add`, `unlock`
- [x] `install-pc.ps1` / `uninstall-pc.ps1`, `pc/publish-pc.ps1` (bản tự chứa 121 MB)
- [x] Chạy thử end-to-end ở chế độ `--dry-run`: đếm giờ → chặn → tự đóng cửa sổ YouTube trong ≤ 10 giây
- [ ] Cài thật trên máy của con và chạy checklist thủ công (kịch bản 1–8, 24, 26–33, 37–39)
- [ ] Kiểm chứng đường dẫn policy của Cốc Cốc (`SOFTWARE\Policies\CocCoc\Browser`) và định dạng tiêu đề TikTok thật

**Tiêu chí xong**: đặt quota ngày 2 phút → xem YouTube trên Chrome/Edge/Firefox (cả ẩn danh) → sau 2 phút tab bị đóng và không mở lại được. Khi tới giờ cấm cũng bị chặn tương tự. Giả lập sang ngày mới → mở lại được.

## Giai đoạn 2 — App TV

- [x] Project Android (`tv/`), `RulesEngine.kt` chạy pass bộ test vectors chung
- [x] `GuardAccessibilityService`: phát hiện YouTube (app + trình duyệt), kick về Home, `BlockActivity`, cảnh báo dạng overlay
- [x] Đếm giờ + `UsageStore` (ghi DB mỗi 30 giây, khi rời YouTube, khi tắt màn hình)
- [x] `PinActivity` (khoá khi sai 5 lần) → `AdminActivity`: mở Cài đặt 10 phút, cộng 15/30 phút, bỏ chặn 1 giờ, tắt bảo vệ
- [x] Chặn trang Settings nhạy cảm, trình cài APK, trình dọn dẹp — **bằng danh sách dự đoán**; cần chỉnh lại theo dữ liệu Giai đoạn 0
- [x] `SelfHealJob`, `BootReceiver`
- [x] Chống chỉnh giờ
- [x] `install-tv.ps1` / `uninstall-tv.ps1`, `tv/build-tv.ps1`
- [ ] Giai đoạn 0 trên TV thật → cập nhật package Xiaomi (HDMI, launcher, dọn dẹp) và từ khoá Settings trong `ScreenRules`
- [ ] Cài lên TV và chạy checklist thủ công (kịch bản 9–14, 19–22, 34–36)

**Tiêu chí xong**: PC tắt → TV vẫn đếm giờ, chặn đúng khi hết quota ngày, quota tuần và khi tới giờ cấm. Thử force stop / gỡ / tắt Accessibility từ remote đều không làm được khi chưa nhập PIN.

## Giai đoạn 3 — Đồng bộ LAN

- [ ] `spec/sync-protocol.md`
- [ ] SyncServer/Client hai bên, HMAC, mDNS + IP dự phòng
- [ ] `offlineCapPercent`
- [ ] Đồng bộ cấu hình (PC → TV)
- [ ] `TvWatchdog` trên PC

**Tiêu chí xong**: xem 30 phút trên TV, sau đó bật PC → PC hiện đúng 30 phút đã dùng trong vòng 1 phút. Cộng giờ trên PC → TV nhận được.

## Giai đoạn 4 — Hoàn thiện

- [ ] Core Audio: tính cả tab YouTube phát nền trên PC
- [ ] Chặn YouTube trong trình duyệt trên TV
- [ ] Nhật ký sự kiện / thống kê theo tuần
- [ ] Bộ cài Inno Setup

## Checklist kiểm thử thủ công

| # | Kịch bản | Kỳ vọng |
|---|---------|---------|
| 1 | Xem YouTube trên Chrome 1 phút | Đã dùng tăng ~1 phút |
| 2 | Hai cửa sổ YouTube cùng lúc | Không tính đôi |
| 3 | Mở Gmail, Google Search | Không tính giờ, không bị chặn |
| 4 | Hết quota ngày khi đang xem trên PC | Tab bị đóng trong ≤ 10 giây |
| 5 | Mở lại youtube.com sau khi bị chặn (Chrome, Edge, Firefox, ẩn danh) | Không truy cập được |
| 6 | Tới 22:00 khi đang xem | Bị chặn dù còn quota; có cảnh báo lúc 21:45 và 21:55 |
| 7 | Tài khoản Standard thử dừng service / sửa hosts / đổi giờ | Bị từ chối |
| 8 | Máy tính sleep 1 giờ khi tab YouTube đang mở | Không bị cộng 1 giờ |
| 9 | PC tắt, xem YouTube trên TV | Vẫn được đếm giờ; hết quota thì về Home + màn hình thông báo |
| 10 | TV tắt màn hình khi YouTube đang mở | Ngừng đếm giờ |
| 11 | Cast từ điện thoại khi đang bị chặn | App YouTube bị đẩy về Home |
| 12 | TV: vào Settings → Apps → LimitYT / Accessibility / Date & time | Bị đẩy ra, yêu cầu PIN |
| 13 | TV: cài APK SmartTube từ USB | Trình cài bị chặn |
| 14 | TV: đổi ngày giờ (sau khi nhập PIN rồi thoát) | Phát hiện giờ bị chỉnh → khoá cho tới khi đồng bộ hoặc nhập PIN |
| 15 | Xem 30 phút trên TV, bật PC | PC thấy 30 phút trong ≤ 1 phút |
| 16 | PC và TV không thấy nhau cả ngày | Tổng vượt quota tối đa theo `offlineCapPercent`; khi đồng bộ lại thì chặn đúng |
| 17 | Thứ Hai 00:00 | Cả hai thiết bị reset quota tuần |
| 18 | Phụ huynh `limityt add 30m` | Cả PC và TV có thêm 30 phút |
| 19 | Đang bị chặn, chuyển TV sang HDMI (camera) bằng remote và qua bảng Inputs | Chuyển được bình thường, không bị kick, không tính giờ |
| 20 | Xen kẽ: HDMI 20 phút → YouTube 10 phút → HDMI 30 phút → YouTube 5 phút | Đã dùng = 15 phút |
| 21 | Hết quota khi đang xem YouTube trên TV | Về Home; từ Home chuyển sang HDMI được ngay |
| 22 | Chạy thử liên tục 3 ngày, TV bật ~18 giờ/ngày, có tắt/mở bằng remote | App vẫn chạy, bộ nhớ không tăng dần, số giờ khớp với ghi chép tay |
| 23 | YouTube mở trên cả PC và TV cùng lúc 10 phút | Đã dùng tăng 20 phút |
| 24 | Khoá Windows (Win+L) khi tab YouTube đang mở | PC ngừng tính giờ |
| 25 | Bật PC lúc TV đang bật | YouTube trên PC bị chặn ≤ 30 giây cho tới khi đồng bộ xong, sau đó số giờ hiển thị đã gồm phần xem trên TV |
| 26 | Ngày trong lịch nghỉ, lúc 09:00 Thứ Ba | Xem được (bỏ giờ học); tới 22:00 vẫn bị chặn |
| 27 | `limityt holiday today on` vào một ngày đi học | Bỏ giờ học cho riêng hôm đó |
| 28 | Xem TikTok trên PC rồi YouTube trên TV | Cộng chung vào một quota |
| 29 | Tìm "youtube" trên Google | Không bị đếm giờ |
| 30 | Kill / suspend Agent bằng Task Manager | YouTube bị chặn trong ≤ 15 giây; Agent tự chạy lại |
| 31 | Khởi động vào Safe Mode with Networking | Service vẫn chạy và vẫn chặn |
| 32 | Mở YouTube bằng Firefox portable có bật DoH | Không vượt được chặn |
| 33 | Cài YouTube PWA ("Cài đặt YouTube" trong Chrome) | Được đếm giờ và bị chặn như tab thường |
| 34 | TV: nhập sai PIN 5 lần | Khoá nhập PIN 15 phút; PC thấy log |
| 35 | TV: chạy app dọn dẹp của Xiaomi (sau khi nhập PIN) rồi kiểm tra `adb shell dumpsys activity services <pkg LimitYT>` | Accessibility service vẫn chạy. Nếu bị dừng → thêm biện pháp ở case C4 |
| 36 | TV: Clear data app LimitYT (sau khi nhập PIN rồi thoát) | YouTube bị chặn với thông báo "Cần kết nối lại"; bật PC → tự ghép cặp lại, số giờ được khôi phục |
| 37 | Hết quota ngày, phụ huynh `add 30m` lúc 21:50 | Xem được tới 22:00 rồi bị chặn vì giờ cấm |
| 38 | `unlock 1h` lúc 22:30 | Xem được tới 23:30, giờ xem vẫn được tính |
| 39 | Chạy `uninstall-pc.ps1` | `hosts` và policy trình duyệt trở về như trước khi cài |
