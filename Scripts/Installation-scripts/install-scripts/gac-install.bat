@ECHO off
 @Set directory=%1
 
 @SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\
 @SET tempPath=%~1
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

	ECHO REGISTRING PERFMON COUNTERS
	ECHO ===============================
		
	%iuPath%INSTALLUTIL.EXE /i "%~1\bin\assemblies\4.0\Alachisoft.NCache.Cache.dll"
	%iuPath%INSTALLUTIL.EXE /i "%~1\bin\assemblies\4.0\Alachisoft.NCache.Web.dll"
	ECHO.
	
	ECHO INSTALLING ASSEMBLIES TO GAC 
	ECHO ================================
	FOR %%f IN (%1\bin\assemblies\4.0\*NCache*.dll) DO (
		"utilities\GacInstall4.0.exe" /i "%%f"
	)
	ECHO. 
		
	REM INSTALLING PROTOBUF-NET and Log4net TO GAC
	"utilities\GacInstall4.0.exe" /i %1\bin\assemblies\4.0\protobuf-net.dll
	"utilities\GacInstall4.0.exe" /i %1\bin\assemblies\4.0\log4net.dll
 )