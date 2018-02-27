@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

@SET gacPath=%WINDIR%\Microsoft.NET\assembly
@SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

ECHO UNINSTALLING SERVICES
ECHO =====================
SC QUERY NCACHESVC > NUL
IF NOT ERRORLEVEL 1060 (
	NET STOP NCACHESVC
	SC DELETE NCACHESVC
	SC QUERY NCACHESVC > NUL
	IF ERRORLEVEL 1060 (
		ECHO SERVICE UNINSTALLED SUCCESSFULLY
		ECHO ================================
	)
)

ECHO Terminate Cache Processes
ECHO =========================
TASKLIST | FIND /I /C "Alachisoft.NCache.CacheHo"
IF %ERRORLEVEL% EQU 0 TASKKILL /F /IM "Alachisoft.NCache.CacheHost.exe"

SET CachePath="%NCHOME%bin\assembly\4.0\Alachisoft.NCache.Cache.dll"
SET WebPath="%NCHOME%bin\assembly\4.0\Alachisoft.NCache.Web.dll"

ECHO UNINSTALLING PERFMON COUNTERS
ECHO =============================
"%iuPath%\INSTALLUTIL.EXE" /u "%CachePath%"
"%iuPath%\INSTALLUTIL.EXE" /u "%WebPath%"
ECHO.

ECHO REMOVING ASSEMBLIES FROM GAC
ECHO ============================
FOR %%f IN (%NCHOME2%bin\assembly\4.0\*NCache*.dll) DO (
	"%UNINSTALLUTILITIES%\GacInstall4.0.exe" /u "%%f"
)
FOR %%f in (%gacPath%\GAC_MSIL\*NCache*) DO (
	IF EXIST %%f ( RMDIR /s /q "%gacPath%\GAC_MSIL\%%~nf" )
)
ECHO.
