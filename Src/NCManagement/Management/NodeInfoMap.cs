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
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Management.Management
{
    public class NodeInfoMap : ICompactSerializable
    {
        private Hashtable map;
        public NodeInfoMap() { this.map = new Hashtable(); }
        public NodeInfoMap(int size) { this.Map = new Hashtable(size); }
        public NodeInfoMap(Hashtable map) { this.Map = map; }
        
        public Hashtable Map
        {
            get
            {
                return map == null ? map = new Hashtable() : map;
            }
            set
            {
                this.map = value;
            }
        }

        public void Deserialize(CompactReader reader)
        {
            try
            {
                int len = reader.ReadInt32();
                while (len-- > 0)
                {
                    this.Map.Add((CacheServer.Channel)reader.ReadInt32(), reader.ReadObject());
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(this.Map.Count);
            IEnumerator iE = this.Map.GetEnumerator();
            while (iE.MoveNext())
            {
                DictionaryEntry current = (DictionaryEntry)iE.Current;
                writer.Write((int)current.Key);
                writer.WriteObject(current.Value);
            }
        }
    }
}