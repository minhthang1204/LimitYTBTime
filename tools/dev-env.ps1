# Dot-source trước khi build: . .\tools\dev-env.ps1
# Đặt .NET SDK, Android SDK, Gradle, NuGet cache và thư mục tạm lên ổ E để không dùng dung lượng ổ C.
# Chỉ có tác dụng trong phiên PowerShell hiện tại, không sửa PATH hệ thống.

$root = if ($env:LIMITYT_DEVTOOLS) { $env:LIMITYT_DEVTOOLS } else { 'E:\DevTools' }

# --- .NET (PC) ---
$env:DOTNET_ROOT                    = "$root\dotnet"
$env:DOTNET_CLI_HOME                = "$root\dotnet-home"
$env:NUGET_PACKAGES                 = "$root\nuget\packages"
$env:NUGET_HTTP_CACHE_PATH          = "$root\nuget\http-cache"
$env:NUGET_PLUGINS_CACHE_PATH       = "$root\nuget\plugins-cache"
$env:DOTNET_CLI_TELEMETRY_OPTOUT    = '1'
$env:DOTNET_NOLOGO                  = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_MULTILEVEL_LOOKUP       = '0'

# --- Android (TV) ---
# JDK 17 có sẵn trên máy; đặt LIMITYT_JAVA_HOME nếu muốn dùng JDK khác.
if ($env:LIMITYT_JAVA_HOME) { $env:JAVA_HOME = $env:LIMITYT_JAVA_HOME }
elseif (-not $env:JAVA_HOME -and (Test-Path 'D:\sdk\java\jdk-17')) { $env:JAVA_HOME = 'D:\sdk\java\jdk-17' }
$env:ANDROID_HOME                   = "$root\android"
$env:ANDROID_SDK_ROOT               = $env:ANDROID_HOME
$env:ANDROID_USER_HOME              = "$root\android-home"   # debug keystore, adb key
$env:GRADLE_USER_HOME               = "$root\gradle-home"
$env:LIMITYT_SIGNING                = "$root\signing\keystore.properties"   # khoá ký APK release (ngoài repo)
$env:GRADLE_OPTS                    = "-Djava.io.tmpdir=$root\tmp"
# Không dùng JAVA_TOOL_OPTIONS: Java in dòng "Picked up..." ra stderr làm script PowerShell báo lỗi.
# java.io.tmpdir trên Windows lấy từ biến TMP (đặt ở dưới); sdkmanager ghi ~/.android theo user.home.
$env:SDKMANAGER_OPTS                = "-Duser.home=$root\java-home"
$env:JAVA_TOOL_OPTIONS              = $null

$env:TEMP                           = "$root\tmp"
$env:TMP                            = "$root\tmp"

$paths = @($env:DOTNET_ROOT, "$env:ANDROID_HOME\platform-tools", "$env:ANDROID_HOME\cmdline-tools\latest\bin", "$root\gradle\gradle-8.11.1\bin")
if ($env:JAVA_HOME) { $paths = @("$env:JAVA_HOME\bin") + $paths }
$env:PATH = (($paths | Where-Object { $env:PATH -notlike "*$_;*" }) + $env:PATH) -join ';'

foreach ($d in $env:DOTNET_ROOT, $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:TEMP, $env:ANDROID_USER_HOME, $env:GRADLE_USER_HOME, "$root\java-home") {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Force $d | Out-Null }
}
