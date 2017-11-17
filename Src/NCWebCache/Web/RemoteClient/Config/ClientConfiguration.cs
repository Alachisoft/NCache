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
using System.IO;
using System.Xml;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Web.Caching;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
namespace Alachisoft.NCache.Web.RemoteClient.Config
{
    /// <summary>
    /// Contains the
    /// </summary>

    class ClientConfiguration
    {

        static string FILENAME = "client.ncconf";
        static string DIRNAME = "Config";
        ArrayList _servers = new ArrayList();
        int _currentServer = 0;
        string _cacheId;
        int _serverPort = -1;
        int _jvcServerPort = -1;
        int _timeout = 90 * 1000;
        int _connectionTimeout = 5000;
        int _retries = 5;
        int _retrySleep = 1000;
        int _itemSizeThreshHold = 0;
        int _retryConnectionDelay = 600 * 1000; //  Delay after which try to reconnect
        private bool _balanceNodes = true; 
        private bool _importHashmap = true;
       internal CacheInitParams initParam;
        Hashtable _mappedServer = new Hashtable();
        private bool _loadServersFromConfigFile = true;
        RtContextValue serverRuntimeContext;
        bool _enableClientLogs = false;
        bool _enableServerPriorities = false;
        private bool _enableDetailedClientLogs = false;
        private Search _search = Search.LocalSearch;
        private Search _result = Search.LocalSearch;
        internal const string OPTIMEOUT = "opTimeOut";
        internal const string CONNECTIONTIMEOUT = "connectionTimeOut";
        internal const string CONNECTIONRETRIES = "connectionRetries";
        internal const string RETRYINTERVAL = "retryInterval";
        internal const string LOADBALANCE = "balanceNodes";
        internal const string PORT = "port";
        internal const string RETRYCONNECTIONDELAY = "retryConnectionDelay"; 
        internal const string SERVERLIST = "serverlist";
        internal const string BINDIP = "bindIP";
        internal const string JVCPORT = "jvc-port";
        private bool _isAzureRemoteClient = false;

        public bool EnableClientLogs
        {
            get { return _enableClientLogs; }
        }

        public bool EnableDetailedClientLogs
        {
            get { return _enableDetailedClientLogs; }
        }

        public  bool IPMappingConfigured 
        {
          get { return _isAzureRemoteClient; }
        }

        public ClientConfiguration(string cacheId)
        {

            _cacheId = cacheId;
            initParam = null;
        }

        // added to set the internal cacheInitparam with the parameters from above
        internal ClientConfiguration(string cacheId, CacheInitParams param)
        {
            _cacheId = cacheId;
            initParam = (CacheInitParams)param.Clone();
            if (initParam != null)
            {
                if (initParam.ServerList != null && initParam.ServerList.Length > 0)
                {
                    foreach (CacheServerInfo serverInfo in initParam.ServerList)
                    {
                        AddServer(serverInfo.ServerInfo);
                    }
                    _loadServersFromConfigFile = false;
                }
                else if (initParam.ServerList != null && initParam.ServerList.Length == 1)
                {
                    AddServer(new RemoteServer(initParam.ServerList[0].Name, initParam.ServerList[0].Port));
                }
            }
        }

        public int ServerCount
        {
            get { return _servers.Count; }
        }

        /// <summary>
        /// Add remote server to list only if it is not present
        /// </summary>
        /// <param name="server"></param>
        public void AddServer(RemoteServer server)
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

        public void RemoveServer(RemoteServer server)
        {
            if (_servers != null && server != null)
            {
                lock (_servers.SyncRoot)
                {
                    if (_servers.Contains(server))
                    {
                        RemoteServer existingServer = (RemoteServer)_servers[_servers.IndexOf(server)];
                        if (!existingServer.IsUserProvided)
                        {
                            if (_currentServer == (_servers.Count - 1))
                                _currentServer--;
                            _servers.Remove(server);
                        }

                    }
                }
            }
        }

