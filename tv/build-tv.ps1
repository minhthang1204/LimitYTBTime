# Build app TV: chạy unit test (gồm spec/rules-test-vectors.json) rồi đóng gói APK.
#   .\tv\build-tv.ps1            → out\tv (APK + install-tv.ps1 + adb.exe)
param([string]$Output = (Join-Path $PSScriptRoot '..\out\tv'))

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\tools\dev-env.ps1')

# Lần đầu: tạo keystore release (mật khẩu ngẫu nhiên) cạnh file keystore.properties. Phải sao lưu cả hai file:
# mất khoá thì bản mới không cài đè lên TV được, phải gỡ app (mất số giờ đã đếm trên TV).
if (-not (Test-Path $env:LIMITYT_SIGNING)) {
    $dir = Split-Path $env:LIMITYT_SIGNING
    New-Item -ItemType Directory -Force $dir | Out-Null
    $bytes = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $password = [Convert]::ToBase64String($bytes) -replace '[+/=]', 'x'
    $ErrorActionPreference = 'Continue'   # keytool in tiến độ ra stderr
    & (Join-Path $env:JAVA_HOME 'bin\keytool.exe') -genkeypair -keystore (Join-Path $dir 'limityt-release.jks') -storetype PKCS12 `
        -alias limityt -keyalg RSA -keysize 3072 -validity 36500 -storepass $password -keypass $password `
        -dname 'CN=LimitYT, O=LimitYT, C=VN' 2>&1 | Out-Null
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE) { throw 'Tạo keystore thất bại.' }
    "storeFile=limityt-release.jks`nstorePassword=$password`nkeyAlias=limityt`nkeyPassword=$password`n" |
        Set-Content -Path $env:LIMITYT_SIGNING -Encoding ascii -NoNewline
    Write-Warning "Đã tạo khoá ký mới trong $dir — hãy sao lưu thư mục này."
}

# Ưu tiên Gradle đã giải nén sẵn trong E:\DevTools (dev-env.ps1 thêm vào PATH); không có thì dùng wrapper (tự tải).
$gradle = (Get-Command gradle.bat -ErrorAction SilentlyContinue).Source
if (-not $gradle) { $gradle = Join-Path $PSScriptRoot 'gradlew.bat' }
& $gradle -p $PSScriptRoot --no-daemon --console=plain testReleaseUnitTest assembleRelease
if ($LASTEXITCODE) { throw 'Build hoặc test thất bại.' }

if (Test-Path $Output) { Remove-Item -Recurse -Force $Output }
New-Item -ItemType Directory -Force $Output | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'app\build\outputs\apk\release\app-release.apk') (Join-Path $Output 'limityt-tv.apk')
& (Join-Path $env:ANDROID_HOME 'build-tools\35.0.0\apksigner.bat') verify --print-certs (Join-Path $Output 'limityt-tv.apk') | Select-String 'SHA-256' | Out-Host
if ($LASTEXITCODE) { throw 'APK chưa được ký đúng.' }
Copy-Item (Join-Path $PSScriptRoot 'install\*') $Output -Include *.ps1, *.cmd
# adb để phụ huynh cài từ máy không có Android SDK.
foreach ($f in 'adb.exe', 'AdbWinApi.dll', 'AdbWinUsbApi.dll') {
    Copy-Item (Join-Path $env:ANDROID_HOME "platform-tools\$f") $Output
}

$size = (Get-ChildItem $Output -Recurse | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Xong: {0} ({1:N1} MB)" -f (Resolve-Path $Output), $size)
