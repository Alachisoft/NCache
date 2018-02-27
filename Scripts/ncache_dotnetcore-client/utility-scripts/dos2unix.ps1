PARAM([String]$Container)

Get-Content "$Container\install" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\install1"
Get-Content "$Container\uninstall" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\uninstall1"
Get-Content "$Container\LICENSE" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\LICENSE1"
Get-Content "$Container\README" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\README1"
Get-Content "$Container\Alachisoft.NCache.Daemon.dll.config" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\Alachisoft.NCache.Daemon.dll.config1"
Get-Content "$Container\client.ncconf" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\client.ncconf1"
Get-Content "$Container\config.ncconf" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\config.ncconf1"
Get-Content "$Container\NCLicense.xml" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\NCLicense.xml1"
Get-Content "$Container\ncached.service" -raw | % {$_ -replace "`r", ""} | Set-Content -NoNewline "$Container\ncached.service1"
