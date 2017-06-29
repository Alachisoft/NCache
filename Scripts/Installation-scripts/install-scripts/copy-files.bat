@ECHO off
 @SET iuPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\
 REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\full" /v InstallPath >nul 2>&1
 SET QUERY_STATUS=%ERRORLEVEL%
 SET ZERO=0
 IF %QUERY_STATUS%==%ZERO% ( 
 
	ECHO COPYING SERVICE...
	ECHO ==================
	XCOPY ..\..\src\build\server\4x\Alachisoft.NCache.Service.exe %1\bin\service\ /Y /Q
	XCOPY  ..\..\src\build\Server\4x\Alachisoft.NCache.CacheHost.exe %1\bin\service\ /Y /Q
	XCOPY ..\..\samples\config\Alachisoft.NCache.CacheHost.exe.config %1\bin\service\ /Y /Q
	XCOPY ..\..\samples\config\Alachisoft.NCache.Service.exe.config %1\bin\service\ /Y /Q
	ECHO.
	
	
	ECHO COPYING DLLS...
	ECHO ===============
	XCOPY "..\..\resources\NCRegistry.dll" %WINDIR%\System32\ /Y /Q
	
	FOR %%f IN (..\..\src\build\Server\4x\*NCache*.dll) DO (
			XCOPY "%%f" %1\bin\assemblies\4.0\ /Y /Q /D	
	) 
	IF EXIST %1\bin\assemblies\4.0 (
		XCOPY "..\..\resources\protobufdotnet\x32\protobuf-net.dll" %1\bin\assemblies\4.0\ /Y /Q
		XCOPY "..\..\resources\log4net\log4net.dll" %1\bin\assemblies\4.0\ /Y /Q
	)
	ECHO.

	ECHO COPYING TOOLS...
	ECHO ================
	FOR %%f IN (..\..\tools\build\server\4.0\*.exe) Do (
		XCOPY "%%f" %1\bin\tools\ /Y /Q
	)
	ECHO.
	
	ECHO COPYING INTEGRATIONS ...
	ECHO ========================
	XCOPY "..\..\integration\build\ContentOptimization" %1\integrations\ContentOptimization\ /I /Y /Q /S
	XCOPY "..\..\integration\build\LINQToNCache" %1\integrations\LINQToNCache /I /Y /Q /S
	XCOPY "..\..\integration\build\NHibernate" %1\integrations\nhibernatencache /I /Y /Q /S
	XCOPY "..\..\integration\build\Memcached\Gateway\Alachisoft.NCache.Integrations.Memcached.Provider.dll" "%~1\integrations\Memcached Wrapper\Gateway\bin\" /I /Y /Q
	XCOPY "..\..\integration\build\Memcached\Gateway\Alachisoft.NCache.Integrations.Memcached.ProxyServer.dll" "%~1\integrations\Memcached Wrapper\Gateway\bin\" /I /Y /Q
	XCOPY "..\..\integration\build\Memcached\Gateway\Alachisoft.NCache.Memcached.exe" "%~1\integrations\Memcached Wrapper\Gateway\bin\" /I /Y /Q
	XCOPY "..\..\samples\config\Alachisoft.NCache.Memcached.exe.config" "%~1\integrations\Memcached Wrapper\Gateway\bin\" /I /Y /Q
	XCOPY "..\..\Resources\memcacheclient\Commons.dll" "%~1\integrations\Memcached Wrapper\Plug-Ins\.NET memcached Client Library\bin\" /I /Y /Q
	XCOPY "..\..\Resources\memcacheclient\ICSharpCode.SharpZipLib.dll" "%~1\integrations\Memcached Wrapper\Plug-Ins\.NET memcached Client Library\bin\" /I /Y /Q
	XCOPY "..\..\integration\build\Memcached\Plug-Ins\Memcached.ClientLibrary.dll" "%~1\integrations\Memcached Wrapper\Plug-Ins\.NET memcached Client Library\bin\" /I /Y /Q
	XCOPY "..\..\integration\build\Memcached\Plug-Ins\BeITMemcached.dll" "%~1\integrations\Memcached Wrapper\Plug-Ins\BeITMemcached\bin\" /I /Y /Q
	XCOPY "..\..\integration\build\Memcached\Plug-Ins\Enyim.Caching.dll" "%~1\integrations\Memcached Wrapper\Plug-Ins\Enyim.Caching\" /I /Y /Q /S
	ECHO.
	
	ECHO COPYING CONFIGS...
	ECHO ==================
	XCOPY "..\..\samples\config\client.ncconf" %1\config\ /Y /Q
	XCOPY "..\..\samples\config\config.ncconf" %1\config\ /Y /Q
	ECHO.
	
	ECHO License and README Files...
	ECHO ==================
	XCOPY "..\..\LICENSE" %1\ /Y /Q
	XCOPY "..\..\README.md" %1\ /Y /Q
	ECHO.
	
	IF NOT EXIST %1\log-files ( MKDIR %1\log-files )
 )

	
	
 