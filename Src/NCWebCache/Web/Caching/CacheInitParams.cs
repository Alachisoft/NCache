// Copyright (c) 2018 Alachisoft
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
    public class CacheInitParams : ICloneable
    {
        private CacheMode _mode = CacheMode.Default;
        private string _server = null;

        private int _port = 9800;


        /* no user choice should be given for client optimization i-e multiPartitionConnection will be true by defult */
        private bool _multiPartitionConnection = true;
        private bool _balanceNodes = true;
        internal int _opTimeout = 90 * 1000; //90 sec
        internal int _connectionTimeout = 5 * 1000; //5 sec
        private int _connectionRetries = 5;
        internal int _retryInterval = 1 * 1000; //1 sec
        private int _commandRetries = 3;
        private int _commandRetryInterval = 0;
        private int _cachePort = -1;


        private bool _enabeKeepAlive = false;
        private int _keepAliveInterval = 30;


        internal int _retryConnectionDelay = 600 * 1000; //600 sec or 10 mins


        private Hashtable _dirtyFlags = new Hashtable();

        //key for dirty flag hashtable: 
        internal const string OPTIMEOUT = "opTimeOut";
        internal const string CONNECTIONTIMEOUT = "connectionTimeOut";
        internal const string CONNECTIONRETRIES = "connectionRetries";
        internal const string RETRYINTERVAL = "retryInterval";
        internal const string LOADBALANCE = "balanceNodes";
        internal const string PORT = "port";
        internal const string CACHESYNCMODE = "cacheSyncMode";
        internal const string RETRYCONNECTIONDELAY = "retryConnectionDelay"; //[KS: for connection retry delay]
        internal const string DEFAULTREADTHRUPROVIDER = "defaultReadThruProvider";
        internal const string DEFAULTWRITETHRUPROVIDER = "defaultWriteThruProvider";
        internal const string SERVERLIST = "serverlist";
        internal const string BINDIP = "bindIP";
        internal const string COMMANDRETRIES = "commandRetries";
        internal const string COMMANDRETRYINTERVAL = "commandRetryInterval";
        internal const string APPNAME = "applicationName";
        internal const string ENABLEKEEPALIVE = "enableKeepAlive";
        internal const string KEEPALIVEINTERVAL = "keepAliveInterval";

        private string _defaultReadThruProvider = string.Empty;
        private string _defaultWriteThruProvider = string.Empty;
        private string _bindIP = string.Empty;


        private ArrayList _serverList = new ArrayList();


        private string _appName;

        /// <summary>
        /// Gets/Sets List of servers provided by the user
        /// </summary>
        public CacheServerInfo[] ServerList
        {
            get
            {
                CacheServerInfo[] _returnList = new CacheServerInfo[_serverList.Count];
                int _serverCount = 0;
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
                if (_serverList.Count > 0)
                {
                    _serverList.Clear();
                }


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
        /// Gets/Sets ID of DefaultReadThruProvider
        /// </summary>
        public string DefaultReadThruProvider
        {
            get { return _defaultReadThruProvider; }
            set
            {
                _defaultReadThruProvider = value;
                _dirtyFlags[DEFAULTREADTHRUPROVIDER] = true;
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

        /// <summary>
        /// Gets/Sets ID of DefaultWriteThruProvider
        /// </summary>
        public string DefaultWriteThruProvider
        {
            get { return _defaultWriteThruProvider; }
            set
            {
                _defaultWriteThruProvider = value;
                _dirtyFlags[DEFAULTWRITETHRUPROVIDER] = true;
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
        /// Gets/Sets the server clients will connect to.
        /// </summary>
        [Obsolete("This property is deprecated. Please use the 'ServerList' property instead.", false)]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
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
        [Obsolete("This property is deprecated. Please use the 'ServerList' property instead.", false)]
        public int Port
        {
            get { return _port; }
            set
            {
                if (value > 0)
                {
                    _port = value;
                    _dirtyFlags[PORT] = true;
                }
            }
        }

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

        /// <summary>
        /// Gets/Sets Enumeration to specify how the Client cache is synchronized with the cluster caches through events. 
        /// </summary>
        public int CommandRetries
        {
            get { return _commandRetries; }
            set
            {
                if (value > 0)
                {
                    _commandRetries = value;
                    _dirtyFlags[COMMANDRETRIES] = true;
                }
            }
        }

        public int CommandRetryInterval
        {
            get { return _commandRetryInterval; }
            set
            {
                if (value > 0)
                {
                    _commandRetryInterval = value;
                    _dirtyFlags[COMMANDRETRYINTERVAL] = true;
                }
            }
        }

        public bool EnableKeepAlive
        {
            get { return _enabeKeepAlive; }
            set
            {
                _enabeKeepAlive = value;
                _dirtyFlags[ENABLEKEEPALIVE] = true;
            }
        }

        /// <summary>
        /// Gets or Sets the KeepAliveInterval, which will be in effect if the EnabledKeepAlive is set 'true' or is specified 'true' from the client configuration.
        /// Note: If the value to be set is lessar than 1 or is greater than 7200 (2 hours in seconds), it will resort back 30 seconds internally.
        /// </summary>
        public int KeepAliveInterval
        {
            get { return _keepAliveInterval; }
            set
            {
                if (value < 1 || value > 2 * 60 * 60)
                {
                    _keepAliveInterval = 30;
                    _dirtyFlags[KEEPALIVEINTERVAL] = true;
                }
                else
                {
                    _keepAliveInterval = value;
                    _dirtyFlags[KEEPALIVEINTERVAL] = true;
                }
            }
        }

        public string AppName
        {
            get { return _appName; }
            set { _appName = value; }
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

        internal int CachePort
        {
            get { return _cachePort; }
            set { _cachePort = value; }
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
                _cloneParam._dirtyFlags = (Hashtable)this._dirtyFlags.Clone(); // creating shallow copy of hashtable
                _cloneParam.ClientRequestTimeOut = this.ClientRequestTimeOut;
                _cloneParam.ConnectionRetries = this.ConnectionRetries;
                _cloneParam.ConnectionTimeout = this.ConnectionTimeout;
                _cloneParam.DefaultReadThruProvider = this.DefaultReadThruProvider;
                _cloneParam.DefaultWriteThruProvider = this.DefaultWriteThruProvider;
                _cloneParam.LoadBalance = this.LoadBalance;
                _cloneParam.Mode = this.Mode;
                _cloneParam.Port = this.Port;
                _cloneParam.RetryConnectionDelay = this.RetryConnectionDelay;
                _cloneParam.RetryInterval = this.RetryInterval;
                _cloneParam.Server = this.Server;
                _cloneParam.BindIP = this.BindIP;
                _cloneParam._enabeKeepAlive = this.EnableKeepAlive;
                _cloneParam._keepAliveInterval = this.KeepAliveInterval;
                _cloneParam.ServerList =
                    (CacheServerInfo[])this.ServerList.Clone(); // creating shallow copy of serverlist
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


            RemoteClient.Config.ClientConfiguration config = new RemoteClient.Config.ClientConfiguration(cacheId);

            int retries = 3;
            while (true)
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
                if (!IsSet(RETRYCONNECTIONDELAY)) this._retryConnectionDelay = config.RetryConnectionDelay; //[KS]
                if (!IsSet(BINDIP)) this._bindIP = config.BindIP;
                if (!IsSet(DEFAULTREADTHRUPROVIDER)) this._defaultReadThruProvider = config.DefaultReadThru;
                if (!IsSet(DEFAULTWRITETHRUPROVIDER)) this._defaultWriteThruProvider = config.DefaultWriteThru;
                this._multiPartitionConnection = true;

                if (!IsSet(LOADBALANCE)) this._balanceNodes = config.BalanceNodes;


                if (!IsSet(PORT)) this._port = config.ServerPort;
            }
        }
    }
}