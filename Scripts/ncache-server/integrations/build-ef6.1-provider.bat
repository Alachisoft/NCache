@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET INTEGRATIONPARENTFOLDER=..\..\..\Integration
SET ARGS=/t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
SET MSBUILDEXE=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

::_________________________________________::
::_____BUILDING EF NCACHE PROVIDER 6.1_____::
::_________________________________________::

ECHO BUILDING INTERGRATION EF NCACHE PROVIDER 6.1 FOR
ECHO ================================================
@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - 6.1\EFCachingProvider.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failEFNCacheProviderSixOne
	IF %ERRORLEVEL%==0 ECHO EFNCacheProvider 6.1 build successful

EXIT /b 0

:failEFNCacheProviderSixOne
ECHO FAILED TO BUILD EF NCACHE PROVIDER 6.1
ECHO ======================================
"%INTEGRATIONPARENTFOLDER%\EFNCacheProvider - 6.1\EFCachingProvider.sln" 
PAUSE
EXIT /b 1
