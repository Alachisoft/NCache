//using PeterKottas.DotNetCore.WindowsService;

using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace Alachisoft.NCache.NetCore.Service
{
    public class Program 
    {
        /// <summary>
        /// Assosicated with Net Core Service of NCache
        /// Pass no argument for running it as a console application.
        /// action:install to install and run service.
        /// action:start for starting service.
        /// action:stop for stopping service.
        /// action:uninstall for uninstalling service.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
            ServiceMain(args);
            
        }

        static void ServiceMain(string [] args)
        {
            PeterKottas.DotNetCore.WindowsService.ServiceRunner<NCacheService>.Run(config =>
            {
                var serviceName = "NCacheSvc";
                var displayName = "NCache";
                var description = "Provides out-proc caching and clustering. Allows local and remote management of NCache configuration.";

                config.SetName(serviceName);
                config.SetDisplayName(displayName);
                config.SetDescription(description);
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new NCacheService(controller);
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        Console.WriteLine("Service {0} started", serviceName);
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        Console.WriteLine("Service {0} stopped", serviceName);
                        service.Stop();

                    });

                    serviceConfig.OnError(e =>
                    {
                        Console.WriteLine("Service {0} errored with exception : {1}", serviceName, e.Message);
                    });
                });
            });
        }

        private static Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string final = "";
                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string bin = directoryInfo.Parent.FullName; /// in bin folder
                string assembly = Path.Combine(bin, "assembly"); /// in assembly folder 
                final = Path.Combine(assembly, "netcore20"); /// from where you neeed the assemblies
                
                return Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
