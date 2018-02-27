@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET TOOLSSRCPATH=..\..\Tools

ECHO BUILDING TOOLS 
ECHO ==============
dotnet	build -c Release    %TOOLSSRCPATH%\NCAutomation\NCAutomation.Client.NetCore.csproj

IF NOT %ERRORLEVEL%==0 GOTO failTools
IF %ERRORLEVEL%==0 ECHO Tools build successful
EXIT /b 0

::_________________________________________::
::_____________HANDLING FAILURE____________::
::_________________________________________::

:failTools
ECHO FAILED TO BUILD TOOLS

EXIT /b 1
