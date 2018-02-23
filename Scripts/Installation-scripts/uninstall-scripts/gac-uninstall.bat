@echo OFF

@set directory=%NCHOME%
@set gacPath=%WINDIR%\Microsoft.NET\assembly
@set iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

ECHO UNINSTALLING SERVICES
ECHO ==========================
SC QUERY NCACHESVC > NUL
IF NOT ERRORLEVEL 1060 (
	NET STOP NCACHESVC
	SC DELETE NCACHESVC
	SC QUERY NCACHESVC > NUL
	IF ERRORLEVEL 1060 (
		ECHO SERVICE UNINSTALLED SUCCESSFULLY
		ECHO ====================================
	)
)

ECHO Terminate Cache Processes 
ECHO ==========================
tasklist | find /I /C "Alachisoft.NCache.CacheHo"
IF %ERRORLEVEL% EQU 0 taskkill /F /IM "Alachisoft.NCache.CacheHost.exe"

SET NCHOME2="%NCHOME%"

ECHO %NCHOME%
ECHO %NCHOME2%

SET CachePath="%NCHOME%bin\assembly\4.0\Alachisoft.NCache.Cache.dll"
SET WebPath="%NCHOME%bin\assembly\4.0\Alachisoft.NCache.Web.dll"

ECHO UNINSTALLING PERFMON COUNTERS
ECHO ================================
"%iuPath%\INSTALLUTIL.EXE" /u %CachePath%
"%iuPath%\INSTALLUTIL.EXE" /u %WebPath%
ECHO.

ECHO REMOVING ASSEMBLIES FROM GAC
ECHO ================================
 FOR %%f IN (%NCHOME2%bin\assembly\4.0\*NCache*.dll) DO (
	"setup-utilities\GacInstall4.0.exe" /u "%%f"
 )
 
 for %%f in (%gacPath%\GAC_MSIL\*NCache*) do (
	IF EXIST %%f ( RMDIR /s /q "%gacPath%\GAC_MSIL\%%~nf" 
	)
	)
	

ECHO.
 
 
