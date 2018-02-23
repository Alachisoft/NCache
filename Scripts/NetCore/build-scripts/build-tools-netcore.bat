@ECHO off

ECHO BUILDING TOOLS 
ECHO =====================
dotnet	build -c Release    ..\..\..\Tools\NCAutomation\NCAutomation.Client.NetCore.csproj
SET BUILD_STATUS=%ERRORLEVEL%
IF NOT %BUILD_STATUS%==0 ECHO failTools
IF %ERRORLEVEL%==0 ECHO tools build successfull
EXIT /b 0
:failTools
ECHO FAILED TO BUILD TOOLS
PAUSE
EXIT /b 1