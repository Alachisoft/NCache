@ECHO off
REM BUILDING NCACHE OPENSOURCE SOLUTION FOR .NET CORE
	ECHO BUILDING NCACHE .NET CORE SOLUTION
	ECHO ==================================
	dotnet	build 	-c Release    ..\..\..\Src\NCWebCache\NCWebCache.Client.NetCore.csproj
	dotnet 	build 	-c Release    ..\..\..\Src\NCSocketServer\NCSocketServer.Client.NetCore.csproj	
	dotnet 	build 	-c Release    ..\..\..\Src\NCSessionStoreProvider\NCSessionStoreProvider.Client.NetCore.csproj	
	dotnet 	publish -c Release    ..\..\..\Src\NCDaemon\NCDaemon.NetCore.csproj	
	dotnet 	publish -c Release    ..\..\..\Src\NCCacheSeparateHost\NCCacheSeparateHost.Client.NetCore.csproj	
	SET BUILD_STATUS=%ERRORLEVEL%
	IF NOT %BUILD_STATUS%==0 GOTO failNCache
	
	IF NOT EXIST ..\..\..\Src\build\NetCore\Client ( MKDIR ..\..\..\Src\build\NetCore\Client ) 
	XCOPY ..\..\..\Src\NCWebCache\bin\Release\netstandard2.0\netstandard2.0\Alachisoft.*.dll 					..\..\..\Src\build\NetCore\Client /Y /Q /S
	XCOPY ..\..\..\Src\NCSocketServer\bin\Release\netstandard2.0\Alachisoft.*.dll 								..\..\..\Src\build\NetCore\Client /Y /Q /S
	XCOPY ..\..\..\Src\NCSessionStoreProvider\bin\Release\netcoreapp2.0\Alachisoft.*.dll 						..\..\..\Src\build\NetCore\Client /Y /Q /S
	
EXIT /b 0

:failNCache
REM SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET CORE , LAUNCH SOLUTION
ECHO FAILED TO BUILD NCACHE
ECHO ======================
PAUSE
EXIT /b 1
