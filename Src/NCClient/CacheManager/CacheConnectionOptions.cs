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
using System.Collections.Generic;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Instance of this class can be used to define the parameters at the time of client connection with the cache.
    /// </summary>
    /// <example>
    /// Following example demonstrates how to use CacheConnectionOptions to get instance of <see cref="ICache"/> from <see cref="CacheManager"/>.
    /// <code>
    /// CacheConnectionOptions cacheConnectionOptions = new CacheConnectionOptions();
    /// 
    /// cacheConnectionOptions.LoadBalance = true;
    /// cacheConnectionOptions.ConnectionRetries = 5;
    /// cacheConnectionOptions.Mode = IsolationLevel.OutProc;
    /// cacheConnectionOptions.ClientRequestTimeOut = TimeSpan.FromSeconds(30);
    /// cacheConnectionOptions.UserCredentials = new Credentials("domain\\user-id", "password");
    /// cacheConnectionOptions.RetryInterval = TimeSpan.FromSeconds(5);
    /// cacheConnectionOptions.ServerList = new List&lt;ServerInfo&gt;();
    /// {
    /// 	new ServerInfo("remoteServer",9800)
    /// };
    /// 
    /// ICache cache = CacheManager.GetCache("myCache", cacheConnectionOptions);
    /// </code>
    /// </example>
    public class CacheConnectionOptions 
    {
        #region Fields 

        private bool _loadBalance = true;
        private bool _enabeKeepAlive = false;
        private bool _enableClientLogs = false;
        private bool _enablePipelining = false;

        private int _port = 9800;
        private int _connectionRetries = 5;
        private int _pipeliningTimeout = 250;
        internal TimeSpan _clientRequestTimeout = TimeSpan.FromSeconds(90);
        private TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(30);
        internal TimeSpan _retryInterval = TimeSpan.FromSeconds(1);  //1 sec
        internal TimeSpan _connectionTimeout = TimeSpan.FromSeconds(5);  //5 sec
        internal TimeSpan _retryConnectionDelay = TimeSpan.FromMinutes(10);  //600 sec or 10 mins

        private string _appName;
        private string _serverName;
        private string _clientBindIP = string.Empty;
        private string _defaultReadThruProvider = string.Empty;
        private string _defaultWriteThruProvider = string.Empty;

        private LogLevel _logLevel = Client.LogLevel.Info;
        private IsolationLevel _mode = IsolationLevel.Default;

        private IList<ServerInfo> _serverList = new List<ServerInfo>();
        //private bool _enableDetailedClientLogs = false;
        #endregion


        private Hashtable _dirtyFlags = new Hashtable();
        //key for dirty flag hashtable: 


        #region  Public Properties 

        /// <summary>
        /// List of <see cref="ServerInfo"/> in the cache.
        /// </summary>
        public IList<ServerInfo> ServerList
        {
            get { return _serverList; }

            set
            {
                if (value == null)
                    return;

                //[Requirment] if user changes server list remove first one 
                if (_serverList.Count > 0)
                {
                    _serverList.Clear();
                }

                //Using foreach instead of AddRange to ignore any duplicates
                //in terms of RemoteServer provided
                foreach (ServerInfo temp in value)
                {
                    if (!_serverList.Contains(temp))
                    {
                        _serverList.Add(temp);
                    }
                }
                _dirtyFlags[ConnectionStrings.SERVERLIST] = true;
            }
        }

        /// <summary>
        /// Gets/Sets the <see cref="IsolationLevel"/> of the cache.
        /// </summary>
        public IsolationLevel? Mode
        {
            get { return _mode; }
            set { _mode = value.Value; }
        }

 

        /// <summary>
        /// Gets/Sets the IP for the client to be binded with
        /// </summary>
        public string ClientBindIP
        {
            get { return _clientBindIP; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _dirtyFlags[ConnectionStrings.BINDIP] = true;
                    _clientBindIP = value;
                }
            }
        }

        /// <summary>
        /// If different client applications are connected to server and because of any issue which results
        /// in connection failure with server, after the client again establishes connection “AppName” is used
        /// to identify these different client applications.
        /// <para>
        /// Data type is string. Its optional.If value is not set it takes the value of the process id.
        /// </para>
        /// </summary>
        public string AppName
        {
            get { return _appName; }
            set { _appName = value; }
        }

        /// <summary>
        /// When this flag is set, client tries to connect to the optimum server in terms of number of connected clients.
        /// <para>
        /// This way almost equal number of clients are connected to every node in the clustered cache and no single node 
        /// is overburdened.
        /// </para>
        /// </summary>
        public bool? LoadBalance
        {
            get { return _loadBalance; }
            set {
                _dirtyFlags[ConnectionStrings.LOADBALANCE] = true;
                _loadBalance = value.Value; }
        }

        /// <summary>
        /// Clients operation timeout specified in seconds.
        /// Clients wait for the response from the server for this time. 
        /// If the response is not received within this time, the operation is not successful.
        /// <para>
        /// Based on the network conditions, OperationTimeout value can be adjusted. 
        /// The default value is 90 seconds.
        /// </para>
        /// </summary>
        public TimeSpan? ClientRequestTimeOut
        {
            get { return _clientRequestTimeout; }
            set
            {
                if (value >= TimeSpan.Zero)
                {
                    _dirtyFlags[ConnectionStrings.CLIENTREQUESTOPTIME] = true;
                    _clientRequestTimeout = value.Value;
                }
            }
        }

        /// <summary>
        /// Client's connection timeout specified in seconds.
        /// </summary>
        public TimeSpan? ConnectionTimeout
        {
            get { return _connectionTimeout; }
            set
            {
                if (value >= TimeSpan.Zero)
                {
                    _dirtyFlags[ConnectionStrings.CONNECTIONTIMEOUT] = true;
                    _connectionTimeout = value.Value;
                }
            }
        }

        /// <summary>
        /// Number of tries to re-establish a broken connection between client and server.
        /// </summary>
        public int? ConnectionRetries
        {
            get { return _connectionRetries; }
            set
            {
                if (value > 0)
                {
                    _dirtyFlags[ConnectionStrings.CONNECTIONRETRIES] = true;
                    _connectionRetries = value.Value;
                }
            }
        }

        /// <summary>
        /// Time in seconds to wait between two connection retries.
        /// </summary>
        public TimeSpan? RetryInterval
        {
            get { return _retryInterval; }
            set
            {
                _dirtyFlags[ConnectionStrings.RETRYINTERVAL] = true;
                _retryInterval = value.Value;
            }
        }

        /// <summary>
        /// The time after which client will try to reconnect to the server.
        /// </summary>
        public TimeSpan? RetryConnectionDelay
        {
            get { return _retryConnectionDelay; }
            set
            {
                if (value.Value >= TimeSpan.Zero)
                {
                    _dirtyFlags[ConnectionStrings.RETRYCONNECTIONDELAY] = true;
                    _retryConnectionDelay = value.Value;
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
        /// Sets the <see cref="Alachisoft.NCache.Client.LogLevel"/> either as Info, Error or Debug.
        /// </summary>
        public LogLevel? LogLevel
        {
            get { return _logLevel; }
            set
            {
                _logLevel = value.Value;
                _dirtyFlags[ConnectionStrings.LOGLEVEL] = true;
            }
        }

        /// <summary>
        /// Enables client logs.
        /// </summary>
        public bool? EnableClientLogs
        {
            get { return _enableClientLogs; }
            set
            {
                _enableClientLogs = value.Value;
                _dirtyFlags[ConnectionStrings.ENABLECLIENTLOGS] = true;
            }
        }

        #endregion

        #region Internal Properties 

   

        internal string ServerName { get; set; }
        internal int? Port
        {
            get
            {
                if (ServerList.Count > 0)
                {
                    return ServerList[0].Port;
                }
                return _port;
            }
            set
            {
                _dirtyFlags[ConnectionStrings.PORT] = true;
                _port = value.Value;
            }
        }

        #endregion

        #region ICloneable

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            CacheConnectionOptions _cloneParam = new CacheConnectionOptions();
            lock (this)
            {
                _cloneParam.AppName = AppName;
                _cloneParam.ClientBindIP = ClientBindIP;
                _cloneParam.ClientRequestTimeOut = ClientRequestTimeOut;
               
                _cloneParam.ConnectionRetries = ConnectionRetries;
                _cloneParam.ConnectionTimeout = ConnectionTimeout;
                
                _cloneParam.LoadBalance = LoadBalance;
                _cloneParam.Mode = Mode;
                _cloneParam.Port = Port;
                _cloneParam.RetryConnectionDelay = RetryConnectionDelay;
                _cloneParam.RetryInterval = RetryInterval;
                _cloneParam.ServerList = ServerList;
                _cloneParam.ServerName = ServerName;
                
                _cloneParam.EnableClientLogs = EnableClientLogs;
                
                _cloneParam.LogLevel = LogLevel;
            }
            return _cloneParam;
        }

        #endregion

        #region Internal Methods

        internal void Initialize(string cacheId)
        {
            bool useDefault = false;
            ClientConfiguration config = new ClientConfiguration(cacheId);

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
                if (!IsSet(ConnectionStrings.CLIENTREQUESTOPTIME)) _clientRequestTimeout = TimeSpan.FromMilliseconds(config.ClientRequestTimeout);
                if (!IsSet(ConnectionStrings.CONNECTIONTIMEOUT)) this._connectionTimeout = TimeSpan.FromMilliseconds(config.ConnectionTimeout);
                if (!IsSet(ConnectionStrings.BINDIP)) this._clientBindIP = config.BindIP;
                if (!IsSet(ConnectionStrings.PORT)) this._port = config.ServerPort;

                if (!IsSet(ConnectionStrings.ENABLECLIENTLOGS)) this._enableClientLogs = config.EnableClientLogs;
                if (!IsSet(ConnectionStrings.LOGLEVEL)) this._logLevel = config.LogLevels;
                if (!IsSet(ConnectionStrings.LOADBALANCE)) this._loadBalance = config.BalanceNodes;
                if (!IsSet(ConnectionStrings.ENABLEPIPELINING)) this._enablePipelining = config.EnablePipelining;
                if (!IsSet(ConnectionStrings.PIPELININGTIMEOUT)) this._pipeliningTimeout = config.PipeliningTimeout;

            }
        }
        #endregion

    }
}
