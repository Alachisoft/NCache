// Copyright (c) 2018 Alachisoft
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common.Logger;
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
        private static ILogger _ncacheLog;
        public static ILogger NCacheLog
        {
            set
            {
                _ncacheLog = value;
            }
        }
        static Hashtable _attributeOrder = new Hashtable();
        static Hashtable _portibilaty = new Hashtable();
        static Hashtable _subTypeHandle = new Hashtable();

        //public static short UserdefinedArrayTypeHandle = 5000;
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
            catch (Exception)
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

        public static Hashtable GetNestedGenericCompactTypes(Hashtable portableTypesInfo, bool throwExceptions, string cacheContext, ref Hashtable nestedGenerics)
        {
            string typeName = "";
            short typeHandle = 0;
            Assembly asm = null;
            Hashtable framework = new Hashtable();      
            Hashtable genericInnerClasses = null;

            IDictionaryEnumerator ide = portableTypesInfo.GetEnumerator();
            while (ide.MoveNext())
            {
                genericInnerClasses = null;
                string assembly = null;
                Hashtable portableTypes = (Hashtable)ide.Value;
                IDictionaryEnumerator ide4 = portableTypes.GetEnumerator();

                while (ide4.MoveNext())
                {
                    Hashtable typeInfo = (Hashtable)ide4.Value;
                    if (typeInfo.Contains("name") && typeInfo["type"].ToString().Equals("net"))
                    {
                        if (typeInfo.Contains("arg-types"))
                        {
                            genericInnerClasses = (Hashtable)typeInfo["arg-types"];
                            if (genericInnerClasses != null)
                            {
                                Hashtable htGenTypes = new Hashtable();
                                IDictionaryEnumerator ide11 = genericInnerClasses.GetEnumerator();
                                while (ide11.MoveNext())
                                {                                   
                                    Hashtable retNestedTypes = GetNestedGenericCompactTypes((Hashtable)ide11.Value, throwExceptions, cacheContext, ref nestedGenerics);
                                    int argTypeNo = htGenTypes.Count + 1;
                                    if (retNestedTypes.Count > 0)
                                    {
                                        Hashtable[] htArr = new Hashtable[retNestedTypes.Count];
                                        for (int i = 0; i < retNestedTypes.Count; i++)
                                        {
                                            htArr[i] = new Hashtable();
                                        }
                                        retNestedTypes.Values.CopyTo(htArr, 0);
                                        Hashtable htArgType = null;
                                        if (retNestedTypes.Count > 1)
                                        {
                                            htArgType = htArr[0];
                                            for (int i = 1; i < htArr.Length; i++)
                                            {
                                                htArgType = GetArgumentTypesCombination(htArgType, htArr[i], i - 1, argTypeNo);
                                            }
                                            IDictionaryEnumerator ide99 = htArgType.GetEnumerator();
                                            while (ide99.MoveNext())
                                            {
                                                htGenTypes.Add((string)ide99.Key, (System.Collections.Queue)ide99.Value);                                              
                                            }
                                        }
                                        else
                                        {
                                            htArgType = htArr[0];       
                                            IDictionaryEnumerator ide12 = htArgType.GetEnumerator();
                                            while (ide12.MoveNext())
                                            {
                                                System.Collections.Queue q = new System.Collections.Queue();
                                                q.Enqueue(new TypeHandlePair((Type)ide12.Key, (short)ide12.Value));
                                                htGenTypes.Add(argTypeNo.ToString(), q);
                                                argTypeNo++;
                                            }
                                        }
                                        
                                    }                                   
                                }
                                genericInnerClasses = htGenTypes;
                            }                          
                        }
                        try
                        {

                            assembly = (string)typeInfo["assembly"];
                            if (assembly != null && assembly.StartsWith("System, "))
                            {
                                Type currentType = typeof(System.Collections.Generic.SortedDictionary<,>);
                                assembly = currentType.Assembly.FullName;
                                asm = currentType.Assembly;
                            }
                            else if (assembly != null && assembly.StartsWith("mscorlib, "))
                            {
                                string str = String.Empty;
                                Type currentType = str.GetType();
                                assembly = currentType.Assembly.FullName;
                                asm = currentType.Assembly;
                            }
                            else
                                asm = Assembly.Load(assembly);

                            if (asm.FullName != assembly)
                                throw new Exception("Loaded assembly version is different from the registered version");
                        }
                        catch (Exception e)
                        { 
                            if (throwExceptions && !(e is FileNotFoundException || e is TypeLoadException))
                                throw new CompactSerializationException(e.Message + "\n GetCompactTypes(" + e.GetType().ToString() + "):" + (string)typeInfo["name"]);
                            else
                            {
                                if(_ncacheLog != null)
                                    _ncacheLog.Error("SerializationUtil.GetCompactTypes", e.Message);
                            }
                            continue;
                        }
                    }
                    else
                        continue;

                    Type type = null;

                    if (typeInfo.Contains("name"))
                    {
                        typeHandle = Convert.ToInt16((String)ide.Key);

                        typeName = (string)typeInfo["name"];
                        try
                        {
                            type = null;
                            type = asm.GetType(typeName);
                        }
                        catch (Exception e)
                        {
                            type = null;
                            if (throwExceptions)
                                throw new CompactSerializationException(e.Message);
                        }
                        if (type != null)
                        {
                            if (!typeInfo.Contains("arg-types") && !framework.Contains(type))
                            {
                                Hashtable ht = new Hashtable();
                                ht.Add(type, typeHandle);
                                int argNum = framework.Count + 1;
                                framework.Add(argNum.ToString(), ht);                               
                            }
                            else if (typeInfo.Contains("arg-types")) //in case of generics    
                            {
                                Hashtable resultantGenerics = AdjustGenericTypes(type, genericInnerClasses, typeHandle, ref nestedGenerics, throwExceptions);
                                int argNum = framework.Count + 1;
                                framework.Add(argNum.ToString(), resultantGenerics);
                            }                            
                        }
                        else
                        {
                            if (throwExceptions)
                                throw new Exception(typeName + " can not be registered with compact framework");
                            continue;
                        }                     
                    }
                }
            }            
            return framework;
        }
        
        private static Hashtable GetArgumentTypesCombination(Hashtable htArgType1, Hashtable htArgType2, int indx, int argTypeNo)
        {
            Hashtable htAll = new Hashtable();
            if (indx != 0)
            {
                IDictionaryEnumerator ide3 = htArgType2.GetEnumerator();
                while (ide3.MoveNext())
                {
                    IDictionaryEnumerator ide2 = htArgType1.GetEnumerator();
                    while (ide2.MoveNext())
                    {                       
                        System.Collections.Queue q = (System.Collections.Queue)((System.Collections.Queue)ide2.Value).Clone();
                        q.Enqueue(new TypeHandlePair((Type)ide3.Key, (short)ide3.Value));                  
                        htAll.Add(argTypeNo.ToString(), q);
                        argTypeNo++;
                    }                    
                }                
            }
            else
            {
                IDictionaryEnumerator ide = htArgType1.GetEnumerator();
                while (ide.MoveNext())
                {
                    IDictionaryEnumerator ide2 = htArgType2.GetEnumerator();
                    while (ide2.MoveNext())
                    {
                        Hashtable ht3 = new Hashtable();
                        System.Collections.Queue q = new System.Collections.Queue();
                        q.Enqueue(new TypeHandlePair((Type)ide.Key, (short)ide.Value));
                        q.Enqueue(new TypeHandlePair((Type)ide2.Key, (short)ide2.Value));                                             
                        htAll.Add(argTypeNo.ToString(), q);
                        argTypeNo++;
                    }
                }
            }
            return htAll;
        }
        private static Hashtable AdjustGenericTypes(Type genericType, Hashtable genInnerTypes, short genTypeHandle, ref Hashtable nestedGenerics, bool throwExceptions)
        {
            Hashtable genNestedTypes = new Hashtable();
            if (genInnerTypes != null)
            {                
                IDictionaryEnumerator ide1 = genInnerTypes.GetEnumerator();
                while (ide1.MoveNext())
                {                    
                    System.Collections.Queue genConcreteTypes = (System.Collections.Queue)ide1.Value;
                    IEnumerator ide = genConcreteTypes.GetEnumerator();
                    int genArgsCount = genericType.GetGenericArguments().Length;
                    Type[] parms = new Type[genArgsCount];
                    SortedList sortedTypes = new SortedList(genArgsCount);
                    int index = 0;
                    short typeHandle = 0;
                    while (ide.MoveNext())
                    {
                        TypeHandlePair thp = (TypeHandlePair)ide.Current;
                        if (!genNestedTypes.Contains((Type)thp._type) && CheckForBuiltinSurrogate((Type)thp._type))
                        {
                            if (!nestedGenerics.Contains((Type)thp._type))
                                nestedGenerics.Add((Type)thp._type, (short)thp._handle);                         
                        }

                        if (typeof(Dictionary<,>) == genericType && index == 0)
                        {
                            Type typ = typeof(System.Collections.Generic.Dictionary<,>);
                            if (!nestedGenerics.Contains(typ))
                                nestedGenerics.Add(typ, genTypeHandle);
                        }
                        if (typeof(List<>).Equals(genericType) && index == 0)
                        {
                            Type typ = typeof(System.Collections.Generic.List<>);
                            if (!nestedGenerics.Contains(typ))
                                nestedGenerics.Add(typ, genTypeHandle);
                        }


                        if (index == 0 || ((Type)thp._type).IsGenericType)
                        {
                            typeHandle = (short)((short)thp._handle - 1);
                            index++;
                        }
                        sortedTypes.Add((short)thp._handle, (Type)thp._type);
                    }
                    for (int indx = 0; indx < sortedTypes.Count; indx++)
                    {
                        if (indx < parms.Length)
                            parms[indx] = (Type)sortedTypes.GetByIndex(indx);                        
                    }
                    Type genType = null;
                    try
                    {
                        genType = genericType.MakeGenericType(parms);
                    }
                    catch (Exception ex)
                    {
                        if (throwExceptions)
                        {
                            string data = "param count : " + parms.Length.ToString() + "\n\n";
                            for (int i = 0; i < parms.Length; i++)
                            {
                                data += "  FullName Arg " + i + " : " + ((Type)parms[i]).FullName;
                                data += "  Name Arg " + i + " : " + ((Type)parms[i]).Name + "\n";
                            }
                            throw new Exception(genericType.FullName + " can not be registered with compact framework ( ex.Message : " + ex.Message + " ) ex.Tostring() : " + ex.ToString() + "Data:" + data);
                        }
                    }
                    if ( genType != null && !genNestedTypes.Contains(genType))
                    {
                        typeHandle = GetUniqueHandle(genNestedTypes, nestedGenerics, sortedTypes);
                        genNestedTypes.Add(genType, typeHandle);
                    }
                }
               
            }
            return genNestedTypes;
        }
        private static short GetUniqueHandle(Hashtable genNestedTypes, Hashtable concreteNested, SortedList sortedTypes)
        {
            short handle = 5000;
            bool uniqueHandle = true;
            IDictionaryEnumerator ideArgTypes = sortedTypes.GetEnumerator();
            while (ideArgTypes.MoveNext())
            {
                uniqueHandle = true;
                handle = (short)((short)ideArgTypes.Key - 1);
                IDictionaryEnumerator ideGeneric = genNestedTypes.GetEnumerator();
                while (ideGeneric.MoveNext())
                {
                    if ((short)ideGeneric.Value == handle)
                    {
                        uniqueHandle = false;
                        break;
                    }
                }
                IDictionaryEnumerator ideConcrete = concreteNested.GetEnumerator();
                while (ideConcrete.MoveNext())
                {
                    if ((short)ideConcrete.Value == handle)
                    {
                        uniqueHandle = false;
                        break;
                    }
                }
                if (uniqueHandle)
                    break;
            }
            return handle;
        }
        private static bool CheckForBuiltinSurrogate(Type type)
        {
            if (!type.IsPrimitive && type != typeof(DateTime) && type != typeof(DateTime[]) && type != typeof(ArrayList) && type != typeof(ArrayList[]) && type != typeof(Hashtable) && type != typeof(Hashtable[]) && type != typeof(String) && type != typeof(String[])
                && type != typeof(Byte[]) && type != typeof(SByte[]) && type != typeof(Char[]) && type != typeof(Boolean[]) && type != typeof(Int16[]) && type != typeof(Int32[]) && type != typeof(Int64[]) && type != typeof(Single[]) && type != typeof(Double[])
                && type != typeof(Decimal[]) && type != typeof(UInt16[]) && type != typeof(UInt32[]) && type != typeof(UInt64[]) && type != typeof(Guid) && type != typeof(Guid[]) && type != typeof(TimeSpan) && type != typeof(TimeSpan[]))
                return true;
            return false;
        }
    }
}