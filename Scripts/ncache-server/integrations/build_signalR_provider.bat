@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET INTEGRATIONPARENTFOLDER=..\..\Integration
SET ARGS=/t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
SET MSBUILDEXE=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

::_________________________________________::
::_________BUILDING SIGNALR.NCACHE_________::
::_________________________________________::

ECHO BUILDING INTERGRATION SIGNALR NCACHE FOR 
ECHO ================================================
	@"%MSBUILDEXE%" "%INTEGRATIONPARENTFOLDER%\SignalR.NCache\SIgnalR.NCache.sln" %ARGS%
	IF NOT %ERRORLEVEL%==0 GOTO :failSignalRNCache
	IF %ERRORLEVEL%==0 ECHO SignalR.NCache build successful

EXIT /b 0

:failSignalRNCache
ECHO FAILED TO BUILD SIGNALR NCACHE 
ECHO =============================
"%INTEGRATIONPARENTFOLDER%\SignalR.NCache\SIgnalR.NCache.sln"
PAUSE
EXIT /b 1