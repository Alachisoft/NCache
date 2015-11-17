@ECHO OFF

::_________________________________________::
::______BUILDING CONTENT OPTIMIZATION______::
::_________________________________________::

ECHO BUILDING INTERGRATION CONTENT OPTIMATION FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\ViewStateCaching\ContentOptimization.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failContentOptimization4x
	if %ERRORLEVEL%==0 echo content optimization 4.0 build successfully
ECHO BUILDING INTEGRATION CONTENT OPTIMIZATION FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\Integration\ViewStateCaching\ContentOptimization2x.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failContentOptimization2x
	if %ERRORLEVEL%==0 echo content optimization 2.0 build successfully

::___________________________________::
::______BUILDING LINQ_TO_NCACHE______::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ_TO_NCACHE FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\LinqToNCache\LinqToNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failLinqToNCache4x
	if %ERRORLEVEL%==0 echo LinqToNCache 4.0 build successfully

ECHO BUILDING INTEGRATION LINQ_TO_NCACHE FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\Integration\LinqToNCache\LinqToNCache.sln /t:Rebuild /p:Configuration=Release.2x
	if not %ERRORLEVEL%==0 goto :failLinqToNCache2x
	if %ERRORLEVEL%==0 echo LinqToNCache 2.0 build successfully

::_______________________________::
::______BUILDING NHIBERNATE______::
::_______________________________::

ECHO BUILDING INTERGRATION NHIBERNATE FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\NHibernate\src\NHibernateNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failNHibernate4x
	if %ERRORLEVEL%==0 echo NHibernate 4.0 build successfully

ECHO BUILDING INTEGRATION NHIBERNATE FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\Integration\NHibernate\src\NHibernateNCache.sln /t:Rebuild /p:Configuration=Release2x /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failNHibernate2x
	if %ERRORLEVEL%==0 echo NHibernate 2.0 build successfully

::______________________________::
::______BUILDING MEMCACHED______::
::______________________________::

ECHO BUILDING MEMCACHE FOR 4.0
ECHO ================================================
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemcached4x
	if %ERRORLEVEL%==0 echo MemCached 4.0 build successfully

ECHO BUILDING MEMCACHE FOR 2.0
ECHO =================================================
	@%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln /t:Rebuild /p:Configuration=Release20 /p:Platform="Any CPU"
	if not %ERRORLEVEL%==0 goto :failMemcached2x
	if %ERRORLEVEL%==0 echo MemCached 2.0 build successfully

ECHO BUILDING MEMCACHE CLIENTS 
ECHO =================================================
CALL build-integrations-client.bat

exit /b 0

:failContentOptimization4x
echo FAILED TO BUILD CONTENT OPTIMIZATION 4X
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\ContentOptimization\ContentOptimization.sln
)
pause
exit /b 1

:failContentOptimization2x
echo FAILED TO BUILD CONTENT OPTIMIZATION 2X
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\ContentOptimization\ContentOptimization2x.sln
)
pause
exit /b 1

:failLinqToNCache4x
echo FAILED TO BUILD CONTENT OPTIMIZATION 4X
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\LinqToNCache\LinqToNCache.sln
)
pause
exit /b 1

:failLinqToNCache2x
echo FAILED TO BUILD CONTENT OPTIMIZATION 2X
echo =======================================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\LinqToNCache\LinqToNCache2x.sln
)
pause
exit /b 1

:failNHibernate4x
ECHO FAILED TO BUILD NHIBERNATE 4X
ECHO =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\NHibernate\src\NHibernateNCache.sln
)
pause
exit /b 1

:failNHibernate2x
ECHO FAILED TO BUILD NHIBERNATE 2x
ECHO =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\NHibernate\src\NHibernateNCache2x.sln
)
pause
exit /b 1

:failMemcached4x
ECHO FAILED TO BUILD MEMCACHED 4X
ECHO ============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln
)
pause
exit /b 1

:failMemcached2x
ECHO FAILED TO BUILD MEMCACHED 2x
ECHO =============================
if exist "c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"(
	@"c:\program files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln
)
pause
exit /b 1
