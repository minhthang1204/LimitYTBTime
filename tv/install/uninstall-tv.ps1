<#
Gỡ app LimitYT khỏi TV qua ADB. Không cần PIN: ai có ADB tới TV (máy tính của phụ huynh) đều gỡ được.
  .\uninstall-tv.ps1 -TvIp 192.168.1.50
#>
param(
    [Parameter(Mandatory)] [string]$TvIp,
    [int]$Port = 5555
)

$ErrorActionPreference = 'Stop'
$pkg = 'vn.limityt.tv'
$target = "${TvIp}:$Port"

$adb = Join-Path $PSScriptRoot 'adb.exe'
if (-not (Test-Path $adb)) { $adb = (Get-Command adb.exe -ErrorAction SilentlyContinue).Source }
if (-not $adb) { throw 'Không tìm thấy adb.exe.' }

& $adb connect $target | Out-Host

# Bỏ service khỏi danh sách Accessibility trước, để Android không giữ tham chiếu tới app đã gỡ.
$enabled = (& $adb -s $target shell settings get secure enabled_accessibility_services | Out-String).Trim()
$rest = ($enabled -split ':' | Where-Object { $_ -and $_ -ne 'null' -and $_ -notlike "$pkg/*" }) -join ':'
if ($rest) { & $adb -s $target shell settings put secure enabled_accessibility_services $rest }
else { & $adb -s $target shell settings delete secure enabled_accessibility_services | Out-Null }

& $adb -s $target uninstall $pkg | Out-Host
