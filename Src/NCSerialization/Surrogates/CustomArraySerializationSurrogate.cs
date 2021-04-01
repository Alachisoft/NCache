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
    sealed public class CustomArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public CustomArraySerializationSurrogate(Type type) : base(type, null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            string typeName = reader.ReadString();
            Type t = Type.GetType(typeName);

            object graph = Array.CreateInstance(t, length);

            return graph;
        }
        /// <summary>
        /// Read an object of type <see cref="INxSerializationSurrogate.ActualType"/> from the stream reader. 
        /// A fresh instance of the object is passed as parameter.
        /// The surrogate should populate fields in the object from data on the stream
        /// </summary>
        /// <param name="reader">stream reader</param>
        /// <param name="graph">a fresh instance of the object that the surrogate must deserialize</param>
        /// <returns>object read from the stream reader</returns>
        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            Array array = (Array)graph;

            for (int i = 0; i < array.Length; i++)
                array.SetValue(reader.ReadObject(), i);

            return array;
        }

        /// <summary>
        /// Write an object of type <see cref="INxSerializationSurrogate.ActualType"/> to the stream writer
        /// </summary>
        /// <param name="writer">stream writer</param>
        /// <param name="graph">object to be written to the stream reader</param>
        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            Array array = (Array)graph;
            writer.Write(array.Length);
            writer.Write(graph.GetType().GetElementType().AssemblyQualifiedName);

            for (int i = 0; i < array.Length; i++)
                writer.WriteObject(array.GetValue(i));
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            Array array = (Array)graph;

            for (int i = 0; i < array.Length; i++)
                reader.SkipObject();
        }
    }
}