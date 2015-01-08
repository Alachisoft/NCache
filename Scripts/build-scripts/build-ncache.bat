@echo off

echo BUILDING NCACHE.4x.sln
echo ======================
@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\src\NCache.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failNCache4x


echo BUILDING NCACHE.2x.sln
echo ======================
@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\src\NCache.sln /t:Rebuild /p:Configuration=Release.2x /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failNCache2x

echo BUILDING Tools.4x.sln
echo =====================
@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\tools\Tools.4x.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
pause
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failTools4x

echo BUILDING Tools.2x.sln
echo =====================
@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\tools\Tools.2x.sln /t:Rebuild /p:Configuration=Release /p:platform="Any CPU"
set BUILD_STATUS=%ERRORLEVEL%
if not %BUILD_STATUS%==0 goto failTools2x
pause
exit /b 0

:failNCache4x
echo FAILED TO BUILD NCACHE.4x.sln
echo =============================
pause
exit /b 1

:failNCache2x
echo FAILED TO BUILD NCACHE.2x.sln
echo =============================
pause
exit /b 1

:failTools4x
echo FAILED TO BUILD TOOLS.4x
pause

:failTools2x
echo FAILED TO BUILD TOOLS.4x
pause