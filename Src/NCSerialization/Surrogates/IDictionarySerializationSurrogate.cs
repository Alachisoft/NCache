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
using System.Collections;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Surrogate for types that inherit from <see cref="System.IDictionary"/>.
    /// </summary>
    class IDictionarySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public IDictionarySerializationSurrogate(Type t) : base(t, null) { }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            int length = reader.ReadInt32();
            IDictionary dict = (IDictionary)graph;
            for (int i = 0; i < length; i++)
            {
                object key = reader.ReadObject();
                object value = reader.ReadObject();
                dict.Add(key, value);
            }
            return dict;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            IDictionary dict = (IDictionary)graph;
            writer.Write(dict.Count);
            for (IDictionaryEnumerator i = dict.GetEnumerator(); i.MoveNext(); )
            {
                writer.WriteObject(i.Key);
                writer.WriteObject(i.Value);
            }
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            int length = reader.ReadInt32();
            IDictionary dict = (IDictionary)graph;
            for (int i = 0; i < length; i++)
            {
                reader.SkipObject();
                reader.SkipObject();
            }
        }
    }
}