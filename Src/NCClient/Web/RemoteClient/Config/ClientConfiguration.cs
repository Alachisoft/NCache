//  Copyright (c) 2026 Alachisoft
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

using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Contains the
    /// </summary>
    class ClientConfiguration
    {
        #region Fields

        private bool _balanceNodes = true;
        private bool _loadServersFromConfigFile = true;

        private int _serverPort = 9800;
        private int _clientRequestTimeout = 90 * 1000;
        private int _connectionTimeout = 5000;
        private int _retries = 5;
        private int _retryInternal = 1000;
        private int _itemSizeThreshHold = 0;
        private int _keepAliveInterval = 30;
        private double _retryConnectionDelay = 10; // 10 min
        private string _cacheId;
        private string _defaultReadThruProvider;
        private string _defaultWriteThruProvider;
        private ArrayList _servers = new ArrayList();
        private Hashtable _compactTypes = new Hashtable();
        internal CacheConnectionOptions _cacheConnectionOptions;
        private Hashtable _mappedServer = new Hashtable();
        private Search _search = Search.LocalSearch;
        private Search _result = Search.LocalSearch;
        private bool _connectUsingLoadBalancer = false;
        private int _loadBalancerConnectionRetries = 15;
        private bool _isConnectedWithPrivateIP = false;
        private bool _enableNagling = false;

        #endregion

        public ClientConfiguration(string cacheId)
        {
            _cacheId = cacheId;
            _cacheConnectionOptions = null;
        }

        internal ClientConfiguration(string cacheId, CacheConnectionOptions cacheConnectionOptions)
        {
            _cacheId = cacheId;
            _cacheConnectionOptions = (CacheConnectionOptions)cacheConnectionOptions.Clone();
            if (_cacheConnectionOptions != null)
            {
                if (_cacheConnectionOptions.ServerList != null && _cacheConnectionOptions.ServerList.Count > 0)
                {
                    foreach (var serverInfo in _cacheConnectionOptions.ServerList)
                    {
                        serverInfo.IsUserProvided = true;
                        if (serverInfo.IsLoadBalancer)
                            ConnectUsingLoadBalancer = true;    //If provided server is a load balancer, we set this flag true to signify that all connections should be done through load balancer  
                        AddServer(serverInfo);
                    }
                    _loadServersFromConfigFile = false;
                }
            }
        }
      
        public bool EnableClientLogs { get; set; }

        public bool EnableDetailedClientLogs { get; set; }

        public bool IPMappingConfigured { get; set; }

        public int ServerCount { get { return _servers.Count; } }

        public int CurrentServerIndex { get; set; }

        public int ItemSizeThreshHold { get { return _itemSizeThreshHold; } }

        public bool BalanceNodes
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.LoadBalance.HasValue)
                    return _cacheConnectionOptions.LoadBalance.Value;

                return _balanceNodes;
            }
            set { _balanceNodes = value; }
        }

        /// <summary>
        /// This is used in the case of when the cluster machines are behind a load balancer. 
        /// This property signifies that the IP in the client config is that of the Load balancer and
        /// not of the underlying machines in the cluster
        /// </summary>
        public bool ConnectUsingLoadBalancer
        {
            get { return _connectUsingLoadBalancer; }
            set { _connectUsingLoadBalancer = value; }
        }

        public int LoadBalancerConnectionRetries
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.LoadBalancerConnectionRetries.HasValue)
                    return _cacheConnectionOptions.LoadBalancerConnectionRetries.Value;

                return _loadBalancerConnectionRetries;
            }
            set
            {
                _loadBalancerConnectionRetries = value;

                if (_loadBalancerConnectionRetries < 2)
                {
                    _loadBalancerConnectionRetries = 2;
                }
            }
        }
        public bool EnableNagling
        {
            get { return _enableNagling; }
        }
        public bool ImportHashmap { get; set; } = true ;


        public int ConnectionRetries
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.IsSet(ConnectionStrings.CONNECTIONRETRIES))
                    return _cacheConnectionOptions.ConnectionRetries.Value;

                return _retries;
            }
        }


        public string BindIP
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.IsSet(ConnectionStrings.BINDIP))
                    return _cacheConnectionOptions.ClientBindIP;

                string bindIP = string.Empty;
                string fileName = null;
                FileStream fs = null;
                try
                {
                    fileName = DirectoryUtil.GetBaseFilePath("client.ncconf");

                    if (fileName == null)
                    {
                        return bindIP;
                    }

                    XmlDocument configuration = new XmlDocument();
                    FileInfo fileInfo = new FileInfo(fileName);

                    fs = fileInfo.OpenRead();
                    configuration.Load(fs);

                    XmlNodeList clientNodesServer = configuration.GetElementsByTagName("ncache-server");
                    XmlNodeList clientNodesClient = configuration.GetElementsByTagName("ncache-client");
                    XmlNodeList clientNodes = (clientNodesClient!=null&&clientNodesClient.Count > 0) ? clientNodesClient : clientNodesServer;

                    CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                        if (clientNodes != null && clientNodes.Count > 0)
                        {
                            XmlNode childNode = clientNodes.Item(0);
                            if (childNode != null)
                            {
                                XmlAttributeCollection attributes = childNode.Attributes;
                                if (attributes != null)
                                {
                                    string currentAttrib = string.Empty;

                                    if (attributes["local-server-ip"] != null)
                                    {
                                        currentAttrib = attributes["local-server-ip"].Value;
                                        if (currentAttrib != null)
                                        {
                                            bindIP = Convert.ToString(currentAttrib);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Thread.CurrentThread.CurrentCulture = cultureInfo;
                    }
                    return bindIP;
                }
                catch (Exception ex)
                {
                    return bindIP;
                }
                finally
                {
                    if (fs != null) fs.Close();
                }
            }
        }

        public ArrayList ServerList
        {
            get
            {
                lock (_servers.SyncRoot)
                {
                    return _servers.Clone() as ArrayList;
                }
            }
        }
        public ServerInfo GetServerAt(int index)
        {
            ServerInfo nextServer = null;
            lock (_servers.SyncRoot)
            {
                if (_servers != null && _servers.Count > 0 && index <_servers.Count)
                {
                    nextServer = _servers[index] as ServerInfo;
                }
            }
            return nextServer;
        }

        public ServerInfo NextServer
        {
            get
            {
                ServerInfo nextServer = null;
                lock (_servers.SyncRoot)
                {
                    if (_servers != null && _servers.Count > 0)
                    {
                        //if servers are removed from the server list, there is 
                        //a chance that _current server is greater than total 
                        //present servers. indexoutofrange exception can occur.
                        //therefore reset the _current server.
                        if (CurrentServerIndex > _servers.Count) CurrentServerIndex = 0;
                        if (CurrentServerIndex < 0) CurrentServerIndex = 0;

                        nextServer = _servers[CurrentServerIndex] as ServerInfo;
                        CurrentServerIndex++;

                        if (CurrentServerIndex > _servers.Count - 1) CurrentServerIndex = 0;
                    }
                }
                return nextServer;
            }
        }

        public int ConfigServerPort { get; private set; } = 9800;

        public int ClientRequestTimeout
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.IsSet(ConnectionStrings.CLIENTREQUESTOPTIME))
                    return Convert.ToInt32(_cacheConnectionOptions.ClientRequestTimeOut.Value.TotalMilliseconds);

                return _clientRequestTimeout;
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.IsSet(ConnectionStrings.CONNECTIONTIMEOUT))
                    return Convert.ToInt32(_cacheConnectionOptions.ConnectionTimeout.Value.TotalMilliseconds);

                return _connectionTimeout;
            }
        }

        internal bool EnableKeepAlive { get; set; } = false;

        internal int KeepAliveInterval { get; private set; } = 30;

        public bool EnableClientLog { get; private set; }
               
        public LogLevel LogLevels { get; private set; }

        public bool EnablePipelining { get; private set; }

        public int PipeliningTimeout { get; private set; }

        internal int ServerPort
        {
            get
            {
                if (_cacheConnectionOptions != null && _cacheConnectionOptions.IsSet(ConnectionStrings.PORT))
                    return _cacheConnectionOptions.Port.Value;

                return _serverPort;
            }
            set
            {
                _serverPort = value;
            }
        }

        public void AddServer(ServerInfo server)
        {
            if (_servers != null && (!string.IsNullOrEmpty(server.Name) || server.IP != null))
            {
                lock (_servers.SyncRoot)
                {
                    if (!_servers.Contains(server))
                    {
                        _servers.Add(server);
                    }
                }
            }
        }

        public void RemoveServer(ServerInfo server)
        {
            if (_servers != null && server != null)
            {
                lock (_servers.SyncRoot)
                {
                    if (_servers.Contains(server))
                    {
                        ServerInfo existingServer = (ServerInfo)_servers[_servers.IndexOf(server)];

                        if (!existingServer.IsUserProvided)
                        {
                            if (CurrentServerIndex == (_servers.Count - 1))
                                CurrentServerIndex--;

                            _servers.Remove(server);
                        }

                    }
                }
            }
        }

        public ServerInfo GetMappedServer(string ip, int port = 9800)
        {
            ServerInfo mapping = null;

            if (_mappedServer != null || _mappedServer.Count != 0)
            {
                foreach (ServerInfo rm in _mappedServer.Keys)
                {
                    if (rm.Name.Equals(ip))
                    {
                        mapping = new ServerInfo();
                        mapping = (ServerInfo)_mappedServer[rm];
                    }
                }
            }
            //Incase the map is null the method will return the original IP and Port
            if (mapping == null)
                mapping = new ServerInfo(ip, port);

            return mapping;
        }

        public void LoadConfiguration()
        {

            FileStream fs = null;
            string c_configFileName = null;
            XmlDocument configuration = new XmlDocument();

            try
            {
                if (_cacheId == null) return;

                c_configFileName = DirectoryUtil.GetBaseFilePath("client.ncconf", _search, out _result);

                if (c_configFileName == null) return;

                if (!File.Exists(c_configFileName))
                    throw new ConfigurationException(ErrorCodes.CacheInit.CONFIG_NOT_FOUND, ErrorMessages.GetErrorMessage(ErrorCodes.CacheInit.CONFIG_NOT_FOUND, c_configFileName));

                FileInfo fileInfo = new FileInfo(c_configFileName);
                fs = fileInfo.OpenRead();
                configuration.Load(fs);
                fileInfo = null;

                bool serverPortFound = false;

                XmlNodeList clientInfoTag = configuration.GetElementsByTagName("ncache-server");

                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    if (clientInfoTag != null && clientInfoTag.Count > 0)
                    {
                        XmlNode portNode = clientInfoTag.Item(0);
                        if (portNode != null)
                        {
                            XmlAttributeCollection attributes = portNode.Attributes;
                            if (attributes != null)
                            {
                                string currentAttrib = string.Empty;

                                if (attributes["port"] != null && attributes["port"].Value != null)
                                {
                                    ConfigServerPort = Convert.ToInt32(attributes["port"].Value);
                                }

                                if (_cacheConnectionOptions != null && _cacheConnectionOptions.Port.HasValue)
                                {
                                    _serverPort = _cacheConnectionOptions.Port.Value;
                                }
                                else
                                {
                                    _serverPort = ConfigServerPort;
                                }
                                if (attributes["enable-nagling"] != null && attributes["enable-nagling"].Value != null)
                                {
                                    _enableNagling = Convert.ToBoolean(attributes["enable-nagling"].Value);
                                }

                                if (_cacheConnectionOptions != null && _cacheConnectionOptions.ClientRequestTimeOut.HasValue)
                                {
                                    _clientRequestTimeout = Convert.ToInt32(_cacheConnectionOptions.ClientRequestTimeOut.Value.TotalMilliseconds);
                                }
                                else
                                {
                                    if (attributes["client-request-timeout"] != null && attributes["client-request-timeout"].Value != null)
                                    {
                                        _clientRequestTimeout = Convert.ToInt32(attributes["client-request-timeout"].Value) * 1000;
                                    }
                                }

                                if (_cacheConnectionOptions != null && _cacheConnectionOptions.ConnectionRetries.HasValue)
                                {
                                    _retries = _cacheConnectionOptions.ConnectionRetries.Value;
                                }
                                else
                                {
                                    if (attributes["connection-retries"] != null && attributes["connection-retries"].Value != null)
                                    {
                                        _retries = Convert.ToInt32(attributes["connection-retries"].Value);
                                    }
                                }
                                if (_cacheConnectionOptions != null && _cacheConnectionOptions.LoadBalancerConnectionRetries.HasValue)
                                {
                                    LoadBalancerConnectionRetries = _cacheConnectionOptions.LoadBalancerConnectionRetries.Value;
                                }
                                else
                                {
                                    if (attributes["load-balancer-connection-retries"] != null && attributes["load-balancer-connection-retries"].Value != null)
                                    {
                                        LoadBalancerConnectionRetries = Convert.ToInt32(attributes["load-balancer-connection-retries"].Value);
                                    }
                                }
                                if (_cacheConnectionOptions != null && _cacheConnectionOptions.ConnectionTimeout.HasValue)
                                {
                                    _connectionTimeout = Convert.ToInt32(_cacheConnectionOptions.ConnectionTimeout.Value.TotalMilliseconds);
                                }
                                else
                                {
                                    if (attributes["connection-timeout"] != null && attributes["connection-timeout"].Value != null)
                                        _connectionTimeout = Convert.ToInt32(attributes["connection-timeout"].Value) * 1000;
                                }



                            }
                            serverPortFound = true;
                        }
                    }

                    if (!serverPortFound)
                        throw new Runtime.Exceptions.ConfigurationException("ncache-server missing in client confiugration");


                    XmlNodeList cacheList = configuration.GetElementsByTagName("cache");
                    XmlNodeList cacheConfig = null;

                    for (int i = 0; i < cacheList.Count; i++)
                    {
                        XmlNode cache = cacheList.Item(i);
                        if (cache.Attributes.GetNamedItem("id").Value.ToLower().Equals(_cacheId.ToLower()))
                        {

                            if (cache.Attributes["load-balance"] != null)
                            {
                                _balanceNodes = Convert.ToBoolean(cache.Attributes["load-balance"].Value);
                            }
                            if (cache.Attributes["load-balancer-connection-retries"] != null)
                            {
                                LoadBalancerConnectionRetries = Convert.ToInt32(cache.Attributes["load-balancer-connection-retries"].Value);
                            }
                            if (_cacheConnectionOptions != null && _cacheConnectionOptions.LoadBalance.HasValue)
                            {
                                _balanceNodes = _cacheConnectionOptions.LoadBalance.Value;
                            }

                 
                            if (_cacheConnectionOptions != null && _cacheConnectionOptions.EnableClientLogs.HasValue)
                            {
                                EnableClientLogs = _cacheConnectionOptions.EnableClientLogs.Value;

                            }
                            else
                            {
                                if (cache.Attributes["enable-client-logs"] != null)
                                    EnableClientLogs = Convert.ToBoolean(cache.Attributes["enable-client-logs"].Value.ToString());
                            }

                        
                            if (_cacheConnectionOptions != null && _cacheConnectionOptions.LogLevel.HasValue)
                            {
                                LogLevels = _cacheConnectionOptions.LogLevel.Value;
                                switch (LogLevels)
                                {
                                    case LogLevel.Debug:
                                    case LogLevel.Info:
                                        EnableDetailedClientLogs = true;
                                        break;
                                    case LogLevel.Error:
                                        EnableDetailedClientLogs = false;
                                        break;

                                }
                            }
                            else
                            {

                                if (cache.Attributes["log-level"] != null)
                                {
                                    var logLevel = cache.Attributes["log-level"].Value.ToString().ToLower();
                                    switch (logLevel)
                                    {
                                        case "info":
                                            LogLevels = LogLevel.Info;
                                            EnableDetailedClientLogs = true;
                                            break;
                                        case "debug":
                                            LogLevels = LogLevel.Debug;
                                            EnableDetailedClientLogs = true;
                                            break;
                                        case "error":
                                            LogLevels = LogLevel.Error;
                                            EnableDetailedClientLogs = false;
                                            break;
                                    }

                                }
                            }
                           
                            ImportHashmap = true;

                            cacheConfig = cache.ChildNodes;
                            break;
                        }
                    }

                    if (cacheConfig == null)
                    {
                        if (!string.IsNullOrEmpty(_cacheId))
                        {
                            if (_result != Search.GlobalSearch)
                            {
                                _search = _result + 1;
                                LoadConfiguration();
                            }
                        }
                        return;
                    }
                    _search = _result;

                    LoadRemoteServerMappingConfig(cacheConfig);
                    LoadRemoteServerConfig(cacheConfig);
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = cultureInfo;
                }
            }

            catch (Runtime.Exceptions.ConfigurationException)
            {
                throw;
            }
            catch (IOException) { throw; }
            catch (Exception e)
            {
                throw new Runtime.Exceptions.ConfigurationException("An error occurred while reading client.ncconf. " + e.Message);
            }

            finally
            {
                if (fs != null) fs.Close();
            }
        }



        private void LoadRemoteServerConfig(XmlNodeList cacheConfig)
        {
            try
            {
                if (!_loadServersFromConfigFile) return;

                int PriorityCounter = 1;
                for (int i = 0; i < cacheConfig.Count; i++)
                {
                    XmlNode currentConfig = cacheConfig.Item(i);

                    if (currentConfig.Name.Equals("server"))
                    {
                        ServerInfo remoteServer = new ServerInfo();

                        try
                        {
                            remoteServer.Name = currentConfig.Attributes["name"].InnerText;

                            if (currentConfig.Attributes["is-load-balancer"] != null)
                            {
                                //If any one server is a load balancer we set the below flags to true to signify that
                                //all clients using this config should connect through the load balancer, since all
                                //machines in the cluster may not be directly accessible
                                bool connectUsingLoadBalancer = Convert.ToBoolean(currentConfig.Attributes["is-load-balancer"].InnerText);

                                remoteServer.IsLoadBalancer = connectUsingLoadBalancer;
                                _connectUsingLoadBalancer = connectUsingLoadBalancer;
                            }
                            remoteServer.Priority = Convert.ToInt16(PriorityCounter);
                            PriorityCounter = PriorityCounter + 1;

                            if (currentConfig.Attributes.GetNamedItem("port-range") != null)
                                remoteServer.PortRange = Convert.ToInt16(currentConfig.Attributes["port-range"].InnerText);
                        }
                        catch (Exception) { }

                        remoteServer.Port = ServerPort;

                        if ((remoteServer.Name != null || remoteServer.IP != null) && remoteServer.Port != -1)
                        {
                            lock (_servers.SyncRoot)
                            {
                                if (!_servers.Contains(remoteServer))
                                {
                                    if (_mappedServer != null && _mappedServer.Count != 0)
                                    {
                                        ServerInfo rm = GetMappedServer(remoteServer.Name, remoteServer.Port);
                                        remoteServer.Name = rm.Name;
                                        remoteServer.Port = rm.Port;

                                        if (!_servers.Contains(remoteServer))
                                            _servers.Add(remoteServer);
                                    }
                                    else
                                    {
                                        remoteServer.IsUserProvided = true;
                                        _servers.Add(remoteServer);
                                    }
                                }
                            }
                        }
                    }
                }
                lock (_servers.SyncRoot)
                {
                    _servers.Sort();
                }
            }
            catch (Exception) { }
        }

        private void LoadRemoteServerMappingConfig(XmlNodeList cacheConfig)
        {
            Hashtable updatedServerMap = new Hashtable();
            try
            {
                for (int i = 0; i < cacheConfig.Count; i++)
                {
                    XmlNode currentConfig = cacheConfig.Item(i);

                    if (currentConfig.Name.Equals("server-end-point"))
                    {
                        XmlNodeList _mappingConfig = currentConfig.ChildNodes;
                        for (int j = 0; j < _mappingConfig.Count; j++)
                        {
                            XmlNode mapNodeConfig = _mappingConfig.Item(j);
                            if (mapNodeConfig.Name.Equals("end-point"))
                            {
                                ServerInfo publicServer = new ServerInfo();
                                ServerInfo privateServer = new ServerInfo();
                                try
                                {
                                    privateServer.Name = mapNodeConfig.Attributes["private-ip"].InnerText;
                                    privateServer.Port = Convert.ToInt32(mapNodeConfig.Attributes["private-port"].InnerText);
                                    publicServer.Name = mapNodeConfig.Attributes["public-ip"].InnerText;
                                    publicServer.Port = Convert.ToInt32(mapNodeConfig.Attributes["public-port"].InnerText);
                                }
                                catch (Exception) { }

                                if (privateServer.Name != null)
                                {
                                    lock (_mappedServer.SyncRoot)
                                    {
                                        if (_mappedServer.Count != 0)
                                        {
                                            foreach (ServerInfo rm in _mappedServer.Keys)
                                            {
                                                if (!rm.Name.Equals(privateServer.Name))
                                                {
                                                    updatedServerMap.Add(privateServer, publicServer);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _mappedServer.Add(privateServer, publicServer);
                                        }
                                    }
                                }
                            }
                        }
                        IPMappingConfigured = true;
                    }
                }
                foreach (ServerInfo rms in updatedServerMap.Keys)
                {
                    _mappedServer.Add(rms, updatedServerMap[rms]);
                }
            }
            catch (Exception) { }
        }

        internal void AddMappedServers(List<Config.Mapping> mappedServerList, Connection connection = null)
        {
            Hashtable updatedServerMap = new Hashtable();
            if (mappedServerList != null)
            {
                foreach (Config.Mapping node in mappedServerList)
                {
                    ServerInfo publicServer = new ServerInfo();
                    ServerInfo privateServer = new ServerInfo();

                    privateServer.Name = node.PrivateIP;
                    privateServer.Port = node.PrivatePort;
                    publicServer.Name = node.PublicIP;
                    publicServer.Port = node.PublicPort;

                    if (privateServer.Name != null)
                    {
                        lock (_mappedServer.SyncRoot)
                        {
                            if (_mappedServer.Count != 0)
                            {
                                bool keyExists = false;
                                foreach (ServerInfo rm in _mappedServer.Keys)
                                {
                                    if (!rm.Name.Equals(privateServer.Name))
                                    {
                                        keyExists = false;
                                    }
                                    else
                                    {
                                        keyExists = true;
                                        ServerInfo originalPublicServer = (ServerInfo)_mappedServer[rm];
                                        string existingServer = originalPublicServer.Name + ":" + originalPublicServer.Port.ToString();
                                        string newServer = publicServer.Name + ":" + publicServer.Port.ToString();

                                        if (!existingServer.Equals(newServer))
                                        {
                                            updatedServerMap.Add(privateServer, publicServer);
                                        }
                                        break;
                                    }
                                }
                                if (keyExists == false)
                                {
                                    updatedServerMap.Add(privateServer, publicServer);
                                }
                            }
                            else
                            {
                                _mappedServer.Add(privateServer, publicServer);
                            }
                        }

                        if (connection != null && connection.IpAddress.Contains(privateServer.Name) || ConnectUsingLoadBalancer)
                            _isConnectedWithPrivateIP = true;
                    }
                }

                foreach (ServerInfo rms in updatedServerMap.Keys)
                {
                    _mappedServer[rms] = updatedServerMap[rms];
                }

                if (!_isConnectedWithPrivateIP && mappedServerList.Count > 0)
                    IPMappingConfigured = true;
            }
        }

        internal bool IsDifferentParamPort(int usedParamPort)
        {
            if (usedParamPort == ConfigServerPort)
                return false;
            return true;
        }

        internal void UpdateMappedServers(Hashtable mappedServerList, int port)
        {
            if (mappedServerList != null && mappedServerList.Count > 0)
            {
                foreach (DictionaryEntry entry in mappedServerList)
                {
                    List<Mapping> mappedServers = new List<Mapping>();
                    string[] ipAndPort = entry.Key.ToString().Split(':');

                    Mapping mapping = new Mapping();

                    mapping.PublicIP = entry.Value.ToString();
                    mapping.PrivateIP = ipAndPort[0];
                    mapping.PrivatePort = port; //client port 9800
                    mapping.PublicPort = port; //client port 9800

                    mappedServers.Add(mapping);

                    AddMappedServers(mappedServers);


                }
            }
        }
    }
}
