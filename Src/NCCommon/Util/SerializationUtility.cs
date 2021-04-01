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
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using Alachisoft.NCache.Runtime.Serialization.IO;

using Runtime = Alachisoft.NCache.Runtime;
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
    public class SerializationUtility
    {
        public const string SerializationConfigAttribute = "serialization";
        /// <summary>
        /// serializes dictionary. Incase of empty dictionary a boolean of value= "false" is serialized ; 
        /// else serializes boolean,count and keys,values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="writer"></param>

        public static void SerializeDictionary<K,V>(IDictionary<K,V> dictionary, Runtime.Serialization.IO.CompactWriter writer)
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
                for (IEnumerator<KeyValuePair<K,V>> i = dictionary.GetEnumerator(); i.MoveNext();)
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
        public static IDictionary<T, V> DeserializeDictionary<T,V>(Runtime.Serialization.IO.CompactReader reader,IEqualityComparer<T> comparer = null)
        {
            T key;
            V val;
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                IDictionary<T, V> dictionary = new HashVector<T, V>(comparer);
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
        /// it desirializes the actual dictionary not hashvector
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IDictionary<T, V> DeserializeDictionaryObject<T, V>(Runtime.Serialization.IO.CompactReader reader, IEqualityComparer<T> comparer = null)
        {
            T key;
            V val;
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                IDictionary<T, V> dictionary = new Dictionary<T, V>(comparer);
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
        public static void SerializeList<T>(IList<T> list, Runtime.Serialization.IO.CompactWriter writer)
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

        public static void SerializeClusteredArray<T>(ClusteredArray<T> array, Runtime.Serialization.IO.CompactWriter writer)
        {
            if (array == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(array.Length);
                writer.Write(array.LengthThreshold);
                for (int i = 0; i < array.Length; i++)
                {
                    writer.WriteObject(array[i]);
                }
            }
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

        public static ClusteredArray<T> DeserializeClusteredArray<T>(Runtime.Serialization.IO.CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                int length = reader.ReadInt32();
                int threshold = reader.ReadInt32();
                ClusteredArray<T> array = new ClusteredArray<T>(threshold, length);

                for (int i = 0; i < length; i++)
                    array[i] = (T)reader.ReadObject();

                return array;
            }
            else
                return null;
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

        public static void SerializeLL<T>(List<List<T>> list, CompactWriter writer)
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
                for (List<List<T>>.Enumerator i = list.GetEnumerator(); i.MoveNext();)
                {
                    SerializeList(i.Current, writer);
                }
            }
        }

        public static List<List<T>> DeserializeLL<T>(CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                int listLenght = reader.ReadInt32();
                List<List<T>> complist = new List<List<T>>();
                for (int i = 0; i < listLenght; i++)
                {
                    List<T> subList = DeserializeList<T>(reader);
                    complist.Add(subList);
                }
                return complist;
            }
            else
                return null;
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

        #region CQ structures
        /// <summary>
        /// Serializes dictionary containing a list used only in CQ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dList"></param>
        /// <param name="writer"></param>
        public static void SerializeDictionaryList<T, V>(IDictionary<T, IList<V>> dList, CompactWriter writer)
        {
            if (dList == null)
            {
                writer.Write(false);
                return;
            }
            else 
            {
                writer.Write(true);
                writer.Write(dList.Count);
                foreach (var item in dList)
                {
                    writer.WriteObject(item.Key);
                    SerializeList((List<V>)item.Value, writer);
                }
            }
        }

        public static void SerializeDictionaryHashSet<T, V>(IDictionary<T, HashSet<V>> dHashSet, CompactWriter writer)
        {
            if (dHashSet == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(dHashSet.Count);
                foreach (var item in dHashSet)
                {
                    writer.WriteObject(item.Key);
                    SerializeHashSet((HashSet<V>)item.Value, writer);
                }
            }
        }

        public static IDictionary<T, IList<V>> DeserializeDictionaryList<T,V>(CompactReader reader)
        {
             bool flag = reader.ReadBoolean();
            IDictionary<T, IList<V>> dList = new HashVector<T, IList<V>>();
            if (flag)
             {
                 T key;
                 int dictionarylength = reader.ReadInt32();
                 for (int i = 0; i < dictionarylength; i++)
                 {
                     List<V> valueList;
                     key = (T)reader.ReadObject();
                     valueList = DeserializeList<V>(reader);
                     dList.Add(key, valueList);
                 }

             }
            return dList;
        }
        public static IDictionary<T, HashSet<V>> DeserializeDictionaryHashSet<T, V>(CompactReader reader)
        {
            bool flag = reader.ReadBoolean();
            IDictionary<T, HashSet<V>> dHashSet = new HashVector<T, HashSet<V>>();
            if (flag)
            {
                T key;
                int dictionarylength = reader.ReadInt32();
                for (int i = 0; i < dictionarylength; i++)
                {
                    HashSet<V> valueHashSet;
                    key = (T)reader.ReadObject();
                    valueHashSet = DeserializeHashSet<V>(reader);
                    dHashSet.Add(key, valueHashSet);
                }

            }
            return dHashSet;
        }


        public static void SerializeDD<T, V, K>(Dictionary<T, Dictionary<V, K>> dList, Runtime.Serialization.IO.CompactWriter writer)
        {
            if (dList == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(dList.Count);
                for (IDictionaryEnumerator i = dList.GetEnumerator(); i.MoveNext(); )
                {
                    writer.WriteObject(i.Key);

                    SerializeDictionary((IDictionary<V, K>)i.Value, writer);
                }
            }
        }
        

        public static IDictionary<T, IDictionary<V, K>> DeserializeDD<T, V, K>(Runtime.Serialization.IO.CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                T key;
               
                int dictionarylength = reader.ReadInt32();
                IDictionary<T, IDictionary<V, K> >dList = new HashVector<T, IDictionary<V, K>>();
                for (int i = 0; i < dictionarylength; i++)
                {
                    IDictionary<V, K> valueList;
                    key = (T)reader.ReadObject();
                    valueList = DeserializeDictionary<V,K>(reader);
                    dList.Add(key, valueList);
                }
                return dList;
            }
            else
                return null;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ConcurrentDictionary<T, TV> DeserializeConcurrentDictionary<T, TV>(Runtime.Serialization.IO.CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                ConcurrentDictionary<T, TV> dictionary = new ConcurrentDictionary<T, TV>();
                int length = reader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    T key = (T)reader.ReadObject();
                    TV val = (TV)reader.ReadObject();

                    dictionary.TryAdd(key, val);
                }
                return dictionary;
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static HashSet<T> DeserializeHashSet<T>(CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                HashSet<T> dictionary = new HashSet<T>();
                DeserializeHashSetInternal<T>(reader, dictionary);
                return dictionary;
            }
            return null;
        }
     
        private static void DeserializeHashSetInternal<T>(CompactReader reader, ICollection<T> deserailzedSet)
        {
            int length = reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                T key = (T)reader.ReadObject();

                deserailzedSet.Add(key);
            }

        }

        public static void SerializeHashSet<T>(HashSet<T> hashSet, CompactWriter writer)
        {
            SerializedSetInternal<T>(hashSet, writer);
        }
       
        private static void SerializedSetInternal<T>(ICollection<T> hashSet, CompactWriter writer)
        {
            if (hashSet == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                writer.Write(hashSet.Count);
                foreach (var item in hashSet)
                {
                    writer.WriteObject(item);
                }
            }
        }
    }
}
