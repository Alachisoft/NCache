@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SETLOCAL ENABLEEXTENSIONS

SET VALUE_NAME=Path
SET TARLOCATION=%CD%
SET KEY_NAME=HKLM\SOFTWARE\7-Zip
SET NCACHETEMPFOLDER=%TARLOCATION%\ncache
SET UTILITYSCRIPTSPATH=%CD%\utility-scripts
SET SETUPUTILITIESPATH=%UTILITYSCRIPTSPATH%\setup-utilities

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

REG QUERY %KEY_NAME% >NUL
IF NOT %ERRORLEVEL% EQU 0 (
  ECHO. Unable to find 7-Zip, Please intall 7-Zip and then try again.
  GOTO failTar
)


ECHO CONVERTING EOL FROM WINDOWS TO UNIX
ECHO ===================================
CALL "%UTILITYSCRIPTSPATH%\dos2unix.bat"

ECHO CREATING FILE STRUCTURE
ECHO ======================+
CALL "%UTILITYSCRIPTSPATH%\build-ncache-directory.bat" "%NCACHETEMPFOLDER%"

ECHO COPPYING REQUIRED ASSEMBLY FILES
ECHO ================================
CALL "%UTILITYSCRIPTSPATH%\copy-ncache-files.bat" "%NCACHETEMPFOLDER%"

ECHO CREATING TAR.GZ
ECHO ===============
REG QUERY %KEY_NAME% >NUL
IF %ERRORLEVEL% EQU 0 (
  FOR /F "usebackq tokens=3*" %%A IN (`REG QUERY "%KEY_NAME%" /v "%VALUE_NAME%"`) DO (
    CALL "%UTILITYSCRIPTSPATH%\targz.bat" "%%A %%B" "%TARLOCATION%"
  )
) ELSE (
  ECHO. Unable to find 7-Zip, Please intall 7-Zip and then try again.
)

ECHO TAR.GZ CREATED SUCCESSFULLY, PLEASE FIND IT AT \SCRIPTS\ncache_dotnetcore-client\
ECHO =================================================================================
EXIT /b 0

:failTar
REM SOME ERROR OCCURRED WHILE CREATING TAR.GZ
ECHO FAILED TO CREATE TAR.GZ
ECHO =======================
PAUSE
EXIT /b 1
