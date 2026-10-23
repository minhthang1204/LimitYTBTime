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

if (-not (Test-Path (Join-Path $Source 'LimitYT.Service.exe'))) { throw "Không thấy LimitYT.Service.exe trong $Source" }

Write-Host '1/6 Dừng bản cũ (nếu có)'
if (Get-Service $serviceName -ErrorAction SilentlyContinue) { Stop-Service $serviceName -Force }
Get-Process 'LimitYT.Agent' -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "2/6 Chép file vào $InstallDir"
New-Item -ItemType Directory -Force $InstallDir | Out-Null
Get-ChildItem $Source -Exclude '*.ps1' | Copy-Item -Destination $InstallDir -Recurse -Force
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

Write-Host '5/6 Tự chạy Agent khi đăng nhập'
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'LimitYT.Agent' -Value "`"$(Join-Path $InstallDir 'LimitYT.Agent.exe')`""

Write-Host '6/6 Khởi động Service'
Start-Service $serviceName

Write-Host ''
Write-Host 'Đã cài xong. Agent sẽ tự chạy trong vài giây (biểu tượng khiên ở khay hệ thống).'
Write-Host "Quản trị:  & '$(Join-Path $InstallDir 'limityt.exe')' status"
Write-Host 'Lưu ý: tài khoản của con phải là Standard User; tài khoản Admin phải có mật khẩu; nên tắt tài khoản Guest.'
Write-Host 'Mở lại các trình duyệt đang chạy để policy (tắt DNS-over-HTTPS) có hiệu lực.'
