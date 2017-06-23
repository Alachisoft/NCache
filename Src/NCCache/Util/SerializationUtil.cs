// Copyright (c) 2017 Alachisoft
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
// limitations under the License.

using System;
using System.Collections;
using System.IO;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Serialization.Formatters;

namespace Alachisoft.NCache.Util
{
    /// <summary>
    /// {^[\t, ]*}internal{[a-z, ]*}class{[A-Z,a-z,\,,\:,\t, ]*$}
    /// #if DEBUG\n\1public\2class\3\n#else\n\1internal\2class\3\n#endif
    /// Utility class to help with common tasks.
    /// </summary>
    public class SerializationUtil
    {
        /// <summary>
        /// Serializes the object using CompactSerialization Framwork.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static object CompactSerialize(object graph, string cacheContext)
        {
            if (graph != null && graph is ICompactSerializable)
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write(NCHeader.Version, 0, NCHeader.Length);
                CompactBinaryFormatter.Serialize(stream, graph, cacheContext);
                return stream.ToArray();
            }
            return graph;
        }

        /// <summary>
        /// Serializes the object using CompactSerialization Framwork.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static object CompactSerialize(Stream stream, object graph, string cacheContext)
        {
            if (graph != null && graph is ICompactSerializable)
            {
                byte[] buffer;
                stream.Position = 0;
                stream.Write(NCHeader.Version, 0, NCHeader.Length);
                CompactBinaryFormatter.Serialize(stream, graph, cacheContext, false);
                buffer = new byte[stream.Position];
                stream.Position = 0;
                stream.Read(buffer, 0, buffer.Length);
                stream.Position = 0;
                return buffer;
            }
            return graph;
        }

        /// <summary>
        /// Deserializes the byte buffer to an object using CompactSerialization Framwork.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static object CompactDeserialize(object buffer, string cacheContext)
        {
            object obj = buffer;
            if (buffer != null && buffer is byte[])
            {
                if (HasHCHeader((byte[])buffer))
                {
                    System.IO.MemoryStream stream = new System.IO.MemoryStream((byte[])buffer);
                    //Skip the NCHeader.
                    stream.Position += NCHeader.Length;
                    obj = CompactBinaryFormatter.Deserialize(stream, cacheContext);
                    return obj;
                }
            }
            return obj;
        }

