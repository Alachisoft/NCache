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
    /// Surrogate for <see cref="System.Int16[]"/> type.
    /// </summary>
    sealed class Int16ArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public Int16ArraySerializationSurrogate() : base(typeof(Int16[]), null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            return new Int16[length];
        }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            Int16[] array = (Int16[])graph;
            for (int i = 0; i < array.Length; i++)
                array[i] = reader.ReadInt16();
            return array;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            Int16[] array = (Int16[])graph;
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.Write(array[i]);
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            Int16[] array = (Int16[])graph;
            for (int i = 0; i < array.Length; i++)
                reader.SkipInt16();
        }
    }
}