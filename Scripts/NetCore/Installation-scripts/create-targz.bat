@ECHO OFF
SETLOCAL ENABLEEXTENSIONS
SET KEY_NAME=HKLM\SOFTWARE\7-Zip
SET VALUE_NAME=Path

ECHO CONVERTING EOL FROM WINDOWS TO UNIX
ECHO ===================================
CALL dos2unix.bat

ECHO CREATING FILE STRUCTURE
ECHO ======================
CALL build-ncache-directory.bat

ECHO COPPYING REQUIRED ASSEMBLY FILES
ECHO ================================
CALL copy-ncache-files.bat

ECHO CREATING TAR.GZ
ECHO ===============
reg query %KEY_NAME% >nul
IF %errorlevel% equ 0  (
  FOR /F "usebackq tokens=3*" %%A IN (`REG QUERY "%KEY_NAME%" /v "%VALUE_NAME%"`) DO (
	ECHO %%A %%B	
	CALL targz.bat "%%A %%B"
    )
) ELSE (
  ECHO. Unable to find 7-Zip, Please intall 7-Zip and then try again.
)

ECHO TAR.GZ CREATED SUCCESSFULLY, PLEASE FIND IT AT \SCRIPTS\NETCORE\
ECHO ================================================================

PAUSE