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
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Surrogate for <see cref="System.Boolean[]"/> type.
    /// </summary>
    sealed class BooleanArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public BooleanArraySerializationSurrogate() : base(typeof(Boolean[]), null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            return new Boolean[length];
        }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            Boolean[] array = (Boolean[])graph;
            for (int i = 0; i < array.Length; i++) array[i] = reader.ReadBoolean();
            return array;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            Boolean[] array = (Boolean[])graph;
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.Write(array[i]);
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            Boolean[] array = (Boolean[])graph;
            for (int i = 0; i < array.Length; i++)
                reader.SkipObject();
        }
    }
}