using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alachisoft.NCache.SocketServer;

namespace Alachisoft.NCache.NetCore.Service
{
    public class NCacheService : BackgroundService
    {
        private Alachisoft.NCache.SocketServer.ServiceHost _serviceHost = new Alachisoft.NCache.SocketServer.ServiceHost();
       
        public NCacheService()
        {
            
        }



        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceHost.Start();
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _serviceHost.Stop();
            return Task.CompletedTask;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
