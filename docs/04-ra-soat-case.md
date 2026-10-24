# 04 — Rà soát các trường hợp biên

Kết quả rà soát thiết kế ở 01–03 để tìm các case còn sót. Cột **Xử lý** cho biết case đã được đưa vào thiết kế ở đâu, hoặc làm ở giai đoạn nào.

Ký hiệu: **[TK]** đã cập nhật vào tài liệu thiết kế · **[GĐx]** làm ở giai đoạn x · **[NPV]** ngoài phạm vi (đã chốt).

## A. Quy tắc & thời gian

| ID | Case | Quyết định | Xử lý |
|----|------|-----------|-------|
| A1 | Nghỉ hè, Tết, lễ, con ốm ở nhà: khung "giờ học" không còn đúng | Có **lịch ngày nghỉ** (các khoảng ngày) + lệnh bật/tắt tay "hôm nay nghỉ". Vào ngày nghỉ: bỏ các khung giờ có `schoolOnly: true`, giữ giờ cấm buổi tối; tuỳ chọn quota ngày riêng cho ngày nghỉ | [TK] 02 §4.1, §5 |
| A2 | Một lần cộng giờ vắt qua 00:00 hoặc qua Thứ Hai 00:00 | Tách `delta` tại mốc: phần trước ghi vào ngày cũ, phần sau ghi vào ngày mới | [TK] 02 §5 |
| A3 | `add` và `unlock` khác nhau thế nào | `add 30m` = cộng thêm quota cho **hôm nay** (tăng cả quota ngày và quota tuần), **không** vượt được giờ cấm. `unlock 1h` = tạm bỏ **mọi** chặn kể cả giờ cấm, **vẫn đếm giờ**; hết thời gian thì áp lại ngay | [TK] 02 §5 |
| A4 | Phụ huynh giảm quota giữa tuần, xuống dưới số giờ đã dùng | Đánh giá lại ngay và chặn ngay | [TK] 02 §5 |
| A5 | Cấu hình sai (quota ngày > quota tuần, giờ sai định dạng, khung giờ phủ cả ngày) | CLI kiểm tra trước khi lưu và từ chối nếu sai. Adjustment âm không làm số giờ đã dùng nhỏ hơn 0 | [GĐ1] |
| A6 | Quota ngày dùng không hết có được cộng dồn sang hôm sau không | **Không** cộng dồn | [TK] 02 §5 |
| A7 | Mở YouTube khi chỉ còn < 15 phút (không đi qua ngưỡng nào) | Cảnh báo ngay một lần khi bắt đầu xem | [GĐ1] |
| A8 | PC và TV lệch giờ vài chục giây quanh 00:00 | Ghi theo ngày của thiết bị ghi; chấp nhận sai số nhỏ | — |

## B. Máy tính

