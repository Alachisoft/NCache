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
    /// Surrogate for <see cref="System.String[]"/> type.
    /// </summary>
    sealed class StringArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public StringArraySerializationSurrogate() : base(typeof(String[]), null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            return new String[length];
        }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            String[] array = (String[])graph;
            for (int i = 0; i < array.Length; i++)
            {
                if (reader.ReadInt16() == 0)
                    continue;

                int length = reader.ReadInt32();
               
                byte[] stream = new byte[length];
                stream = reader.ReadBytes(length);
                array[i] =  UTF8Encoding.UTF8.GetString(stream);
                //array[i] = reader.ReadString();
            }
            return array;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            String[] array = (String[])graph;
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                //string str = (string[])graph;
                if (array[i] != null)
                {
                    writer.Write((short)1);
                    byte[] stream = UTF8Encoding.UTF8.GetBytes(array[i] as string);
                    writer.Write(stream.Length);
                    writer.Write(stream); 
                }
                else
                {
                    writer.Write((short)0);
                    //writer.WriteObject(null);
                }
                //writer.Write(array[i]);
            }
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            String[] array = (String[])graph;
            for (int i = 0; i < array.Length; i++)
            {
                int length = reader.ReadInt16();
                if (length == 0)
                {
                    array[i] = null;
                    continue;
                }
                length = reader.ReadInt32();
                reader.ReadBytes(length);
            }
        }
    }
}