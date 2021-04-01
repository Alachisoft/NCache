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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Serialization.Formatters;

namespace Alachisoft.NCache.Storage
{
    [Serializable]
    class StoreItem : ICompactSerializable
    {
        public object Key;
        public object Value;

        public StoreItem() { }
        public StoreItem(object key, object val)
        {
            Key = key; Value = val;
        }

        /// <summary>
        /// Convert a key-value pair to binary form.
        /// </summary>
        static public byte[] ToBinary(object key, object val,string cacheContext)
        {
            StoreItem item = new StoreItem(key, val);
            return CompactBinaryFormatter.ToByteBuffer(item,cacheContext);
        }

        /// <summary>
        /// Convert a binary form of key-value pair to StoreItem
        /// </summary>
        static public StoreItem FromBinary(byte[] buffer,string cacheContext)
        {
            return (StoreItem)CompactBinaryFormatter.FromByteBuffer(buffer,cacheContext);
        }

        #region /               -- ICompactSerializable Members --                /

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            Key = reader.ReadObject();
            Value = reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.WriteObject(Key);
            writer.WriteObject(Value);
        }

        #endregion
    }
}