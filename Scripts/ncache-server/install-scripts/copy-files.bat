@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET ROOT="..\.."
SET RESOURCES="..\..\Resources"
SET SRC4X="..\..\Src\build\Server\4x"
SET SRCTOOLS4X="..\..\Tools\build\Server\4x"
SET SRCINTEGRATION="..\..\Integration\build"
SET SRCNETCORE="..\..\Src\build\NetCore\Client"

@SET DIRECTORY=%~1

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v InstallPath >nul 2>&1

IF %ERRORLEVEL%==0 (
	ECHO COPYING SERVICE...
	ECHO ==================
	XCOPY "%SRC4X%\Alachisoft.NCache.Service.exe" "%DIRECTORY%\bin\service\" /Y /Q
	XCOPY "%SRC4X%\Alachisoft.NCache.CacheHost.exe" "%DIRECTORY%\bin\service\" /Y /Q
	XCOPY "%SETUPUTILITIESPATH%\Alachisoft.NCache.CacheHost.exe.config" "%DIRECTORY%\bin\service\" /Y /Q
	XCOPY "%SETUPUTILITIESPATH%\Alachisoft.NCache.Service.exe.config" "%DIRECTORY%\bin\service\" /Y /Q
	ECHO.

	ECHO COPYING DLLS...
	ECHO ===============
	XCOPY "%RESOURCES%\ncregistry.dll" %WINDIR%\System32\ /Y /Q

	FOR %%f IN (%SRC4X%\*NCache*.dll) DO (
		XCOPY "%%f" "%DIRECTORY%\bin\assembly\4.0\" /Y /Q /D
	) 
	FOR %%f IN (%SRCNETCORE%\Alachisoft*.dll) DO (
		XCOPY "%%f" "%DIRECTORY%\bin\assembly\netcore20\" /Y /Q /D
	)
	IF EXIST %DIRECTORY%\bin\assembly\4.0 (
		XCOPY "%RESOURCES%\64bitOracle11g\Oracle.ManagedDataAccess.dll" "%DIRECTORY%\bin\assembly\4.0\" /Y /Q
		XCOPY "%RESOURCES%\protobufdotnet\x32\protobuf-net.dll" "%DIRECTORY%\bin\assembly\4.0\" /Y /Q
		XCOPY "%RESOURCES%\log4net\log4net.dll" "%DIRECTORY%\bin\assembly\4.0\" /Y /Q
	)
	ECHO.

	IF EXIST %SRCTOOLS4X% (
		ECHO COPYING TOOLS...
		ECHO ================
		XCOPY "%SRCTOOLS4X%\ncacheps.dll" "%DIRECTORY%\bin\tools\ncacheps\" /Y /Q
		ECHO.
	)

	ECHO COPYING INTEGRATIONS ...
	ECHO ========================
	XCOPY "%SRCINTEGRATION%\ContentOptimization" "%DIRECTORY%\integrations\ContentOptimization\" /I /Y /Q /S 
	XCOPY "%SRCINTEGRATION%\LINQToNCache\4.0" "%DIRECTORY%\integrations\LINQToNCache\4.0" /I /Y /Q /S 
	IF EXIST %SRCINTEGRATION%\LINQToNCache\dotnetcore ( XCOPY "%SRCINTEGRATION%\LINQToNCache\dotnetcore" "%DIRECTORY%\integrations\LINQToNCache\dotnetcore\" /I /Y /Q /S )
	XCOPY "%SRCINTEGRATION%\NHibernate" "%DIRECTORY%\integrations\nhibernate\" /I /Y /Q /S 
	IF EXIST %SRCINTEGRATION%\NCache.SignalR ( XCOPY "%SRCINTEGRATION%\NCache.SignalR" "%DIRECTORY%\integrations\NCache.SignalR\" /I /Y /Q /S )
	XCOPY "%SRCINTEGRATION%\PowerShell\Scripts" "%DIRECTORY%\integrations\PowerShell\" /I /Y /Q /S 
	IF EXIST %SRCINTEGRATION%\MSEntityFramework ( XCOPY "%SRCINTEGRATION%\MSEntityFramework" "%DIRECTORY%\integrations\MSEntityFramework\" /I /Y /Q /S )
	XCOPY "%SRCINTEGRATION%\OutputCache" "%DIRECTORY%\integrations\OutputCache\" /I /Y /Q /S 

	ECHO.

	ECHO COPYING CONFIGS...
	ECHO ==================
	XCOPY "%SETUPUTILITIESPATH%\client.ncconf" "%DIRECTORY%\config\" /Y /Q
	XCOPY "%SETUPUTILITIESPATH%\config.ncconf" "%DIRECTORY%\config\" /Y /Q
	XCOPY "%SETUPUTILITIESPATH%\efcaching.ncconf" "%DIRECTORY%\config\" /Y /Q
	ECHO.

	ECHO License and README Files...
	ECHO ===========================
	XCOPY "%ROOT%\LICENSE" "%DIRECTORY%\" /Y /Q
	XCOPY "%ROOT%\Scripts\README.md" "%DIRECTORY%\" /Y /Q
	ECHO.

	IF NOT EXIST "%DIRECTORY%\log-files" (
		MKDIR "%DIRECTORY%\log-files"
	)

	EXIT /b 0
)
