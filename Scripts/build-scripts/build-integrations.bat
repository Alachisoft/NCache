@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET INTEGRATIONPARENTFOLDER=..\..\Integration
SET ARGS=/t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
SET MSBUILDEXE=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

::_________________________________________::
::________BUILDING ALACHISOFT.COMMON_______::
::_________________________________________::

ECHO BUILDING INTERGRATION ALACHISOFT.COMMON FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\Alachisoft.Common\Alachisoft.Common.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failAlachisoftDotCommon
	IF %ERRORLEVEL%==0 ECHO Alachisoft.Common build successful

::__________________________________________::
::_BUILDING ALACHISOFT.CONTENT OPTIMIZATION_::
::__________________________________________::

ECHO BUILDING INTERGRATION ALACHISOFT.CONTENT OPTIMIZATION FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\Alachisoft.ContentOptimization\Alachisoft.ContentOptimization.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failAlachisoftDotContentOptimization
	IF %ERRORLEVEL%==0 ECHO Alachisoft.ContentOptimization build successful

::_________________________________________::
::______BUILDING CONTENT OPTIMIZATION______::
::_________________________________________::

ECHO BUILDING INTERGRATION CONTENT OPTIMATION FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\ContentOptimization\ContentOptimization.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failContentOptimization
	IF %ERRORLEVEL%==0 ECHO ContentOptimization build successful

::_________________________________________::
::_______BUILDING EF NCACHE PROVIDER_______::
::_________________________________________::

ECHO BUILDING INTERGRATION EF NCACHE PROVIDER FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\EFNCacheProvider\EFNCacheProvider\EFNCacheProvider.4x.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failEFNCacheProvider
	IF %ERRORLEVEL%==0 ECHO EFNCacheProvider build successful
	
::___________________________________::
::______BUILDING LINQ TO NCACHE______::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ TO NCACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\LinqToNCache\LinqToNCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failLinqToNCache
	IF %ERRORLEVEL%==0 ECHO LinqToNCache build successful

::_________________________________________::
::___________BUILDING NHIBERNATE___________::
::_________________________________________::

ECHO BUILDING INTERGRATION NHIBERNATE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\NHibernate\src\NHibernateNCache.Professional.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failNHibernate
	IF %ERRORLEVEL%==0 ECHO NHibernate build successful

::_________________________________________::
::___________BUILDING OUTPUTCACHE__________::
::_________________________________________::

ECHO BUILDING INTERGRATION OUTPUT CACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\OutputCache\OutputCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failOutputCache
	IF %ERRORLEVEL%==0 ECHO OutputCache build successful

::_________________________________________::
::_________BUILDING SIGNALR.NCACHE_________::
::_________________________________________::

ECHO BUILDING INTERGRATION SIGNALR NCACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\SignalR.NCache\SIgnalR.NCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failSignalRNCache
	IF %ERRORLEVEL%==0 ECHO SignalR.NCache build successful

::_________________________________________::
::_____BUILDING EF NCACHE PROVIDER 6.1_____::
::_________________________________________::

ECHO BUILDING INTERGRATION EF NCACHE PROVIDER 6.1 FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - 6.1\EFCachingProvider.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failEFNCacheProviderSixOne
	IF %ERRORLEVEL%==0 ECHO EFNCacheProvider 6.1 build successful

EXIT /b 0

:failAlachisoftDotCommon
ECHO FAILED TO BUILD ALACHISOFT.COMMON
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\Alachisoft.Common\Alachisoft.Common.sln"
PAUSE
EXIT /b 1

:failAlachisoftDotContentOptimization
ECHO FAILED TO BUILD ALACHISOFT.CONTENT OPTIMIZATION 
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\Alachisoft.ContentOptimization\Alachisoft.ContentOptimization.sln"
PAUSE
EXIT /b 1

:failContentOptimization
ECHO FAILED TO BUILD CONTENT OPTIMIZATION 
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\ContentOptimization\ContentOptimization.sln"
PAUSE
EXIT /b 1

:failEFNCacheProvider
ECHO FAILED TO BUILD EF NCACHE PROVIDER 
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\EFNCacheProvider\EFNCacheProvider\EFNCacheProvider.4x.sln"
PAUSE
EXIT /b 1

:failLinqToNCache
ECHO FAILED TO BUILD LINQ TO NCACHE 
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\LinqToNCache\LinqToNCache.sln"
PAUSE
EXIT /b 1

:failNHibernate
ECHO FAILED TO BUILD NHIBERNATE 
ECHO =============================
"%INTEGRATIONPARENTFOLDER%\NHibernate\src\NHibernateNCache.Professional.sln"
PAUSE
EXIT /b 1

:failOutputCache
ECHO FAILED TO BUILD OUTPUTCACHE 
ECHO =============================
"%INTEGRATIONPARENTFOLDER%\OutputCache\OutputCache.sln"
PAUSE
EXIT /b 1

:failSignalRNCache
ECHO FAILED TO BUILD SIGNALR NCACHE 
ECHO =============================
"%INTEGRATIONPARENTFOLDER%\SignalR.NCache\SIgnalR.NCache.sln"
PAUSE
EXIT /b 1

:failEFNCacheProviderSixOne
ECHO FAILED TO BUILD EF NCACHE PROVIDER 6.1
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - 6.1\EFCachingProvider.sln"
PAUSE
EXIT /b 1
