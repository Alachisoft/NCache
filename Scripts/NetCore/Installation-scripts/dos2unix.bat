@ECHO OFF

Powershell.exe -File dos2unix.ps1

DEL setup-utilities\install
REN setup-utilities\install1 install

DEL setup-utilities\uninstall
REN setup-utilities\uninstall1 uninstall

DEL setup-utilities\LICENSE
REN setup-utilities\LICENSE1 LICENSE

DEL setup-utilities\README
REN setup-utilities\README1 README

DEL setup-utilities\Alachisoft.NCache.Daemon.dll.config
REN setup-utilities\Alachisoft.NCache.Daemon.dll.config1 Alachisoft.NCache.Daemon.dll.config

DEL setup-utilities\client.ncconf 
REN setup-utilities\client.ncconf1 client.ncconf 

DEL setup-utilities\config.ncconf
REN setup-utilities\config.ncconf1 config.ncconf

DEL setup-utilities\NCLicense.xml
REN setup-utilities\NCLicense.xml1 NCLicense.xml

DEL setup-utilities\ncached.service
REN setup-utilities\ncached.service1 ncached.service
