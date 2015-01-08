// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;

//using Alachisoft.NCache.SocketServer;
namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Summary description for NCacheHost.
    /// </summary>
    [CLSCompliant(false)]
	public class CacheHost : HostBase, IDisposable
    {
        public CacheHost(CacheServer server)
            : base(server, CacheServer.ObjectUri)
        {
            CacheServer.Instance = server;
        }

        public CacheHost() : this(new CacheServer()) { }

        protected override int GetHttpPort()
        {
            return CacheConfigManager.HttpPort;
        }

        protected override int GetTcpPort()
        {
            return CacheConfigManager.NCacheTcpPort;
        }

        /// <summary> </summary>
        public CacheServer CacheServer
        {
            get { return _remoteableObject as CacheServer; }
        }

        public void RegisterMonitorServer()
        {

        }
    }


}
