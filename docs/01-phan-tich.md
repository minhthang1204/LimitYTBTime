# 01 — Phân tích yêu cầu & khả thi

## 1. Mục tiêu

Giới hạn thời gian xem YouTube của con/em trên:

- **Máy tính Windows**: mọi trình duyệt (Chrome, Edge, Firefox, Cốc Cốc, Brave, Opera…).
- **Smart TV Xiaomi EA43 2022**: app YouTube.

Khi hết quota hoặc đang trong khung giờ cấm: không mở được YouTube trên bất kỳ trình duyệt nào của máy tính, và không dùng được app YouTube trên TV.

## 2. Các quyết định đã chốt

| # | Câu hỏi | Quyết định |
|---|---------|-----------|
| D1 | Quota chung hay riêng | **Chung** cho PC + TV |
| D2 | Người bị giới hạn | Con/em, dùng **tài khoản Windows Standard** (không Admin) |
| D3 | Quy tắc | Quota **tuần** + quota **ngày** (giống nhau mọi ngày) + **khung giờ cấm** (buổi tối + giờ học) + **cảnh báo** trước khi hết |
| D4 | Reset | **Thứ Hai 00:00** giờ Việt Nam; chỉ phụ huynh được cộng giờ / mở khoá (mật khẩu/PIN) |
| D5 | PC khi xem TV | **PC thường tắt** → TV phải tự đếm giờ và tự chặn, không phụ thuộc PC |
| D6 | Đồng bộ | **Chỉ mạng LAN**, không dùng cloud. Chấp nhận có thể vượt quota một chút khi hai thiết bị không đồng bộ được |
| D7 | App trên TV | Chấp nhận cài APK tự viết + bật Accessibility qua ADB một lần |
| D8 | Mức chống vượt mặt | Con/em **có thể sẽ thử** gỡ app, xoá dữ liệu, cài client YouTube khác → app TV phải tự bảo vệ |
| D9 | Cách dùng TV | TV **bật ~18 giờ/ngày** để xem camera qua **cổng HDMI**; con xem YouTube xen kẽ giữa các lúc xem camera |
| D10 | Người lớn xem YouTube trên TV | **Không** → không cần "chế độ phụ huynh"; PIN mở khoá tạm thời là đủ |
| D11 | Sau khi chặn trên TV | Về **màn hình chính** (Home) |
| D12 | YouTube mở cùng lúc trên PC và TV | **Cộng cả hai** |
| D13 | Ai dùng PC | **Chỉ con dùng** → chặn áp dụng cho cả máy |
| D14 | Ngày nghỉ | **Lịch ngày nghỉ + bật/tắt tay**: ngày nghỉ thì bỏ các khung "giờ học", giữ giờ cấm buổi tối |
| D15 | Phạm vi dịch vụ | **YouTube + TikTok**, tính chung một quota. Thiết kế dùng danh sách "dịch vụ bị giới hạn" nên thêm dịch vụ khác chỉ cần sửa cấu hình |
| D16 | Thực tế trên TV | **Không có app TikTok**; **1 hồ sơ người dùng**; **không có** chế độ tắt màn hình chỉ phát tiếng. Chưa biết app dọn dẹp của Xiaomi có buộc dừng được app bên thứ ba không |

> Từ đây trở đi, trong mọi tài liệu, "YouTube" được hiểu là **mọi dịch vụ bị giới hạn** (hiện là YouTube và TikTok), trừ khi nói rõ riêng một dịch vụ. Danh sách các trường hợp biên đã rà soát nằm ở [04-ra-soat-case.md](04-ra-soat-case.md).

Giá trị mặc định (chỉnh lúc cài đặt): quota tuần **7 giờ**, quota ngày **1,5 giờ**, khung giờ cấm **22:00–06:00 mỗi ngày** và **07:00–11:00, 13:00–17:00 từ Thứ Hai tới Thứ Sáu**, cảnh báo khi còn **15 phút** và **5 phút**.

## 3. Yêu cầu chức năng

