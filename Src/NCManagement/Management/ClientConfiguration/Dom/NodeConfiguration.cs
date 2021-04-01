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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Management.ClientConfiguration.Dom
{
    [Serializable]
    public class NodeConfiguration : ICloneable, ICompactSerializable
    { 
        private int _connectionRetries = 5;
        private int _retryInterval = 1; 

        private int _requestTimeout = 90;
        private int _connectionTimeout = 5;
        private int _serverPort = 9800;

        private int _configurationId;
        private int _retryConnectionDelay = 0;       
        private int _jvcServerPort = 9600;
        private string _localServerIp = "";

        public int ConfigurationId
        {
            get { return _configurationId; }
            set { _configurationId = value; }
        }

        [ConfigurationAttribute("connection-retries")]
        public int ConnectionRetries
        {
            get { return _connectionRetries; }
            set { _connectionRetries = value; }
        }

        [ConfigurationAttribute("retry-connection-delay")]
        public int RetryConnectionDelay
        {
            get { return _retryConnectionDelay; }
            set { _retryConnectionDelay = value; }
        }

        [ConfigurationAttribute("retry-interval")]
        public int RetryInterval
        {
            get { return _retryInterval; }
            set { _retryInterval = value; }
        }

        [ConfigurationAttribute("client-request-timeout")]
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = value; }
        }

        [ConfigurationAttribute("connection-timeout")]
        public int ConnectionTimeout
        {
            get { return _connectionTimeout; }
            set { _connectionTimeout = value; }
        }

        [ConfigurationAttribute("port")]
        public int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value; }
        }

        internal int JvcServerPort
        {
            get { return _jvcServerPort; }
            set { _jvcServerPort = value; }
        }
        
        [ConfigurationAttribute("local-server-ip")]
        public String LocalServerIp
        {

            get
            {
                if (_localServerIp != ClientConfigManager.BindIP)
                {
                    _localServerIp = ClientConfigManager.BindIP;
                }
                return _localServerIp;
            }
            set
            {
                if (_localServerIp != ClientConfigManager.BindIP)
                    _localServerIp = ClientConfigManager.BindIP;
            }
        }
        #region ICloneable Members

        public object Clone()
        {
            NodeConfiguration config = new NodeConfiguration();
            config._configurationId = _configurationId;
            config._connectionRetries = _connectionRetries;
            config._connectionTimeout = _connectionTimeout;
            config._retryInterval = _retryInterval;
            config._serverPort = _serverPort;
            config._requestTimeout = _requestTimeout;
            config._retryConnectionDelay = _retryConnectionDelay;
            config._jvcServerPort = _jvcServerPort;
            config._localServerIp = _localServerIp;

            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _connectionRetries = reader.ReadInt32();
            _retryInterval = reader.ReadInt32();
            _requestTimeout = reader.ReadInt32();
            _connectionTimeout = reader.ReadInt32();
            _serverPort = reader.ReadInt32();
            _configurationId =reader.ReadInt32();
            _retryConnectionDelay = reader.ReadInt32();
            _jvcServerPort = reader.ReadInt32();
            _localServerIp = reader.ReadObject() as string;

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_connectionRetries);
            writer.Write(_retryInterval);
            writer.Write(_requestTimeout);
            writer.Write(_connectionTimeout);
            writer.Write(_serverPort);
            writer.Write(_configurationId);
            writer.Write(_retryConnectionDelay);
            writer.Write(_jvcServerPort);
            writer.WriteObject(_localServerIp);

        }

        #endregion
    }
}
