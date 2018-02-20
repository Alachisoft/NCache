@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET CURRENTPATH=%CD%
SET INTEGRATIONPARENTFOLDER=..\..\Integration
SET DOTNETCORE=C:\Program Files\dotnet\dotnet.exe

::_________________________________________::
::_____BUILDING EF NCACHE PROVIDER CORE____::
::_________________________________________::

ECHO BUILDING INTERGRATION EF NCACHE PROVIDER CORE FOR
ECHO ================================================
	CD "%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - Core\"
	@"%DOTNETCORE%" restore
	@"%DOTNETCORE%" build -c Release
	IF NOT %ERRORLEVEL%==0 GOTO :failEFNCacheProviderCore
	CD %CURRENTPATH%
	IF %ERRORLEVEL%==0 ECHO EFNCacheProvider Core build successful

::___________________________________::
::___BUILDING LINQ TO NCACHE CORE____::
::___________________________________::

ECHO BUILDING INTERGRATION LINQ TO NCACHE CORE FOR 
ECHO ================================================
	CD "%INTEGRATIONPARENTFOLDER%\LinqToNCache.NetCore\"
	@"%DOTNETCORE%" restore
	@"%DOTNETCORE%" build -c Release
	IF NOT %ERRORLEVEL%==0 GOTO :failLinqToNCacheCore
	CD %CURRENTPATH%
	IF %ERRORLEVEL%==0 ECHO LinqToNCache Core build successful

EXIT /b 0

:failEFNCacheProviderCore
ECHO FAILED TO BUILD EF NCACHE PROVIDER CORE
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - Core\EntityFrameworkCoreCaching.sln"
PAUSE
EXIT /b 1

:failLinqToNCacheCore
ECHO FAILED TO BUILD LINQ TO NCACHE CORE
ECHO =======================================
"%INTEGRATIONPARENTFOLDER%\LinqToNCache.NetCore\LinqToNCache.sln"
PAUSE
EXIT /b 1
