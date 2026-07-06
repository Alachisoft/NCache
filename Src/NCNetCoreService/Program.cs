using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

namespace Alachisoft.NCache.NetCore.Service
{

    /// <summary>
    /// Entry point for the NCache Windows Service application.
    /// Configures the generic host, registers the background service,
    /// sets up Windows Service lifetime and Event Log integration,
    /// and starts the service.
    /// </summary>
    public class Program 
    {
        static void Main(string[] args)
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
                throw;

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
           }).UseWindowsServiceInContainer();
    }

    public static class HostBuilderExtension
    {
        public static IHostBuilder UseWindowsServiceInContainer(this IHostBuilder hostBuilder)
        {
            hostBuilder.UseContentRoot(AppContext.BaseDirectory);
            hostBuilder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddEventLog();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IHostLifetime, WindowsServiceLifetime>();
                services.Configure<EventLogSettings>(settings =>
                {
                    if (string.IsNullOrEmpty(settings.SourceName))
                    {
                        settings.SourceName = hostContext.HostingEnvironment.ApplicationName;
                    }
                });
            });

            return hostBuilder;
        }
    }
}
