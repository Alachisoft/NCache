@echo off

echo BUILDING Tools
echo =====================
@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\tools\Tools.4x.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failTools
if %ERRORLEVEL%==0 echo tools build successfull
exit /b 0
:failTools
echo Tools Compilation failed
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\tools\Tools.4x.sln
)
pause
exit /b 1
