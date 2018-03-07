@ECHO OFF

ECHO REMOVING ENVIRONMENT VARIABLE(S)...
ECHO ===================================

@SET SearchPSstring=%NCHOME%\bin\tools\;
ECHO "%PSModulePath%" | FIND /i "%SearchPSstring%"
if %ERRORLEVEL%==0 (
	GOTO Work
) ELSE (
	GOTO NoWork
)

:Work
CALL SET PSModulePathTMP=%%PSModulePath:%SearchPSstring%=%%
ECHO Hello
ECHO %PSModulePathTMP%
SETX /M PSModulePath "%PSModulePathTMP%"
	
:NoWork
ECHO.

REG DELETE "HKLM\SYSTEM\CURRENTCONTROLSET\CONTROL\SESSION MANAGER\ENVIRONMENT" /V NCHOME /F
ECHO.

ECHO REMOVING REGISTRY VALUE(S) ...
ECHO ==============================
REG DELETE "HKLM\SOFTWARE\ALACHISOFT\NCACHE" /F
ECHO.
