NCache ASP.NET Core Session State Services

Requirements:
	=> The session store services only work for ASP.NET Core that uses .NET Framework as the target runtime.
	
	=> It is required to build Src before building this Integration. Use build-scripts "build-ncache-netcore.bat" to build net core project and "build-ncache.bat" to build framework Ncache Server available in scripts folder.

Contents:
	The package contains the following two main modules.
		=> Session Storage Services: Complete session management service and middleware that utilizes NCache as it's cache. 
		
		=> NCache Distributed Cache: A distributed cache module that implements IDistributedCache and uses NCache. This can 
		be used with default session management module of ASP.NET Core
		
Instructions:
	Session Storage Services:
		=> Open up the Startup.cs of your application.
				
		=> In the ConfigureServices(IServiceCollection services) method initialize the NCache Session Storage Services by calling
		
					services.AddNCacheSession(configuration => { configuration.CacheName = "UserCache"; });
					
			Replace the "UserCache" with the name of your cache. The cache name is mandatory.
			If a call is already present to services.AddSession(), it is recommended to remove it as it will interfere with NCache Session Services.
			
			
		=> In the Configure(IApplicationBuilder app) method, add the following to your pipeline before the middleware that calls your code
					
		            app.UseNCacheSession();
					
			If a call is already present to app.UseSession(), it is recommended to remove it as it will interfere with NCache Session Services.
					
		=> Run your application.
		
	NCache Distributed Cache
		=> Open up the Startup.cs of your application.

		
		=> In the ConfigureServices(IServiceCollection services) method initialize the NCache Distributed Cache by calling
					
					services.AddNcacheDistributedCache(configuration => { configuration.CacheName = "UserCache"; });
					
			Replace the "UserCache" with the name of your cache. The cache name is mandatory.
			If a call is already present to services.AddDistributedMemoryCache(), it is recommended to remove it as it will interfere with NCache Distributed Cache.
			
			
		=> Run your application.