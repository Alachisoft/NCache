// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Web.RemoteClient.Config;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Holds the set of parameters that control the initialization behavior of the cache.
    /// </summary>

    /// <summary>
    /// Provides the properties to aid the ServerList property in CacheInitParams
    /// </summary>
    public class CacheServerInfo : IComparable
    {
        private RemoteServer _serverInfo;
        
        /// <summary>
        ///Initializes the instance of <see cref="Alachisoft.NCache.Web.Caching.CacheServerInfo"/> for this application.
        /// </summary>
        /// <param name="name">String IP/Name of the server to be connected with</param>
        /// <param name="port">Port for the server to be connected with </param>
        public CacheServerInfo(string name, int port)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name/IP cannot be null");
            }
            if (port < 1)
                throw new ArgumentException("Invalid value of port.Port cannot be less than 0");
            ServerInfo = new RemoteServer(name, port);
                    
        }

        /// <summary>
        /// Initializes the instance of <see cref="Alachisoft.NCache.Web.Caching.CacheServerInfo"/> for this application.
        /// </summary>
        internal CacheServerInfo()
        {
            ServerInfo = new RemoteServer();
        }
        
        internal RemoteServer ServerInfo
        {
            get { return _serverInfo; }
            set { _serverInfo = value; }
        }
        
        /// <summary>
        /// Get/Set the Port 
        /// </summary>
        public int Port
        {
            get { return ServerInfo.Port; }
        }

        internal bool IsUserProvided
        {
            get { return ServerInfo.IsUserProvided; }
            set { ServerInfo.IsUserProvided = value; }
        }

        /// <summary>
        /// Get the Name/IP of the server
        /// </summary>
        public string Name
        {
            get { return ServerInfo.Name; }
         
        }
        
        public override bool Equals(object obj)
        {
            return ServerInfo.Equals(obj);
        }
        
        public override string ToString()
        {
            return ServerInfo.ToString();
        }

        public int CompareReal(object o)
        {
            return ServerInfo.CompareReal(o);
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return ServerInfo.CompareTo(obj);
        }

        #endregion
    }
}
