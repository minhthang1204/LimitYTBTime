<#
Cài hoặc cập nhật app LimitYT lên TV Xiaomi qua ADB (docs/02-kien-truc.md §7.5).
Cách dễ nhất: bấm đúp CaiDat-TV.cmd — script tự hỏi IP của TV, PIN, file cấu hình.

Chuẩn bị trên TV: bật Developer options → USB debugging + Network debugging (xem CAI-DAT.md).
Lần đầu chạy, TV hiện hộp thoại "Allow USB debugging?" → chọn "Always allow from this computer".

  .\install-tv.ps1                                             # hỏi từng bước
  .\install-tv.ps1 -TvIp 192.168.1.50                          # không hỏi IP
  .\install-tv.ps1 -TvIp 192.168.1.50 -ConfigPath config.json  # gửi kèm cấu hình (config.json của PC)
  .\install-tv.ps1 -TvIp 192.168.1.50 -ChangePin               # đổi PIN
Chạy lại để cài bản mới: dữ liệu trên TV (giờ đã xem, PIN, cấu hình) được giữ nguyên.
#>
param(
    [string]$TvIp,
    [int]$Port = 5555,
    [string]$Apk = (Join-Path $PSScriptRoot 'limityt-tv.apk'),
    [string]$ConfigPath,
    [switch]$ChangePin
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8   # adb trả về UTF-8
$pkg = 'vn.limityt.tv'
. (Join-Path $PSScriptRoot 'adb-common.ps1')

try {
    if (-not (Test-Path $Apk)) { throw "Không thấy file $Apk. Hãy giải nén file zip (Extract All) trước khi cài." }
    $interactive = -not $TvIp
    $TvIp = Resolve-TvIp $TvIp
    Connect-Tv $TvIp $Port

    Write-Host '1/4 Cài app lên TV (khoảng 10–30 giây) ...'
    Invoke-Adb install -r $Apk | Out-Null

    Write-Host '2/4 Cấp quyền ...'
    Invoke-Adb shell pm grant $pkg android.permission.WRITE_SECURE_SETTINGS | Out-Null
    Invoke-Adb shell appops set $pkg SYSTEM_ALERT_WINDOW allow | Out-Null
    # Không cho Android tự thu hồi quyền / cho app ngủ khi lâu không mở. Lệnh không có trên mọi bản Android nên bỏ qua lỗi.
    Invoke-AdbRaw -s $script:target shell appops set $pkg AUTO_REVOKE_PERMISSIONS_IF_UNUSED ignore | Out-Null
    Invoke-AdbRaw -s $script:target shell dumpsys deviceidle whitelist +$pkg | Out-Null

    $status = Invoke-Adb shell am broadcast -f 0x20 -n "$pkg/.setup.SetupReceiver" -a vn.limityt.tv.STATUS
    $paired = $status -match 'paired=true'

    $extras = @()
    if (-not $paired -or $ChangePin) {
        Write-Host ''
        Write-Host '3/4 Đặt PIN phụ huynh: 6 chữ số. Cần PIN này để vào Cài đặt của TV và để cộng giờ trên TV. Khi gõ, màn hình hiện dấu *.'
        while ($true) {
            $p1 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR((Read-Host 'PIN phụ huynh (6 số)' -AsSecureString)))
            $p2 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR((Read-Host 'Nhập lại PIN' -AsSecureString)))
            if ($p1 -notmatch '^\d{6}$') { Write-Warning 'PIN phải gồm đúng 6 chữ số.'; continue }
            if ($p1 -ne $p2) { Write-Warning 'Hai lần nhập không khớp.'; continue }
            break
        }
        $extras += '--es', 'pin', $p1
    }
    else {
        Write-Host '3/4 TV đã có PIN — giữ nguyên (đổi PIN: chạy với -ChangePin).'
    }

    if (-not $ConfigPath -and $interactive) {
        Write-Host ''
        Write-Host 'Muốn TV dùng cùng giới hạn với máy tính của con? Kéo thả file limityt-config.json vào cửa sổ này (xem CAI-DAT.md),'
        $ConfigPath = (Read-Host 'hoặc nhấn Enter để giữ cấu hình hiện tại của TV (lần đầu: cấu hình mặc định)').Trim().Trim('"')
    }
    if ($ConfigPath) {
        if (-not (Test-Path $ConfigPath)) { throw "Không thấy file cấu hình $ConfigPath" }
        $json = Get-Content $ConfigPath -Raw -Encoding UTF8
        $extras += '--es', 'config', [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
    }

    Write-Host '4/4 Ghép cặp ...'
    $result = Invoke-Adb shell am broadcast -f 0x20 -n "$pkg/.setup.SetupReceiver" -a vn.limityt.tv.SETUP @extras
    if ($result -notmatch 'data="OK') { throw "Ghép cặp thất bại:`n$result" }

    Write-Host ''
    Write-Host 'Trạng thái trên TV:' -ForegroundColor Cyan
    (Invoke-Adb shell dumpsys activity service "$pkg/.guard.GuardAccessibilityService") -split "`r?`n" |
        Where-Object { $_ -match 'paired=|Hôm nay|Tuần này|còn|chặn|tới|ngày nghỉ' } | ForEach-Object { '  ' + $_.Trim() }
    if ($result -notmatch 'accessibility=true') {
        Write-Warning 'Dịch vụ Trợ năng của LimitYT chưa bật được. Chạy lại script; nếu vẫn vậy, xem mục Xử lý sự cố trong CAI-DAT.md.'
    }

    Write-Host ''
    Write-Host 'XONG. Giữ "Network debugging" BẬT trên TV để lần sau cập nhật từ máy tính này.' -ForegroundColor Green
}
catch {
    Write-Host ''
    Write-Host "LỖI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