| ID | Case | Quyết định | Xử lý |
|----|------|-----------|-------|
| B1 | Tiêu đề chứa từ khoá nhưng không phải YouTube (tìm "youtube" trên Google → `youtube - Tìm với Google`) bị đếm nhầm | Khớp **mẫu ở cuối tiêu đề** (`... - YouTube - <Trình duyệt>`, `... \| TikTok`), không khớp chuỗi con. Giai đoạn 4: đọc URL thanh địa chỉ bằng UI Automation | [TK] 02 §6.3 |
| B2 | Con dùng Task Manager để **kill hoặc suspend Agent**. Tài khoản Standard **được phép** làm việc này với tiến trình của chính mình | **Fail-closed**: mất heartbeat 15 giây → chặn YouTube/TikTok cho tới khi Agent hoạt động lại (Service tự khởi động lại Agent) | [TK] 02 §6.3 |
| B3 | Khởi động vào **Safe Mode with Networking** thì service không chạy | Đăng ký service trong `HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Network` | [GĐ1] |
| B4 | IPv6: `hosts` chỉ có `0.0.0.0` | Thêm dòng `::` cho mỗi domain | [GĐ1] |
| B5 | Firefox portable không đọc `policies.json` trong thư mục cài đặt | Đặt policy Firefox qua registry `HKLM\SOFTWARE\Policies\Mozilla\Firefox`. Firefox đọc policy từ registry dù được cài ở đâu | [TK] 02 §6.2 |
| B6 | Xem qua app thay vì trình duyệt: YouTube/TikTok PWA, app Microsoft Store, FreeTube, yt-dlp | Đếm giờ và đóng theo mẫu tiêu đề ở **mọi tiến trình**, không chỉ trình duyệt. Danh sách exe cấm (`FreeTube.exe`, `yt-dlp.exe`…) bị Agent kill. Giai đoạn 4: Software Restriction Policies, chỉ cho chạy exe trong `Program Files` / `Windows` | [GĐ1] / [GĐ4] |
| B7 | Picture-in-Picture: video chạy trong cửa sổ nổi trong khi tab đang active là tab khác | Đếm giờ khi có cửa sổ PiP của trình duyệt **và** trình duyệt đó đang phát âm thanh (Core Audio) | [GĐ4] |
| B8 | Mất điện / tắt nóng | Ghi DB mỗi 30 giây và khi Service dừng | [GĐ1] |
| B9 | Windows Firewall chặn cổng 47600 inbound, nhất là khi mạng bị đánh dấu Public | Script cài thêm rule cho cổng 47600, chỉ cho phép từ `LocalSubnet` | [GĐ3] |
| B10 | Antivirus (Defender, Kaspersky, Bkav…) chặn sửa `hosts` | Kiểm thử với Defender. Ghi `hosts` thất bại thì ghi log, báo trên tray, và dựa vào `URLBlocklist` + đóng cửa sổ | [GĐ1] |
| B11 | Trình duyệt hiện "Do tổ chức của bạn quản lý" | Đây là hệ quả bình thường của policy; ghi chú trong hướng dẫn cho phụ huynh | [GĐ1] |
| B12 | Có nhiều tài khoản Windows | Máy chỉ con dùng → áp dụng **cho cả máy**. Agent chạy trong mọi phiên đăng nhập. Tài khoản Admin của phụ huynh **phải có mật khẩu**; tắt tài khoản Guest | [TK] 01 §2 |
| B13 | Kết nối HTTP/QUIC mở trước khi chặn vẫn tiếp tục chạy | Đã có: tắt QUIC + đóng cửa sổ | — |
| B14 | Đổi tiêu đề tab YouTube bằng DevTools (`document.title = ...`) hoặc tiện ích kiểu "Tab Renamer" → Agent không đếm giờ, không đóng tab | Policy: `DeveloperToolsAvailability=2`, `ExtensionInstallBlocklist=["*"]` (Chrome/Edge/Brave/Cốc Cốc); `DisableDeveloperTools`, `BlockAboutConfig`, `ExtensionSettings {"*": blocked}` (Firefox). Tiện ích được phép đặt trong `browserExtensionAllowlist` | [GĐ1] (rà soát 24/10) |
| B15 | Trình duyệt không nhận policy: Opera (có VPN sẵn), Tor Browser, Chromium/Vivaldi portable… → vượt `hosts` và DoH | Agent tắt ngay khi thấy, khớp `pcBlockedProcessPatterns` trên tên exe **và** ProductName/FileDescription (đổi tên exe không lọt). Kèm Psiphon, Lantern, Ultrasurf. Brave: `TorDisabled=1` | [GĐ1] (rà soát 24/10) |
| B16 | Chép trình duyệt portable thành `explorer.exe` → Agent bỏ qua cửa sổ (trước đây bỏ qua theo tên) | Bỏ qua theo đường dẫn đầy đủ `%windir%\explorer.exe` | [GĐ1] (rà soát 24/10) |
| B17 | Chương trình tự viết kết nối pipe Agent, gửi `watching = false` liên tục, rồi tắt Agent thật → giờ xem không bao giờ bị đếm | Service chỉ nhận heartbeat từ đúng `LimitYT.Agent.exe` trong thư mục cài (chỉ Admin ghi được), kiểm tra bằng `GetNamedPipeClientProcessId` + `QueryFullProcessImageName` | [GĐ1] (rà soát 24/10) |
| B18 | Giao diện YouTube thay thế (Invidious, Piped, FreeTube, Poke): tiêu đề không có chữ "YouTube" | Thêm mẫu tiêu đề cho các tên này + domain một số instance phổ biến. Instance mới vẫn lọt `hosts`, nhưng vẫn bị đếm giờ và đóng theo tiêu đề | [GĐ1] (rà soát 24/10) |
| B19 | Đổi múi giờ Windows (tài khoản Standard **được phép**) | Không ảnh hưởng: Service dùng giờ UTC + múi giờ trong cấu hình, không dùng múi giờ hệ thống | — |

