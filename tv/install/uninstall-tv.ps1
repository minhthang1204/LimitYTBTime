<#
Gỡ app LimitYT khỏi TV qua ADB. Cách dễ nhất: bấm đúp GoCaiDat-TV.cmd.
Không cần PIN: chỉ máy tính đã được TV cho phép ADB ("Always allow") mới gỡ được.
  .\uninstall-tv.ps1 [-TvIp 192.168.1.50]
#>
param(
    [string]$TvIp,
    [int]$Port = 5555
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$pkg = 'vn.limityt.tv'
. (Join-Path $PSScriptRoot 'adb-common.ps1')

try {
    Connect-Tv (Resolve-TvIp $TvIp) $Port

    # Bỏ service khỏi danh sách Accessibility trước, để Android không giữ tham chiếu tới app đã gỡ.
    $enabled = (Invoke-AdbRaw -s $script:target shell settings get secure enabled_accessibility_services).Output
    $rest = ($enabled -split ':' | Where-Object { $_ -and $_ -ne 'null' -and $_ -notlike "$pkg/*" }) -join ':'
    if ($rest) { Invoke-AdbRaw -s $script:target shell settings put secure enabled_accessibility_services $rest | Out-Null }
    else { Invoke-AdbRaw -s $script:target shell settings delete secure enabled_accessibility_services | Out-Null }

    $r = Invoke-AdbRaw -s $script:target uninstall $pkg
    if ($r.Output -match 'Success') { Write-Host 'Đã gỡ LimitYT khỏi TV.' -ForegroundColor Green }
    elseif ($r.Output -match 'Unknown package|not installed') { Write-Host 'TV không có LimitYT.' }
    else { throw "Gỡ thất bại:`n$($r.Output)" }
}
catch {
    Write-Host "LỖI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
