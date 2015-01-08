@ECHO OFF

::_________________________________________::
::______BUILDING CONTENT OPTIMIZATION______::
::_________________________________________::

ECHO BUILDING INTERGRATION CONTENT OPTIMATION FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\ContentOptimization\ContentOptimization.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING INTEGRATION CONTENT OPTIMIZATION FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe ..\..\Integration\ContentOptimization\ContentOptimization2x.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

::___________________________________::
::______BUILDING LINQ_TO_NCACHE______::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ_TO_NCACHE FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\LinqToNCache\LinqToNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING INTEGRATION LINQ_TO_NCACHE FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe ..\..\Integration\LinqToNCache\LinqToNCache2x.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

::_______________________________::
::______BUILDING NHIBERNATE______::
::_______________________________::

ECHO BUILDING INTERGRATION NHIBERNATE FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\NHibernate\src\NHibernateNCache.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING INTEGRATION NHIBERNATE FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe ..\..\Integration\NHibernate\src\NHibernateNCache2x.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

::______________________________::
::______BUILDING MEMCACHED______::
::______________________________::

ECHO BUILDING MEMCACHE FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING MEMCACHE FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe ..\..\Integration\MemCached\ProxyServer\src\MemCached.sln /t:Rebuild /p:Configuration=Release20 /p:Platform="Any CPU"
)

ECHO BUILDING MEMCACHE CLIENTS 
ECHO =================================================
CALL build-integrations-client.bat
