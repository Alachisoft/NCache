@ECHO OFF

::__________________________________________::
::______BUILDING .NET MEMCACHED CLIENT______::
::__________________________________________::

ECHO BUILDING INTERGRATION .NET MEMCACHED CLIENT FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemCachedClient4x
	if %ERRORLEVEL%==0 echo MemCached Client 4.0 build successfully
	
ECHO BUILDING INTERGRATION .NET MEMCACHED CLIENT FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln" /t:Rebuild /p:Configuration=Release2x /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemCachedClient2x
	if %ERRORLEVEL%==0 echo MemCached 2.0 build successfully

::_________________________________________::
::______BUILDING BeITMEMCACHED CLIENT______::
::_________________________________________::

ECHO BUILDING BeITMEMCACHED FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failBeITMemcached4x
	if %ERRORLEVEL%==0 echo BeITMemCached 4.0 build successfully

ECHO BUILDING BeITMEMCACHED FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln /t:Rebuild /p:Configuration=Release2x /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failBeITMemcached2x
	if %ERRORLEVEL%==0 echo BeITMemcached 2.0 build successfully

::_________________________________________::
::______BUILDING ENYIM CACHING CLIENT______::
::_________________________________________::

ECHO BUILDING ENYIM CACHING FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\Enyim.Caching\Src\Enyim.Caching\Enyim.Caching.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failEnyimCaching
	if %ERRORLEVEL%==0 echo Enyim.Caching 4.0 build successfully

exit /b 0

:failMemCachedClient4x
echo FAILED TO BUILD MECACHED CLIENT 4X
echo ==================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln"
)
exit /b 1

:failMemCachedClient2x
echo FAILED TO BUILD MECACHED CLIENT 2X
echo ==================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln"
)
exit /b 1

:failBeITMemcached4x
echo FAILED TO BUILD BEITMEMCACHED 4X
echo ================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln"
)
exit /b 1

:failBeITMemcached2x
echo FAILED TO BUILD BEITMEMCACHED 2X
echo ================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln"
)
pause
exit /b 1

:failEnyimCaching
echo FAILED TO BUILD ENYIM.CACHING
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\Enyim.Caching\Src\Enyim.Caching\Enyim.Caching.sln"
)
pause
exit /b 1
