# Cài đặt LimitYT

Hướng dẫn từng bước để cài LimitYT lên **máy tính Windows của con** và **TV Xiaomi (Android TV)**.

> **Máy của con không cần cài thêm gì.** File tải về đã có đủ mọi thứ chương trình cần (không cần .NET, Android SDK hay công cụ lập trình nào). Bạn chỉ cần bấm đúp các file `.cmd` có sẵn trong đó; không phải gõ lệnh, trừ vài thao tác tuỳ chọn có ghi rõ.

## Tải về

| File | Dùng cho | Tải |
|---|---|---|
| `LimitYT-PC.zip` (~50 MB) | Máy tính Windows của con | [⬇ Tải bản mới nhất](https://github.com/minhthang1204/LimitYTBTime/releases/latest/download/LimitYT-PC.zip) |
| `LimitYT-TV.zip` (~5 MB) | Cài lên TV, chạy từ một máy tính bất kỳ cùng mạng Wi-Fi với TV | [⬇ Tải bản mới nhất](https://github.com/minhthang1204/LimitYTBTime/releases/latest/download/LimitYT-TV.zip) |
| `SHA256SUMS.txt` | Kiểm tra file tải về không bị hỏng (tuỳ chọn) | [⬇ Tải](https://github.com/minhthang1204/LimitYTBTime/releases/latest/download/SHA256SUMS.txt) |

Các phiên bản cũ và ghi chú thay đổi: [trang Releases](https://github.com/minhthang1204/LimitYTBTime/releases).

## Tổng quan

| # | Việc | Làm ở đâu | Thời gian |
|---|---|---|---|
| 1 | Kiểm tra Windows và tài khoản | Máy của con | 5–10 phút |
| 2 | Cài LimitYT cho máy tính | Máy của con, đăng nhập bằng tài khoản **Admin** | 5 phút |
| 3 | Thử xem LimitYT có chặn đúng không | Máy của con | 5 phút |
| 4 | Bật chế độ nhà phát triển trên TV | TV, dùng remote | 5 phút |
| 5 | Cài LimitYT lên TV | Máy tính của phụ huynh (hoặc máy của con, đăng nhập Admin) | 5 phút |

Chuẩn bị trước:

- [ ] Mật khẩu tài khoản Administrator của máy con. Nếu chưa có, xem bước 1.2.
- [ ] Máy tính và TV dùng **cùng mạng Wi-Fi** (hoặc cùng router).
- [ ] Remote TV.
- [ ] Nghĩ trước một **PIN 6 số** cho TV và ghi lại ở chỗ con không thấy.

---

## Phần 1 — Máy tính của con

### 1.1. Kiểm tra Windows

1. Bấm **Start** → **Settings** (Cài đặt) → **System** → **About** (Giới thiệu).
2. Dòng **System type** phải là **64-bit operating system**. LimitYT không chạy được trên Windows 32-bit.
3. Ở trang **System → Activation** (Kích hoạt), nếu tên Windows có chữ **"in S mode"** thì Windows chỉ chạy được app từ Microsoft Store. Phải chuyển ra khỏi S mode (miễn phí, làm trong Microsoft Store) rồi mới cài được LimitYT.
4. Chỉ trên Windows 11: mở **Windows Security** (Bảo mật Windows) → **App & browser control** → **Smart App Control**.
   - **Off** hoặc **Evaluation**: được, chuyển sang bước tiếp theo.
   - **On**: Windows sẽ chặn LimitYT. Xem mục [Smart App Control đang bật](#smart-app-control-đang-bật) ở cuối trang.

### 1.2. Tài khoản Windows

**Bước này rất quan trọng.** Nếu con dùng tài khoản Admin, con tự gỡ được LimitYT.

Mở **Settings → Accounts** (Tài khoản):

1. **Tài khoản của phụ huynh (Administrator)** phải có **mật khẩu** mà con không biết. Vào **Sign-in options** (Tùy chọn đăng nhập) → **Password** để đặt mật khẩu. Nếu máy chưa có tài khoản Admin riêng của phụ huynh, tạo một tài khoản ở **Other users** (Người dùng khác) → **Add account**, rồi đổi loại tài khoản thành **Administrator** như ở ý 2.
2. **Tài khoản của con** phải là **Standard User**: vào **Other users** (hoặc **Family**) → bấm vào tài khoản của con → **Change account type** → chọn **Standard User** → **OK**.
3. Kiểm tra: đăng nhập vào tài khoản của con, mở **Settings → Accounts → Your info**. Dưới tên con phải là **Standard user**, không phải *Administrator*.

### 1.3. Tải về và giải nén

Từ bước này, **đăng nhập máy của con bằng tài khoản Administrator của phụ huynh**.

1. Mở trình duyệt, vào trang này rồi bấm link [⬇ LimitYT-PC.zip](https://github.com/minhthang1204/LimitYTBTime/releases/latest/download/LimitYT-PC.zip).
2. Trình duyệt có thể cảnh báo *"file này không thường được tải xuống"*. Bấm **Keep** (Giữ lại). Trên Edge: bấm **⋯ → Keep → Show more → Keep anyway**.
3. Mở thư mục **Downloads** (Tải xuống), **chuột phải** vào `LimitYT-PC.zip` → **Extract All…** (Giải nén tất cả) → **Extract**.
4. Một thư mục mới mở ra. Bên trong có thư mục `LimitYT-PC`; mở nó, bạn sẽ thấy các file `CaiDat-PC.cmd`, `GoCaiDat-PC.cmd`, `QuanTri-PC.cmd`, `CAI-DAT.md` và nhiều file `.dll`/`.exe` khác.

> **Đừng** bấm đúp các file ngay khi đang xem bên trong file zip, vì như vậy chưa giải nén. Phải làm **Extract All** trước.

### 1.4. Cài

1. **Bấm đúp** `CaiDat-PC.cmd`.
2. Nếu hiện màn hình xanh **"Windows protected your PC"**, bấm **More info** → **Run anyway**. Màn hình này hiện vì LimitYT không có chữ ký số của một công ty phần mềm.
3. Hộp thoại **"Do you want to allow this app to make changes to your device?"** hiện ra → bấm **Yes**. Nếu Windows hỏi mật khẩu, nhập mật khẩu Admin.
4. Một cửa sổ đen hiện tiến trình cài. Khi xong, cửa sổ trông giống thế này:

   ```
   1/6 Dừng bản cũ (nếu có)
   2/6 Chép file vào C:\Program Files\LimitYT
   3/6 Tạo thư mục dữ liệu (chỉ Administrators/SYSTEM truy cập được)
   4/6 Đăng ký Windows Service
   5/6 Tự chạy Agent khi đăng nhập, tạo lối tắt "LimitYT - Quan tri" trong Start Menu
   6/6 Khởi động Service

   Đã cài xong. Agent sẽ tự chạy trong vài giây (biểu tượng khiên ở khay hệ thống).
   ...
   Press any key to continue . . .
   ```

   Nhấn phím bất kỳ để đóng. Nếu thấy chữ **màu đỏ**, chụp ảnh màn hình lại và xem mục [Xử lý sự cố](#xử-lý-sự-cố).
5. **Đóng hẳn rồi mở lại tất cả trình duyệt** (Chrome, Edge…) để thiết lập mới có hiệu lực.
6. **Windows Security** có thể báo phát hiện *"SettingsModifier:Win32/HostsFileHijack"*. LimitYT chặn YouTube bằng cách sửa file `hosts`, nên Windows cảnh báo như vậy. Chọn **Allow on device** (Cho phép trên thiết bị) → **Start actions**. Nếu chọn *Remove*, việc chặn sẽ kém hiệu quả.

> **Muốn cài sang ổ khác** (khi ổ C đầy)? Bấm chuột phải nút Start → **Terminal (Admin)**, rồi gõ hai dòng sau, mỗi dòng nhấn Enter. Sửa đường dẫn ở dòng đầu cho đúng thư mục đã giải nén:
> ```powershell
> cd "$env:USERPROFILE\Downloads\LimitYT-PC\LimitYT-PC"
> powershell -ExecutionPolicy Bypass -File .\install-pc.ps1 -InstallDir 'D:\LimitYT'
> ```

### 1.5. Sau khi cài thì có gì thay đổi

- Ở **góc phải thanh taskbar** có biểu tượng **khiên**. Nếu không thấy, bấm mũi tên **^** để hiện các biểu tượng ẩn. Rê chuột lên biểu tượng để xem còn bao nhiêu phút.
- Khi còn 15 phút và 5 phút, Windows hiện thông báo nhắc con.
- Hết giờ, hoặc tới giờ cấm, tab YouTube/TikTok **tự đóng trong vòng 10 giây** và không mở lại được.
- Trình duyệt:
  - chỉ dùng được **Chrome, Edge, Firefox, Brave, Cốc Cốc**. Opera, Tor, Vivaldi, Chromium… và VPN như Psiphon sẽ **tự tắt**;
  - phím **F12** và mọi **tiện ích (extension)** bị tắt. Muốn giữ tiện ích nào, xem bước 1.8;
  - có dòng *"Do tổ chức của bạn quản lý"* (Managed by your organization). Đây là bình thường.
- Lối tắt **LimitYT - Quan tri** trong Start Menu dành cho phụ huynh. Con có thấy lối tắt này, nhưng không mở được vì cần mật khẩu Admin.

Giới hạn mặc định:

- **7 giờ/tuần** và **90 phút/ngày**, tính chung cho YouTube và TikTok, chung cho cả máy tính và TV;
- cấm xem **22:00–06:00** hằng ngày;
- cấm xem trong **giờ học 07:00–11:00 và 13:00–17:00**, từ Thứ Hai đến Thứ Sáu. Giờ học được bỏ qua vào ngày nghỉ;
- quota tuần reset lúc **Thứ Hai 00:00**.

### 1.6. Thử xem có chặn đúng không (5 phút)

1. Bấm **Start** → gõ `LimitYT` → mở **LimitYT - Quan tri** → bấm **Yes** (nhập mật khẩu Admin nếu được hỏi).
2. Một cửa sổ xanh hiện trạng thái, ví dụ:

   ```
   Hôm nay Thứ Hai 26/10 (ngày đi học):
     đã dùng 0 phút / 90 phút  (PC 0 phút, TV 0 phút)
   Tuần này (từ 26/10):
     đã dùng 0 phút / 7 giờ
   Trạng thái: được phép xem, còn 1 giờ 30 phút liên tục
   Agent: đang chạy (phiên 1)
   ```

   Dòng **Agent** phải là *đang chạy*. Nếu là *không có kết nối*, đăng xuất rồi đăng nhập lại, hoặc khởi động lại máy.
3. Mở youtube.com, xem một video khoảng **1–2 phút**. Nếu đang trong giờ cấm thì video bị chặn ngay; như vậy là chặn đúng.
4. Quay lại cửa sổ quản trị, gõ `limityt status` rồi nhấn **Enter**. Dòng *đã dùng* phải tăng khoảng 1–2 phút.
5. Thử chặn: gõ `limityt add -90m` rồi nhấn Enter. Lệnh này tạm trừ 90 phút của hôm nay, nên coi như đã hết giờ. Trong vòng 10 giây, tab YouTube phải tự đóng, và mở lại youtube.com không được.
6. Trả lại giờ: gõ `limityt add 90m` rồi nhấn Enter. Hai lệnh này bù trừ nhau, nên số giờ hôm nay trở lại như cũ.
7. Gõ `exit` để đóng cửa sổ quản trị.

### 1.7. Chỉnh giới hạn

Mở **LimitYT - Quan tri** (như ở bước 1.6), gõ lệnh rồi nhấn **Enter**:

| Việc cần làm | Gõ |
|---|---|
| Xem đã dùng bao nhiêu, có đang chặn không | `limityt status` |
| Cho xem thêm giờ hôm nay | `limityt add 30m` (thêm lý do: `limityt add 30m "làm bài tốt"`) |
| Trừ giờ (phạt) | `limityt add -15m` |
| Bỏ mọi chặn một lúc, kể cả giờ cấm (vẫn đếm giờ) | `limityt unlock 1h` · huỷ: `limityt unlock off` |
| Đổi quota mỗi ngày / mỗi tuần | `limityt set daily 60m` · `limityt set weekly 5h` |
| Quota riêng cho ngày nghỉ | `limityt set holiday-daily 2h` · bỏ: `limityt set holiday-daily off` |
| Xem các khung giờ cấm (có đánh số) | `limityt window list` |
| Thêm khung giờ cấm | `limityt window add Sat-Sun 12:00-14:00` |
| Thêm khung giờ học (tự bỏ vào ngày nghỉ) | `limityt window add Mon-Fri 07:00-11:00 --school` |
| Xoá khung giờ số 2 | `limityt window remove 2` |
| Thêm lịch nghỉ (hè, Tết…) | `limityt holiday add 2027-06-01..2027-08-31 Nghỉ hè` |
| Hôm nay con nghỉ học / đi học bù / theo lịch | `limityt holiday today on` · `off` · `auto` |
| Lịch sử theo ngày, 4 tuần gần nhất | `limityt history 4` |

Thời lượng viết dạng `30m` (phút), `2h` (giờ), `1h30m`. Tên ngày dùng `Mon Tue Wed Thu Fri Sat Sun`; nhiều ngày liền nhau viết `Mon-Fri`.

### 1.8. Cho phép một tiện ích trình duyệt (tuỳ chọn)

Ví dụ: con cần tiện ích Google Docs Offline để học.

1. Trên máy khác, mở trang của tiện ích trên Chrome Web Store. **ID** của tiện ích là đoạn 32 chữ cái ở cuối địa chỉ trang, ví dụ `ghbmnnjooekpmoecnnnilnnbdlolhkhi`.
2. Trên máy của con, mở **LimitYT - Quan tri**, gõ `notepad C:\ProgramData\LimitYT\config.json` rồi nhấn Enter.
3. Tìm dòng `"browserExtensionAllowlist": []` và đặt ID vào giữa hai dấu ngoặc vuông, trong ngoặc kép:

   ```json
   "browserExtensionAllowlist": [ "ghbmnnjooekpmoecnnnilnnbdlolhkhi" ],
   ```

   Nhiều tiện ích thì cách nhau bằng dấu phẩy: `[ "id1", "id2" ]`.
4. **Ctrl+S** để lưu, rồi đóng Notepad. Trong vòng 1 phút, LimitYT áp lại thiết lập; đóng rồi mở lại trình duyệt.

---

## Phần 2 — TV Xiaomi

### 2.1. Bật chế độ nhà phát triển trên TV (làm một lần)

Dùng remote. Tên mục có thể là tiếng Việt hoặc tiếng Anh tuỳ ngôn ngữ của TV.

1. Mở **Settings** (biểu tượng bánh răng ⚙) → **Device Preferences** (Tùy chọn thiết bị) → **About** (Giới thiệu).
   - Trên giao diện Google TV, đường dẫn là **Settings → System → About**.
2. Chọn dòng **Build** (Bản dựng; trên Google TV là *Android TV OS build*) và bấm nút **OK** của remote **7 lần liên tiếp**, tới khi hiện *"You are now a developer"*.
3. Bấm **Back**. Ở danh sách trước đó, giờ có thêm mục **Developer options** (Tùy chọn cho nhà phát triển). Mở mục này.
4. Bật **USB debugging** (Gỡ lỗi USB).
5. Nếu có mục **Network debugging** / **ADB over network** (Gỡ lỗi qua mạng), bật luôn.

### 2.2. Lấy địa chỉ IP của TV

1. **Settings → Network & Internet** (Mạng và Internet) → chọn mạng Wi-Fi đang kết nối.
2. Ghi lại dòng **IP address**, ví dụ `192.168.1.50`.
3. Nên vào trang quản trị router và đặt **IP cố định** cho TV (mục *DHCP reservation* hoặc *Gán IP tĩnh*). Nếu không, sau khi router khởi động lại, IP của TV có thể đổi; khi đó chỉ cần nhập IP mới lúc chạy lại script.

### 2.3. Cài lên TV

Làm trên **máy tính của phụ huynh**. Nếu dùng máy của con thì đăng nhập bằng tài khoản Admin. Máy tính phải cùng mạng với TV, và TV phải đang bật.

1. Tải [⬇ LimitYT-TV.zip](https://github.com/minhthang1204/LimitYTBTime/releases/latest/download/LimitYT-TV.zip), **chuột phải → Extract All… → Extract** (giống bước 1.3).
2. Mở thư mục `LimitYT-TV`, **bấm đúp** `CaiDat-TV.cmd`. Nếu hiện *"Windows protected your PC"*: **More info → Run anyway**.
3. Cửa sổ hỏi **IP của TV** → gõ IP đã ghi ở bước 2.2 → **Enter**. Lần sau, script tự gợi ý IP này, chỉ cần nhấn Enter.
4. Nếu Windows hỏi có cho **adb.exe** truy cập mạng không, chọn **Allow** (cho phép).
5. **Nhìn lên TV**: hộp thoại **"Allow USB debugging?"** hiện ra → dùng remote tick **Always allow from this computer** → chọn **OK**. Cửa sổ trên máy tính chờ tối đa 2 phút cho bước này.
6. Cửa sổ chạy lần lượt `1/4 Cài app lên TV`, `2/4 Cấp quyền`, rồi `3/4 Đặt PIN phụ huynh`. Gõ **PIN 6 số** → Enter, gõ lại lần nữa → Enter. Khi gõ, màn hình chỉ hiện dấu `*`.
7. Script hỏi file cấu hình:
   - Muốn TV dùng **cùng giới hạn đã chỉnh trên máy tính của con**: làm theo bước 2.4, rồi **kéo thả** file `limityt-config.json` vào cửa sổ → Enter.
   - Dùng giới hạn mặc định (giống bước 1.5): nhấn **Enter**.
8. Thấy dòng chữ xanh **`XONG`** là cài xong. Nhấn phím bất kỳ để đóng cửa sổ.

> Giữ **Network debugging BẬT** trên TV. Chỉ máy tính đã chọn *Always allow* mới kết nối được, và lần sau cần nó để cập nhật LimitYT. Nếu con kết nối từ máy khác, chẳng hạn từ điện thoại, LimitYT sẽ tự đóng hộp thoại cho phép.

### 2.4. Chép cấu hình từ máy tính của con sang TV (tuỳ chọn)

Bản này **máy tính và TV chưa tự đồng bộ** với nhau. Mỗi thiết bị tự đếm giờ và tự chặn theo cấu hình của nó. Nếu đã đổi giới hạn trên máy tính (bước 1.7) và muốn TV dùng giống vậy:

1. Trên máy tính của con: mở **LimitYT - Quan tri**, gõ lệnh sau rồi nhấn Enter:

   ```powershell
   Copy-Item C:\ProgramData\LimitYT\config.json "$env:PUBLIC\Desktop\limityt-config.json"
   ```

2. File `limityt-config.json` xuất hiện trên màn hình Desktop. Chép file này sang máy dùng để cài TV, ví dụ bằng USB.
3. Trên máy cài TV: bấm đúp `CaiDat-TV.cmd` lần nữa → Enter (giữ IP cũ) → khi được hỏi file cấu hình, **kéo thả** `limityt-config.json` vào cửa sổ → Enter. PIN và số giờ đã xem trên TV vẫn được giữ.
4. Xoá file `limityt-config.json` trên Desktop của máy con sau khi dùng xong.

Mỗi lần đổi giới hạn trên máy tính, làm lại bước 2.4.

### 2.5. Kiểm tra trên TV

1. Trên TV, mở danh sách ứng dụng → mở **LimitYT**. Màn hình hiện đã xem bao nhiêu và còn bao nhiêu. Không được có dòng chữ đỏ *"Dịch vụ Trợ năng chưa bật"*.
2. Thử vào **Settings → Apps → LimitYT** (hoặc **Settings → Device Preferences → Accessibility**). TV phải tự thoát ra và hiện màn hình *"Tạm dừng — Cần PIN của phụ huynh…"*.
3. Chuyển sang **HDMI (camera)** bằng nút nguồn vào trên remote. Việc này luôn làm được, kể cả khi đang bị chặn.
4. Mở YouTube. Nếu đang trong giờ cấm, TV phải về màn hình chính và hiện *"Tạm dừng — Đang trong giờ cấm"*.

### 2.6. Dùng hằng ngày trên TV

Mở app **LimitYT** → **Phụ huynh (PIN)**. Màn hình chặn cũng có nút này. Sau khi nhập PIN có các lựa chọn:

| Lựa chọn | Tác dụng |
|---|---|
| Mở Cài đặt / cài ứng dụng trong 10 phút | Vào được Settings, cài app, cho phép máy tính mới kết nối. **Không** bỏ chặn YouTube |
| Cho xem thêm 15 / 30 phút hôm nay | Cộng giờ, nhưng vẫn không xem được trong giờ cấm |
| Bỏ mọi chặn trong 1 giờ (vẫn đếm giờ) | Xem được kể cả trong giờ cấm |
| Khoá lại ngay | Huỷ các lần mở khoá ở trên |
| Tắt bảo vệ (để gỡ cài đặt) | Dùng trước khi gỡ app |

Nhập sai PIN 5 lần thì TV khoá nhập PIN 15 phút. Mỗi lần khoá sau dài gấp đôi lần trước.

---

## Cập nhật lên bản mới

1. Tải file zip mới (link ở mục [Tải về](#tải-về)) và giải nén ra một thư mục mới.
2. **Máy tính**: bấm đúp `CaiDat-PC.cmd` trong thư mục mới.
3. **TV**: bấm đúp `CaiDat-TV.cmd` trong thư mục mới → Enter để giữ IP → Enter để giữ cấu hình.

Số giờ đã xem, cấu hình và PIN đều được giữ nguyên.

## Gỡ cài đặt

- **Máy tính**: bấm đúp `GoCaiDat-PC.cmd` (trong thư mục đã giải nén) → **Yes** → trả lời câu hỏi giữ dữ liệu:
  - **Y**: giữ số giờ và cấu hình để cài lại sau;
  - **N**: xoá hết.

  File `hosts` và thiết lập trình duyệt được trả về như trước khi cài.
- **TV**: trên TV mở **LimitYT → Phụ huynh (PIN) → Tắt bảo vệ**. Sau đó, trên máy đã dùng để cài TV, bấm đúp `GoCaiDat-TV.cmd` → Enter.

---

## Xử lý sự cố

### Máy tính

| Hiện tượng | Cách xử lý |
|---|---|
| Bấm đúp `.cmd` thì cửa sổ hiện rồi tắt ngay | Chưa giải nén, hoặc đã bấm *No* ở hộp thoại cho phép. Làm lại bước 1.3 → 1.4 |
| Chữ đỏ: *Không thấy LimitYT.Service.exe…* | Đang chạy bên trong file zip. Phải **Extract All** trước |
| *"Windows protected your PC"* | Bấm **More info → Run anyway** |
| Chữ vàng: *Smart App Control đang BẬT* | Xem mục [Smart App Control đang bật](#smart-app-control-đang-bật) |
| Phần mềm diệt virus (Kaspersky, Bkav, Avast…) xoá file hoặc chặn LimitYT | Thêm thư mục `C:\Program Files\LimitYT` vào **danh sách loại trừ (Exclusions)** của phần mềm đó, rồi cài lại |
| Windows Security báo *HostsFileHijack* | Chọn **Allow on device** (xem bước 1.4, ý 6) |
| Không thấy biểu tượng khiên; `status` báo *Agent: không có kết nối* | Đăng xuất rồi đăng nhập lại. Vẫn không được: khởi động lại máy |
| Biểu tượng khiên báo *không kết nối được dịch vụ* | Mở **LimitYT - Quan tri**, gõ `Start-Service LimitYT` |
| YouTube vẫn mở được dù `status` báo *ĐANG CHẶN* | Đóng **hẳn** mọi cửa sổ trình duyệt rồi mở lại. Nếu `status` có dòng `LỖI:`, phần mềm diệt virus đang chặn LimitYT sửa file `hosts`: thêm vào Exclusions như ở trên |
| Muốn biết con có dùng trình duyệt lạ không | Mỗi khi trình duyệt bị chặn bị tắt, biểu tượng khiên hiện thông báo *"… không được phép dùng trên máy này"* |

### TV

| Hiện tượng | Cách xử lý |
|---|---|
| *Không kết nối được tới …* | Kiểm tra: TV đang bật, máy tính và TV cùng mạng Wi-Fi, IP đúng (bước 2.2), đã bật **USB debugging** (bước 2.1) |
| Chờ hết 2 phút mà TV không hiện hộp thoại *Allow USB debugging?* | Trên TV: tắt rồi bật lại **USB debugging** trong Developer options, rồi chạy lại `CaiDat-TV.cmd` |
| Hộp thoại *Allow USB debugging?* tự đóng ngay | LimitYT đang chặn máy lạ kết nối vào TV. Trên TV: **LimitYT → Phụ huynh (PIN) → Mở Cài đặt / cài ứng dụng trong 10 phút**, rồi chạy lại `CaiDat-TV.cmd` trong 10 phút đó |
| App LimitYT trên TV báo *Dịch vụ Trợ năng chưa bật* | Chạy lại `CaiDat-TV.cmd` |
| Quên PIN TV | Trên máy đã dùng để cài TV: bấm chuột phải nút Start → **Terminal**, gõ `cd` tới thư mục `LimitYT-TV`, rồi gõ `powershell -ExecutionPolicy Bypass -File .\install-tv.ps1 -ChangePin` |
| Không chuyển được sang HDMI/camera | Không được xảy ra. Hãy báo lỗi, kèm tên hãng/model TV |

### Smart App Control đang bật

Smart App Control (chỉ có trên Windows 11) chặn mọi chương trình không có chữ ký số của công ty phần mềm, trong đó có LimitYT.

Muốn dùng LimitYT thì phải tắt: **Windows Security → App & browser control → Smart App Control settings → Off**.

> Lưu ý: sau khi tắt Smart App Control, Windows **không cho bật lại** nếu không cài lại Windows. Windows Defender (diệt virus) vẫn hoạt động bình thường.

---

## Phát hành bản mới (dành cho người phát triển)

Phần này chỉ cần trên máy phát triển. Cách build xem trong [README.md](README.md).

1. Sửa số phiên bản trong file `VERSION` (ví dụ `0.9.1`), rồi commit.
2. Chạy `.\tools\release.ps1`. Kết quả nằm trong `out\release\<phiên bản>\`: `LimitYT-PC.zip`, `LimitYT-TV.zip`, `SHA256SUMS.txt`.
3. Gắn tag và đẩy lên GitHub:

   ```powershell
   git tag v0.9.1
   git push origin main v0.9.1
   ```

4. Trên GitHub: **Releases → Draft a new release** → chọn tag `v0.9.1` → kéo thả **đủ 3 file** ở bước 2 → **Publish release**.
   - **Không** tick *Set as a pre-release*. Link "bản mới nhất" ở đầu trang chỉ trỏ tới bản phát hành chính thức, không trỏ tới pre-release.
   - Giữ nguyên tên file. Link tải phụ thuộc vào đúng tên `LimitYT-PC.zip` và `LimitYT-TV.zip`.
5. APK phải được ký bằng **cùng một khoá** (`E:\DevTools\signing\`) thì mới cài đè được lên TV mà không mất dữ liệu.

### Kiểm tra file tải về bằng mã băm (tuỳ chọn)

Chạy trong PowerShell, ở thư mục chứa các file vừa tải. Mỗi dòng phải hiện `OK`:

```powershell
Get-Content SHA256SUMS.txt | ForEach-Object {
    $hash, $name = $_ -split '\s+', 2
    if (Test-Path $name) { '{0}: {1}' -f $name, $(if ((Get-FileHash $name).Hash -eq $hash) { 'OK' } else { 'SAI — tải lại' }) }
}
```
