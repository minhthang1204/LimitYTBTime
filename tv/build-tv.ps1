# Build app TV: chạy unit test (gồm spec/rules-test-vectors.json) rồi đóng gói APK.
#   .\tv\build-tv.ps1            → out\tv (APK + install-tv.ps1 + adb.exe)
param([string]$Output = (Join-Path $PSScriptRoot '..\out\tv'))

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\tools\dev-env.ps1')

# Ưu tiên Gradle đã giải nén sẵn trong E:\DevTools (dev-env.ps1 thêm vào PATH); không có thì dùng wrapper (tự tải).
$gradle = (Get-Command gradle.bat -ErrorAction SilentlyContinue).Source
if (-not $gradle) { $gradle = Join-Path $PSScriptRoot 'gradlew.bat' }
& $gradle -p $PSScriptRoot --no-daemon --console=plain testReleaseUnitTest assembleRelease
if ($LASTEXITCODE) { throw 'Build hoặc test thất bại.' }

if (Test-Path $Output) { Remove-Item -Recurse -Force $Output }
New-Item -ItemType Directory -Force $Output | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'app\build\outputs\apk\release\app-release.apk') (Join-Path $Output 'limityt-tv.apk')
Copy-Item (Join-Path $PSScriptRoot 'install\*.ps1') $Output
# adb để phụ huynh cài từ máy không có Android SDK.
foreach ($f in 'adb.exe', 'AdbWinApi.dll', 'AdbWinUsbApi.dll') {
    Copy-Item (Join-Path $env:ANDROID_HOME "platform-tools\$f") $Output
}

$size = (Get-ChildItem $Output -Recurse | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Xong: {0} ({1:N1} MB)" -f (Resolve-Path $Output), $size)
