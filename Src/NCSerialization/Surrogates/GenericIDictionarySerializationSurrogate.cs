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
    /// Surrogate for generic collection of key/value pairs.
    /// </summary>
    sealed class GenericIDictionarySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public GenericIDictionarySerializationSurrogate(Type type) : base(type, null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int argumentsCount = reader.ReadInt32();
            Type[] arguments = new Type[argumentsCount];

            for (int i = 0; i < argumentsCount; i++)
            {
                string typeName = reader.ReadString();
                arguments[i] = Type.GetType(typeName);
            }

            object graph = SurrogateHelper.CreateGenericType("System.Collections.Generic.Dictionary", arguments);

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
            int length = reader.ReadInt32();
            IDictionary dictionary = graph as IDictionary;

            for (int i = 0; i < length; i++)
            {
                object key = reader.ReadObject();
                object value = reader.ReadObject();
                dictionary.Add(key, value);
            }

            return graph;
        }

        /// <summary>
        /// Write an object of type <see cref="INxSerializationSurrogate.ActualType"/> to the stream writer
        /// </summary>
        /// <param name="writer">stream writer</param>
        /// <param name="graph">object to be written to the stream reader</param>
        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            Type[] arguments = graph.GetType().GetGenericArguments();
            writer.Write(arguments.Length);

            for (int i = 0; i < arguments.Length; i++)
            {
                writer.Write(arguments[i].AssemblyQualifiedName);
            }

            IDictionary dictionary = (IDictionary)graph;
            writer.Write(dictionary.Count);

            for (IDictionaryEnumerator i = dictionary.GetEnumerator(); i.MoveNext(); )
            {
                writer.WriteObject(i.Key);
                writer.WriteObject(i.Value);
            }
        }


        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            int length = reader.ReadInt32();
            IDictionary dictionary = graph as IDictionary;

            for (int i = 0; i < length; i++)
            {
                reader.SkipObject();
                reader.SkipObject();
            }
        }
    }
}