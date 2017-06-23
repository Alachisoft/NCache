@ECHO OFF
::_________________________________________::
::______BUILDING CONTENT OPTIMIZATION______::
::_________________________________________::

ECHO BUILDING INTERGRATION CONTENT OPTIMATION FOR
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\ViewStateCaching\ContentOptimization.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failContentOptimization
	if %ERRORLEVEL%==0 echo content optimization  build successfull
	
::___________________________________::
::______BUILDING LINQ_TO_NCACHE______::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ_TO_NCACHE FOR 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\LinqToNCache\LinqToNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failLinqToNCache
	if %ERRORLEVEL%==0 echo LinqToNCache  build successfully
	
::_______________________________::
::______BUILDING NHIBERNATE______::
::_______________________________::

ECHO BUILDING INTERGRATION NHIBERNATE FOR 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\NHibernate\src\NHibernateNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failNHibernate
	if %ERRORLEVEL%==0 echo NHibernate build successfully
	
::______________________________::
::______BUILDING MEMCACHED______::
::______________________________::

ECHO BUILDING MEMCACHE FOR 
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemcached
	if %ERRORLEVEL%==0 echo MemCached  build successfully

ECHO BUILDING MEMCACHE CLIENTS 
ECHO =================================================
CALL build-integrations-client.bat

exit /b 0

:failContentOptimization
echo FAILED TO BUILD CONTENT OPTIMIZATION 
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\ContentOptimization\ContentOptimization.sln
)
pause
exit /b 1

:failLinqToNCache
echo FAILED TO BUILD CONTENT OPTIMIZATION 
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\LinqToNCache\LinqToNCache.sln
)
pause
exit /b 1

:failNHibernate
ECHO FAILED TO BUILD NHIBERNATE 
ECHO =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\NHibernate\src\NHibernateNCache.sln
)
pause
exit /b 1

:failMemcached
ECHO FAILED TO BUILD MEMCACHED
ECHO ============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln
)
pause
exit /b 1

