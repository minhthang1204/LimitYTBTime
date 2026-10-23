# LimitYT — Giới hạn thời gian xem YouTube

Giới hạn thời gian xem **YouTube và TikTok** trên **máy tính Windows** (mọi trình duyệt) và **Smart TV Xiaomi** (Android TV), với các quy tắc:

- quota **tuần** + quota **ngày**, dùng chung cho cả PC và TV;
- **khung giờ cấm** (buổi tối, giờ học), có **lịch ngày nghỉ** để bỏ giờ học vào nghỉ hè, Tết, ngày nghỉ;
- **cảnh báo** trước khi hết giờ;
- reset Thứ Hai 00:00; chỉ phụ huynh được cộng giờ hoặc mở khoá.

> Trạng thái: **Giai đoạn 1 (PC)** và **Giai đoạn 2 (TV)** đã code xong, chưa cài thử trên máy thật. Đồng bộ LAN (Giai đoạn 3) chưa làm, nên hiện PC và TV **đếm giờ riêng**, mỗi bên tự áp quota.

## Cách hoạt động (tóm tắt)

- **PC**: Windows Service giữ quy tắc và thực hiện chặn bằng `hosts` + chính sách trình duyệt. Một tray Agent đọc tiêu đề cửa sổ để biết có đang xem YouTube không, và đóng tab khi bị chặn.
- **TV**: app Android riêng dùng Accessibility Service để biết YouTube có đang mở không và đẩy về Home khi không được phép. App chạy độc lập kể cả khi PC tắt. App không bao giờ can thiệp vào nguồn vào HDMI (dùng để xem camera).
- **Đồng bộ** (Giai đoạn 3): PC và TV trao đổi số giờ đã dùng qua mạng LAN mỗi khi thấy nhau. Không dùng cloud.

## Cấu trúc

```
docs/      tài liệu phân tích, kiến trúc, kế hoạch
spec/      rules-test-vectors.json — bộ ca kiểm thử chung cho C# (PC) và Kotlin (TV)
tools/     dev-env.ps1 — đặt SDK, cache, thư mục tạm lên E:\DevTools
pc/        Service, Agent, CLI (.NET 10) + install/ + publish-pc.ps1
tv/        app Android (Kotlin) + install/ + build-tv.ps1
```

## Tài liệu

1. [Phân tích yêu cầu & khả thi](docs/01-phan-tich.md)
2. [Kiến trúc & thiết kế](docs/02-kien-truc.md)
3. [Kế hoạch triển khai & kiểm thử](docs/03-ke-hoach.md)
4. [Rà soát các trường hợp biên](docs/04-ra-soat-case.md)

## Build (máy phát triển)

.NET 10 SDK, Android SDK, Gradle và mọi cache được đặt trên ổ E để không dùng ổ C (xem `tools/dev-env.ps1`). JDK 17 lấy từ `D:\sdk\java\jdk-17`.

```powershell
. .\tools\dev-env.ps1                 # đặt SDK, cache, thư mục tạm lên E:\DevTools
dotnet test pc\LimitYT.Core.Tests     # test PC, gồm 27 ca trong spec/rules-test-vectors.json
.\pc\publish-pc.ps1                   # → out\pc (tự chứa, máy đích không cần cài .NET)
.\tv\build-tv.ps1                     # test TV (cùng 27 ca) + APK → out\tv
```

Chạy thử PC mà không sửa file hosts/registry của máy:

```powershell
$env:LIMITYT_DATA_DIR = 'E:\DevTools\tmp\limityt-smoke'
pc\LimitYT.Service\bin\Debug\net10.0-windows\win-x64\LimitYT.Service.exe --dry-run
```

## Cài lên máy của con

Chép thư mục `out\pc` sang máy của con. Mở PowerShell bằng **Run as administrator** trong thư mục đó, rồi chạy:

```powershell
.\install-pc.ps1                              # mặc định cài vào Program Files
.\install-pc.ps1 -InstallDir 'E:\LimitYT'     # hoặc ổ khác nếu ổ C đầy
& 'C:\Program Files\LimitYT\limityt.exe' status
```

Gỡ cài đặt bằng `.\uninstall-pc.ps1`. Script trả file hosts và policy trình duyệt về như cũ.

## Cài lên TV

1. Trên TV: *Settings → Device Preferences → About* → bấm 7 lần vào *Build*. Vào *Developer options* → bật **USB debugging** và **Network debugging**.
2. Trên máy tính, trong thư mục `out\tv` (có sẵn `adb.exe`):

```powershell
.\install-tv.ps1 -TvIp 192.168.1.50           # hỏi PIN phụ huynh 6 số ở lần đầu
.\install-tv.ps1 -TvIp 192.168.1.50 -ChangePin
.\uninstall-tv.ps1 -TvIp 192.168.1.50
```

Lần đầu kết nối, TV hiện hộp thoại "Allow USB debugging?" → chọn **Always allow**. Muốn cài bản mới thì chạy lại `install-tv.ps1`, dữ liệu được giữ nguyên.

Trên TV, mở app **LimitYT** để xem còn bao nhiêu giờ. Nút **Phụ huynh (PIN)** để cộng giờ, mở Cài đặt 10 phút, hoặc tắt bảo vệ trước khi gỡ.

> APK được ký bằng debug key ở `E:\DevTools\android-home\debug.keystore`. Hãy giữ file này: nếu mất, bản build mới không cài đè được và phải gỡ app (mất số giờ đã đếm trên TV).

## Điều kiện để chặn có hiệu quả

- Con/em dùng **tài khoản Windows Standard** (không Admin).
- TV đã cài app LimitYT qua ADB, và phụ huynh giữ PIN.
- PC và TV cùng mạng LAN; TV có IP cố định.