        public string BindIP
        {
            get
            {
                if ((initParam != null && initParam.IsSet(BINDIP)))
                {
                    return initParam.BindIP;
                }

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

                    XmlNodeList clientNodes = configuration.GetElementsByTagName("ncache-client");
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

                                    if (attributes["bind-ip-address"] != null)
                                    {
                                        currentAttrib = attributes["bind-ip-address"].Value;
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

        public bool BalanceNodes
        {
            get
            {
                if (initParam != null && initParam.IsSet(LOADBALANCE))
                {
                    return initParam.LoadBalance;
                }
                else
                {
                    return this._balanceNodes;
                }
            }
            set { _balanceNodes = value; }
        }

        public bool ImportHashmap
        {
            get { return this._importHashmap; }
            
        }
      
        public RemoteServer GetMappedServer(string ip,int port)
        {
            RemoteServer mapping=null;
            if (_mappedServer != null || _mappedServer.Count != 0)
            {
                foreach (RemoteServer rm in _mappedServer.Keys)
                {
                    if (rm.Name.Equals(ip))
                    {
                        mapping = new RemoteServer();
                        mapping = (RemoteServer)_mappedServer[rm];
                    }
                }
            }
            // Incase the map is null the method will return the original IP and Port
            if (mapping == null)
            {
                mapping = new RemoteServer(ip, port);
            }

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
                if (c_configFileName == null)
                {
                    return;
                }
                FileInfo fileInfo = new FileInfo(c_configFileName);
                fs = fileInfo.OpenRead();
                configuration.Load(fs);
                fileInfo = null;

                bool serverPortFound = false;

                XmlNodeList serverPortList = configuration.GetElementsByTagName("ncache-server");
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    if (serverPortList != null && serverPortList.Count > 0)
                    {
                        XmlNode portNode = serverPortList.Item(0);
                        if (portNode != null)
                        {
                            XmlAttributeCollection attributes = portNode.Attributes;
                            if (attributes != null)
                            {
                                string currentAttrib = string.Empty;

                                if (initParam.ServerList != null && initParam.ServerList.Length > 0)
                                {
                                    if (initParam.ServerList[0].Port > 0)
                                        _serverPort = initParam.ServerList[0].Port;
                                }
                                else
                                {
                                    if (attributes["port"] != null)
                                    {
                                        currentAttrib = attributes["port"].Value;
                                        if (currentAttrib != null)
                                        {
                                            _serverPort = Convert.ToInt32(currentAttrib);
                                        }
                                    }
                                }

                                if (initParam != null && initParam.IsSet(OPTIMEOUT))
                                {
                                    _timeout = initParam.ClientRequestTimeOut * 1000; // because the property return value/1000
                                }
                                else
                                {
                                    if (attributes["client-request-timeout"] != null)
                                    {
                                        currentAttrib = attributes["client-request-timeout"].Value;

                                    }
                                    if (currentAttrib != null)
                                    {
                                        _timeout = Convert.ToInt32(currentAttrib);
                                        _timeout *= 1000; //convert to miliseconds
                                    }
                                }

                                if (initParam != null && initParam.IsSet(CONNECTIONRETRIES))
                                {
                                    _retries = initParam.ConnectionRetries;

                                }
                                else
                                {
                                    if (attributes["connection-retries"] != null)
                                    {
                                        currentAttrib = attributes["connection-retries"].Value;

                                    }
                                    if (currentAttrib != null)
                                    {
                                        _retries = Convert.ToInt32(currentAttrib);
                                    }
                                }

                                if (initParam != null && initParam.IsSet(RETRYINTERVAL))
                                {
                                    _retrySleep = initParam.RetryInterval * 1000;
                                }
                                else
                                {
                                    if (attributes["retry-interval"] != null)
                                    {
                                        currentAttrib = attributes["retry-interval"].Value;

                                        if (currentAttrib != null)
                                        {
                                            _retrySleep = Convert.ToInt32(currentAttrib);
                                            _retrySleep *= 1000; //convert to milliseconds.
                                        }
                                    }
                                }

                                if (initParam != null && initParam.IsSet(CONNECTIONTIMEOUT))
                                {
                                    _connectionTimeout = initParam.ConnectionTimeout * 1000;
                                }
                                else
                                {
                                    if (attributes["connection-timeout"] != null)
                                    {
                                        currentAttrib = attributes["connection-timeout"].Value;

                                    }
                                    if (currentAttrib != null)
                                    {
                                        _connectionTimeout = Convert.ToInt32(currentAttrib);
                                        _connectionTimeout *= 1000; //convert to miliseconds
                                    }
                                }

                                if (initParam != null && initParam.IsSet(RETRYCONNECTIONDELAY))
                                {
                                    _retryConnectionDelay = initParam.RetryConnectionDelay * 1000;
                                }
                                else
                                {
                                    if (attributes["retry-connection-delay"] != null)
                                    {
                                        currentAttrib = attributes["retry-connection-delay"].Value;

                                    }
                                    if (currentAttrib != null)
                                    {
                                        _retryConnectionDelay = Convert.ToInt32(currentAttrib);
                                        _retryConnectionDelay *= 1000; //convert to miliseconds
                                    }
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
                                this._balanceNodes = Convert.ToBoolean(cache.Attributes["load-balance"].Value);
                            }
                            if (initParam != null && initParam.IsSet(LOADBALANCE))
                            {
                                this._balanceNodes = initParam.LoadBalance;
                            }



                            try
                            {
                                if (cache.Attributes["enable-client-logs"] != null)
                                    this._enableClientLogs = Convert.ToBoolean(cache.Attributes["enable-client-logs"].Value.ToString());
                            }
                            catch (Exception)
                            { }

                            if (cache.Attributes["log-level"] != null)
                                this._enableDetailedClientLogs = cache.Attributes["log-level"].Value.ToString().ToLower() == "info" ? true : false;

                            this.serverRuntimeContext = RtContextValue.NCACHE;
                            _importHashmap = true;

                            cacheConfig = cache.ChildNodes;
                            break;
                        }
                    }

                    if (cacheConfig == null)
                    {
                        if (!String.IsNullOrEmpty(_cacheId))
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
                    System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
                }
            }
            catch (Runtime.Exceptions.ConfigurationException)
            {
                throw;
            }
            catch (System.IO.IOException) { throw; }
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
               for (int i = 0; i < cacheConfig.Count; i++)
                {
                    XmlNode currentConfig = cacheConfig.Item(i);

                    if (currentConfig.Name.Equals("server"))
                    {
                        RemoteServer remoteServer = new RemoteServer();
                        try
                        {
                            remoteServer.Name = currentConfig.Attributes["name"].InnerText;
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
                                    if (_mappedServer != null || _mappedServer.Count != 0)
                                    {
                                        RemoteServer rm = GetMappedServer(remoteServer.Name, remoteServer.Port);
                                        rm.Priority = remoteServer.Priority;
                                        rm.IsUserProvided = true;
                                        if(!_servers.Contains(rm))
                                            _servers.Add(rm);
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
                                RemoteServer publicServer = new RemoteServer();
                                RemoteServer privateServer = new RemoteServer();
                                try
                                {
                                    privateServer.Name = mapNodeConfig.Attributes["private-ip"].InnerText;
                                    privateServer.Port = Convert.ToInt32(mapNodeConfig.Attributes["private-port"].InnerText);
                                    publicServer.Name = mapNodeConfig.Attributes["public-ip"].InnerText;
                                    publicServer.Port = Convert.ToInt32(mapNodeConfig.Attributes["public-port"].InnerText);
                                }
                                catch (Exception) { }

                                if (privateServer.Name != null )
                                {
                                    lock (_mappedServer.SyncRoot)
                                    {
                                        if (_mappedServer.Count != 0)
                                        {
                                            foreach (RemoteServer rm in _mappedServer.Keys)
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
                        _isAzureRemoteClient = true;
                    }
                }
                foreach (RemoteServer rms in updatedServerMap.Keys)
                {
                    _mappedServer.Add(rms, updatedServerMap[rms]);
                }
            }
            catch (Exception) { }
        }
        internal void AddMappedServers(List<NCache.Config.Mapping> mappedServerList)
        {
            Hashtable updatedServerMap = new Hashtable();
            if (mappedServerList != null)
            {
                foreach (NCache.Config.Mapping node in mappedServerList)
                {
                    RemoteServer publicServer = new RemoteServer();
                    RemoteServer privateServer = new RemoteServer();

                    privateServer.Name = node.PrivateIP;
                    privateServer.Port = node.PrivatePort;
                    publicServer.Name = node.PublicIP;
                    publicServer.Port = node.PublicPort;

                    if (privateServer.Name != null )
                    {
                        lock (_mappedServer.SyncRoot)
                        {
                            if (_mappedServer.Count != 0)
                            {
                                bool keyExists= false;
                                foreach (RemoteServer rm in _mappedServer.Keys)
                                {
                                    if (!rm.Name.Equals(privateServer.Name))
                                    {
                                        keyExists = false;
                                    }
                                    else 
                                    {
                                        keyExists = true;
                                        RemoteServer originalPublicServer = (RemoteServer)_mappedServer[rm];
                                        String existingServer = originalPublicServer.Name + ":" + originalPublicServer.Port.ToString();
                                        String newServer = publicServer.Name + ":" + publicServer.Port.ToString();

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
                    }
                }
                foreach (RemoteServer rms in updatedServerMap.Keys)
                {
                    _mappedServer[rms] = updatedServerMap[rms];
                }
            }
        }
                                                
                                                    
                                          

        /// <summary>
        /// Get a copy of list of servers
        /// </summary>
        public ArrayList ServerList
        {
            get
            {
                lock (_servers.SyncRoot)
                {
                    return this._servers.Clone() as ArrayList;
                }
            }
        }

        public RemoteServer NextServer
        {
            get
            {
                RemoteServer nextServer = null;
                lock (_servers.SyncRoot)
                {
                    if (_servers != null && _servers.Count > 0)
                    {
                        //if servers are removed from the server list, there is 
                        //a chance that _current server is greater than total 
                        //present servers. indexoutofrange exception can occur.
                        //therefore reset the _current server.
                        if (_currentServer > _servers.Count) _currentServer = 0;

                        nextServer = _servers[_currentServer] as RemoteServer;
                        _currentServer++;
                        if (_currentServer > _servers.Count - 1) _currentServer = 0;
                    }
                }
                return nextServer;
            }
        }

        public int Timeout
        {
            get
            {
                if (initParam != null && initParam.IsSet(OPTIMEOUT))
                {
                    return initParam.ClientRequestTimeOut * 1000;
                }
                else
                {
                    return _timeout;
                }
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                if (initParam != null && initParam.IsSet(CONNECTIONTIMEOUT))
                {
                    return initParam.ConnectionTimeout * 1000;
                }
                else
                {
                    return _connectionTimeout;
                }
            }
        }

        public int RetryConnectionDelay
        {
            get
            {
                if (initParam != null && initParam.IsSet(RETRYCONNECTIONDELAY))
                {
                    return initParam.RetryConnectionDelay ;
                }
                else
                {
                    return _retryConnectionDelay;
                }
            }
        }

        public int ConnectionRetries
        {
            get
            {
                if (initParam != null && initParam.IsSet(CONNECTIONRETRIES))
                {
                    return initParam.ConnectionRetries;
                }
                else
                {
                    return _retries;
                }
            }
        }

        public int RetryInterval
        {
            get
            {
                if (initParam != null && initParam.IsSet(RETRYINTERVAL))
                    return initParam.RetryInterval * 1000;
                return _retrySleep;
            }
        }

        public int ServerPort
        {
            get
            {
                if (initParam.ServerList != null && initParam.ServerList.Length > 0)
                {
                    if(initParam.ServerList[0].Port > 0)
                        return initParam.ServerList[0].Port;
                    else
                        return this._serverPort;
                }
                else
                {
                    if (serverRuntimeContext == RtContextValue.JVCACHE)
                        return this._jvcServerPort;
                    else
                        return this._serverPort;
                }
            }
        }

        
    }
}
