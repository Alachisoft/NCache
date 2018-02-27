@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET INTEGRATIONPARENTFOLDER=..\..\Integration

::_________________________________________::
::_____________COPYING SCRIPTS_____________::
::_________________________________________::

XCOPY /Y "%INTEGRATIONPARENTFOLDER%\PowerShell\Scripts\InstallNCache.ps1" "%INTEGRATIONPARENTFOLDER%\build\PowerShell\Scripts\"
IF NOT %ERRORLEVEL%==0 GOTO :failCopying
XCOPY /Y "%INTEGRATIONPARENTFOLDER%\PowerShell\Scripts\UninstallNCache.ps1" "%INTEGRATIONPARENTFOLDER%\build\PowerShell\Scripts\"
IF NOT %ERRORLEVEL%==0 GOTO :failCopying
ECHO PowerShell scripts copied successfully!

EXIT /b 0

:failCopying
ECHO FAILED TO COPY POWERSHELL SCRIPTS 
ECHO =======================================
PAUSE
EXIT /b 1
