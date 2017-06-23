@echo off

ECHO uninstalling assemblies from gac, service and performance monitors
CALL uninstall-scripts\gac-uninstall.bat

ECHO removing ncache directory
CALL uninstall-scripts\remove-directory.bat

ECHO unsetting environment variables and registry keys
CALL uninstall-scripts\remove-env.bat

pause