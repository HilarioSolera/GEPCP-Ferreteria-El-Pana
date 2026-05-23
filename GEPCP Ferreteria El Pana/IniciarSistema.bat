@echo off
setlocal
cd /d "%~dp0"

set "SERVER_URL=http://localhost:5002"
set "SERVER_EXE=GEPCP Ferreteria El Pana.exe"
set "TMP_STATUS=%TEMP%\gepcp_status.txt"
set "TMP_CHECK=%TEMP%\gepcp_check.txt"

if exist "%TMP_STATUS%" del "%TMP_STATUS%" >nul 2>&1
if exist "%TMP_CHECK%" del "%TMP_CHECK%" >nul 2>&1

curl -s -o nul -w "%%{http_code}" %SERVER_URL%/api/server/ping -X POST > "%TMP_STATUS%" 2>&1
set /p STATUS=<"%TMP_STATUS%"

if "%STATUS%"=="200" (
	curl -s -X POST %SERVER_URL%/api/server/shutdown > nul 2>&1
	timeout /t 2 /nobreak > nul
)

set "GEPCP_NO_AUTO_BROWSER=1"
start "" /B "%~dp0%SERVER_EXE%"

:WAIT
timeout /t 1 /nobreak > nul
curl -s -o nul -w "%%{http_code}" %SERVER_URL% > "%TMP_CHECK%" 2>&1
set /p CHECK=<"%TMP_CHECK%"
if not "%CHECK%"=="200" goto WAIT

start "" "%SERVER_URL%"

if exist "%TMP_STATUS%" del "%TMP_STATUS%" >nul 2>&1
if exist "%TMP_CHECK%" del "%TMP_CHECK%" >nul 2>&1
exit /b 0
