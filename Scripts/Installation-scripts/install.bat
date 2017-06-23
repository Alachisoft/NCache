@ECHO OFF

SET InstallDir="C:\Program Files\NCache"
reg query "HKLM\SOFTWARE\Alachisoft\NCache" /v InstallDir >nul 2>&1
echo
set query_status_nc=%ERRORLEVEL%
if %query_status_nc%==1 (
	install-scripts\main.bat %InstallDir%
)
if %query_status_nc%==0 (
echo Another Version of NCache is already installed
)
pause