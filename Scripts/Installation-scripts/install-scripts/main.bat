@ECHO off

ECHO setting environment variables and registry keys
CALL install-scripts\write-env.bat %1

ECHO Copying required files
CALL install-scripts\copy-files.bat %1

ECHO installing assemblies to gac and performance monitors
CALL install-scripts\gac-install.bat %1

ECHO modifying config files
CALL install-scripts\modify-config-files.bat %1

ECHO installing service
CALL install-scripts\install-service.bat %1

pause