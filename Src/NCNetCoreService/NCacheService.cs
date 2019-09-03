using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.NetCore.Service
{
    public class NCacheService : IMicroService
    {
        private Alachisoft.NCache.SocketServer.ServiceHost _serviceHost = new Alachisoft.NCache.SocketServer.ServiceHost();
        private IMicroServiceController _controller;
        private string[] _args;
         
        public NCacheService()
        {
            _controller = null;
        }

        public NCacheService(IMicroServiceController controller)
        {
            _controller = controller;
        }

        public NCacheService(string[] args)
        {
            _args = args;
        }

        public void Start()
        {
            _serviceHost.Start(_args);
        }

        public void Stop()
        {
            _serviceHost.Stop();
            if (_controller != null)
            {
                _controller.Stop();
            }
        }
    }
}
