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
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\ViewStateCaching\Alachisoft.ContentOptimization\Alachisoft.ContentOptimization.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failAlachisoftDotContentOptimization
	IF %ERRORLEVEL%==0 ECHO Alachisoft.ContentOptimization build successful

::_________________________________________::
::_______BUILDING VIEW STATE CACHING_______::
::_________________________________________::

ECHO BUILDING INTERGRATION VIEW STATE CACHING FOR
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\ViewStateCaching\ContentOptimization.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failViewStateCaching
	IF %ERRORLEVEL%==0 ECHO ViewStateCaching build successful
	
::___________________________________::
::______BUILDING LINQ TO NCACHE______::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ TO NCACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\LinqToNCache\LinqToNCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failLinqToNCache
	IF %ERRORLEVEL%==0 ECHO LinqToNCache build successful

::_________________________________________::
::_____BUILDING NHIBERNATE OPEN SOURCE_____::
::_________________________________________::

ECHO BUILDING INTERGRATION NHIBERNATE OPEN SOURCE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\NHibernate\src\NHibernateNCache.OpenSource.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failNHibernate
	IF %ERRORLEVEL%==0 ECHO NHibernateNCache build successful

::_________________________________________::
::___________BUILDING OUTPUTCACHE__________::
::_________________________________________::

ECHO BUILDING INTERGRATION OUTPUT CACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\OutputCache\OutputCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failOutputCache
	IF %ERRORLEVEL%==0 ECHO OutputCache build successful


:failAlachisoftDotCommon
ECHO FAILED TO BUILD ALACHISOFT.COMMON
ECHO =======================================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\Alachisoft.Common\Alachisoft.Common.sln" %ARGS%
PAUSE
EXIT /b 1

:failAlachisoftDotContentOptimization
ECHO FAILED TO BUILD ALACHISOFT.CONTENT OPTIMIZATION 
ECHO =======================================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\ViewStateCaching\Alachisoft.ContentOptimization\Alachisoft.ContentOptimization.sln" %ARGS%
PAUSE
EXIT /b 1

:failViewStateCaching
ECHO FAILED TO BUILD VIEW STATE CACHING 
ECHO =======================================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\ViewStateCaching\ContentOptimization.sln" %ARGS%
PAUSE
EXIT /b 1

:failLinqToNCache
ECHO FAILED TO BUILD LINQ TO NCACHE 
ECHO =======================================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\LinqToNCache\LinqToNCache.sln" %ARGS%
PAUSE
EXIT /b 1

:failNHibernate
ECHO FAILED TO BUILD NHIBERNATE OPEN SOURCE
ECHO =============================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\NHibernate\src\NHibernateNCache.OpenSource.sln" %ARGS%
PAUSE
EXIT /b 1

:failOutputCache
ECHO FAILED TO BUILD OUTPUTCACHE 
ECHO =============================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\OutputCache\OutputCache.sln" %ARGS%
PAUSE
EXIT /b 1


