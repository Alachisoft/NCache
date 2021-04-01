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
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.Util
{
    public class TypeInfoMap
    {
        private int _typeHandle = 0;
        private Hashtable _map;
        private Hashtable _typeToHandleMap;

        public TypeInfoMap(Hashtable indexClasses, Dictionary<string, IEnumerable<KeyValuePair<string, string>>> luceneClassAttribs)
        {
            CreateMap(indexClasses, luceneClassAttribs);
        }

        public TypeInfoMap(string protocolString)
        {
            // Used on client side i guess. Verify
            CreateMap(protocolString);
        }

        private void CreateMap(Hashtable indexClasses, Dictionary<string, IEnumerable<KeyValuePair<string, string>>> luceneClassAttribs)
        {
            _map = new Hashtable();
            _typeToHandleMap = new Hashtable();

            ArrayList typeList = new ArrayList();
            IDictionaryEnumerator enumerator = indexClasses.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Hashtable innerProps = enumerator.Value as Hashtable;
                typeList.Add(innerProps["name"]);
            }

            //we sort types to generate same type handles on all server nodes
            typeList.Sort();
            Hashtable typetoHandleMap = new Hashtable();


            foreach (string type in typeList)
            {
                typetoHandleMap[type] = _typeHandle;
                _typeHandle++;
            }


            IDictionaryEnumerator ie = indexClasses.GetEnumerator();
            while (ie.MoveNext())
            {
                Hashtable innerProps = ie.Value as Hashtable;
                if (innerProps != null)
                {
                    Hashtable type = new Hashtable();
                    Hashtable attributes = new Hashtable();
                    ArrayList attribList = new ArrayList();
                    // previous code iterated over all the properties and extracted attributes
                    // now directly taking out only attributes

                    Hashtable attribs = innerProps["attributes"] as Hashtable;
                    if (attribs != null)
                    {
                        IDictionaryEnumerator ide = attribs.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            Hashtable attrib = ide.Value as Hashtable;
                            if (attrib != null)
                            {
                                attribList.Add(attrib["id"] as string);
                                attributes.Add(attrib["id"] as string, attrib["data-type"] as string);
                            }
                        }
                    }
                    // Handle Lucene Attribs here
                    string typeId = innerProps["id"] as string;
                    if (luceneClassAttribs.ContainsKey(typeId))
                    {
                        IEnumerable<KeyValuePair<string, string>> lAttribs = luceneClassAttribs[typeId];
                        foreach (KeyValuePair<string, string> kvp in lAttribs)
                        {
                            attribList.Add(kvp.Key);
                            attributes.Add(kvp.Key, kvp.Value);
                        }
                    }

                    int typehandle = (int)typetoHandleMap[innerProps["name"]];
                    type.Add("name", innerProps["name"] as string);
                    type.Add("attributes", attributes);
                    attribList.Sort();
                    type.Add("sequence", attribList);
                    _map.Add(typehandle, type);

                    _typeToHandleMap.Add(type["name"] as string, typehandle);
                }
            }
        }

        private void CreateMap(string value)
        {
            int startIndex = 0;
            int endIndex = value.IndexOf('"', startIndex + 1);

                int typeCount = Convert.ToInt32(value.Substring(startIndex, (endIndex) - (startIndex)));
                _map = new Hashtable(typeCount);
                _typeToHandleMap = new Hashtable(typeCount);

                int typeHandle;
                string typeName;

                for (int i = 0; i < typeCount; i++)
                {
                    startIndex = endIndex + 1;
                    endIndex = value.IndexOf('"', endIndex + 1);
                    typeHandle = Convert.ToInt32(value.Substring(startIndex, (endIndex) - (startIndex)));

                    startIndex = endIndex + 1;
                    endIndex = value.IndexOf('"', endIndex + 1);
                    typeName = value.Substring(startIndex, (endIndex) - (startIndex));

                    Hashtable typeMap = new Hashtable();
                    typeMap.Add("name", typeName);

                    startIndex = endIndex + 1;
                    endIndex = value.IndexOf('"', endIndex + 1);
                    int attributesCount = Convert.ToInt32(value.Substring(startIndex, (endIndex) - (startIndex)));

                    ArrayList attributes = new ArrayList(attributesCount);
                    string attributeName;

                    for (int j = 0; j < attributesCount; j++)
                    {
                        startIndex = endIndex + 1;
                        endIndex = value.IndexOf('"', endIndex + 1);
                        attributeName = value.Substring(startIndex, (endIndex) - (startIndex));

                        attributes.Add(attributeName);
                    }

                    typeMap.Add("sequence", attributes);
                    _map.Add(typeHandle, typeMap);
                    _typeToHandleMap.Add(typeMap["name"] as string, typeHandle);
                }
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle">Handle-id for the Type</param>
        /// <returns>The complete name of the Type for the given handle-id.</returns>
        public string GetTypeName(int handle)
        {
            return (string)((Hashtable)_map[handle])["name"];
        }

        public ArrayList GetAttribList(int handleId)
        {
            return (ArrayList)((Hashtable)_map[handleId])["sequence"];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle">Handle-id of the Type</param>
        /// <returns>Hastable contaning the attribut list of the Type.</returns>
        public Hashtable GetAttributes(int handle)
        {
            return (Hashtable)((Hashtable)_map[handle])["attributes"];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName">Name of the Type</param>
        /// <returns>Hastable contaning the attribut list of the Type.</returns>
        public ArrayList GetAttributes(string typeName)
        {
            int handle = GetHandleId(typeName);
            if (handle != -1 && _map.Contains(handle))
                return (ArrayList)(((Hashtable)_map[handle])["attributes"]);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName">Name of the Type</param>
        /// <returns>Hastable contaning the attribut list of the Type.</returns>
        public String GetAttributeType(string typeName,string attributeName)
        {
            String attributeType = String.Empty;
            int handle = GetHandleId(typeName);
            if (handle != -1 && _map.Contains(handle))
            {
                Hashtable tbl= (Hashtable)((Hashtable)_map[handle])["attributes"];
                attributeType = tbl[attributeName] as string;              
            }

            return attributeType;
        }

        public string ToProtocolString()
        {
           StringBuilder protocolString = new StringBuilder();
            protocolString.Append(_map.Count).Append("\"");

            IDictionaryEnumerator mapDic = _map.GetEnumerator();
            while (mapDic.MoveNext())
            {
                protocolString.Append((int)mapDic.Key).Append("\"");

                Hashtable type = mapDic.Value as Hashtable;
                protocolString.Append(type["name"] as string).Append("\"");

                ArrayList attributes = (ArrayList)type["sequence"];
                protocolString.Append(attributes.Count).Append("\"");

                for (int i = 0; i < attributes.Count; i++)
                {
                    protocolString.Append(attributes[i] as string).Append("\"");
                }
            }
            string typeInfo=  protocolString.ToString();
            return typeInfo;
        }

        public int GetHandleId(string typeName)
        {
            if (_typeToHandleMap.Contains(typeName))
                return (int)_typeToHandleMap[typeName];
            return -1;
        }
    }
}
