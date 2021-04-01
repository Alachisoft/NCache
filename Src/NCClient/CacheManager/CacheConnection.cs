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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Instance of this class can be used to define the parameters to establish connection with cache.
    /// </summary>
    public class CacheConnection : ICloneable
    {
        /// <summary>
        /// Initializes new instance of CacheConnection
        /// </summary>
        /// <param name="server">Specifies the name of server on which cache is running.</param>
        /// <param name="port">Specifies the port of server on which cache is running.</param>
        public CacheConnection(string server = null, int port = 8250)
        {
            //Server = string.IsNullOrWhiteSpace(server) ? Environment.MachineName: server;
            Server = server;
            Port = port;
        }

        /// <summary>
        /// Name of server on which cache is running.
        /// </summary>
        public string Server { get; private set; }

        /// <summary>
        /// Port of server on which cache is running.
        /// </summary>
        public int Port { get; private set; }
        
        /// <summary>
        /// Method not implemented.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            throw new NotImplementedException();
        }

        
    }
}
