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
using System.Collections;

using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Config.Dom;
using Renci.SshNet.Common;
using Alachisoft.NCache.Management.ServiceControl;

namespace Alachisoft.NCache.Management
{
    public class CacheServiceClient
    {
        /// <summary> CacheServer object running on other nodes. </summary>
        /// 

        //protected ICacheServer _server = null;
        protected ICacheServer _server = null;

        /// <summary> Address of the machine. </summary>
        protected string _address;
        /// <summary> Port </summary>
        protected int _port;
        /// <summary> Cluster IP address </summary>
        private string _clusterIpAddress;
        /// <summary> Bind Ip address </summary>
        private string _bindIpAddress;       
        /// <summary> User Id used for authentication and authorization </summary>
        private string _userId = Security.UserName;
        /// <summary> Password used for authentication and authorization </summary>
        private string _password = Security.Passwd;

        private bool _useRemoting = false;

        Action<CredentialsEventArgs> _getCacheCredentials;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address"></param>
        public CacheServiceClient(string address)
            : this(address, RuntimeContext.CurrentContext == RtContextValue.JVCACHE ? CacheConfigManager.JvCacheTcpPort : CacheConfigManager.NCacheTcpPort , null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address"></param>
        public CacheServiceClient(string address, Action<CredentialsEventArgs> getCredentialsAction)
            : this(address, RuntimeContext.CurrentContext == RtContextValue.JVCACHE ? CacheConfigManager.JvCacheTcpPort : CacheConfigManager.NCacheTcpPort, getCredentialsAction)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public CacheServiceClient(string address, int port, Action<CredentialsEventArgs> getCredentialsAction)
        {
           
            _address = address;
            _port = port;
            _getCacheCredentials = getCredentialsAction;

            Initialize();

            _bindIpAddress = _server.GetBindIP();
            _clusterIpAddress = _server.GetClusterIP();

        }
        /// <summary>
        /// initilaise
        /// </summary>
        protected virtual void Initialize()
        {

            CacheService cacheService = null;
          
            if (RuntimeContext.CurrentContext == RtContextValue.JVCACHE)
            {
                cacheService = new JvCacheRPCService(_address, _port);
                cacheService.OnGetSecurityCredentials += new EventHandler<CredentialsEventArgs>(OnGetSecurityCredentials);   
             
            }
            else
            {
                cacheService = new NCacheRPCService(_address, _port);
            }

            try
            {
                _server = cacheService.GetCacheServer(TimeSpan.FromSeconds(7));
            }
            catch (SshAuthenticationException)
            {
                throw new Exception("Could not authenticate on server. Incorrect Username or Password.");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                cacheService.Dispose();
            }
        }

        public void OnGetSecurityCredentials(object sender, CredentialsEventArgs e)
        {            
            if (_getCacheCredentials != null)
                _getCacheCredentials(e);
            else
            {
                return;
            }
        }

        protected ICacheServer CacheServer
        {
            get { return _server; }
        }

        public string BindIP
        {
            get
            { return _bindIpAddress; }
        }

        public string ClusterIP
        {
            get { return _clusterIpAddress; }
        }

        /// <summary>
        /// Get the list of running server caches
        /// </summary>
        /// <returns></returns>
        public virtual Hashtable GetServerCaches()
        {
            return GetServerCaches(_userId, _password);
        }

        /// <summary>
        /// Get the list of running server caches
        /// </summary>
        /// <returns></returns>
        public virtual Hashtable GetServerCaches(string userId, string password)
        {
            try
            {
                Hashtable cacheList = new Hashtable();
                IDictionary coll = _server.GetCacheProps();

                IDictionaryEnumerator ie = coll.GetEnumerator();
                while (ie.MoveNext())
                {
                    if (ie.Value is CacheServerConfig)
                    {
                        CacheServerConfig cacheProp = (CacheServerConfig)ie.Value;
                        if (cacheProp.Cluster != null)
                        {
                            if ((cacheProp.Cluster.Topology == "partitioned-server")
                                || (cacheProp.Cluster.Topology == "replicated-server")
                                || (cacheProp.Cluster.Topology == "partitioned-replicas-server")
                                || (cacheProp.Cluster.Topology == "mirror-server"))
                            {
                                cacheList.Add(ie.Key, cacheProp);
                            }
                        }
                    }
                    else if (ie.Value is Hashtable)
                    {
                        cacheList.Add(ie.Key, ie.Value);
                    }
                }
                return cacheList;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
