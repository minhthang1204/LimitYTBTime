# Dùng chung cho install-tv.ps1 / uninstall-tv.ps1: tìm adb.exe, hỏi và nhớ IP của TV, kết nối.

$script:adb = Join-Path $PSScriptRoot 'adb.exe'
if (-not (Test-Path $script:adb)) { $script:adb = (Get-Command adb.exe -ErrorAction SilentlyContinue).Source }
if (-not $script:adb) { throw 'Không tìm thấy adb.exe (phải nằm cùng thư mục với script này).' }
$script:ipFile = Join-Path $PSScriptRoot 'tv-ip.txt'

# IP lấy theo thứ tự: tham số → hỏi người dùng (gợi ý IP đã dùng lần trước). IP được nhớ vào tv-ip.txt.
function Resolve-TvIp([string]$TvIp) {
    if (-not $TvIp) {
        $last = if (Test-Path $script:ipFile) { (Get-Content $script:ipFile -Raw).Trim() } else { '' }
        Write-Host 'Địa chỉ IP của TV: xem trên TV ở Settings → Network & Internet → (mạng đang dùng).'
        $prompt = if ($last) { "Nhập IP của TV [Enter = $last]" } else { 'Nhập IP của TV (ví dụ 192.168.1.50)' }
        $TvIp = (Read-Host $prompt).Trim()
        if (-not $TvIp) { $TvIp = $last }
    }
    if ($TvIp -notmatch '^\d{1,3}(\.\d{1,3}){3}$') { throw "IP '$TvIp' không hợp lệ." }
    Set-Content $script:ipFile $TvIp -Encoding ascii -ErrorAction SilentlyContinue
    return $TvIp
}

# adb ghi cả ra stderr; PowerShell 5.1 coi đó là lỗi khi đang ở chế độ Stop, nên chạy ở chế độ Continue.
function Invoke-AdbRaw {
    $ErrorActionPreference = 'Continue'
    $out = & $script:adb @args 2>&1 | ForEach-Object { "$_" } | Out-String
    return [pscustomobject]@{ Output = $out.Trim(); ExitCode = $LASTEXITCODE }
}

function Invoke-Adb {
    $r = Invoke-AdbRaw -s $script:target @args
    if ($r.ExitCode) { throw "adb $($args -join ' ') thất bại:`n$($r.Output)" }
    return $r.Output
}

# Kết nối tới TV; lần đầu chờ tối đa 2 phút để người dùng bấm "Allow" trên TV.
function Connect-Tv([string]$TvIp, [int]$Port) {
    $script:target = "${TvIp}:$Port"
    Write-Host "Kết nối tới TV $script:target ..."
    for ($i = 0; $i -lt 60; $i++) {
        $connect = Invoke-AdbRaw connect $script:target
        $state = (Invoke-AdbRaw -s $script:target get-state).Output
        if ($state -eq 'device') { Write-Host 'Đã kết nối.'; return }
        if ($i -eq 0) {
            if ($connect.Output -match 'failed|cannot|refused|timed out|unable') {
                throw "Không kết nối được tới $script:target.`n$($connect.Output)`nKiểm tra: TV đang bật, cùng mạng với máy này, đúng IP, đã bật USB debugging / Network debugging."
            }
            Write-Host ''
            Write-Host '>>> Nhìn lên TV: nếu có hộp thoại "Allow USB debugging?", tick "Always allow from this computer" rồi bấm OK.' -ForegroundColor Yellow
            Write-Host '    Đang chờ (tối đa 2 phút)...'
        }
        Start-Sleep -Seconds 2
        # Đang chờ bấm Allow ('unauthorized') thì không ngắt kết nối, kẻo TV hỏi lại liên tục.
        if ($state -match 'offline' -and $i % 5 -eq 4) { Invoke-AdbRaw disconnect $script:target | Out-Null }
    }
    throw 'TV chưa cho phép kết nối. Chạy lại và bấm "Allow" trên TV. Nếu hộp thoại tự đóng ngay: trên TV mở LimitYT → Phụ huynh (PIN) → Mở Cài đặt 10 phút, rồi chạy lại.'
}
