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
using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Management.ClientConfiguration.Dom
{
    [Serializable]
    public class CacheServer : ICloneable, ICompactSerializable
    {
        private string _serverName;
        private int _priority;

        [ConfigurationAttribute("name")]
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public override string ToString()
        {
            return _serverName;
        }

        public override bool Equals(object obj)
        {
            if (obj is CacheServer)
            {
                CacheServer server = obj as CacheServer;
                if (server.ServerName != null)
	            {
		            return server.ServerName.ToLower().Equals(_serverName.ToLower()); 
	            }
                else
                {
                    return false;
                }

            }

            return false;
        }

        public override int GetHashCode()
        {
            if (_serverName!=null)
            return _serverName.ToLower().GetHashCode();
            return String.Empty.GetHashCode();
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheServer server = new CacheServer();
            server._serverName = _serverName;
            server._priority = _priority;

            return server;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _serverName = reader.ReadObject() as string;
            _priority = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_serverName);
            writer.Write(_priority);
        }

        #endregion
    }
}
