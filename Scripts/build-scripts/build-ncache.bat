@echo off
REM BUILDING NCACHE OPENSOURCE SOLUTION FOR .NET 
	echo BUILDING NCACHE.sln
	echo ======================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\src\NCache.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
	set BUILD_STATUS=%ERRORLEVEL%
	if not %BUILD_STATUS%==0 goto failNCache
exit /b 0

:failNCache
REM SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET , LAUNCH SOLUTION
echo FAILED TO BUILD NCACHE
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\src\NCache.sln
)
pause
exit /b 1

pause
exit /b 1
