NCache ASP.NET Core Distributed Cache

Requirements:
	=> The distributed cache services require an installation of NCache 4.6 SP3 Enterprise or higher versions on the target machine to work.

Contents:
	The package contains the following module.
		=> NCache Distributed Cache: A distributed cache module that implements IDistributedCache and uses NCache. This can 
		be used with default session management module of ASP.NET Core
		
Instructions:
	=> Open up the Startup.cs of your application.

		
	=> In the ConfigureServices(IServiceCollection services) method initialize the NCache Distributed Cache by calling
					
			services.AddNcacheDistributedCache(configuration => { configuration.CacheName = "UserCache"; });
					
		Replace the "UserCache" with the name of your cache. The cache name is mandatory.
		If a call is already present to services.AddDistributedMemoryCache(), it is recommended to remove it as it will 
		interfere with NCache Distributed Cache.
			
			
	=> Run your application.