@ECHO off

ECHO BUILDING Tools
ECHO =====================
@"%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" "..\..\Tools\Tools.4x.sln" /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
SET BUILD_STATUS=%ERRORLEVEL%
IF NOT %BUILD_STATUS%==0 GOTO failTools
IF %ERRORLEVEL%==0 ECHO Tools build successful
EXIT /b 0
:failTools
ECHO Tools Compilation failed
"..\..\Tools\Tools.4x.sln"
PAUSE
EXIT /b 1