## C. TV

| ID | Case | Quyết định | Xử lý |
|----|------|-----------|-------|
| C1 | App chưa ghép cặp, hoặc mất cấu hình (Clear data lọt qua lớp chặn) | **Fail-closed**: không có cấu hình → chặn YouTube/TikTok và hiện "Cần kết nối lại với máy tính". Khi PC bật, TvWatchdog tự ghép cặp lại qua ADB. Số giờ của TV được khôi phục từ bản sao trên PC | [TK] 02 §7.4 |
| C2 | Chế độ **tắt màn hình, chỉ phát tiếng** | **Không áp dụng**: TV không có chế độ này (đã xác nhận). Chỉ đếm khi `isInteractive` | — |
| C3 | Con tự bật Developer options rồi dùng ADB từ máy khác | Chặn màn hình Developer options và mục *About → Build* khi chưa nhập PIN. Không tắt Network debugging, vì TvWatchdog cần nó | [TK] 02 §7.1 |
| C4 | App **dọn dẹp / tăng tốc** của Xiaomi có thể buộc dừng app LimitYT (chưa biết) | Không cần biết trước. **Mặc định luôn chặn** trình dọn dẹp khi chưa nhập PIN, nên con không chạy được nó. Việc trình dọn dẹp có tự chạy ngầm và dừng app không thì kiểm thử ở Giai đoạn 2 (kịch bản 35). Nếu có: thêm LimitYT vào danh sách ngoại lệ (whitelist) của trình dọn dẹp, và SelfHeal + TvWatchdog bật lại | [GĐ0] lấy package · [GĐ2] kiểm thử |
| C5 | Safe mode của Android TV tắt mọi app bên thứ ba | Rất khó vào được trên TV. TvWatchdog cảnh báo nếu phát hiện. Chấp nhận rủi ro | — |
| C6 | Con dò PIN | PIN 6 số. Sai 5 lần → khoá 15 phút, mỗi lần khoá sau gấp đôi lần trước. Ghi log để phụ huynh biết | [TK] 02 §7.1 |
| C7 | Phụ huynh quên PIN TV | Đặt lại từ PC (`limityt set pin`); PIN mới truyền sang TV qua đồng bộ cấu hình | [GĐ3] |
| C8 | Mở YouTube bằng giọng nói (Google Assistant) hoặc Cast từ điện thoại | Đều mở app → bị chặn như thường | — |
| C9 | Trailer/preview tự phát trên màn hình chính | Không đếm giờ; chấp nhận | — |
| C10 | TV có nhiều hồ sơ người dùng | **Không áp dụng**: TV có 1 hồ sơ (đã xác nhận) | — |
| C14 | TikTok trên TV | TV không có app TikTok. Còn hai đường: (1) cài APK → trình cài bị chặn, package TikTok vẫn nằm trong danh sách chặn; (2) mở tiktok.com bằng trình duyệt trên TV → chặn như YouTube trong trình duyệt | [TK] 02 §4.1 |
| C11 | Cài thêm biến thể khác: YouTube Kids, TikTok TV, APK YouTube/TikTok bản cũ | Danh sách package + chặn trình cài APK. App mới cài có tên chứa "Tube"/"Tok" thì tạm chặn và báo phụ huynh | [TK] 02 §4.1 |
| C12 | Con chuyển Wi-Fi của TV sang hotspot điện thoại | TV vẫn chặn theo quy tắc cục bộ; mất đồng bộ thì chạy chế độ offline | — |
| C13 | Bảng chọn nguồn vào HDMI bị chặn nhầm (đã nêu ở 01 §9) | `neverBlockPackages` được kiểm tra trước mọi luật khác; có kịch bản kiểm thử 19 | — |
| C15 | Con cài app ADB trên điện thoại, kết nối tới TV qua mạng (Network debugging phải bật cho phụ huynh), rồi bấm **Allow** trên hộp thoại "Allow USB debugging?" bằng remote → có toàn quyền: gỡ app, tắt Accessibility | Hộp thoại của System UI có chữ "USB debugging / RSA key fingerprint / Gỡ lỗi USB" cần PIN: app bấm **Back** (= từ chối) rồi về Home. Phụ huynh muốn cho máy mới kết nối: nhập PIN → "Mở Cài đặt 10 phút". Máy tính đã chọn "Always allow" không bị hỏi lại | [GĐ2] (rà soát 24/10) |
| C16 | Hộp thoại ADB xuất hiện khi app LimitYT bị tắt hoặc chưa cài | Không bảo vệ được. Chấp nhận: app chỉ tắt được qua đường đã bị khoá PIN | — |

