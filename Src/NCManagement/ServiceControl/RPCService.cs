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
using Alachisoft.NCache.Management.RPC;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Management.ServiceControl
{
    public class RPCService : CacheService
    {
        public int retries = 1;

        static RPCService() { Alachisoft.NCache.Management.CacheServer.RegisterCompactTypes(); }

        public RPCService(string serivceName, string address, int port) : base(serivceName, address, port) 
        {
            
        }
        static bool _enableTracing;
        public static bool EnableTracing 
        { 
            get{return _enableTracing;}
            set { _enableTracing = value; }
        }

        private void TryIntializeServer(TimeSpan timeout)
        {
            try
            {

                ConsoleTraceProvider traceProvider = EnableTracing ? new ConsoleTraceProvider() : null;
               ((RemoteCacheServer)cacheServer).Initialize(traceProvider);

            }
            catch (Exception e)
            {
                if (retries-- > 0)
                {
                    Start(timeout);
                    System.Threading.Thread.Sleep(3000);
                    TryIntializeServer(timeout);
                }
                else
                    throw new ManagementException(e.Message, e);

            }
            finally
            {
                retries = 1;
            }
        }

        public override ICacheServer GetCacheServer(TimeSpan timeout)
        {
            cacheServer = new RemoteCacheServer(ServerName, (int)Port);
            TryIntializeServer(timeout);
            return cacheServer;
        }
    }
}
