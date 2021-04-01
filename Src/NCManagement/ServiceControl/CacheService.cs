//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Net.Sockets;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.ServiceControl
{
    /// <summary>
    /// Represents the NCache remoting objects and allows you to start the service and get
    /// and instance of the CacheServer on a node.
    /// </summary>
    [CLSCompliant(false)]
    public class CacheService : ServiceBase
    {
        private string _serviceName;
        protected ICacheServer cacheServer;

        public event EventHandler<CredentialsEventArgs> OnGetSecurityCredentials = delegate { };
        private string address;
        private bool p;

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="server">name of machine where the service is running.</param>
        /// <param name="port">port used by the remote server.</param>
        /// <param name="useTcp">use tcp channel for remoting.</param>
        public CacheService(string serviceName,string server, long port):base(server,port,true) { this._serviceName = serviceName;}

        public CacheService(string server, int port, bool useTCP)  : base(server, port, useTCP) { }

        public CacheService(string address, bool p) : this(address, CacheConfigManager.HttpPort, true) { }

        public ICacheServer CacheServer { get { return this.cacheServer; } }

        /// <summary>
        /// Returns the instance of Cache manager running on the node, starts the service
        /// if not running.
        /// </summary>
        /// <returns></returns>
        public virtual ICacheServer GetCacheServer(TimeSpan timeout)
        {
            ICacheServer cm = null;
            try
            {
                cm = ConnectCacheServer();
            }
            catch (SocketException socketException)
            {

                if (socketException.SocketErrorCode == SocketError.TimedOut) //Machine is not accesible my be should down or we cannot reach it so no need to start service
                {
                    throw new ManagementException(socketException.Message, socketException);
                }
                try
                {
                    Start(timeout);
                    cm = ConnectCacheServer();

                }
                catch (ManagementException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }
            }
            catch (Exception exception)
            {
                try
                {
                    Start(timeout);

                    cm = ConnectCacheServer();
                 
                }
                catch (ManagementException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }
            }
            return cm;
        }



        public virtual void RestartSvcAfterNICChanged(TimeSpan timeout, string previousServerNode)
        {
            Restart(timeout);
        }

        /// <summary>
        /// Returns a running instance of CacheServer; does not start the service.
        /// If user specifies in the client application an ip address to server channels to bind to
        /// then this method creates the server channel bound to that ip. also the object uri then uses 
        /// the ip address instead of server name.
        /// </summary>
        /// <returns></returns>
        public virtual ICacheServer ConnectCacheServer()
        {
            return null;
        }

        /// <summary>
        /// Starts the Cache service on target machine.
        /// </summary>
        public void Start(TimeSpan timeout)
        {
            Start(timeout, _serviceName);
        }

        /// <summary>
        /// Stops the Cache service on target machine.
        /// </summary>
        public void Stop(TimeSpan timeout)
        {
            Stop(timeout, _serviceName);
        }
        /// <summary>
        /// Restart the NCache service on target machine.
        /// </summary>
        public void Restart(TimeSpan timeout)
        {
            Restart(timeout, _serviceName);
        }

        public bool isRunning(TimeSpan timeout)
        {
            return isServiceRunning(timeout, _serviceName);
        }

        public void GetSecurityCredentials(object sender,CredentialsEventArgs e)
        {
            OnGetSecurityCredentials(sender, e);
        }
    }
}