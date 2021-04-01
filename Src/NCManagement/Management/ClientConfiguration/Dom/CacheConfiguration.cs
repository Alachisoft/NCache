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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Config;

namespace Alachisoft.NCache.Management.ClientConfiguration.Dom
{
    [Serializable]
    public class CacheConfiguration : ICloneable, ICompactSerializable
    {
        private string _cacheId;
        private string _defaultReadThruProvider = string.Empty;
        private string _defaultWriteThruProvider = string.Empty;
        private bool _loadBalance = true;
        private bool _enableClientLogs = false;
        private string _logLevel = "error";
        private bool _isRegisteredLocal;
        private Dictionary<int, CacheServer> _serversPriorityList = new Dictionary<int, CacheServer>();
        private static string _serverName;
        private static string _bindIp;
        private RtContextValue _serverRuntimeContext;
        private ServerMapping _serverMapping = null;

        static CacheConfiguration()
        {
            try
            {
                _serverName = Environment.MachineName;
            }
            catch (Exception) { }
        }

        public bool IsRegisteredLocal
        {
            get { return _isRegisteredLocal; }
            set { _isRegisteredLocal = value; }
        }

        [ConfigurationAttribute("id")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }


        [ConfigurationAttribute("load-balance")]
        public bool LoadBalance
        {
            get { return _loadBalance; }
            set { _loadBalance = value; }
        }

        [ConfigurationAttribute("enable-client-logs")]
        public bool EnableClientLogs
        {
            get { return _enableClientLogs; }
            set { _enableClientLogs = value; }
        }

        [ConfigurationAttribute("log-level")]
        public string LogLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        public RtContextValue ServerRuntimeContext
        {
            get { return _serverRuntimeContext; }
            set { _serverRuntimeContext = value; }
        }

        public string RuntimeContextString
        {
            get
            {
                string contextString = string.Empty;

                switch (_serverRuntimeContext)
                {
                    case RtContextValue.NCACHE:
                        contextString = "NCACHE";
                        break;
                }

                return contextString;
            }
            set
            {
                switch (value)
                {
                    case "NCACHE":
                        _serverRuntimeContext = RtContextValue.NCACHE;
                        break;
                }
            }
        }

        [ConfigurationSection("server")]
        public CacheServer[] Servers
        {
            get
            {
                CacheServer[] servers = new CacheServer[_serversPriorityList.Count];
                foreach (KeyValuePair<int, CacheServer> pair in _serversPriorityList)
                {
                    int priority = pair.Key;
                    CacheServer server = pair.Value;
                    server.Priority = priority;
                    servers[priority] = server;
                }

                return servers;
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                {
                    _serversPriorityList[i] = value[i];
                }
            }
        }
        [ConfigurationSection("server-end-point")]
        public ServerMapping ServerMapping
        {
            get
            {

                return _serverMapping;
            }
            set
            {
                _serverMapping = value;
            }
        }


     

        public Dictionary<int, CacheServer> ServersPriorityList
        {
            get
            {
                return _serversPriorityList;
            }

            set { _serversPriorityList = value; }
        }

        public string BindIp
        {
            get { return _bindIp; }
            set { _bindIp = value; }
        }


        public bool RemoveServer(string serverName)
        {
            int serverPriority = 0;
            bool found = false;

            if (_serversPriorityList != null)
            {
                foreach (CacheServer server in _serversPriorityList.Values)
                {
                    if (server.ServerName.ToLower() == serverName.ToLower())
                    {
                        serverPriority = server.Priority;
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    _serversPriorityList.Remove(serverPriority);
                    return true;
                }
            }

            return false;
        }

        public bool AddServer(string serverName, int priority)
        {
            CacheServer server = new CacheServer();
            server.ServerName = serverName;
            server.Priority = priority;
            _serversPriorityList[priority] = server;
            return true;
        }

        public bool AddLocalServer()
        {
            return AddServer(!String.IsNullOrEmpty(BindIp) ? BindIp : _serverName, 0);
        }

        private void BringLocalServerToFirstPriority()
        {
            Dictionary<int, CacheServer> tempList = new Dictionary<int, CacheServer>();
            int localServerPriority = 0;
            bool localServerFound = false;

            foreach (KeyValuePair<int, CacheServer> pair in _serversPriorityList)
            {
                string serverName = pair.Value.ServerName.ToLower();
                if ((serverName.CompareTo(_serverName.ToLower()) == 0) || (serverName.CompareTo(_bindIp.ToLower()) == 0))
                {
                    localServerFound = true;
                    localServerPriority = pair.Key;
                    break;
                }
            }

            if (localServerFound)
            {
                tempList.Add(0, _serversPriorityList[localServerPriority]);

                int priority = 1;
                foreach (KeyValuePair<int, CacheServer> pair in _serversPriorityList)
                {
                    if (pair.Key != localServerPriority)
                        tempList.Add(priority++, pair.Value);
                }

                _serversPriorityList = tempList;
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheConfiguration configuration = new CacheConfiguration();
            configuration.BindIp = _bindIp;
            configuration._cacheId = _cacheId;
            configuration._defaultReadThruProvider = _defaultReadThruProvider;
            configuration._defaultWriteThruProvider = _defaultWriteThruProvider;
            configuration._loadBalance = _loadBalance;
            configuration.Servers = Servers != null ? Servers.Clone() as CacheServer[] : null;
            configuration._serverRuntimeContext = _serverRuntimeContext;
            configuration._serverMapping = _serverMapping != null ? _serverMapping.Clone() as ServerMapping : null;
            configuration._enableClientLogs = _enableClientLogs;
            configuration._logLevel = _logLevel;
            return configuration;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cacheId = reader.ReadObject() as string;
            _defaultReadThruProvider = reader.ReadObject() as string;
            _defaultWriteThruProvider = reader.ReadObject() as string;
            _loadBalance = reader.ReadBoolean();
            _isRegisteredLocal = reader.ReadBoolean();
            _serversPriorityList = new Dictionary<int, CacheServer>();
            int lenght = reader.ReadInt32();
            for (int i = 0; i < lenght; i++)
            {
                _serversPriorityList.Add(reader.ReadInt32(), reader.ReadObject() as CacheServer);
            }
            _serverName = reader.ReadObject() as string;
            _bindIp = reader.ReadObject() as string;
            _serverRuntimeContext = reader.ReadObject() as string == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE;
            if (reader.ReadBoolean())
            {
                _serverMapping = reader.ReadObject() as ServerMapping;
            }
            _enableClientLogs = reader.ReadBoolean();
            _logLevel = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cacheId);
            writer.WriteObject(_defaultReadThruProvider);
            writer.WriteObject(_defaultWriteThruProvider);
            writer.Write(_loadBalance);
            writer.Write(_isRegisteredLocal);
            writer.Write(this._serversPriorityList.Count);
            for (IDictionaryEnumerator i = _serversPriorityList.GetEnumerator(); i.MoveNext(); )
            {
                writer.Write((int)i.Key);
                writer.WriteObject(i.Value);
            }
            writer.WriteObject(_serverName);
            writer.WriteObject(_bindIp);
            writer.WriteObject(_serverRuntimeContext == RtContextValue.JVCACHE ? "1" : "0");
            if (_serverMapping == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.WriteObject(_serverMapping);
            }
            writer.Write(_enableClientLogs);
            writer.WriteObject(_logLevel);
        }

        #endregion
    }
}