| ID | Yêu cầu |
|----|---------|
| F1 | Cấu hình quota tuần, quota ngày, khung giờ cấm |
| F2 | Đo thời gian xem YouTube trên PC (theo giây) |
| F3 | Đo thời gian xem app YouTube trên TV. Chỉ tính khi màn hình bật và YouTube đang ở foreground |
| F4 | Cho phép xem khi: không ở giờ cấm **và** quota ngày còn **và** quota tuần còn |
| F5 | Chặn YouTube trên PC: mọi trình duyệt, kể cả tab ẩn danh |
| F6 | Chặn app YouTube và các client YouTube khác trên TV |
| F7 | PC và TV đồng bộ số giờ đã dùng qua LAN mỗi khi thấy nhau |
| F8 | Mỗi thiết bị **tự chạy độc lập** khi không thấy thiết bị kia |
| F9 | Cảnh báo trước khi hết quota hoặc trước khi tới giờ cấm |
| F10 | Tự reset quota ngày lúc 00:00 và quota tuần lúc Thứ Hai 00:00 |
| F11 | Phụ huynh (quyền Administrator Windows trên PC, PIN trên TV) có thể: cộng/trừ giờ, mở khoá tạm thời, đổi cấu hình |
| F12 | **Không bao giờ** chặn, đếm giờ, hay cản việc chuyển nguồn vào HDMI / app TV Input của TV. Xem camera phải luôn hoạt động, kể cả khi YouTube đang bị chặn |
| F13 | Đếm đúng khi con xem **nhiều đoạn ngắn xen kẽ** trong ngày (chuyển qua lại giữa HDMI và YouTube) |

## 4. Yêu cầu phi chức năng

- Khó vượt mặt đối với một đứa trẻ hiểu công nghệ ở mức cơ bản. Không đặt mục tiêu chống hacker chuyên nghiệp.
- Tự khởi động cùng Windows / cùng TV.
- Không làm hỏng các dịch vụ Google khác (Gmail, Drive, Google Search, Play Store).
- Không cần Internet và không cần server: dữ liệu nằm trên hai thiết bị.
- Tiêu tốn tài nguyên thấp.
- App TV phải **chạy ổn định nhiều ngày liên tục** (TV bật ~18 giờ/ngày): không rò rỉ bộ nhớ, tự phục hồi sau khi TV vào/ra chế độ chờ, không ghi xuống bộ nhớ flash quá dày.

## 5. Phân tích khả thi — Máy tính

### 5.1. Đo thời gian

| Cách | Ưu | Nhược | Kết luận |
|------|----|-------|----------|
| **A. Đọc tiêu đề cửa sổ trình duyệt** (`EnumWindows` + `GetWindowText`, dạng `... - YouTube - Google Chrome`) | Chạy với mọi trình duyệt, kể cả ẩn danh, không cần cài vào trình duyệt | Chỉ thấy tab đang active của mỗi cửa sổ | **MVP** |
| B. Browser extension | Chính xác từng tab | Phải viết cho từng trình duyệt; dễ bị gỡ | Tuỳ chọn về sau |
| C. Đo traffic mạng | Không phụ thuộc trình duyệt | Phức tạp, sai số lớn | Loại |

Bổ sung về sau: dùng **Core Audio API** để bắt trường hợp tab YouTube không active nhưng đang phát âm thanh.

### 5.2. Chặn

Kết hợp ba lớp:

1. **File `hosts`**: trỏ `youtube.com`, `www.youtube.com`, `m.youtube.com`, `music.youtube.com`, `youtu.be`, `youtube-nocookie.com`, `youtubei.googleapis.com`, `i.ytimg.com`… về `0.0.0.0`, sau đó flush DNS. `*.googlevideo.com` không chặn được bằng `hosts` vì subdomain sinh ngẫu nhiên, nhưng cũng không cần: khi trang và API đã bị chặn thì player không tải được.
2. **Chính sách trình duyệt** (registry `HKLM\SOFTWARE\Policies`, kể cả Firefox, để áp được cho cả bản portable):
   - áp **thường trực**: tắt DNS-over-HTTPS và QUIC, để trình duyệt không bỏ qua `hosts`;
   - áp **khi bị chặn**: `URLBlocklist`.
3. **Đóng tab/cửa sổ YouTube đang mở** vì kết nối cũ có thể vẫn còn sống.

