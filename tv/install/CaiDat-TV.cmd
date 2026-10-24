@echo off
chcp 65001 >nul
rem Bấm đúp để cài hoặc cập nhật LimitYT lên TV. Không cần quyền Administrator.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-tv.ps1"
echo.
pause
