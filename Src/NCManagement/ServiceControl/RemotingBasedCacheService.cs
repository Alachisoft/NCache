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
using Alachisoft.NCache.ServiceControl;
using System.Net.Sockets;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Remoting;
#if !NETCORE
using System.Runtime.Remoting;
#endif
using Alachisoft.NCache.Common.Util;


namespace Alachisoft.NCache.Management.ServiceControl
{
    /// <summary>
    /// Represents the NCache remoting objects and allows you to start the service and get
    /// and instance of the CacheServer on a node.
    /// </summary>
    [CLSCompliant(false)]
	public class RemotingBasedCacheService : CacheService
    {
        string _cacheserver="NCache";
        static string _servicename = "NCacheSvc";
        /// <summary>
        /// Constructor
        /// </summary>
        public RemotingBasedCacheService(string server):this(server,true)
        { }

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="server">name of machine where the service is running.</param>
        /// <param name="useTcp">use tcp channel for remoting.</param>

        public RemotingBasedCacheService(string server, bool useTcp)
            : this(server, useTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort, useTcp)
        {
        }

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="server">name of machine where the service is running.</param>
        /// <param name="port">port used by the remote server.</param>
        /// <param name="useTcp">use tcp channel for remoting.</param>
        public RemotingBasedCacheService(string server, long port, bool useTcp)
            : base(_servicename, server, port)
        {
        }

        /// <summary>
        /// Returns a running instance of CacheServer; does not start the service.
        /// If user specifies in the client application an ip address to server channels to bind to
        /// then this method creates the server channel bound to that ip. also the object uri then uses 
        /// the ip address instead of server name.
        /// </summary>
        /// <returns></returns>
#if !NETCORE
        public override ICacheServer ConnectCacheServer()
        {
            string objectUri = "";
            string protocol;
            string ip = "";

            try
            {
                if (_channel == null)
                {
                    _channel = new RemotingChannels();


                    ip = ServiceConfiguration.BindToIP.ToString();

                    if (ip != null && ip.Length > 0)
                    {
                        if (_useTcp)
                            _channel.RegisterTcpChannels("NCC", ip, 0);
                        else
                            _channel.RegisterHttpChannels("NCC", ip, 0);
                    }
                    else
                    {
                        if (_useTcp)
                            _channel.RegisterTcpChannels("NCC", 0);
                        else
                            _channel.RegisterHttpChannels("NCC", 0);
                    }
                }
                protocol = _useTcp ? @"tcp://" : @"http://";

                if (ip != null && ip.Length > 0)
                {
                    objectUri = protocol + ip + ":" + _port + @"/" +
                    CacheHost.ApplicationName + @"/" + Alachisoft.NCache.Management.CacheServer.ObjectUri;
                }
                else
                {
                    objectUri = protocol + _serverName + ":" + _port + @"/" +
                    CacheHost.ApplicationName + @"/" + Alachisoft.NCache.Management.CacheServer.ObjectUri;
                }

                CacheServer server = (CacheServer)

                    Activator.GetObject(typeof(CacheServer), objectUri, WellKnownObjectMode.Singleton);

                int ccount = server.Caches.Count;

                return server;
            }

            catch (SocketException socketException)
            {
                throw;
            }
            catch (ManagementException mexp)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }
#endif
    }
}
