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

using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// Contains new hashmap and related information for client
    /// </summary>
    public sealed class NewHashmap : ICompactSerializable
    {
        private long _lastViewId;
        private Hashtable _map;
        private ArrayList _members;
        private byte[] _buffer;
        private bool _updateMap = false;
        private bool _forcefulUpdate = false;


        /// <summary>
        /// Default constructor
        /// </summary>
        public NewHashmap()
        {
        }

        public NewHashmap(long lastViewid, Hashtable map, ArrayList members)
        {
            this._lastViewId = lastViewid;
            this._map = map;
            this._members = new ArrayList(members.Count);
            
            foreach (Address address in members)
            {
                this._members.Add(address.IpAddress.ToString());
            }            
        }

        /// <summary>
        /// Last view id that was published
        /// </summary>
        public long LastViewId
        {
            get { return this._lastViewId; }
        }

        /// <summary>
        /// New hash map
        /// </summary>
        public Hashtable Map
        {
            get { return this._map; }
        }

        /// <summary>
        /// Just change the view id
        /// </summary>
        public bool UpdateMap
        {
            get { return _updateMap; }
            set { _updateMap = value; }
        }

        /// <summary>
        /// List of server members (string representation of IP addresses)
        /// </summary>
        public ArrayList Members
        {
            get { return this._members; }
        }

        /// <summary>
        /// Returned the serialized object of NewHashmap
        /// </summary>
        public byte[] Buffer
        {
            get { return this._buffer; }
        }

        public bool ForcefulUpdate
        {
            get { return _forcefulUpdate; }
            set { _forcefulUpdate = value; }
        }

        /// <summary>
        /// Serialize NewHashmap
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="serializationContext">Serialization context used to serialize the object</param>
        public static void Serialize(NewHashmap instance, string serializationContext, bool updateClientMap)
        {
            Hashtable mapInfo = null;
            if (instance != null)
            {
                mapInfo = new Hashtable();
                mapInfo.Add("ViewId", instance._lastViewId);
                mapInfo.Add("Members", instance._members);
                mapInfo.Add("Map", instance._map);
                mapInfo.Add("UpdateMap", updateClientMap);
                mapInfo.Add("ForcefulUpdate", instance.ForcefulUpdate);

                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, mapInfo);
                instance._buffer = stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialize NewHashmap
        /// </summary>
        /// <param name="serializationContext"></param>
        /// <returns></returns>
        public static NewHashmap Deserialize(byte[] buffer, string serializationContext)
        {
            NewHashmap hashmap = null;

            if (buffer != null && buffer.Length > 0)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream(buffer);
                Hashtable map = formatter.Deserialize(stream) as Hashtable;
                if (map != null)
                {
                    hashmap = new NewHashmap();
                    hashmap._lastViewId = (long)map["ViewId"];
                    hashmap._members = (ArrayList)map["Members"];
                    hashmap._map = (Hashtable)map["Map"];
                    hashmap._updateMap = (map["UpdateMap"] != null) ? (bool)map["UpdateMap"] : false;
                    hashmap._forcefulUpdate = (map["ForcefulUpdate"] != null) ? (bool)map["ForcefulUpdate"] : false;
                }
            }
            return hashmap;
        }

        #region ICompactSerializable Members

        /// <summary>
        /// Deserialize the object
        /// </summary>
        /// <param name="reader"></param>
        public void Deserialize(CompactReader reader)
        {
            this._lastViewId = reader.ReadInt64();
            this._members = reader.ReadObject() as ArrayList;
            this._map = reader.ReadObject() as Hashtable;
            this._updateMap = reader.ReadBoolean();
            this._forcefulUpdate = reader.ReadBoolean();
        }

        /// <summary>
        /// Serialize the object
        /// </summary>
        /// <param name="writer"></param>
        public void Serialize(CompactWriter writer)
        {
            writer.Write(this._lastViewId);
            writer.WriteObject(this._members);
            writer.WriteObject(this._map);
            writer.Write(this._updateMap);
            writer.Write(this._forcefulUpdate);
        }

        #endregion


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ("); builder.Append(this._lastViewId); builder.Append(") ");
            builder.Append("[");
            for (int i = 0; i < this.Members.Count; i++)
            {
                builder.Append(this.Members[i]);
                if (i < (this.Members.Count - 1))
                {
                    builder.Append(",");
                }
            }
            builder.Append("] }");
            return builder.ToString();
        }
    }
}
