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
using System.Collections;
using Alachisoft.NCache.Web.RemoteClient.Config;

namespace Alachisoft.NCache.Web.Caching
{
    public class CacheInitParams: ICloneable

    {
        private CacheMode           _mode = CacheMode.Default;
        private string              _server = null;
        private int                 _port = 9800;

        /* no user choice should be given for client optimization i-e multiPartitionConnection will be true by defult */
        private bool                _multiPartitionConnection = true;
        private bool                _balanceNodes= true;
       
        internal int                _opTimeout = 90 * 1000;  //90 sec
        internal int                _connectionTimeout = 5 * 1000;  //5 sec
        private int                 _connectionRetries = 5;
        internal int                _retryInterval = 1 * 1000;   //1 sec

        internal int _retryConnectionDelay = 600 * 1000; //600 sec or 10 mins

       

        private Hashtable _dirtyFlags = new Hashtable();
        //key for dirty flag hashtable: 
        internal const string OPTIMEOUT = "opTimeOut";
        internal const string CONNECTIONTIMEOUT = "connectionTimeOut";
        internal const string CONNECTIONRETRIES = "connectionRetries";
        internal const string RETRYINTERVAL = "retryInterval";
        internal const string LOADBALANCE = "balanceNodes";
        internal const string PORT = "port";
       
        internal const string RETRYCONNECTIONDELAY = "retryConnectionDelay"; 
        internal const string SERVERLIST = "serverlist";
        internal const string BINDIP = "bindIP";
        private string _bindIP = string.Empty;

       
        private ArrayList _serverList = new ArrayList();

        /// <summary>
        /// Gets/Sets List of servers provided by the user
        /// </summary>
        public CacheServerInfo[] ServerList
        {
            get
            {
                CacheServerInfo[] _returnList = new CacheServerInfo[_serverList.Count];
                int _serverCount=0;
                foreach (object temp in _serverList.ToArray())
                {
                    _returnList[_serverCount] = new CacheServerInfo();
                    _returnList[_serverCount].ServerInfo = (RemoteServer)temp;
                    _serverCount++;
                }
                
                return _returnList;
            }

            set 
            {
                // if user changes server list remove first one 
                if (_serverList.Count > 0)
                {
                    _serverList.Clear();
                }
                //using foreach instead of AddRange to ignore any duplicates
                //in terms of RemoteServer provided

                foreach (CacheServerInfo temp in value)
                {
                    if (!_serverList.Contains(temp))
                    {
                        _serverList.Add(temp.ServerInfo);
                    }
                }
                _dirtyFlags[SERVERLIST] = true;

            }
        }
        /// <summary>
        /// Gets/Sets the IP for the client to be binded with
        /// </summary>
        public string BindIP
        {
            get { return _bindIP; }
            set
            {
                _bindIP = value;
                _dirtyFlags[BINDIP] = true;
            }
        }

//_________________________End of new additions_______________________________________________

        /// <summary>
        /// Gets/Sets the cache mode (inproc/outproc)
        /// </summary>
        public CacheMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        /// <summary>
        /// When this flag is set, client tries to connect to the optimum server in terms of number of connected clients.
        /// This way almost equal number of clients are connected to every node in the clustered cache and no single node 
        /// is overburdened.
        /// </summary>
        public bool LoadBalance
        {
            get { return _balanceNodes; }
            set 
            { 
                _balanceNodes = value;
                _dirtyFlags[LOADBALANCE] = true;
            }
        }

        /// <summary>
        /// Gets/Sets the port on which the clients will connect to a server.
        /// </summary>
   

        /// <summary>
        /// Clients operation timeout specified in seconds.
        /// Clients wait for the response from the server for this time. 
        /// If the response is not received within this time, the operation is not successful.
        /// Based on the network conditions, OperationTimeout value can be adjusted. 
        /// The default value is 90 seconds.
        /// </summary>
        public int ClientRequestTimeOut
        {
            get { return _opTimeout / 1000; }
            set
            {
                _opTimeout = value * 1000;
                if (value >= 0)
                {
                    _dirtyFlags[OPTIMEOUT] = true;
                }
            }
        }

