@ECHO off
REM BUILDING NCRegistry OPENSOURCE SOLUTION FOR .NET
	ECHO BUILDING NCRegistry
	ECHO ======================
	@"%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" "..\..\Src\NCRegistry\NCRegistry.2x.vcxproj" /t:Rebuild /p:Configuration=Release /p:platform="x64"
	SET BUILD_STATUS=%ERRORLEVEL%
	IF NOT %BUILD_STATUS%==0 GOTO failNCacheRegistry

EXIT /b 0

:failNCacheRegistry
REM SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET , LAUNCH SOLUTION
ECHO FAILED TO BUILD NCRegistry
ECHO =============================
"..\..\Src\NCRegistry\NCRegistry.2x.vcxproj"
PAUSE
EXIT /b 1
