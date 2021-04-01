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
    /// Surrogate for generic <see cref="System.Array"/> types.
    /// </summary>
    sealed  public class ArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public ArraySerializationSurrogate(Type t) : base(t, null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            return Array.CreateInstance(ActualType.GetElementType(), length);
        }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            Array array = (Array)graph;
            for (int i = 0; i < array.Length; i++)
                array.SetValue(reader.ReadObject(), i);
            return array;
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            Array array = (Array)graph;
            for (int i = 0; i < array.Length; i++)
                reader.SkipObject();
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            Array array = (Array)graph;
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.WriteObject(array.GetValue(i));
        }
    }
}