## D. Đồng bộ & quản trị

| ID | Case | Quyết định | Xử lý |
|----|------|-----------|-------|
| D1 | Thiết kế cũ từ chối gói tin lệch giờ quá 5 phút. Chuyện này xung đột với việc phát hiện chỉnh giờ: giờ sai sẽ làm hỏng chính việc đồng bộ dùng để kiểm tra giờ | Chống phát lại bằng **số thứ tự tăng dần theo từng peer**, không dựa vào giờ. Độ lệch giờ chỉ được ghi nhận và báo, không làm đồng bộ thất bại | [TK] 02 §8 |
| D2 | Cấu hình bị sửa đồng thời trên PC và TV (ra cùng `version`) | So sánh theo `(version, updatedAtUtc)`; nếu vẫn hoà thì **PC thắng** | [TK] 02 §4.2 |
| D3 | Phụ huynh quên mật khẩu app trên PC | **Không áp dụng**: PC không có mật khẩu riêng; quyền quản trị = quyền Administrator Windows | — |
| D4 | Gỡ cài đặt sạch | `uninstall-pc.ps1` xoá khối `hosts`, policy, service, và tuỳ chọn xoá dữ liệu. TV: nhập PIN → "Gỡ bảo vệ" (tắt Accessibility) → `adb uninstall` | [GĐ1] / [GĐ2] |
| D5 | Nâng cấp phiên bản | DB có số version schema và migration; cài đè vẫn giữ dữ liệu | [GĐ1] |
| D6 | Phụ huynh muốn xem con đã dùng bao nhiêu | `limityt history`; tổng kết tuần trên tray | [GĐ4] |
| D7 | Đổi cấu hình trên PC lúc TV đang tắt | TV dùng cấu hình cũ cho tới lần đồng bộ tiếp theo | — |

## E. Ngoài phạm vi (đã chốt)

- Điện thoại, máy tính bảng.
- Video đã tải về từ trước. Các trang "tải hộ" kiểu y2mate tải video qua server của họ, nên không bị `hosts` chặn. Có thể thêm domain các trang này vào danh sách chặn khi cần.
- Dịch vụ video khác ngoài **YouTube và TikTok** (Facebook Watch, FPT Play…). Kiến trúc dùng danh sách **dịch vụ bị giới hạn**, nên thêm dịch vụ chỉ cần sửa cấu hình.
- Video YouTube nhúng trong trang web khác khi **còn** quota: không được đếm giờ. Khi hết quota thì bị chặn theo, vì domain embed đã bị chặn.
- Máy ảo, boot từ USB, reset BIOS, factory reset TV.
