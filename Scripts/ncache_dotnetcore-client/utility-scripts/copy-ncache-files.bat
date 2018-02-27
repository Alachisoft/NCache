@ECHO OFF

::_________________________________________::
::_____SETTING SOME VARIABLES FOR HELP_____::
::_________________________________________::

SET ncache=%~1
SET SRCPATH=..\..\Src
SET TOOLSPATH=..\..\Tools

::_________________________________________::
::________CARRYING OUT SCRIPT WORK_________::
::_________________________________________::

ECHO COPYING REQUIRED DLLs TO LIB FOLDER
ECHO ===================================
XCOPY "%SRCPATH%\NCWebCache\bin\Release\netstandard2.0\netstandard2.0\Alachisoft.*.dll" 				"%ncache%\lib" /Y /Q /S
XCOPY "%SRCPATH%\NCSocketServer\bin\Release\netstandard2.0\Alachisoft.*.dll" 							"%ncache%\lib" /Y /Q /S

ECHO COPYING REQUIRED DEPENDENCIES TO LIB FOLDER
ECHO ===========================================
XCOPY "%SRCPATH%\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.dll"							"%ncache%\lib" /Y /Q /S
XCOPY "%SRCPATH%\NCDaemon\bin\Release\netcoreapp2.0\publish\*.dll" 										"%ncache%\lib" /Y /Q /S
DEL	"%ncache%\lib\*.CacheHost.*"
DEL	"%ncache%\lib\*.Daemon.*"
RMDIR "%ncache%\lib\runtimes" /Q /S

ECHO COPYING REQUIRED FILES FOR SERVICE
ECHO ===================================
XCOPY "%SRCPATH%\NCDaemon\bin\Release\netcoreapp2.0\publish\*.Daemon.dll" 								"%ncache%\bin\service" /Y /Q /S
XCOPY "%SRCPATH%\NCDaemon\bin\Release\netcoreapp2.0\publish\*.Daemon.runtimeconfig.json" 				"%ncache%\bin\service" /Y /Q /S
XCOPY "%SETUPUTILITIESPATH%\*.Daemon.*"																	"%ncache%\bin\service" /Y /Q /S
XCOPY "%SETUPUTILITIESPATH%\ncached.service"															"%ncache%\bin\service" /Y /Q /S

ECHO COPYING REQUIRED FILES FOR CACHE HOST
ECHO =====================================
XCOPY "%SRCPATH%\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.dll" 				"%ncache%\bin\service" /Y /Q /S
XCOPY "%SRCPATH%\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.runtimeconfig.json" 	"%ncache%\bin\service" /Y /Q /S
XCOPY "%SRCPATH%\NCCacheSeparateHost\bin\Release\netcoreapp2.0\publish\*.CacheHost.dll.config" 			"%ncache%\bin\service" /Y /Q /S

ECHO COPYING REQUIRED FILES FOR TOOLS
ECHO ================================
XCOPY "%TOOLSPATH%\NCAutomation\bin\Release\netcoreapp2.0\ncacheps.dll" 							    "%ncache%\bin\ncacheps" /Y /Q /S
XCOPY "%TOOLSPATH%\ncacheps\*.xml"																		"%ncache%\bin\ncacheps" /Y /Q /S
XCOPY "%SETUPUTILITIESPATH%\*.psd1"																		"%ncache%\bin\ncacheps" /Y /Q /S

ECHO COPYING CONFIG FILES
ECHO ====================
XCOPY "%SETUPUTILITIESPATH%\*.ncconf"																	"%ncache%\config" /Y /Q /S
XCOPY "%SETUPUTILITIESPATH%\*.xml"																		"%ncache%\config" /Y /Q /S

ECHO COPYING GETTING STARED FILES
ECHO ============================
XCOPY "%SETUPUTILITIESPATH%\*.md"																		"%ncache%\docs" /Y /Q /S
