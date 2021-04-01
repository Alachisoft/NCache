using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;

namespace Alachisoft.NCache.NetCore.Service
{
    public class Program 
    {
       
        public static void Main(string[] args)
        {
            try
            {
                var serviceName = "NCacheSvc";
                var displayName = "NCache";
                var description = "Provides out-proc caching and clustering. Allows local and remote management of NCache configuration.";
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {


            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<NCacheService>()
            .Configure<EventLogSettings>(config =>
            {
                config.LogName = "NCacheSvc";
                config.SourceName = "NCache";
            });
        }).UseWindowsService();
    }
}
