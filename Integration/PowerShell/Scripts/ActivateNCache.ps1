$server_name;
$Server_UserName;
$Server_UserPassword;

$array=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30

$bKey=$false;
$bFirstName=$false;
$bLastName=$false;
$bEmail=$false;
$bServer_UserName=$false;
$bServer_UserPassword=$false;

$server_name=get-childitem env:computername;
$servers=$server_name.value;
$cmd;

$i=0;

$args | foreach-object { $array[$i]=$_;   $i++ }

if ($args -contains "-?" -or $args -contains "/?" -or $help)
{
@"
NAME
    NActivate
	
SYNOPSIS
    Activates NCache on target machines. 
	
SYNTAX
    ActivateNCache.ps1 /k key /f first-name /l last-name /e email-address /uid server-username /pwd server-password [option[...]]. 

DETAILED DESCRIPTION
    This script Activates NCache on multiple machines. This script uses command line tool 
	NActivate.exe of NCache.

PARAMETERS
    /k key
        Allows the user to specify a valid license key.
		
    /f first-name
        Allows the user to specify his first name.
		
    /l last-name
        Allows the user to specify his last name.
	
    /e email-address
	   Allows the user to specify his email address.
    
    /uid
        Allows the user to specify server machine username.
        
    /pwd	
        Allows the user to specify server machine password.

OPTIONS		
	
    /servers server-names
	Allows the user to specify a comma seperated list of names or IPs of the machines
	on which NCache is to be Activaited.
	
    /company company-name
	Allows the user to specify the company name.
	
    /a address
	Allows the user to specify the company's address.
	
    /city city-name
	Allows the user to specify the company's city name.
	
    /s state
	Allows the user to specify state name.
		
    /c country-name
	Allows the user to specify the country name.
	
    /p phone-number
	Allows the user to specify the company's phone number.
	
    /z zip-code
	Allows the user to specify the zip code.
	
    /? 
	Show this help information.
	
"@
exit 1
}

$i=0;
while($i -lt $args.length)
{
	switch ($array[$i])
	{ 
		"/servers"      
		{
			$servers=$array[$i+1];
			$i++;
			$i++; 
		}
		"/k" 	     	
		{
			$cmd =$cmd +"/k "+$array[$i+1];
			$i++; 
			$bKey=$true;
		}
		"/f" 	        
		{
			$cmd =$cmd +" /f "+$array[$i+1];
			$i++; 
			$bFirstName=$true;
		}
		"/l" 	     	
		{
			$cmd =$cmd +" /l "+$array[$i+1];
			$i++; 
			$bLastName=$true;
		}
		"/e" 	        
		{
			$cmd =$cmd +" /e "+$array[$i+1];
			$i++;
			$bEmail=$true;
		}
		"/company"      
		{
			$cmd =$cmd +" /company "+$array[$i+1];
			$i++;
		}
		"/a" 	        
		{
			$cmd =$cmd +" /a "+$array[$i+1];
			$i++;
		}
		"/city"
		{
			$cmd =$cmd +" /city "+$array[$i+1];
			$i++;
		}
		"/s"
		{
			$cmd =$cmd +" /s "+$array[$i+1];
			$i++;
		}	
		"/c"
		{
			$cmd =$cmd +" /c "+$array[$i+1];
			$i++;
		}
		"/p"
		{
			$cmd =$cmd +" /p "+$array[$i+1];
			$i++; 
		}
		"/z"
		{
			$cmd =$cmd +" /z "+$array[$i+1];
			$i++; 
		}  
		"/uid"
		{
			$Server_UserName=$array[$i+1];
			$i++; 
			$bServer_UserName=$true;
		}
		"/pwd"
		{
			$Server_UserPassword=$array[$i+1];
			$i++; 
			$bServer_UserPassword=$true;
		}
		default 
		{
			$i++;
		}
	}
}

if ($bKey) 
{
	if ($bFirstName) 
	{
		if($bLastName)
		{ 
			if($bEmail)
			{
				if($bServer_UserName)
				{
					if($bServer_UserPassword)
					{
						Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
						$i=0;
						$check = $servers -is [array] ;
						if($check)
						{
							while($i -lt $servers.length)
							{
								$System = $servers[$i];
								echo "Activating NCache on Machine $System"
								echo "Status: "
								winrs -r:$System -u:$Server_UserName -p:$Server_UserPassword "`"$env:NCHOME\bin\NActivate\NActivate.exe`" $cmd"
								$i++;						
							}
						}
						else
						{
							$System = $servers;
							echo "Activating NCache on Machine $System";
							echo "Status: ";
							winrs -r:$System -u:$Server_UserName -p:$Server_UserPassword "`"$env:NCHOME\bin\NActivate\NActivate.exe`" $cmd"
						}
					}
					else 
					{write-host "Argument '/pwd' for Server password is missing" -foregroundcolor  RED;}
				}
				else 
				{write-host "Argument '/uid' for Server username is missing" -foregroundcolor  RED;}
			}
			else 
			{write-host "Argument '/e' for Email is missing" -foregroundcolor  RED;}
		}
		else 
		{write-host "Argument '/l' for lastname is missing" -foregroundcolor  RED;}
	}
	else 
	{write-host "Argument '/f' for firstname is missing" -foregroundcolor  RED;}
}
else 
{write-host "Argument '/k' for valid License Key missing" -foregroundcolor  RED;}