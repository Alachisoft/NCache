
 @Set directory=%1

 @SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\
 @SET tempPath=%~1
 
 SET CachePath=%NCHOME%\bin\assembly\4.0\Alachisoft.NCache.Cache.dll

 SET WebPath=%NCHOME%\bin\assembly\4.0\Alachisoft.NCache.Web.dll

 
 REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v InstallPath >nul 2>&1
 SET QUERY_STATUS=%ERRORLEVEL%
 IF %QUERY_STATUS%==0 ( 
	ECHO VERIFYING IF SERVICE ALREADY EXISTS
	ECHO =======================================
	SC QUERY NCACHESVC > NUL
	IF ERRORLEVEL 1060 ( 
		ECHO SERVICE DOES NOT EXIST
		ECHO ==========================
	)
	IF NOT ERRORLEVEL 1060 (
		ECHO SERVICE EXISTS, REMOVING PREVIOUS SERVICE
		ECHO =============================================
		NET STOP NCACHESVC
		SC DELETE NCACHESVC
		SC QUERY NCACHESVC > NUL
		IF NOT ERRORLEVEL 1060 (
		ECHO SERVICE REMOVED SUCCESSFULLY
		ECHO =================================
		)
	) 
 
	REM SETLOCAL ENABLEEXTENSIONS 
	REM SETLOCAL enabledelayedexpansion
	
	ECHO INSTALLING ASSEMBLIES TO GAC 
	ECHO ================================
	FOR %%f IN (%1\bin\assembly\4.0\*NCache*.dll) DO (
		"setup-utilities\GacInstall4.0.exe" /i "%%f"
	)
	ECHO. 
	
	"setup-utilities\GacInstall4.0.exe" /i "%%f" ..\..\..\Integration\build\ContentOptimization\Alachisoft.NCache.Adapters.dll
	"setup-utilities\GacInstall4.0.exe" /i "%%f" ..\..\..\Integration\build\ContentOptimization\Alachisoft.Common.dll
	"setup-utilities\GacInstall4.0.exe" /i "%%f" ..\..\..\Integration\build\ContentOptimization\Alachisoft.ContentOptimization.dll
	
	REM INSTALLING PROTOBUF-NET and Log4net TO GAC
	"setup-utilities\GacInstall4.0.exe" /i %1\bin\assembly\4.0\protobuf-net.dll
	"setup-utilities\GacInstall4.0.exe" /i %1\bin\assembly\4.0\log4net.dll
	
	ECHO REGISTRING PERFMON COUNTERS
	ECHO ===============================
	
    ECHO CachePath:  %CachePath%
    ECHO WebPath:  %CachePath%
    ECHO iuPath: %iuPath%
	"%iuPath%INSTALLUTIL.EXE" /i "%CachePath%"
	"%iuPath%INSTALLUTIL.EXE" /i "%WebPath%"
	ECHO.
	
 )