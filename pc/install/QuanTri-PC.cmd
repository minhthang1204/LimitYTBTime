@echo off
chcp 65001 >nul
rem Mở cửa sổ quản trị LimitYT (cần quyền Administrator).
net session >nul 2>&1
if errorlevel 1 (
  echo Đang xin quyền Administrator... Chọn "Yes" ^(hoặc nhập mật khẩu Admin^) ở hộp thoại hiện ra.
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
powershell -NoExit -NoProfile -ExecutionPolicy Bypass -File "%~dp0quan-tri.ps1"
