# Cửa sổ quản trị LimitYT cho phụ huynh: mở bằng QuanTri-PC.cmd (lối tắt "LimitYT - Quan tri" trong Start Menu).
# Hiện trạng thái, rồi để cửa sổ mở cho phụ huynh gõ lệnh: limityt add 30m, limityt set daily 60m...
$svc = Get-CimInstance Win32_Service -Filter "Name='LimitYT'"
if (-not $svc) {
    Write-Warning 'LimitYT chưa được cài trên máy này. Hãy bấm đúp CaiDat-PC.cmd trước.'
    return
}
$exe = Join-Path (Split-Path $svc.PathName.Trim('"')) 'limityt.exe'
Set-Alias -Name limityt -Value $exe -Scope Global

$Host.UI.RawUI.WindowTitle = 'LimitYT - Quản trị'
Write-Host '=== LimitYT ===' -ForegroundColor Cyan
if ($svc.State -ne 'Running') { Write-Warning "Service LimitYT đang ở trạng thái $($svc.State). Gõ: Start-Service LimitYT" }
limityt status
Write-Host ''
Write-Host 'Gõ lệnh rồi nhấn Enter. Ví dụ:' -ForegroundColor Cyan
@(
    '  limityt status                       xem đã dùng bao nhiêu, có đang chặn không'
    '  limityt add 30m                      cho xem thêm 30 phút hôm nay  (add -15m = trừ 15 phút)'
    '  limityt unlock 1h                    bỏ mọi chặn 1 giờ, kể cả giờ cấm  (unlock off = huỷ)'
    '  limityt set daily 60m                quota mỗi ngày'
    '  limityt set weekly 6h                quota mỗi tuần'
    '  limityt window list                  xem các khung giờ cấm'
    '  limityt holiday today on             hôm nay con nghỉ học (bỏ giờ học)'
    '  limityt holiday add 2027-06-01..2027-08-31 Nghỉ hè'
    '  limityt history 4                    lịch sử 4 tuần'
    '  exit                                 đóng cửa sổ'
) | ForEach-Object { Write-Host $_ }
