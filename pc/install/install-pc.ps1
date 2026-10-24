#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Cài LimitYT lên máy của con. Chạy bằng PowerShell "Run as administrator" trong thư mục đã đóng gói (out\pc).

.PARAMETER InstallDir
  Thư mục cài đặt. Mặc định Program Files; có thể đặt ổ khác (ví dụ E:\LimitYT) nếu ổ C đầy.
  Thư mục được khoá quyền: chỉ Administrators/SYSTEM được sửa, người dùng thường chỉ đọc/chạy.
#>
param(
    [string]$Source = $PSScriptRoot,
    [string]$InstallDir = (Join-Path $env:ProgramFiles 'LimitYT')
)

$ErrorActionPreference = 'Stop'
$serviceName = 'LimitYT'
$admins = '*S-1-5-32-544'
$system = '*S-1-5-18'
$users = '*S-1-5-32-545'

if (-not (Test-Path (Join-Path $Source 'LimitYT.Service.exe'))) {
    throw "Không thấy LimitYT.Service.exe trong $Source. Hãy giải nén file zip (Extract All) trước khi cài, không chạy trực tiếp trong file zip."
}
if (-not [Environment]::Is64BitOperatingSystem) { throw 'LimitYT cần Windows 64-bit.' }

# File tải từ mạng bị Windows đánh dấu; bỏ đánh dấu để Agent không hiện cảnh báo bảo mật mỗi lần đăng nhập.
Get-ChildItem $Source -Recurse -File | Unblock-File

# Smart App Control (Windows 11) chặn chương trình chưa ký số như LimitYT.
$sac = Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name VerifiedAndReputablePolicyState -ErrorAction SilentlyContinue
if ($sac.VerifiedAndReputablePolicyState -eq 1) {
    Write-Warning 'Smart App Control đang BẬT nên có thể chặn LimitYT. Xem mục "Xử lý sự cố" trong CAI-DAT.md.'
}

Write-Host '1/6 Dừng bản cũ (nếu có)'
if (Get-Service $serviceName -ErrorAction SilentlyContinue) { Stop-Service $serviceName -Force }
Get-Process 'LimitYT.Agent' -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "2/6 Chép file vào $InstallDir"
New-Item -ItemType Directory -Force $InstallDir | Out-Null
Get-ChildItem $Source -Exclude '*.ps1', '*.cmd' | Copy-Item -Destination $InstallDir -Recurse -Force
# Công cụ quản trị cho phụ huynh (mở bằng lối tắt trong Start Menu, cần mật khẩu Admin).
foreach ($f in 'quan-tri.ps1', 'QuanTri-PC.cmd') {
    if (Test-Path (Join-Path $Source $f)) { Copy-Item (Join-Path $Source $f) $InstallDir -Force }
}
icacls $InstallDir /inheritance:r /grant:r "${admins}:(OI)(CI)F" "${system}:(OI)(CI)F" "${users}:(OI)(CI)RX" | Out-Null

Write-Host '3/6 Tạo thư mục dữ liệu (chỉ Administrators/SYSTEM truy cập được)'
$data = Join-Path $env:ProgramData 'LimitYT'
New-Item -ItemType Directory -Force $data | Out-Null
icacls $data /inheritance:r /grant:r "${admins}:(OI)(CI)F" "${system}:(OI)(CI)F" | Out-Null

Write-Host '4/6 Đăng ký Windows Service'
$exe = Join-Path $InstallDir 'LimitYT.Service.exe'
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    sc.exe config $serviceName binPath= "`"$exe`"" start= auto | Out-Null
} else {
    New-Service -Name $serviceName -BinaryPathName "`"$exe`"" -DisplayName 'LimitYT' `
        -Description 'Giới hạn thời gian xem YouTube/TikTok' -StartupType Automatic | Out-Null
}
sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
# Vẫn chạy khi khởi động vào Safe Mode with Networking (case B3).
New-Item -Force "HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\$serviceName" -Value 'Service' | Out-Null

Write-Host '5/6 Tự chạy Agent khi đăng nhập, tạo lối tắt "LimitYT - Quan tri" trong Start Menu'
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'LimitYT.Agent' -Value "`"$(Join-Path $InstallDir 'LimitYT.Agent.exe')`""
$admin = Join-Path $InstallDir 'QuanTri-PC.cmd'
if (Test-Path $admin) {
    $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut(
        (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\LimitYT - Quan tri.lnk'))
    $shortcut.TargetPath = $admin
    $shortcut.WorkingDirectory = $InstallDir
    $shortcut.Description = 'Quản trị LimitYT (cần mật khẩu Administrator)'
    $shortcut.Save()
}

Write-Host '6/6 Khởi động Service'
Start-Service $serviceName

Write-Host ''
Write-Host 'Đã cài xong. Agent sẽ tự chạy trong vài giây (biểu tượng khiên ở khay hệ thống).'
Write-Host 'Quản trị: Start Menu → "LimitYT - Quan tri"'
Write-Host 'Lưu ý: tài khoản của con phải là Standard User; tài khoản Admin phải có mật khẩu; nên tắt tài khoản Guest.'
Write-Host 'Mở lại các trình duyệt đang chạy để policy (tắt DNS-over-HTTPS) có hiệu lực.'
