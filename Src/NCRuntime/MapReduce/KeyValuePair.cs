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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// Object representing key value pair structure.
    /// </summary>
    public class KeyValuePair : ICompactSerializable
    {
        object _key;
        object _value;

        public KeyValuePair() { }
        /// <summary>
        /// Initialize a new instance of key-value pair class.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public KeyValuePair(object key, object value)
        {
            this._key = key;
            this._value = value;
        }
        /// <summary>
        /// Sets/returns value from intermediate Key-Value pair.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        /// <summary>
        ///  Sets/returns key from intermediate Key-Value pair.
        /// </summary>
        public object Key
        {
            get { return _key; }
            set { _key = value; }
        }
        /// <summary>
        /// Compact Deseralize key-value pair
        /// </summary>
        /// <param name="reader">Compact deseralization instance</param>
        public void Deserialize(Serialization.IO.CompactReader reader)
        {
            this._key = reader.ReadObject();
            this._value = reader.ReadObject();
        }
        /// <summary>
        /// Compact Seralize  key-value pair.
        /// </summary>
        /// <param name="writer">Compact seralization instance</param>
        public void Serialize(Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this._key);
            writer.WriteObject(this._value);
        }
    }
}
