<#
Cài hoặc cập nhật app LimitYT lên TV Xiaomi qua ADB (docs/02-kien-truc.md §7.5).

Chuẩn bị trên TV: bật Developer options → USB debugging + Network debugging (xem docs/03-ke-hoach.md, Giai đoạn 0).
Lần đầu chạy, TV hiện hộp thoại "Allow USB debugging?" → chọn "Always allow from this computer".

  .\install-tv.ps1 -TvIp 192.168.1.50                         # cài lần đầu: hỏi PIN phụ huynh, dùng cấu hình mặc định
  .\install-tv.ps1 -TvIp 192.168.1.50 -ConfigPath config.json # gửi kèm cấu hình (cùng định dạng config.json của PC)
  .\install-tv.ps1 -TvIp 192.168.1.50 -ChangePin              # đổi PIN
Chạy lại script để cài bản mới: dữ liệu (giờ đã xem, PIN, cấu hình) được giữ nguyên.
#>
param(
    [Parameter(Mandatory)] [string]$TvIp,
    [int]$Port = 5555,
    [string]$Apk = (Join-Path $PSScriptRoot 'limityt-tv.apk'),
    [string]$ConfigPath,
    [switch]$ChangePin
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8   # adb trả về UTF-8
$pkg = 'vn.limityt.tv'
$target = "${TvIp}:$Port"

$adb = Join-Path $PSScriptRoot 'adb.exe'
if (-not (Test-Path $adb)) { $adb = (Get-Command adb.exe -ErrorAction SilentlyContinue).Source }
if (-not $adb) { throw 'Không tìm thấy adb.exe (đặt cạnh script này hoặc thêm platform-tools vào PATH).' }

function Invoke-Adb {
    $out = & $adb -s $target @args 2>&1 | Out-String
    if ($LASTEXITCODE) { throw "adb $($args -join ' ') thất bại:`n$out" }
    return $out.Trim()
}

Write-Host "Kết nối $target ..."
& $adb connect $target | Out-Host
$state = & $adb -s $target get-state 2>&1
if ("$state".Trim() -ne 'device') {
    throw "TV chưa sẵn sàng ($state). Kiểm tra IP, Network debugging, và chọn 'Always allow' trên TV rồi chạy lại."
}

if (-not (Test-Path $Apk)) { throw "Không thấy file APK: $Apk" }
Write-Host 'Cài APK ...'
Invoke-Adb install -r $Apk | Out-Host

Write-Host 'Cấp quyền ...'
Invoke-Adb shell pm grant $pkg android.permission.WRITE_SECURE_SETTINGS | Out-Null
Invoke-Adb shell appops set $pkg SYSTEM_ALERT_WINDOW allow | Out-Null
# Không cho Android tự thu hồi quyền / đưa app vào chế độ ngủ đông khi lâu không mở.
& $adb -s $target shell appops set $pkg AUTO_REVOKE_PERMISSIONS_IF_UNUSED ignore 2>&1 | Out-Null
& $adb -s $target shell dumpsys deviceidle whitelist +$pkg 2>&1 | Out-Null

$status = Invoke-Adb shell am broadcast -f 0x20 -n "$pkg/.setup.SetupReceiver" -a vn.limityt.tv.STATUS
$paired = $status -match 'paired=true'

$extras = @()
if (-not $paired -or $ChangePin) {
    while ($true) {
        $p1 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR((Read-Host 'PIN phụ huynh (6 số)' -AsSecureString)))
        $p2 = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR((Read-Host 'Nhập lại PIN' -AsSecureString)))
        if ($p1 -notmatch '^\d{6}$') { Write-Warning 'PIN phải gồm đúng 6 chữ số.'; continue }
        if ($p1 -ne $p2) { Write-Warning 'Hai lần nhập không khớp.'; continue }
        break
    }
    $extras += '--es', 'pin', $p1
}
if ($ConfigPath) {
    $json = Get-Content $ConfigPath -Raw -Encoding UTF8
    $extras += '--es', 'config', [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
}

Write-Host 'Ghép cặp ...'
$result = Invoke-Adb shell am broadcast -f 0x20 -n "$pkg/.setup.SetupReceiver" -a vn.limityt.tv.SETUP @extras
if ($result -notmatch 'data="OK') { throw "Ghép cặp thất bại:`n$result" }
($result -split "`n" | Select-Object -Last 1) | Out-Host

Write-Host ''
Write-Host 'Trạng thái trên TV:'
Invoke-Adb shell dumpsys activity service "$pkg/.guard.GuardAccessibilityService" |
    Select-String -Pattern 'paired=|Hôm nay|Tuần này|còn|chặn|tới' | ForEach-Object { '  ' + $_.Line.Trim() }

Write-Host ''
Write-Host 'Xong. Giữ Network debugging BẬT để lần sau cập nhật/kiểm tra từ máy tính.'