Firewall chặn theo IP bị loại vì YouTube dùng chung IP với các dịch vụ khác của Google.

## 6. Phân tích khả thi — TV Xiaomi EA43 2022

Model này chạy **Android TV**. Cần xác nhận phiên bản Android ở Giai đoạn 0. App YouTube có package `com.google.android.youtube.tv`.

Vì **PC thường tắt** (D5), cách "PC điều khiển TV qua ADB" trong bản phân tích trước không còn đủ. Chọn cách: **app Android TV riêng (Kotlin)**.

| Thành phần trên TV | Vai trò |
|---------------------|---------|
| **AccessibilityService** | Nhận sự kiện đổi cửa sổ → biết app nào đang ở foreground. Nếu là YouTube mà không được phép xem → `performGlobalAction(GLOBAL_ACTION_HOME)` và hiện màn hình thông báo. Đây vừa là cơ chế đo, vừa là cơ chế chặn |
| Bộ đếm giờ | Khi YouTube ở foreground và `PowerManager.isInteractive` thì cộng giờ, ghi xuống bộ nhớ |
| Tự bảo vệ | Chặn các màn hình Settings có thể vô hiệu hoá app (xem mục 7) |
| Đồng bộ LAN | HTTP server nhỏ + tìm PC qua mDNS (NSD) |

Tại sao không dùng các cách khác:

- `pm disable-user` qua ADB: app thường không tự gọi được lệnh này. Nếu PC đã disable YouTube rồi tắt máy, thì tới Thứ Hai app không tự mở lại được.
- Device Owner (quyền quản trị thiết bị đầy đủ): chỉ đặt được khi TV **chưa có tài khoản Google nào**, nên không thực tế.
- Giới hạn có sẵn của Google TV: chỉ áp cho hồ sơ trẻ em và chỉ tính theo ngày.
- Chặn DNS ở router: không đo được thời gian.

Quyền cấp **một lần qua ADB** lúc cài đặt:

- bật Accessibility: `settings put secure enabled_accessibility_services ...`;
- cấp `WRITE_SECURE_SETTINGS`: `pm grant <app> android.permission.WRITE_SECURE_SETTINGS`. Có quyền này, app tự bật lại được Accessibility nếu bị tắt;
- cho phép vẽ đè màn hình (`SYSTEM_ALERT_WINDOW`) để hiện thông báo chặn.

## 7. Các đường vượt mặt và cách xử lý

### Máy tính

| Đường vượt mặt | Biện pháp |
|----------------|-----------|
| Tắt service / sửa `hosts` | Tài khoản Standard không làm được |
| Đổi giờ hệ thống | Tài khoản Standard không đổi được giờ. Ngoài ra Service đếm bằng đồng hồ đơn điệu và phát hiện giờ bị nhảy |
| Bật DoH trong trình duyệt | Policy khoá, không bật được |
| Trình duyệt portable / lạ | Bất kỳ tiến trình nào có cửa sổ tiêu đề chứa "YouTube" đều bị đóng |
| VPN / proxy | Việc phát hiện qua tiêu đề cửa sổ vẫn hoạt động → đóng cửa sổ |
| Trang mirror (Invidious, Piped…) | Có danh sách domain và từ khoá tiêu đề bổ sung, cấu hình được |

### TV

