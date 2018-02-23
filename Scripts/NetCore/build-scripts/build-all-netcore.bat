@ECHO OFF

ECHO BUILDING .NET CORE NCACHE SOURCE 
ECHO ======================
CALL build-ncache-netcore.bat

ECHO BUILDING .NET CORE TOOLS
ECHO ===========================
CALL build-tools-netcore.bat

pause