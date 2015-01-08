$server_name;
$Server_Username;
$Server_Userpassword;

$array=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30

$bCache_Id=$false;
$bServer_Username=$false;
$bServer_UserPassword=$false;
$cacheId;
$bCacheId=$false;
$bSize=$false;
$bServers=$false;
$topology = "local-cache";

$server_name=get-childitem env:computername;
$servers=$server_name.value;
$cmd;

$i=0;

$args | foreach-object { $array[$i]=$_;   $i++ }

if ($args -contains "-?" -or $args -contains "/?" -or $help)
{
@"
NAME
    CreateCache
	
SYNOPSIS
    Creates cache as per specified cache id.
	
SYNTAX
    CreateCache.ps1 cache-id /uid server-username /pwd server-password [option[...]]. 

DETAILED DESCRIPTION
    This script Create cache. This script uses command line tool 
	CreateCache.exe of NCache.

PARAMETERS
    /cacheid 
        Specifies a unique id of cache to be created on given server.
        A cache with given id is created on the server.
    
    /uid
        Allows the user to specify server machine username.
        
    /pwd	
        Allows the user to specify server machine password.
	
	/size cache-size
       Specifies the size(MB) of the cache to be created.
	
	/s server-name
       Specifies comma separated server name where NCache is installed and 'NCache service' is running. 

OPTIONS		
	
    /p port
       Specifies the port if the server channel is not using the default port.
       The default is 8251 for http and 8250 for tcp channels
	
    /evict-policy eviction-policy
    /e
        Specifies the eviction policy for cache items. Cached items will be
        cleaned from the cache according to the specified policy if the cache
        reaches its limit. Possible values are
            i.   Priority
            ii.  LFU
            iii. LRU (default)
		
    /ratio eviction-ratio
    /r
        Specifies the eviction ratio(Percentage) for cache items. Cached items will be
        cleaned from the cache according to the specified ratio if the cache
        reaches its limit. Default value is 5 (percent)
	
    /interval clean-interval
    /i
        Specifies the time interval(seconds) after which cache eviction is called.
        Default clean-interval is 15 (seconds)

    /topology topology-name
    /t
         Specifies the topology in case of clustered cache. Possible values are
             i.   local-cache (default)
             ii.  mirror
             iii. replicated
             iv.  partitioned
             iv.  partitioned-replicas-server
         
    /replication-strategy
    /rs
        Only in case of 'partition-replicas-server' being the topology, this specifies 
        the replication strategy
            i.   async (default)
            ii.  sync
	
    /cluster-port
    /c
        Specifies the port of the server, at which server listens. Default is 7800
        
	/def-priority default-priority
    /d
        Specifies the default priority in case of priority based eviction policy is selected.

        Possible values are
            i.   high
            ii.  above-normal
            iii. normal (default)
            iv.  below-normal
            v.   low
    
    /nologo
        Suppresses display of the logo banner
   
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
		"/cacheid"
		{
			$cacheId=$array[$i+1];
			$i++; 
			$bCacheId=$true;
		}
		"/s"
		{
			$servers=$array[$i+1];
			$cmd= $cmd +" /s "+ $servers;
			$i++;
			$bServers=$true;
		}
		"/p"
		{
			$cmd= $cmd + " " + "/p"+$array[$i+1];
			$i++;
		}
		"/size"
		{
			$cmd= $cmd +" /S "+$array[$i+1];
			$i++;
			$bSize=$true;
		}
		"/e" 
		{
			$cmd= $cmd + " /e "+$array[$i+1];
			$i++;
		}
		"/r" 
		{
			$cmd= $cmd + " /r "+$array[$i+1];
			$i++;
		}
		"/i" 
		{
			$cmd= $cmd + " /i "+$array[$i+1];
			$i++;
		}
		"/t"
		{
			$topology=$array[$i+1];
			$cmd= $cmd + " /t "+$array[$i+1];
			$i++;
		}
		"/rs"
		{
			$cmd= $cmd + " /rs "+$array[$i+1];
			$i++;
		}
		"/c"
		{
			$cmd= $cmd + " /c "+$array[$i+1];
			$i++;
		}
		"/d"
		{
			$cmd= $cmd + " /d "+$array[$i+1];
			$i++;
		}
		"/uid"
		{
			$Server_Username=$array[$i+1];
			$i++;
			$bServer_Username=$true;
		}
		"/pwd"
		{
			$Server_Userpassword=$array[$i+1];
			$i++; 
			$bServer_UserPassword=$true;
		}
		"/nologo"
		{
			$cmd= $cmd + " /nologo ";
			$i++;
		}
		default 
		{
			$i++;
		}
	}
}

$cmd= $cacheID+$cmd;

if($bCacheId -eq $false)
{
    write-host "Argument '/cacheid' for cache-id is missing" -foregroundcolor  RED;
	exit 1;
}

if($bServer_Username -eq $false)
{
    write-host "Argument '/uid' for Server username is missing" -foregroundcolor  RED;
	exit 1;
}

if($bServer_UserPassword -eq $false)
{
    write-host "Argument '/pwd' for Server password is missing" -foregroundcolor  RED;
	exit 1;
}

if($bServers -eq $false)
{
	write-host "Argument '/s' for Server  is missing" -foregroundcolor  RED;
	exit 1;
}
if($bSize -eq $false)
{
	write-host "Argument '/size' for Cache Size is missing" -foregroundcolor  RED;
	exit 1;
}

Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
$check=$servers -is [array]

if($check) 
	{
	if ($topology -ne "local-cache") 
	{ 
	 winrs -r:$server_name -u:$Server_UserName -p:$Server_UserPassword "Powershell createcache $cmd";
	 $i=1;
	 while($i -lt $servers.length)
	   	  {
		   $command=" addnode $cache_Id /e "+ $servers[0] + " /n " + $servers[$i]
		   winrs -r:$server_name -u:$Server_Username -p:$Server_Userpassword "Powershell $command";  
		   $i++
		  }
	}
	else {"Topology can not be local-cache for a Cluster"; 	  }
	}
else{
     $command =" createcache $cmd"
	 winrs -r:$servers -u:$Server_Username -p:$Server_Userpassword "Powershell $command"
	}
" "