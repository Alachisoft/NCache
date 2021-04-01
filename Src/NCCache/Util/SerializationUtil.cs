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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures;

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

        public static short UserdefinedArrayTypeHandle = 5000;
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
                    byte[] serializedData = null;
                    if (serializedObject is byte[])
                    {
                       
                        serializedData = (byte[])serializedObject;
                    }
                    else if (serializedObject is UserBinaryObject)
                    {
                        serializedData = ((UserBinaryObject)serializedObject).GetFullObject();
                    }
                    deserialized = CompactBinaryFormatter.FromByteBuffer(serializedData, serializationContext);
                }
            }
            catch (Exception ex)
            {
                _ncacheLog.Error(ex.ToString());
                //Kill the exception; it is possible that object was serialized by Java
                //or from any other domain which can not be deserialized by us.
                deserialized = serializedObject;
            }

            return deserialized;
        }

        public static object SafeSerialize(object serializableObject, SerializationFormat serializer, string serializationContext, ref BitSet flag)
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
            Hashtable tbl = new Hashtable(new EqualityComparer());
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

        private static Hashtable GetNonCompactFields(Hashtable nonCompactAttributes) 
        {
            Hashtable nonCompactFieldsTable = new Hashtable();
            IDictionaryEnumerator nonCompactIDE= nonCompactAttributes.GetEnumerator();
            while (nonCompactIDE.MoveNext()) 
            {
                NonCompactField tempNonCompactField = new NonCompactField();
                Hashtable tempAttrib = (Hashtable)nonCompactIDE.Value;
                tempNonCompactField.ID = (string)tempAttrib["id"];
                tempNonCompactField.Name = (string)tempAttrib["name"];
                tempNonCompactField.Type = (string)tempAttrib["type"];
                nonCompactFieldsTable.Add(tempNonCompactField.ID,tempNonCompactField);
            }
            return nonCompactFieldsTable;
        }



        public static Hashtable CheckNonCompactAttrib(Type type, Hashtable compactTypes) 
        {
            string typeName = type.FullName;
            IEnumerator ieOuter = compactTypes.GetEnumerator();
            while (ieOuter.MoveNext()) 
            {
                DictionaryEntry entry = (DictionaryEntry)ieOuter.Current;
                Hashtable hshTable = (Hashtable)entry.Value;
                if (hshTable.Contains(typeName)) 
                {
                    Hashtable hshTypeInfo = (Hashtable)hshTable[typeName];
                    if (hshTypeInfo.Contains("non-compact-fields"))
                        return (Hashtable)hshTypeInfo["non-compact-fields"];                    
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the type, handle info and non-compact fields for the gives types.
        /// </summary>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public static Hashtable GetCompactTypes(Hashtable portableTypesInfo, bool throwExceptions, string cacheContext)
        {
            string typeName = "";
            short typeHandle = 0;
            Assembly asm = null;
            Hashtable framework = new Hashtable(new EqualityComparer());
            Hashtable typeTable = new Hashtable(new EqualityComparer());
            Hashtable porabilaty = new Hashtable(new EqualityComparer());
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
                        #region [For Generics]
                        if (typeInfo.Contains("arg-types"))
                        {
                            Hashtable nestedGenerics = new Hashtable(new EqualityComparer()); 
                            genericInnerClasses = (Hashtable)typeInfo["arg-types"];

                            Hashtable ht6 = GetGenericComapctTypes(genericInnerClasses, typeInfo, throwExceptions, cacheContext, ref nestedGenerics);

                            IDictionaryEnumerator ide55 = ht6.GetEnumerator();
                            while (ide55.MoveNext())
                            {
                                try
                                {
                                    Type nestedType = (Type)ide55.Key;

                                    if (!framework.ContainsKey(nestedType))
                                    {
                                        Hashtable _handleNonCompactFields = new Hashtable(new EqualityComparer()); 
                                        _handleNonCompactFields.Add("handle", ide55.Value);

                                        if (typeInfo.Contains("non-compact-fields"))
                                            _handleNonCompactFields.Add("non-compact-fields", GetNonCompactFields(typeInfo["non-compact-fields"] as Hashtable));

                                        framework.Add(nestedType,_handleNonCompactFields);
                                        
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (throwExceptions)
                                        throw new Exception("Class Name : " + typeInfo["name"].ToString() + " and Class Id : " + typeInfo["id"].ToString() + "\n" + "ide55.Key" + ide55.Key.ToString());
                                }
                            }
                            IDictionaryEnumerator ide15 = nestedGenerics.GetEnumerator();
                            while (ide15.MoveNext())
                            {
                                try
                                {
                                    Type nestedConcreteType = (Type)ide15.Key;

                                    if (!framework.ContainsKey(nestedConcreteType))
                                    {
                                        Hashtable _handleNonCompactFields = new Hashtable(new EqualityComparer());
                                        _handleNonCompactFields.Add("handle", ide15.Value);

                                        Hashtable nonCompactAttrib = CheckNonCompactAttrib(nestedConcreteType, portableTypesInfo);

                                        if (nonCompactAttrib!=null)
                                        {
                                            _handleNonCompactFields.Add("non-compact-fields", GetNonCompactFields(nonCompactAttrib));
                                        }

                                        framework.Add(nestedConcreteType, _handleNonCompactFields);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (throwExceptions)
                                        throw new Exception("Class Name : " + typeInfo["name"].ToString() + " and Class Id : " + typeInfo["id"].ToString() + "\n" + "ide55.Key" + ide15.Key.ToString());
                                }
                            }
                            nestedGenerics.Clear();
                            continue;
                        }
                        else if (typeInfo.Contains("is-generic") && typeInfo["is-generic"].ToString() == "True")
                            continue;

                        #endregion
                        try
                        {

                            assembly = (string)typeInfo["assembly"];

                            if (assembly != null && assembly.StartsWith("System, "))
                            {
                                Type currentType = typeof(SortedDictionary<,>);
                                assembly = currentType.Assembly.FullName;
                                asm = currentType.Assembly;
                            }
                            else if (assembly != null && assembly.StartsWith("mscorlib, "))
                            {
                                string str = string.Empty;
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
                                if (_ncacheLog != null)
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

                        bool portable = false;
                        try
                        {
                            portable = Convert.ToBoolean(typeInfo["portable"]);
                        }
                        catch (Exception)
                        {
                            portable = false;
                        }

                        //remove Version number attached
                        if (portable)
                        {
                            string[] typeNameArray = typeName.Split(':');
                            typeName = typeNameArray[0];
                            for (int i = 1; i < typeNameArray.Length - 1; i++)
                            {
                                typeName += "." + typeNameArray[i];
                            }
                        }

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
                            if (!framework.Contains(type))
                            {
                                Hashtable handleNonCompactFields = new Hashtable(new EqualityComparer());
                                handleNonCompactFields.Add("handle", typeHandle);

                                if (typeInfo.Contains("non-compact-fields"))
                                    handleNonCompactFields.Add("non-compact-fields", GetNonCompactFields(typeInfo["non-compact-fields"] as Hashtable));

                                framework.Add(type, handleNonCompactFields);
                            }
                        }
                        else
                        {
                            if (throwExceptions)
                                throw new Exception(typeName + " can not be registered with compact framework");
                            continue;
                        }
                        
                       
                        Hashtable attributeOrder = (Hashtable)typeInfo["attribute"];
                        Hashtable attributeUnion = null;
                        Hashtable[] orderedUnion = null;

                        if (portable)
                        {
                            attributeUnion = (Hashtable)portableTypes["Alachisoft.NCache.AttributeUnion"];
                            orderedUnion = new Hashtable[((Hashtable)(attributeUnion["attribute"])).Count];
                            
                            //Order AttributeUnion with orderNumber
                            if (attributeUnion != null && attributeUnion["attribute"] != null)
                            {
                                IDictionaryEnumerator union = ((Hashtable)attributeUnion["attribute"]).GetEnumerator();
                                while (union.MoveNext())
                                {
                                    Hashtable attribute = (Hashtable)union.Value;
                                    orderedUnion[Convert.ToInt32(attribute["order"]) - 1] = (Hashtable)attribute;
                                }
                            }

                        }

                        #region New Ordering
                        // Order of 0 means skip, -1 means it comes after EOF
                        if (attributeOrder != null && attributeOrder.Count > 0 && portable)
                        {
                            string[][] temp = new string[2][];
                            //Attribute Name
                            temp[0] = new string[orderedUnion.Length];
                            //Order Number
                            temp[1] = new string[orderedUnion.Length];

                            //Create a List of attributes of a class
                            Hashtable[] attributearray = new Hashtable[attributeOrder.Values.Count];
                            attributeOrder.Values.CopyTo(attributearray, 0);
                            ArrayList attributeList = new ArrayList();
                            attributeList.AddRange(attributearray);

                            int count = 0;
                            Hashtable toRemove = null;
                            while (count < orderedUnion.Length)
                            {
                                string attrib = (string)((Hashtable)orderedUnion[count])["order"];
                                //Serach for mapped-to in attributeList
                                foreach (Hashtable attribute in attributeList)
                                {
                                    if ((attrib) == (string)attribute["order"])
                                    {
                                        temp[0][count] = (string)attribute["name"];
                                        temp[1][count] = Convert.ToString(count + 1);
                                        toRemove = attribute;
                                        break;
                                    }
                                }

                                if (toRemove != null)
                                {
                                    attributeList.Remove(toRemove);
                                    toRemove = null;
                                }

                                if (temp[0][count] == null)
                                {
                                    temp[0][count] = "skip.attribute";
                                    temp[1][count] = "0";
                                }
                                count++;
                            }

                            //Add in all thats left of attributeList as after EOF
                            if (attributeList != null && attributeList.Count > 0)
                            {
                                string[][] temporary = new string[2][];

                                temporary = (string[][])temp.Clone();

                                temp[0] = new string[((Hashtable)attributeUnion["attribute"]).Count + attributeList.Count];
                                temp[1] = new string[((Hashtable)attributeUnion["attribute"]).Count + attributeList.Count];

                                for (int i = 0; i < temporary[0].Length; i++)
                                {
                                    temp[0][i] = temporary[0][i];
                                    temp[1][i] = temporary[1][i];
                                }
                                for (int i = temporary[0].Length; i < temporary[0].Length + attributeList.Count; i++)
                                {
                                    temp[0][i] = (string)((Hashtable)(attributeList[i - temporary[0].Length]))["name"];
                                    temp[1][i] = "-1";
                                }

                            }

                            if (CheckAlreadyRegistered(portable, cacheContext, (string)ide.Key, type, typeName))
                                continue;


                            typeTable.Add(typeName, temp);

                            _attributeOrder[cacheContext] = typeTable;

                            if (!porabilaty.Contains(Convert.ToInt16(ide.Key)))
                            {
                                porabilaty.Add(Convert.ToInt16(ide.Key), portable);
                                _portibilaty[cacheContext] = porabilaty; 
                            }

                            PopulateSubHandle(portable, cacheContext, (string)ide.Key, (string)typeInfo["handle-id"], type);
                        }
                        else
                        {
                            if (CheckAlreadyRegistered(portable, cacheContext, (string)ide.Key, type, typeName))
                                continue;

                            typeTable.Add(typeName, null);

                            _attributeOrder[cacheContext] = typeTable;

                            if (!porabilaty.Contains(Convert.ToInt16(ide.Key)))
                            {
                                porabilaty.Add(Convert.ToInt16(ide.Key), portable);
                                _portibilaty[cacheContext] = porabilaty; 
                            }

                            PopulateSubHandle(portable, cacheContext, (string)ide.Key, (string)typeInfo["handle-id"], type);
                        }

                        #endregion
                    }

                }
            }
            // framework Contains all Types registered to cache and its typeHandle as shown by config
            return framework;
        }

        private static Hashtable GetGenericComapctTypes(Hashtable genericInnerClasses, Hashtable typeInfo, bool throwExceptions, string cacheContext, ref Hashtable nestedGenerics)
        {
            if (genericInnerClasses != null)
            {
                Hashtable htGenTypes = new Hashtable(new EqualityComparer());
                bool isNotAsseblyFound = false;

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
                            htArr[i] = new Hashtable(new EqualityComparer());
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
                Assembly asm = null;
                string exceptionMsg = null;
                try
                {

                    string assembly = (string)typeInfo["assembly"];
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
                        if (_ncacheLog != null)
                        _ncacheLog.Error("SerializationUtil.GetCompactTypes", e.Message);
                        isNotAsseblyFound = true;
                        exceptionMsg = e.ToString();
                    }

                }
                Type type = null;
                short typeHandle;
                string typeName = "";
                if (typeInfo.Contains("name"))
                {
                    string str = typeInfo["id"].ToString();
                    typeHandle = Convert.ToInt16(str);

                    typeName = (string)typeInfo["name"];
                    try
                    {
                        if(asm!=null)
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
                        if (!typeInfo.Contains("arg-types") && !genericInnerClasses.Contains(type))
                            genericInnerClasses.Add(type, typeHandle);
                        else if (typeInfo.Contains("arg-types")) //in case of generics    
                        {
                            genericInnerClasses = AdjustGenericTypes(type, genericInnerClasses, typeHandle, ref nestedGenerics, throwExceptions);
                        }                        
                    }
                    else
                    {
                       
                        if(isNotAsseblyFound)
                            if (_ncacheLog != null)
                            _ncacheLog.Error("SerializationUtil.GetCompactTypes", exceptionMsg);
                        isNotAsseblyFound = false;
                    }
                }
                
            }

            return genericInnerClasses;
        }

        public static Hashtable GetNestedGenericCompactTypes(Hashtable portableTypesInfo, bool throwExceptions, string cacheContext, ref Hashtable nestedGenerics)
        {
            string typeName = "";
            short typeHandle = 0;
            Assembly asm = null;
            Hashtable framework = new Hashtable(new EqualityComparer());       
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
                                Hashtable htGenTypes = new Hashtable(new EqualityComparer());
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
                                            htArr[i] = new Hashtable(new EqualityComparer());
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
                                Hashtable ht = new Hashtable(new EqualityComparer());
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
            Hashtable htAll = new Hashtable(new EqualityComparer()); 
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
                        Hashtable ht3 = new Hashtable(new EqualityComparer());
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
        private static Hashtable AdjustGenericTypes(Type genericType, Hashtable genInnerTypes, short genTypeHandle, ref Hashtable nestedGenerics, bool throwExceptions)
        {
            Hashtable genNestedTypes = new Hashtable(new EqualityComparer());
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
                            Type typ = typeof(Dictionary<,>);

                            if (!nestedGenerics.Contains(typ))
                                nestedGenerics.Add(typ, genTypeHandle);
                        }
                        if (typeof(List<>).Equals(genericType) && index == 0)
                        {
                            Type typ = typeof(List<>);

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
                && type != typeof(Decimal) && type != typeof(Decimal[]) && type != typeof(UInt16[]) && type != typeof(UInt32[]) && type != typeof(UInt64[]) && type != typeof(Guid) && type != typeof(Guid[]) && type != typeof(TimeSpan) && type != typeof(TimeSpan[]))
                return true;
            return false;
        }
        private static bool IsDotnetBuiltinType(string typeName)
        {           
            bool isBuiltinType = false;
            try
            {
                Type type = Type.GetType(typeName);
                if (type.IsPrimitive)
                    isBuiltinType = true;
                else
                {
                    switch (type.FullName)
                    {
                        case "System.DateTime":
                            isBuiltinType = true;
                            break;
                        case "System.DateTime[]":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.ArrayList":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.ArrayList[]":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.Hashtable":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.Hashtable[]":
                            isBuiltinType = true;
                            break;
                        case "System.String":
                            isBuiltinType = true;
                            break;
                        case "System.String[]":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.Generic.List`1":
                            isBuiltinType = true;
                            break;
                        case "System.Collections.Generic.Dictionary`2":
                            isBuiltinType = true;
                            break;
                    }
                }
            }
            catch (Exception)
            { }
            return isBuiltinType;
        }
        private static bool CheckAlreadyRegistered(bool portable, string cacheContext, string handle, Type type, string typeName)
        {
            if (portable)
            {
                if (_subTypeHandle.Contains(cacheContext))
                    if (((Hashtable)_subTypeHandle[cacheContext]).Contains(handle))
                        if (((Hashtable)((Hashtable)_subTypeHandle[cacheContext])[handle]).Contains(type))
                            return true;
            }
            else
            {
                if (_attributeOrder.Contains(cacheContext))
                    if (((Hashtable)_attributeOrder[cacheContext]).Contains(typeName))
                        return true;
            }

            return false;
        }


        public static void PopulateSubHandle(bool portable, string cacheContext, string handle, string subHandle, Type type)
        {
            if (portable)
            {
                if (!_subTypeHandle.Contains(cacheContext))
                    _subTypeHandle[cacheContext] = new Hashtable();

                if (!((Hashtable)_subTypeHandle[cacheContext]).Contains(handle))
                    ((Hashtable)_subTypeHandle[cacheContext]).Add(handle, new Hashtable(new EqualityComparer()));

                if (!((Hashtable)((Hashtable)_subTypeHandle[cacheContext])[handle]).Contains(type))
                    ((Hashtable)((Hashtable)_subTypeHandle[cacheContext])[handle]).Add(type, subHandle);
                else
                    throw new ArgumentException("Sub-Handle '" + subHandle + "' already present in " + cacheContext + " in class " + type.Name + " with Handle " + handle);
            }
        }

        /// <summary>
        /// Retruns back all registered Types w.r.t a cache, is only called at cache initialization
        /// </summary>
        /// <param name="cacheContext">Cache Name</param>
        /// <returns>Retruns back all registered Types w.r.t a cache</returns>
        public static Hashtable GetAttributeOrder(string cacheContext)
        {
            return (Hashtable)_attributeOrder[cacheContext];
        }

        public static bool GetPortibilaty(short handle, string cacheContext)
        {
            if (_portibilaty != null && _portibilaty.Contains(cacheContext) && ((Hashtable)_portibilaty[cacheContext]).Contains(handle))
                return (bool)((Hashtable)_portibilaty[cacheContext])[handle];
            else
                return false;
        }

        public static short GetSubTypeHandle(string cacheContext, string handle, Type type)
        {
            if (_subTypeHandle == null)
                return 0;

            if (!(((Hashtable)_subTypeHandle).Contains(cacheContext)))
                return 0;
            if (!(((Hashtable)_subTypeHandle[cacheContext]).Contains(handle)))
                return 0;
            return Convert.ToInt16((string)((Hashtable)((Hashtable)_subTypeHandle[cacheContext])[handle])[type]);
        }


        internal static void CompactSerialize(CacheEntry entry, string cacheContext, bool compressionEnable, long compressionThreshold)
        {
            throw new NotImplementedException();
        }

        public static object SafeSerializeInProc(object serializableObject, string serializationContext, ref BitSet flag, SerializationFormat serializationFormat, ref long size, UserObjectType userObjectType, bool seralize ,bool isCustomAttributeBaseSerialzed = false)
        {
            if (seralize)
                return SafeSerializeOutProc(serializableObject, serializationContext, ref flag, seralize, serializationFormat, ref size, userObjectType, isCustomAttributeBaseSerialzed);

            object serializableObjectUnser = serializableObject;

            if (size <= 0)
            {

                Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
                    return serializableObject;
                }
                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
                size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
            }
           

            return serializableObjectUnser;
        }

        public static object SafeSerializeOutProc(object serializableObject, string serializationContext, ref BitSet flag, bool isSerializationEnabled, SerializationFormat serializationFormat, ref long size, UserObjectType userObjectType, bool isCustomAttributeBaseSerialzed = false)
        {
            if (serializableObject != null && isSerializationEnabled)
            {
                Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
                    return serializableObject;
                }

                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);

                size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
            }
            return serializableObject;
        }

        public static T SafeDeserializeInProc<T>(object serializedObject, string serializationContext, BitSet flag, UserObjectType userObjectType, bool serailze)
        {
            if (serailze)
                return SafeDeserializeOutProc<T>(serializedObject, serializationContext, flag, serailze, userObjectType);

            return (T)serializedObject;
        }

        public static T SafeDeserializeOutProc<T>(object serializedObject, string serializationContext, BitSet flag, bool isSerializationEnabled, UserObjectType userObjectType)
        {
            object deserialized = serializedObject;
            if (serializedObject is byte[] && isSerializationEnabled)
            {
                if (flag != null && flag.IsBitSet(BitSetConstants.BinaryData))
                {
                    return (T)serializedObject;
                }
                deserialized = CompactBinaryFormatter.FromByteBuffer((byte[])serializedObject, serializationContext);
            }
            return (T)deserialized;
        }
    }
}