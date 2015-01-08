@ECHO OFF

::__________________________________________::
::______BUILDING .NET MEMCACHED CLIENT______::
::__________________________________________::

ECHO BUILDING INTERGRATION .NET MEMCACHED CLIENT FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING INTERGRATION .NET MEMCACHED CLIENT FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe "..\..\Integration\MemCached\Clients\.NET memcached Client Library\Src\clientlib_2.0.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

::_________________________________________::
::______BUILDING BeITMEMCACHED CLIENT______::
::_________________________________________::

ECHO BUILDING BeITMEMCACHED FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)

ECHO BUILDING BeITMEMCACHED FOR 2.0
ECHO =================================================
if exist %widnir%\Microsoft.NET\Framework\3.5\ (
	@%windir%\Microsoft.NET\Framework\3.5\MSBuild.exe ..\..\Integration\MemCached\Clients\BeITMemcached\Src\BeITMemcached.sln /t:Rebuild /p:Configuration=Release20 /p:Platform="Any CPU"
)

::_________________________________________::
::______BUILDING ENYIM CACHING CLIENT______::
::_________________________________________::

ECHO BUILDING ENYIM CACHING FOR 4.0
ECHO ================================================
if exist %windir%\Microsoft.NET\Framework\v4.0.30319\ (
	@%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe ..\..\Integration\MemCached\Clients\Enyim.Caching\Src\Enyim.Caching\Enyim.Caching.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
)
