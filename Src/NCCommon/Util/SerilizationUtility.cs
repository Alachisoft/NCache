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

using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Note: 
    /// 
    /// Serialization Scheme: Methods in this class follow the below given routine
    /// 1. Flag [True,False]. true in case of data 
    /// if True:
    ///     2. "Size" of the structure
    /// 3. actual data i.e Key and value both as objects.
    /// 
    /// Deserialization Scheme: 
    /// 1. Read Flag
    ///     Incase of False: return null
    /// 2. Read size
    /// 3. extract data (Key,Value) and cast them accordingly
    /// 4. return the casted data structure
    /// </summary>
    public class SerilizationUtility
    {
        /// <summary>
        /// serializes dictionary. Incase of empty dictionary a boolean of value= "false" is serialized ; 
        /// else serializes boolean,count and keys,values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="writer"></param>
        public static void SerializeDictionary (IDictionary dictionary, Runtime.Serialization.IO.CompactWriter writer)
        {

            if (dictionary== null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(dictionary.Count);
                for (IDictionaryEnumerator i = dictionary.GetEnumerator(); i.MoveNext(); )
                {
                    writer.WriteObject(i.Key);
                    writer.WriteObject(i.Value);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="writer"></param>
        public static void SerializeDictionary<K, V>(IDictionary<K, V> dictionary, Runtime.Serialization.IO.CompactWriter writer)
        {

            if (dictionary == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(dictionary.Count);
                for (IEnumerator<KeyValuePair<K, V>> i = dictionary.GetEnumerator(); i.MoveNext(); )
                {
                    writer.WriteObject(i.Current.Key);
                    writer.WriteObject(i.Current.Value);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IDictionary<T, V> DeserializeDictionary<T, V>(Runtime.Serialization.IO.CompactReader reader)
        {
            T key;
            V val;
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                IDictionary<T, V> dictionary = new HashVector<T, V>();
                int Length = reader.ReadInt32();
                for (int i = 0; i < Length; i++)
                {
                    key = (T)reader.ReadObject();
                    val = (V)reader.ReadObject();

                    dictionary.Add(key, val);
                }
                return dictionary;
            }
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="writer"></param>
        public static void SerializeList<T>(List<T> list, Runtime.Serialization.IO.CompactWriter writer)
        {
            if (list == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(list.Count);
                for (int i = 0; i < list.Count;i++ )
                {
                    writer.WriteObject(list[i]);
                    
                }
            }
        }

        public static List<T> DeserializeList<T>(Runtime.Serialization.IO.CompactReader reader)
        {
             bool flag = reader.ReadBoolean();

             if (flag)
             {
                 int length = reader.ReadInt32();
                 List<T> list = new List<T>();

                 for (int i = 0; i < length; i++)
                     list.Add((T)reader.ReadObject());

                 return list;
             }
             else
                 return null;
        }

        public static void SerializeClusteredList<T>(ClusteredList<T> list, Runtime.Serialization.IO.CompactWriter writer)
        {
            if (list == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    writer.WriteObject(list[i]);

                }
            }
        }


        public static ClusteredList<T> DeserializeClusteredList<T>(Runtime.Serialization.IO.CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                int length = reader.ReadInt32();
                ClusteredList<T> list = new ClusteredList<T>();

                for (int i = 0; i < length; i++)
                    list.Add((T)reader.ReadObject());

                return list;
            }
            else
                return null;
        }
    }
}
