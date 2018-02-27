@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET BUILDSCRIPTSPATH="build-scripts"
SET INTEGRATIONSPATH="integrations"

::_________________________________________::
::__________BUILDING NCACHE SERVER_________::
::_________________________________________::

ECHO BUILDING .NET CORE NCACHE SOURCE 
ECHO ================================
CALL "%BUILDSCRIPTSPATH%\build-ncache-netcore.bat"

::_________________________________________::
::__________BUILDING NCACHE TOOLS__________::
::_________________________________________::

ECHO BUILDING .NET CORE TOOLS
ECHO ========================
CALL "%BUILDSCRIPTSPATH%\build-tools-netcore.bat"

::_________________________________________::
::__________BUILDING INTEGRATIONS__________::
::_________________________________________::

ECHO BUILDING INTEGRATIONS
ECHO =====================
CALL "%INTEGRATIONSPATH%\build-integrations-core.bat"

EXIT /b 0
