using PeterKottas.DotNetCore.WindowsService;
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.NetCore.Service
{
    public class Program 
    {
        public static void Main(string[] args)
        {
            //var fileName = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "log.txt");
            ServiceRunner<NCacheService>.Run(config =>
            {
                var name = "NCacheSvc"/*config.GetDefaultName()*/;
                config.SetName(name);
                config.SetName("NCache Service");
                config.SetDescription("NCache Service");
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new NCacheService(controller);
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        Console.WriteLine("Service {0} started", name);
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        Console.WriteLine("Service {0} stopped", name);
                        service.Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                       // File.AppendAllText(fileName, $"Exception: {e.ToString()}\n");
                        Console.WriteLine("Service {0} errored with exception : {1}", name, e.Message);
                    });
                });
            });
        }
    }
}
