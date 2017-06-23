@ECHO OFF
::__________________________________________::
::______BUILDING .NET MEMCACHED CLIENT______::
::__________________________________________::

ECHO BUILDING INTERGRATION .NET MEMCACHED CLIENT 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemCachedClient
	if %ERRORLEVEL%==0 echo MemCached Client  build successfully
	
::_________________________________________::
::______BUILDING BeITMEMCACHED CLIENT______::
::_________________________________________::

ECHO BUILDING BeITMEMCACHED 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failBeITMemcached
	if %ERRORLEVEL%==0 echo BeITMemCached  build successfully

::_________________________________________::
::______BUILDING ENYIM CACHING CLIENT______::
::_________________________________________::

ECHO BUILDING ENYIM CACHING 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\Enyim.Caching\Src\Enyim.Caching.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failEnyimCaching
	if %ERRORLEVEL%==0 echo Enyim.Caching  build successfully

exit /b 0

:failMemCachedClient
echo FAILED TO BUILD MECACHED CLIENT 
echo ==================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln"
)
exit /b 1


:failBeITMemcached
echo FAILED TO BUILD BEITMEMCACHED 
echo ================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln"
)
exit /b 1


:failEnyimCaching
echo FAILED TO BUILD ENYIM.CACHING
echo =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" "..\..\Integration\MemCached\Clients\Enyim.Caching\Src\Enyim.Caching.sln"
)
pause
exit /b 1
