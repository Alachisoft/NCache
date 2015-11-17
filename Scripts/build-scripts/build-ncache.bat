@echo off
rem BUILDING NCACHE OPENSOURCE SOLUTION FOR .NET 4X
	echo BUILDING NCACHE.4x.sln
	echo ======================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\src\NCache.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
	set BUILD_STATUS=%ERRORLEVEL%
	if not %BUILD_STATUS%==0 goto failNCache4x
rem BUILDING NCACHE OPENSOURCE SOLUTION FOR .NET 2X
	echo BUILDING NCACHE.2x.sln
	echo ======================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\src\NCache.sln /t:Rebuild /p:Configuration=Release.2x /p:platform="Any CPU"
	set BUILD_STATUS=%ERRORLEVEL%
	if not %BUILD_STATUS%==0 goto failNCache2x
exit /b 0

:failNCache4x
rem SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET 4X, LAUNCH SOLUTION
echo FAILED TO BUILD NCACHE.4x.sln
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\src\NCache.sln
)
pause
exit /b 1

:failNCache2x
rem SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET 2X, LAUNCH SOLUTION
echo FAILED TO BUILD NCACHE.2x.sln
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\src\NCache.sln
)
pause
exit /b 1