        public static object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag)
        {
            object deserialized = serializedObject;
            try
            {
                if(!flag.IsBitSet(BitSetConstants.BinaryData))
                {
                    if (serializedObject is byte[])
                    {
                        deserialized = CompactBinaryFormatter.FromByteBuffer((byte[])serializedObject, serializationContext);
                    }
                    else if (serializedObject is UserBinaryObject)
                    {
                        deserialized = CompactBinaryFormatter.FromByteBuffer(((UserBinaryObject)serializedObject).GetFullObject(), serializationContext);
                    }
                }
            }
            catch (Exception ex)
            {
                //Kill the exception; it is possible that object was serialized by Java
                //or from any other domain which can not be deserialized by us.
                deserialized = serializedObject;
            }

            return deserialized;
        }

        public static object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag)
        {
            if (serializableObject != null)
            {
                if (serializableObject is byte[])
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    return serializableObject;
                }

                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
            }

            return serializableObject;
        }

        /// <summary>
        /// Deserializes the byte buffer to an object using CompactSerialization Framwork.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static object CompactDeserialize(Stream stream, object buffer, string cacheContext)
        {
            object obj = buffer;
            if (buffer != null && buffer is byte[])
            {
                if (HasHCHeader((byte[])buffer))
                {
                    byte[] tmp = (byte[])buffer;
                    stream.Position = 0;
                    stream.Write(tmp, 0, tmp.Length);
                    stream.Position = 0;
                    //Skip the NCHeader.

                    stream.Position += NCHeader.Length;
                    obj = CompactBinaryFormatter.Deserialize(stream, cacheContext, false);
                    stream.Position = 0;
                    return obj;
                }
            }
            return obj;
        }

        /// <summary>
        /// CompactBinarySerialize which takes object abd return byte array
        /// </summary>
        /// <param name="serializableObject"></param>
        /// <param name="serializationContext"></param>
        /// <returns></returns>
        public static byte[] CompactBinarySerialize(object serializableObject, string serializationContext)
        {
            return CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
        }

        /// <summary>
        /// convert bytes into object
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="serializationContext"></param>
        /// <returns></returns>
        public static object CompactBinaryDeserialize(byte[] buffer, string serializationContext)
        {
            return CompactBinaryFormatter.FromByteBuffer(buffer, serializationContext);
        }

        /// <summary>
        /// Checks whtether the given buffer has NCHeader attached or not.
        /// </summary>
        /// <param name="serializedBuffer"></param>
        /// <returns></returns>
        internal static bool HasHCHeader(byte[] serializedBuffer)
        {
            byte[] header = new byte[5];
            if (serializedBuffer.Length >= NCHeader.Length)
            {
                for (int i = 0; i < NCHeader.Length; i++)
                {
                    header[i] = serializedBuffer[i];
                }
                return NCHeader.CompareTo(header);
            }
            return false;
        }

        /// <summary>
        /// Called recursively and iterates through the string at every '_dictionary' occurance until the string ends
        /// </summary>
        /// <param name="protocolString">String sent by the server</param>
        /// <param name="startIndex">start of string</param>
        /// <param name="endIndex">sent by the server</param>
        /// <returns>complete hashmap as stored in config read by service</returns>
        public static Hashtable GetTypeMapFromProtocolString(string protocolString, ref int startIndex, ref int endIndex)
        {
            endIndex = protocolString.IndexOf('"', startIndex + 1);
            Hashtable tbl = new Hashtable();
            string token = protocolString.Substring(startIndex, (endIndex) - (startIndex));

            if (token == "__dictionary")
            {
                startIndex = endIndex + 1;
                endIndex = protocolString.IndexOf('"', endIndex + 1);
                int dicCount = Convert.ToInt32(protocolString.Substring(startIndex, (endIndex) - (startIndex)));

                for (int i = 0; i < dicCount; i++)
                {
                    startIndex = endIndex + 1;
                    endIndex = protocolString.IndexOf('"', endIndex + 1);
                    string key = protocolString.Substring(startIndex, (endIndex) - (startIndex));

                    startIndex = endIndex + 1;
                    endIndex = protocolString.IndexOf('"', endIndex + 1);
                    string value = protocolString.Substring(startIndex, (endIndex) - (startIndex));

                    if (value == "__dictionary")
                    {
                        tbl[key] = GetTypeMapFromProtocolString(protocolString, ref startIndex, ref endIndex);
                    }
                    else
                    {
                        tbl[key] = value;
                    }
                }
            }
            return tbl;
        }

        public static string GetProtocolStringFromTypeMap(Hashtable typeMap)
        {
            System.Collections.Stack st = new Stack();
            StringBuilder protocolString = new StringBuilder();
            protocolString.Append("__dictionary").Append("\"");
            protocolString.Append(typeMap.Count).Append("\"");

            IDictionaryEnumerator mapDic = typeMap.GetEnumerator();
            while (mapDic.MoveNext())
            {
                if (mapDic.Value is Hashtable)
                {
                    st.Push(mapDic.Value);
                    st.Push(mapDic.Key);
                }
                else
                {
                    protocolString.Append(mapDic.Key).Append("\"");
                    protocolString.Append(mapDic.Value).Append("\"");
                }
            }

            while (st.Count != 0 && st.Count % 2 == 0)
            {
                protocolString.Append(st.Pop() as string).Append("\"");
                protocolString.Append(GetProtocolStringFromTypeMap(st.Pop() as Hashtable));
            }
            return protocolString.ToString();
        }

        //Helper method to convert Attrib object to NonComapactField


        public struct TypeHandlePair
        {
            public Type _type;           
            public short _handle;
            public TypeHandlePair(Type type, short handle)
            {
                this._type = type;
                this._handle = handle;               
            }
        }
    }
}
