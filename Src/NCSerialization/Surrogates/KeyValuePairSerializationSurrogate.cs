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
using System.Collections.Generic;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Surrogate for KeyValuePair structure with the specified key and value.
    /// </summary>
    class KeyValuePairSerializationSurrogate<K, V> : ContextSensitiveSerializationSurrogate
    {
        public KeyValuePairSerializationSurrogate() : base(typeof(KeyValuePair<K, V>), null) { }

        /// <summary>
        /// Read an object of type <see cref="SerializationSurrogate.ActualType"/> from the stream reader
        /// </summary>
        /// <param name="reader">The reader from which the data is deserialized</param>
        /// <returns>object read from the stream reader</returns>
        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            KeyValuePair<K, V> pair = (KeyValuePair<K, V>)graph;
            
            K key = reader.ReadObjectAs<K>();
            V value = reader.ReadObjectAs<V>();

            pair = new KeyValuePair<K, V>(key, value);
            
            return pair;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            KeyValuePair<K, V> pair = (KeyValuePair<K, V>)graph;
            writer.WriteObjectAs<K>(pair.Key);
            writer.WriteObjectAs<V>(pair.Value);
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            KeyValuePair<K, V> pair = (KeyValuePair<K, V>)graph;

            reader.SkipObjectAs<K>();
            reader.SkipObjectAs<V>();
        }
    }
}