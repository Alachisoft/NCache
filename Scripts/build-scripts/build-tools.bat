@echo off

echo BUILDING Tools.4x.sln
echo =====================
@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\tools\Tools.4x.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failTools4x
echo BUILDING Tools.2x.sln
echo =====================
@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\tools\Tools.2x.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failTools2x
exit /b 0

:failTools4x
echo Tools 4x Compilation failed
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\tools\Tools.4x.sln
)
pause
exit /b 1

:failTools2x
echo Tools 2x Compilation failed 
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\tools\Tools.2x.sln
)
pause
exit /b 1