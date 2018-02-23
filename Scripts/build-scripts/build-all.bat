@ECHO OFF

ECHO BUILDING NCACHE SOURCE
ECHO ======================
CALL build-ncache.bat

ECHO BUILDING COMMAND LINE TOOLS
ECHO ===========================
CALL build-tools.bat

ECHO BUILDING INTEGRATIONS
ECHO =====================
CALL build-integrations.bat

ECHO COPYING POWERSHELL SCRIPTS
ECHO =====================
CALL copy-powershell-scripts.bat