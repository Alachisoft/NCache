@ECHO off
REM BUILDING NCACHE OPENSOURCE SOLUTION FOR .NET 
	ECHO BUILDING NCACHE.sln
	ECHO ======================
	@"%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" "..\..\Src\NCache.sln" /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
	SET BUILD_STATUS=%ERRORLEVEL%
	IF NOT %BUILD_STATUS%==0 GOTO failNCache
EXIT /b 0

:failNCache
REM SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET , LAUNCH SOLUTION
ECHO FAILED TO BUILD NCACHE
ECHO =============================
"..\..\Src\NCache.sln"
PAUSE
EXIT /b 1

PAUSE
EXIT /b 1
