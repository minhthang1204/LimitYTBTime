@echo off
chcp 65001 >nul
rem Bấm đúp để cài hoặc cập nhật LimitYT trên máy tính này.
net session >nul 2>&1
if errorlevel 1 (
  echo Đang xin quyền Administrator... Chọn "Yes" ^(hoặc nhập mật khẩu Admin^) ở hộp thoại hiện ra.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-pc.ps1"
echo.
pause
