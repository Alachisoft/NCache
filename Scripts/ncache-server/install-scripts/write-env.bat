@ECHO OFF

ECHO SETTING ENVIRONMENT VARIABLE ...
ECHO ================================
SETX /M NCHOME %1\
Echo "%PSModulePath%;%~1\bin\tools\"
SETX /M PSModulePath "%PSModulePath%;%~1\bin\tools\"
ECHO.

ECHO SETTING REGISTRY VALUES ...
ECHO ===========================

ECHO REMOVING PREVIOUS REGISTRY VALUES ...
ECHO =====================================
REG QUERY "HKLM\Software\Alachisoft" >nul 2>&1
IF %ERRORLEVEL%==0 (
REG DELETE HKLM\Software\Alachisoft /F
}

ECHO ADDING NEW REGISTRY VALUES ...
ECHO ==============================
REG ADD HKLM\Software\Alachisoft\NCache /V NCacheTcp.Port /D 8250 /T REG_SZ
REG ADD HKLM\Software\Alachisoft\NCache /V Http.Port /D 8251 /T REG_SZ
REG ADD HKLM\Software\Alachisoft\NCache /V InstallDir /D %1\ /T REG_SZ
ECHO.
