@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET SRCPATH=..\..\Src

::_________________________________________::
::___________BUILDING SOURCE CODE__________::
::_________________________________________::


ECHO BUILDING NCACHE .NET CORE SOLUTION
ECHO ==================================
dotnet	build 	-c Release    %SRCPATH%\NCWebCache\NCWebCache.Client.NetCore.csproj
dotnet 	build 	-c Release    %SRCPATH%\NCSocketServer\NCSocketServer.Client.NetCore.csproj	
dotnet 	build 	-c Release    %SRCPATH%\NCSessionStoreProvider\NCSessionStoreProvider.Client.NetCore.csproj	
dotnet 	publish -c Release    %SRCPATH%\NCDaemon\NCDaemon.NetCore.csproj
dotnet 	publish -c Release    %SRCPATH%\NCCacheSeparateHost\NCCacheSeparateHost.Client.NetCore.csproj

IF NOT %ERRORLEVEL%==0 GOTO failNCache

IF NOT EXIST %SRCPATH%\build\NetCore\Client ( MKDIR %SRCPATH%\build\NetCore\Client ) 
XCOPY %SRCPATH%\NCWebCache\bin\Release\netstandard2.0\netstandard2.0\Alachisoft.*.dll 					%SRCPATH%\build\NetCore\Client /Y /Q /S
XCOPY %SRCPATH%\NCSocketServer\bin\Release\netstandard2.0\Alachisoft.*.dll 								%SRCPATH%\build\NetCore\Client /Y /Q /S
XCOPY %SRCPATH%\NCSessionStoreProvider\bin\Release\netcoreapp2.0\Alachisoft.*.dll 						%SRCPATH%\build\NetCore\Client /Y /Q /S
	
EXIT /b 0

::_________________________________________::
::_____________HANDLING FAILURE____________::
::_________________________________________::

:failNCache
REM SOME ERROR OCCURRED WHILE COMPILING NCACHE OPENSOURCE SOLUTION FOR .NET CORE , LAUNCH SOLUTION
ECHO FAILED TO BUILD NCACHE
ECHO ======================
%SRCPATH%\NCache.Client.NetCore.sln
EXIT /b 1
