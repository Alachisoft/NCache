@ECHO off
 @SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\
 REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\full" /v InstallPath >nul 2>&1
 SET QUERY_STATUS=%ERRORLEVEL%
 SET ZERO=0
 IF %QUERY_STATUS%==%ZERO% ( 
 
	ECHO COPYING SERVICE...
	ECHO ==================
	XCOPY "..\..\..\Src\build\Server\4x\Alachisoft.NCache.Service.exe" %1\bin\service\ /Y /Q
	XCOPY "..\..\..\Src\build\Server\4x\Alachisoft.NCache.CacheHost.exe" %1\bin\service\ /Y /Q
	XCOPY "..\..\..\Samples\config\Alachisoft.NCache.CacheHost.exe.config" %1\bin\service\ /Y /Q
	XCOPY "..\..\..\Samples\config\Alachisoft.NCache.Service.exe.config" %1\bin\service\ /Y /Q
	ECHO.
	
	
	ECHO COPYING DLLS...
	ECHO ===============
	XCOPY "..\..\..\Resources\ncregistry.dll" %WINDIR%\System32\ /Y /Q
	
	FOR %%f IN (..\..\..\Src\build\Server\4x\*NCache*.dll) DO (
			XCOPY "%%f" %1\bin\assembly\4.0\ /Y /Q /D	
	) 
	FOR %%f IN (..\..\..\Src\build\NetCore\Client\Alachisoft*.dll) DO (
			XCOPY "%%f" %1\bin\assembly\netcore20\ /Y /Q /D	
	) 
	IF EXIST %1\bin\assembly\4.0 (
		XCOPY "..\..\..\Resources\64bitOracle11g\Oracle.ManagedDataAccess.dll" %1\bin\assembly\4.0\ /Y /Q
		XCOPY "..\..\..\Resources\protobufdotnet\x32\protobuf-net.dll" %1\bin\assembly\4.0\ /Y /Q
		XCOPY "..\..\..\Resources\log4net\log4net.dll" %1\bin\assembly\4.0\ /Y /Q
		XCOPY "..\..\..\Resources\snmp\SharpSnmpLib.dll" %1\bin\assembly\4.0\ /Y /Q
		XCOPY "..\..\..\Resources\ssh.net\Renci.SshNet.dll" %1\bin\assembly\4.0\ /Y /Q
	)
	ECHO.

	ECHO COPYING TOOLS...
	ECHO ================
	FOR %%f IN (..\..\..\Tools\build\Server\4.0\*.exe) Do (
		XCOPY "%%f" %1\bin\tools\ /Y /Q
	)
	XCOPY "..\..\..\Tools\build\Server\4x\ncacheps.dll" %1\bin\tools\ncacheps\ /Y /Q
	ECHO.
	
	ECHO COPYING INTEGRATIONS ...
	ECHO ========================
	XCOPY "..\..\..\Integration\build\ContentOptimization" %1\integrations\ContentOptimization\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\LINQToNCache\4.0" %1\integrations\LINQToNCache\4.0 /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\LINQToNCache\dotnetcore" %1\integrations\LINQToNCache\dotnetcore\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\NHibernate" %1\integrations\nhibernate\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\NCache.SignalR" %1\integrations\NCache.SignalR\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\PowerShell\Scripts" %1\integrations\PowerShell\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\MSEntityFramework" %1\integrations\MSEntityFramework\ /I /Y /Q /S
	XCOPY "..\..\..\Integration\build\OutputCache" %1\integrations\OutputCache\ /I /Y /Q /S
	
	ECHO.
	
	ECHO COPYING CONFIGS...
	ECHO ==================
	XCOPY "..\setup-utilities\config\client.ncconf" %1\config\ /Y /Q
	XCOPY "..\setup-utilities\config\config.ncconf" %1\config\ /Y /Q
	XCOPY "..\setup-utilities\config\efcaching.ncconf" %1\config\ /Y /Q
	ECHO.
	
	ECHO License and README Files...
	ECHO ==================
	XCOPY "..\..\..\LICENSE" %1\ /Y /Q
	XCOPY "..\..\..\Scripts\README.md" %1\ /Y /Q
	ECHO.
	
	IF NOT EXIST %1\log-files ( MKDIR %1\log-files )
 )



	
	
 