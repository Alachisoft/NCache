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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    public class DeleteQueryResultSet : ICompactSerializable
    {
        /// <summary> List of keys which are dependiong on this item. </summary>
        private Hashtable _keysDependingOnMe = new Hashtable();
        private Hashtable _keysEffected = new Hashtable();
        private int _keysEffectedCount;

        public Hashtable KeysDependingOnMe
        {
            get { return _keysDependingOnMe; }
            set
            {
                lock (this)
                { _keysDependingOnMe = value; }
            }
        }

        public Hashtable KeysEffected
        {
            get { return _keysEffected; }
            set
            {
                lock (this)
                { _keysEffected = value; }
            }
        }

        public int KeysEffectedCount
        {
            get { return _keysEffectedCount; }
            set { _keysEffectedCount = value; }
        }

        public object[] RemoveKeys
        {           
            get
            {
                Hashtable keysTable = new Hashtable(KeysDependingOnMe);
                ICollection keyList = KeysEffected.Keys;
                foreach (string key in keyList)
                {
                    if (!keysTable.Contains(key))
                        keysTable.Add(key, null);
                }

                object[] keys = new object[keysTable.Count];
                keysTable.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _keysDependingOnMe = (Hashtable)reader.ReadObject();
            _keysEffected = (Hashtable)reader.ReadObject();
            _keysEffectedCount = reader.ReadInt32();

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_keysDependingOnMe);
            writer.WriteObject(_keysEffected);
            writer.Write(_keysEffectedCount);
        }

        #endregion
    }
}
