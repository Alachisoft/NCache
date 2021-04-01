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
using System.Text;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Surrogate for <see cref="System.String"/> type.
    /// </summary>
    sealed class StringSerializationSurrogate : SerializationSurrogate
    {
        public StringSerializationSurrogate() : base(typeof(String), null) { }
        public override object Read(CompactBinaryReader reader) 
        {
            int length = reader.ReadInt32();
            byte[] stream = new byte[length];
            stream = reader.ReadBytes(length);
            return UTF8Encoding.UTF8.GetString(stream);
            //return reader.ReadString(); 
        }
        public override void Write(CompactBinaryWriter writer, object graph) { 
        
            //Asad In case of Special strings the generated stream length and string length are different
            //So we should first convert the string to stream and then write stream lenght
            //Fixed as bug reported in bugzilla [1397]
            string str = (string)graph;
            byte[] stream = UTF8Encoding.UTF8.GetBytes(graph as string);
            int length =  (int) stream.Length;
            writer.Write(length);
            writer.Write(stream);
            //writer.Write((string)graph); 
        
        }
        public override void Skip(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            reader.SkipBytes(length);
        }
    }
}