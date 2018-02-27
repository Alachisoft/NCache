@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET InstallDir="C:\Program Files\NCache"
SET INSTALLSCRIPTSPATH=%CD%\install-scripts
SET SETUPUTILITIESPATH=%CD%\install-scripts\setup-utilities

::_________________________________________::
::____________INSTALLING NCACHE____________::
::_________________________________________::

REG QUERY "HKLM\SOFTWARE\Alachisoft\NCache" /v InstallDir >NUL 2>&1
ECHO

IF %ERRORLEVEL%==0 (
    ECHO Another version of NCache is already installed!
)
IF %ERRORLEVEL%==1 (
    ECHO Setting environment variables and registry keys
    CALL "%INSTALLSCRIPTSPATH%\write-env.bat" %InstallDir%

    ECHO Copying required files
    CALL "%INSTALLSCRIPTSPATH%\copy-files.bat" %InstallDir%

    ECHO Installing assemblies to gac and performance monitors
    CALL "%INSTALLSCRIPTSPATH%\gac-install.bat" %InstallDir%

    ECHO Modifying config files
    CALL "%INSTALLSCRIPTSPATH%\modify-config-files.bat" %InstallDir%

    ECHO Installing service
    CALL "%INSTALLSCRIPTSPATH%\install-service.bat" %InstallDir%
)

EXIT /b 0