| Đường vượt mặt | Biện pháp |
|----------------|-----------|
| Tắt Accessibility của app | (1) App chặn màn hình Settings → Accessibility khi chưa nhập PIN. (2) Có `WRITE_SECURE_SETTINGS` thì app tự bật lại khi khởi động/định kỳ. (3) Khi PC bật: PC kiểm tra qua ADB và bật lại |
| Force stop / Clear data / Gỡ app trong Settings → Apps | App chặn trang thông tin của chính nó (và của YouTube) khi chưa nhập PIN. Dữ liệu giờ đã dùng có bản sao trên PC, nên sau khi bị xoá sẽ được khôi phục ở lần đồng bộ tiếp theo |
| Cài SmartTube / client YouTube khác | Chặn trình cài đặt APK (`packageinstaller`) khi chưa nhập PIN; danh sách package client YouTube đã biết; app mới được cài mà có tên chứa "Tube" thì tạm chặn và báo phụ huynh |
| Dùng trình duyệt trên TV vào youtube.com | Chặn trình duyệt đang mở trang có tiêu đề chứa "YouTube" (đọc qua Accessibility). Nếu firmware không đọc được tiêu đề → chặn luôn app trình duyệt |
| Đổi giờ / ngày trên TV | Chặn trang Settings → Date & time. Phát hiện giờ nhảy (so `elapsedRealtime` với `currentTimeMillis`) → khoá cho tới khi đồng bộ được với PC hoặc nhập PIN |
| Tắt Developer options / Network debugging | Không ảnh hưởng tới app đang chạy; chỉ làm PC mất khả năng sửa lại qua ADB |
| Factory reset TV | Không chống được, nhưng sẽ mất luôn mọi app và tài khoản. PC sẽ báo mất kết nối TV |
| Cast từ điện thoại lên TV | Cast mở app YouTube trên TV → bị chặn như thường |
| Xem trên điện thoại | **Ngoài phạm vi** |

## 8. Vượt quota do đồng bộ chỉ qua LAN

Mỗi thiết bị chỉ biết số giờ của thiết bị kia **tính tới lần đồng bộ gần nhất**.

Vì TV bật ~18 giờ/ngày (D9), TV gần như luôn có mặt trên mạng khi PC được bật. Rủi ro vượt quota vì vậy nhỏ hơn nhiều so với dự tính ban đầu. Thiết kế tận dụng điều này:

- **PC đồng bộ trước rồi mới cho xem**: khi Service khởi động hoặc máy thức dậy từ sleep, PC chặn YouTube tối đa **30 giây** để thử đồng bộ với TV. Đồng bộ được thì mở theo quy tắc bình thường; không đồng bộ được thì chạy offline (xem dưới).
- Khi hai bên thấy nhau, đồng bộ **mỗi 30 giây**. Tổng số giờ vì vậy luôn chính xác trong phạm vi khoảng 30 giây.
- Quota **ngày** giới hạn mức vượt khi offline: trong một ngày, phần vượt tối đa bằng quota ngày của thiết bị không đồng bộ được.
- Tuỳ chọn `offlineCapPercent` (mặc định **50% cho PC, 100% cho TV**): thiết bị nào **đang không đồng bộ được** chỉ được dùng tối đa X% phần quota còn lại tại thời điểm mất kết nối. Trường hợp này giờ chủ yếu xảy ra khi TV tắt hẳn hoặc mạng hỏng.
- Khi đồng bộ được lại, mọi số liệu được cộng chính xác. Nếu tổng đã vượt quota thì cả hai thiết bị chặn ngay.

**Xem cùng lúc trên hai thiết bị** (D12): thời gian được cộng cả hai. Để con không bị trừ oan vì quên tab YouTube trên PC, PC **không đếm giờ** khi phiên Windows bị khoá (Win+L), khi màn hình tắt, hoặc khi màn hình chờ đang chạy.

## 9. Giới hạn đã biết

1. Có thể vượt quota một chút khi hai thiết bị không đồng bộ được (mục 8).
2. Tab YouTube phát ở nền trên PC chỉ được tính giờ từ Giai đoạn 3 (Core Audio).
3. Policy trình duyệt chỉ có hiệu lực sau khi trình duyệt khởi động lại. `hosts` có hiệu lực ngay.
4. Người có quyền Admin trên PC, hoặc người biết dùng ADB và có quyền vào Developer options trên TV, luôn gỡ được chương trình.
5. Firmware Xiaomi có thể khác Android TV gốc ở tên màn hình Settings. Cần thu thập thực tế trên TV ở Giai đoạn 0.
6. Trên Xiaomi, bảng chọn nguồn vào (Inputs) có thể thuộc package Settings. Bộ lọc chặn Settings phải được kiểm thử để chắc chắn **không** chặn nhầm việc chuyển sang HDMI. Đây là kịch bản kiểm thử bắt buộc.
7. Nếu con để YouTube phát trên TV mà không xem (ví dụ bỏ đi chỗ khác), thời gian đó vẫn bị tính. Tính năng "Bạn vẫn đang xem chứ?" của YouTube phần nào giới hạn trường hợp này.
