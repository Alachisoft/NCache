@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET UNINSTALLSCRIPTSPATH=%CD%\uninstall-scripts
SET UNINSTALLUTILITIES=%UNINSTALLSCRIPTSPATH%\utilities

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

ECHO Uninstalling assemblies from GAC, service and performance monitors
CALL "%UNINSTALLSCRIPTSPATH%\gac-uninstall.bat"

ECHO Removing NCache directory
CALL "%UNINSTALLSCRIPTSPATH%\remove-directory.bat"

ECHO Unsetting environment variables and registry keys
CALL "%UNINSTALLSCRIPTSPATH%\remove-env.bat"

EXIT /b 0
