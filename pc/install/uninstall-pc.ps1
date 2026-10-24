#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Gỡ LimitYT: trả file hosts và policy trình duyệt về như trước khi cài (case D4).
.PARAMETER KeepData
  Giữ lại cấu hình và lịch sử trong ProgramData\LimitYT.
#>
param(
    [string]$InstallDir,
    [switch]$KeepData
)

$ErrorActionPreference = 'Stop'
$serviceName = 'LimitYT'

# Không chỉ định thì lấy thư mục cài từ đường dẫn của Service (có thể đã cài sang ổ khác).
if (-not $InstallDir) {
    $svc = Get-CimInstance Win32_Service -Filter "Name='$serviceName'"
    $InstallDir = if ($svc) { Split-Path $svc.PathName.Trim('"') } else { Join-Path $env:ProgramFiles 'LimitYT' }
}
Write-Host "Gỡ LimitYT trong $InstallDir"

# Dừng service trước, nếu không nó sẽ ghi lại khối chặn ngay sau khi dọn.
if (Get-Service $serviceName -ErrorAction SilentlyContinue) { Stop-Service $serviceName -Force }
Get-Process 'LimitYT.Agent' -ErrorAction SilentlyContinue | Stop-Process -Force

$exe = Join-Path $InstallDir 'LimitYT.Service.exe'
if (Test-Path $exe) { & $exe --cleanup }

if (Get-Service $serviceName -ErrorAction SilentlyContinue) { sc.exe delete $serviceName | Out-Null }
Remove-Item "HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\$serviceName" -ErrorAction SilentlyContinue
Remove-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'LimitYT.Agent' -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\LimitYT - Quan tri.lnk') -ErrorAction SilentlyContinue
if (Test-Path $InstallDir) { Remove-Item -Recurse -Force $InstallDir }
if (-not $KeepData) { Remove-Item -Recurse -Force (Join-Path $env:ProgramData 'LimitYT') -ErrorAction SilentlyContinue }

Write-Host 'Đã gỡ LimitYT.'
