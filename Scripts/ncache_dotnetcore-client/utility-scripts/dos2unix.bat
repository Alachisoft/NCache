@ECHO OFF

Powershell.exe -ExecutionPolicy Bypass -File "%UTILITYSCRIPTSPATH%\dos2unix.ps1" -Container "%SETUPUTILITIESPATH%"

DEL "%SETUPUTILITIESPATH%\install"
REN "%SETUPUTILITIESPATH%\install1" install

DEL "%SETUPUTILITIESPATH%\uninstall"
REN "%SETUPUTILITIESPATH%\uninstall1" uninstall

DEL "%SETUPUTILITIESPATH%\LICENSE"
REN "%SETUPUTILITIESPATH%\LICENSE1" LICENSE

DEL "%SETUPUTILITIESPATH%\README"
REN "%SETUPUTILITIESPATH%\README1" README

DEL "%SETUPUTILITIESPATH%\Alachisoft.NCache.Daemon.dll.config"
REN "%SETUPUTILITIESPATH%\Alachisoft.NCache.Daemon.dll.config1" Alachisoft.NCache.Daemon.dll.config

DEL "%SETUPUTILITIESPATH%\client.ncconf"
REN "%SETUPUTILITIESPATH%\client.ncconf1" client.ncconf 

DEL "%SETUPUTILITIESPATH%\config.ncconf"
REN "%SETUPUTILITIESPATH%\config.ncconf1" config.ncconf

DEL "%SETUPUTILITIESPATH%\NCLicense.xml"
REN "%SETUPUTILITIESPATH%\NCLicense.xml1" NCLicense.xml

DEL "%SETUPUTILITIESPATH%\ncached.service"
REN "%SETUPUTILITIESPATH%\ncached.service1" ncached.service
