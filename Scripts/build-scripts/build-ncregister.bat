@echo off
REM BUILDING NCRegistry OPENSOURCE SOLUTION FOR .NET 
	echo BUILDING NCRegistry
	echo ======================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\src\NCRegistry\NCRegistry.2x.vcxproj /t:Rebuild /p:Configuration=Release /p:platform="x64"
	set BUILD_STATUS=%ERRORLEVEL%
	if not %BUILD_STATUS%==0 goto failNCacheRegistry

exit /b 0

:failNCacheRegistry
rem SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET , LAUNCH SOLUTION
echo FAILED TO BUILD NCRegistry
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\src\NCRegistry\NCRegistry.2x.vcxproj
)
pause
exit /b 1
