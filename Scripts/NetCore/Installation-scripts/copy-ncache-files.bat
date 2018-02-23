@Echo off
SET ncache=..\..\..\Scripts\NetCore\ncache
SET setup-utilities=..\..\..\Scripts\NetCore\Installation-scripts\setup-utilities

ECHO COPYING REQUIRED DLLs TO LIB FOLDER
ECHO ===================================
XCOPY ..\..\..\Src\NCWebCache\bin\Release\netstandard2.0\netstandard2.0\Alachisoft.*.dll 					%ncache%\lib /Y /Q /S
XCOPY ..\..\..\Src\NCSocketServer\bin\Release\netstandard2.0\Alachisoft.*.dll 								%ncache%\lib /Y /Q /S

ECHO COPYING REQUIRED DEPENDENCIES TO LIB FOLDER
ECHO ===========================================
XCOPY ..\..\..\Src\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.dll 								%ncache%\lib /Y /Q /S
XCOPY ..\..\..\Src\NCDaemon\bin\Release\netcoreapp2.0\publish\*.dll 										%ncache%\lib /Y /Q /S
del	%ncache%\lib\*.CacheHost.*
del	%ncache%\lib\*.Daemon.*
rmdir %ncache%\lib\runtimes /Q /S

ECHO COPYING REQUIRED FILES FOR SERVICE
ECHO ===================================
XCOPY ..\..\..\Src\NCDaemon\bin\Release\netcoreapp2.0\publish\*.Daemon.dll 									%ncache%\bin\service /Y /Q /S
XCOPY ..\..\..\Src\NCDaemon\bin\Release\netcoreapp2.0\publish\*.Daemon.runtimeconfig.json 					%ncache%\bin\service /Y /Q /S
XCOPY %setup-utilities%\*.Daemon.*																			%ncache%\bin\service /Y /Q /S
XCOPY %setup-utilities%\ncached.service																		%ncache%\bin\service /Y /Q /S

ECHO COPYING REQUIRED FILES FOR CACHE HOST
ECHO =====================================
XCOPY ..\..\..\Src\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.dll 					%ncache%\bin\service /Y /Q /S
XCOPY ..\..\..\Src\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.runtimeconfig.json 	%ncache%\bin\service /Y /Q /S
XCOPY ..\..\..\Src\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.dll.config 			%ncache%\bin\service /Y /Q /S

ECHO COPYING REQUIRED FILES FOR TOOLS
ECHO ================================
XCOPY ..\..\..\Tools\NCAutomation\bin\Release\netcoreapp2.0\ncacheps.dll 									%ncache%\bin\ncacheps /Y /Q /S
XCOPY ..\..\..\Tools\ncacheps\*.xml																			%ncache%\bin\ncacheps /Y /Q /S
XCOPY %setup-utilities%\*.psd1																				%ncache%\bin\ncacheps /Y /Q /S

ECHO COPYING CONFIG FILES
ECHO ====================
XCOPY %setup-utilities%\*.ncconf																			%ncache%\config /Y /Q /S
XCOPY %setup-utilities%\*.xml																				%ncache%\config /Y /Q /S

ECHO COPYING GETTING STARED FILES
ECHO ============================
XCOPY %setup-utilities%\*.md																			  	%ncache%\docs /Y /Q /S



