@echo off
chcp 65001 >nul
rem Bấm đúp để gỡ LimitYT khỏi máy tính này.
net session >nul 2>&1
if errorlevel 1 (
  echo Đang xin quyền Administrator... Chọn "Yes" ^(hoặc nhập mật khẩu Admin^) ở hộp thoại hiện ra.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
echo Gỡ LimitYT: trả file hosts và policy trình duyệt về như cũ.
choice /c YN /m "Giữ lại số giờ đã xem và cấu hình (để cài lại sau)"
if errorlevel 2 (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall-pc.ps1"
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall-pc.ps1" -KeepData
)
echo.
pause
