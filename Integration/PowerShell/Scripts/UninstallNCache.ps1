if ($args -contains "-?" -or $args -contains "/?" -or $help)
{
@"
NAME
    UninstallNCache
	
SYNOPSIS
    Uninstalls NCache on target machine(s) in quite mode 
	
SYNTAX
    UninstallNCache.ps1 server-name(s) [option[...]]. 

DETAILED DESCRIPTION
    This script uninstall NCache on multiple machines in quite mode. This script uses win32_Product class
	which is a [WMICLASS].It uses the uninstall method of this class.

	At least one or maximum n number of server-name/IP can be specified.
	Server names must be separated with BLANK space.

OPTIONS	
	/? 
	Show this help information.

"@
exit 1
}

if ($args.length -eq 0)
{
	"No server name/IP is specified."
exit 1	
}

$args | foreach-object{
$System = "$_"
echo "Uninstalling NCache from Machine $_";
echo "Status:"
$Wmi_obj=Get-WmiObject -Class Win32_Product -computername $System | Where-Object {$_.Name -eq "NCache"}
$Wmi_obj.Uninstall()
}