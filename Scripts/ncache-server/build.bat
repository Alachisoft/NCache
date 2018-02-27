@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET BUILDSCRIPTSPATH="build-scripts"
SET INTEGRATIONSPATH="integrations"

::_________________________________________::
::__________BUILDING NCACHE SERVER_________::
::_________________________________________::

ECHO BUILDING NCACHE SOURCE
ECHO ======================
CALL "%BUILDSCRIPTSPATH%\build-ncache.bat"

::_________________________________________::
::__________BUILDING NCACHE TOOLS__________::
::_________________________________________::

ECHO BUILDING COMMAND LINE TOOLS
ECHO ===========================
CALL "%BUILDSCRIPTSPATH%\build-tools.bat"

::_________________________________________::
::__________BUILDING INTEGRATIONS__________::
::_________________________________________::

ECHO BUILDING INTEGRATIONS
ECHO =====================
CALL "%INTEGRATIONSPATH%\build-integrations.bat"
REM CALL "%INTEGRATIONSPATH%\build-ef6.1-provider.bat"
REM CALL "%INTEGRATIONSPATH%\build_signalR_provider.bat"

::_________________________________________::
::________COPYING POWERSHELL SCRIPTS_______::
::_________________________________________::

ECHO COPYING POWERSHELL SCRIPTS
ECHO ==========================
CALL "%BUILDSCRIPTSPATH%\copy-powershell-scripts.bat"
