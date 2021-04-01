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
using Alachisoft.NCache.Common.RPCFramework;
using Alachisoft.NCache.Common.RPCFramework.DotNetRPC;
using Alachisoft.NCache.Management;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// This class provides a static cache server instance
    /// </summary>
    public sealed class CacheProvider
    {
        private static CacheServer _cacheServer;
        private static RPCService<CacheServer> _managementRpcService;

        /// <summary>
        /// Get/Set RPC Service instance for Cache Server
        /// </summary>
        public static RPCService<CacheServer> ManagementRpcService
        {
            get { return _managementRpcService; }
        }

        /// <summary>
        /// Get/Set static instance of Cache server 
        /// </summary>
        public static CacheServer Provider
        {
            get { return _cacheServer; }
            set
            {
                _cacheServer = value;
                _managementRpcService = new Common.RPCFramework.RPCService<CacheServer>(new TargetObject<CacheServer>(value));
            }
        }
    }
}