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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Management.ClientConfiguration
{
    public class CacheServerList : ICompactSerializable
    {
        private Dictionary<int, ClientConfiguration.Dom.CacheServer> _serversList = new Dictionary<int, ClientConfiguration.Dom.CacheServer>();

        public Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer> ServersList
        {
            get { return _serversList; }
            set { _serversList = value; }
        }

        public CacheServerList() { }

        public CacheServerList(Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer> serversList)
        {
            _serversList = serversList;
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _serversList = new Dictionary<int, Dom.CacheServer>();
            int length = reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                int key = reader.ReadInt32();
                ClientConfiguration.Dom.CacheServer value = reader.ReadObject() as ClientConfiguration.Dom.CacheServer;
                _serversList.Add(key, value);
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_serversList.Count);
            for (IDictionaryEnumerator i = _serversList.GetEnumerator(); i.MoveNext(); )
            {
                writer.Write((int)i.Key);
                writer.WriteObject(i.Value);
            }
        }

        #endregion
    }
}