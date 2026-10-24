# Đóng gói bản phát hành: test + build PC và TV, rồi tạo các file để tải lên GitHub Releases.
#   .\tools\release.ps1
#
# Kết quả trong out\release\<phiên bản>\:
#   LimitYT-PC.zip, LimitYT-TV.zip, SHA256SUMS.txt
# Tên file không chứa số phiên bản để link .../releases/latest/download/LimitYT-PC.zip trong CAI-DAT.md luôn trỏ tới bản mới nhất.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$version = (Get-Content (Join-Path $repo 'VERSION') -Raw).Trim()
$release = Join-Path $repo "out\release\$version"
$stage = Join-Path $release 'stage'

if (Test-Path $release) { Remove-Item -Recurse -Force $release }
& (Join-Path $repo 'pc\publish-pc.ps1') -Output (Join-Path $stage 'LimitYT-PC')
& (Join-Path $repo 'tv\build-tv.ps1') -Output (Join-Path $stage 'LimitYT-TV')
foreach ($dir in 'LimitYT-PC', 'LimitYT-TV') {
    Copy-Item (Join-Path $repo 'CAI-DAT.md') (Join-Path $stage $dir)
    Set-Content (Join-Path $stage "$dir\VERSION.txt") $version -Encoding ascii
    Compress-Archive -Path (Join-Path $stage $dir) -DestinationPath (Join-Path $release "$dir.zip") -CompressionLevel Optimal
}
Remove-Item -Recurse -Force $stage

# Mã băm để người tải kiểm tra file không bị hỏng / bị thay đổi.
Get-ChildItem $release -Filter *.zip | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
} | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii

Write-Host ''
Write-Host "Bản phát hành $version trong $release :"
Get-ChildItem $release | ForEach-Object { '  {0,-16} {1,6:N1} MB' -f $_.Name, ($_.Length / 1MB) }
Write-Host ''
Write-Host "Tải 3 file trên lên GitHub Releases với tag v$version (xem mục 'Phát hành bản mới' trong CAI-DAT.md)."
