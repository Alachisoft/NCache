@ECHO OFF

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

IF EXIST "%~1" (
    RMDIR "%~1" /s /q
)

MKDIR "%~1"
MKDIR "%~1\bin"
MKDIR "%~1\bin\service"
MKDIR "%~1\bin\ncacheps"
MKDIR "%~1\config"
MKDIR "%~1\docs"
MKDIR "%~1\lib"
MKDIR "%~1\log-files"
MKDIR "%~1\log-files\ClientLogs"
MKDIR "%~1\integrations"
MKDIR "%~1\integrations\LINQToNCache"
