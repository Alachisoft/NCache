@ECHO off
 @SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\
 REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\full" /v InstallPath >nul 2>&1
 SET QUERY_STATUS=%ERRORLEVEL%
 SET ZERO=0
 @Set directory=%~1
 IF %QUERY_STATUS%==%ZERO% ( 
 
	ECHO COPYING SERVICE...
	ECHO ==================
	XCOPY "..\..\Src\build\Server\4x\Alachisoft.NCache.Service.exe" "%directory%\bin\service\" /Y /Q
	XCOPY "..\..\Src\build\Server\4x\Alachisoft.NCache.CacheHost.exe" "%directory%\bin\service\" /Y /Q
	XCOPY "setup-utilities\Alachisoft.NCache.CacheHost.exe.config" "%directory%\bin\service\" /Y /Q
	XCOPY "setup-utilities\Alachisoft.NCache.Service.exe.config" "%directory%\bin\service\" /Y /Q
	ECHO.
	
	
	ECHO COPYING DLLS...
	ECHO ===============
	XCOPY "..\..\Resources\ncregistry.dll" %WINDIR%\System32\ /Y /Q
	
	FOR %%f IN (..\..\Src\build\Server\4x\*NCache*.dll) DO (
			XCOPY "%%f" "%directory%\bin\assembly\4.0\" /Y /Q /D	
	) 
	FOR %%f IN (..\..\Src\build\NetCore\Client\Alachisoft*.dll) DO (
			XCOPY "%%f" "%directory%\bin\assembly\netcore20\" /Y /Q /D	
	) 
	IF EXIST %directory%\bin\assembly\4.0 (
		XCOPY "..\..\Resources\64bitOracle11g\Oracle.ManagedDataAccess.dll" "%directory%\bin\assembly\4.0\" /Y /Q
		XCOPY "..\..\Resources\protobufdotnet\x32\protobuf-net.dll" "%directory%\bin\assembly\4.0\" /Y /Q
		XCOPY "..\..\Resources\log4net\log4net.dll" "%directory%\bin\assembly\4.0\" /Y /Q

	)
	ECHO.

	ECHO COPYING TOOLS...
	ECHO ================
	FOR %%f IN (..\..\Tools\build\Server\4.0\*.exe) Do (
		XCOPY "%%f" "%directory%\bin\tools\" /Y /Q
	)
	XCOPY "..\..\Tools\build\Server\4x\ncacheps.dll" "%directory%\bin\tools\ncacheps\" /Y /Q
	ECHO.
	
	ECHO COPYING INTEGRATIONS ...
	ECHO ========================
	XCOPY "..\..\Integration\build\ContentOptimization" "%directory%\integrations\ContentOptimization\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\LINQToNCache\4.0" "%directory%\integrations\LINQToNCache\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\LINQToNCache\dotnetcore" "%directory%\integrations\LINQToNCache\dotnetcore\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\NHibernate" "%directory%\integrations\nhibernate\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\NCache.SignalR" "%directory%\integrations\NCache.SignalR\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\PowerShell\Scripts" "%directory%\integrations\PowerShell\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\MSEntityFramework" "%directory%\integrations\MSEntityFramework\" /I /Y /Q /S
	XCOPY "..\..\Integration\build\OutputCache" "%directory%\integrations\OutputCache\" /I /Y /Q /S
	
	ECHO.
	
	ECHO COPYING CONFIGS...
	ECHO ==================
	XCOPY ".\setup-utilities\client.ncconf" "%directory%\config\" /Y /Q
	XCOPY ".\setup-utilities\config.ncconf" "%directory%\config\" /Y /Q
	XCOPY ".\setup-utilities\efcaching.ncconf" "%directory%\config\" /Y /Q
	ECHO.
	
	ECHO License and README Files...
	ECHO ==================
	XCOPY "..\..\LICENSE" "%directory%\" /Y /Q
	XCOPY "..\..\Scripts\README.md" "%directory%\" /Y /Q
	ECHO.
	
	IF NOT EXIST "%directory%\log-files" ( MKDIR "%directory%\log-files" )
 )



	
	
 