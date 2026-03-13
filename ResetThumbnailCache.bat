@echo off
:: FeColorizer — Reset Thumbnail Cache
:: Clears Windows thumbcache so Explorer re-generates folder thumbnails from scratch.

net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Stopping Explorer...
taskkill /f /im explorer.exe >nul 2>&1

echo Clearing thumbnail cache...
del /f /s /q "%LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1

echo Restarting Explorer...
start explorer.exe

echo Done. Thumbnail cache has been cleared.
timeout /t 3 >nul
