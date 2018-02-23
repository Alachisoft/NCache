Get-Content setup-utilities\install -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\install1
Get-Content setup-utilities\uninstall -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\uninstall1
Get-Content setup-utilities\LICENSE -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\LICENSE1
Get-Content setup-utilities\README -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\README1
Get-Content setup-utilities\Alachisoft.NCache.Daemon.dll.config -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\Alachisoft.NCache.Daemon.dll.config1
Get-Content setup-utilities\client.ncconf -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\client.ncconf1
Get-Content setup-utilities\config.ncconf -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\config.ncconf1
Get-Content setup-utilities\NCLicense.xml -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\NCLicense.xml1
Get-Content setup-utilities\ncached.service -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline setup-utilities\ncached.service1


