@echo off
chcp 65001 >nul
rem Bấm đúp để gỡ LimitYT khỏi TV.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall-tv.ps1"
echo.
pause