        /// <summary>
        /// Client's connection timeout specified in seconds.
        /// </summary>
        public int ConnectionTimeout
        {
            get { return _connectionTimeout / 1000; }
            set
            {
                _connectionTimeout = value * 1000;
                if (value >= 0)
                {
                    _dirtyFlags[CONNECTIONTIMEOUT] = true;
                }
            }
        }

        /// <summary>
        /// Number of tries to re-establish a broken connection between client and server.
        /// </summary>
        public int ConnectionRetries
        {
            get { return _connectionRetries; }
            set
            {
                _connectionRetries = value;
                if (value >= 0)
                {
                    _dirtyFlags[CONNECTIONRETRIES] = true;
                }
            }
        }

        /// <summary>
        /// Time in seconds to wait between two connection retries.
        /// </summary>
        public int RetryInterval
        {
            get { return _retryInterval / 1000; }
            set
            {
                _retryInterval = value * 1000;
                {
                    _dirtyFlags[RETRYINTERVAL] = true;
                }
            }
        }

        /// <summary>
        /// The time after which client will try to reconnect to the server.
        /// </summary>
        public int RetryConnectionDelay
        {
            get { return _retryConnectionDelay; }
            set
            {
                _retryConnectionDelay = value * 1000;
                if (value >= 0)
                {
                    _dirtyFlags[RETRYCONNECTIONDELAY] = true;
                }
            }
        }

       

        internal bool IsSet(string paramId)
        {
            if (_dirtyFlags == null || _dirtyFlags.Count == 0) return false;

            if (_dirtyFlags.Contains(paramId))
            {
                return Convert.ToBoolean(_dirtyFlags[paramId]);
            }

            return false;
        }

        /// <summary>
        /// Creates Clone for deep copy of the initParam
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            CacheInitParams _cloneParam = new CacheInitParams();
            lock (this)
            {
                _cloneParam._dirtyFlags = (Hashtable)this._dirtyFlags.Clone();// creating shallow copy of hashtable
               
                _cloneParam.ClientRequestTimeOut = this.ClientRequestTimeOut;
                _cloneParam.ConnectionRetries = this.ConnectionRetries;
                _cloneParam.ConnectionTimeout = this.ConnectionTimeout;
                _cloneParam.LoadBalance = this.LoadBalance;
                _cloneParam.Mode = this.Mode;
                
                _cloneParam._retryConnectionDelay = this._retryConnectionDelay;
                _cloneParam.RetryInterval = this.RetryInterval;
               
                _cloneParam.BindIP = this.BindIP;
                _cloneParam.ServerList = (CacheServerInfo[])this.ServerList.Clone();// creating shallow copy of serverlist
            }
            return _cloneParam;
        }
        /// <summary>
        /// Reads the client.ncconf to set the parameters user has not provided values for.
        /// If the client.ncconf is not found or the values for some parameters are not set in client.ncconf,
        /// then default values are set for such parameters.
        /// </summary>
        internal void Initialize(string cacheId)
        {
            bool useDefault = false;
            Web.RemoteClient.Config.ClientConfiguration config = new Web.RemoteClient.Config.ClientConfiguration(cacheId); 
            int retries = 3;
            while(true)
            {
                try
                {
                    config.LoadConfiguration();
                    break;
                }
                catch (Exception ie)
                {
                    if (--retries == 0)
                    {
                        useDefault = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }

            if (!useDefault)
            {
                if (!IsSet(OPTIMEOUT)) this._opTimeout = config.Timeout;
                if (!IsSet(CONNECTIONTIMEOUT)) this._connectionTimeout = config.ConnectionTimeout;
                if (!IsSet(CONNECTIONRETRIES)) this._connectionRetries = config.ConnectionRetries;
                if (!IsSet(RETRYINTERVAL)) this._retryInterval = config.RetryInterval;
                if (!IsSet(RETRYCONNECTIONDELAY)) this._retryConnectionDelay = config.RetryConnectionDelay; 
                if (!IsSet(BINDIP)) this._bindIP =config.BindIP;
                if (!IsSet(PORT)) this._port = config.ServerPort;
                if (!IsSet(LOADBALANCE)) this._balanceNodes = config.BalanceNodes;
            }
        }
    }
}
