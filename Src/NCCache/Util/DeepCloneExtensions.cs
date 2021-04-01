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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Util
{
    public static class DeepCloneExtensions
    {
        public static Hashtable DeepClone(this Hashtable hashtable)
        {
            if (hashtable == null)
                return null;

            if (hashtable.Count == 0)
                return new Hashtable();

            var clonedHashtable = new Hashtable(hashtable.Count);

            foreach (DictionaryEntry entry in hashtable)
            {
                if (entry.Value is Hashtable innerHashtable)
                {
                    clonedHashtable[entry.Key] = innerHashtable.DeepClone();
                }
                else if (entry.Value is ICloneable cloneable)
                {
                    clonedHashtable[entry.Key] = cloneable.Clone();
                }
                else
                {
                    clonedHashtable[entry.Key] = entry.Value;
                }
            }
            return clonedHashtable;
        }

        public static HashVector DeepClone(this HashVector hashVector)
        {
            if (hashVector == null)
                return null;

            if (hashVector.Count == 0)
                return new HashVector();

            var clonedHashVector = new HashVector(hashVector.Count);

            foreach (DictionaryEntry entry in hashVector)
            {
                if (entry.Value is HashVector innerHashVector)
                {
                    clonedHashVector[entry.Key] = innerHashVector.DeepClone();
                }
                else if (entry.Value is ICloneable cloneable)
                {
                    clonedHashVector[entry.Key] = cloneable.Clone();
                }
                else
                {
                    clonedHashVector[entry.Key] = entry.Value;
                }
            }
            return clonedHashVector;
        }

        public static IDictionary DeepClone(this IDictionary dictionary)
        {
            if (dictionary == null)
                return null;

            var clonedDictionary = Activator.CreateInstance(dictionary.GetType()) as IDictionary;

            if (dictionary.Count == 0)
                return clonedDictionary;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is IDictionary innerDictionary)
                {
                    clonedDictionary[entry.Key] = innerDictionary.DeepClone();
                }
                else if (entry.Value is ICloneable cloneable)
                {
                    clonedDictionary[entry.Key] = cloneable.Clone();
                }
                else
                {
                    clonedDictionary[entry.Key] = entry.Value;
                }
            }
            return clonedDictionary;
        }

        public static ArrayList DeepClone(this ArrayList arrayList)
        {
            if (arrayList == null)
                return null;

            if (arrayList.Count == 0)
                return new ArrayList();

            var clonedArrayList = new ArrayList(arrayList.Count);

            foreach (var item in arrayList)
            {
                if (item is ArrayList innerArrayList)
                {
                    clonedArrayList.Add(innerArrayList.DeepClone());
                }
                else if (item is ICloneable cloneable)
                {
                    clonedArrayList.Add(cloneable.Clone());
                }
                else
                {
                    clonedArrayList.Add(item);
                }
            }
            return clonedArrayList;
        }

        public static byte[] DeepClone(this byte[] bytes)
        {
            if (bytes == null)
                return null;

            var clonedBytes = new byte[bytes.Length];

            if (bytes.Length > 0)
                Buffer.BlockCopy(bytes, 0, clonedBytes, 0, bytes.Length);

            return clonedBytes;
        }

        public static bool[] DeepClone(this bool[] bools)
        {
            if (bools == null)
                return null;

            var clonedBools = new bool[bools.Length];

            if (bools.Length > 0)
                Buffer.BlockCopy(bools, 0, clonedBools, 0, bools.Length * sizeof(bool));

            return clonedBools;
        }

        public static string[] DeepClone(this string[] strings)
        {
            if (strings == null)
                return null;

            var clonedStrings = new string[strings.Length];

            if (strings.Length > 0)
                Array.Copy(strings, clonedStrings, strings.Length);

            return clonedStrings;
        }

        public static DateTime[] DeepClone(this DateTime[] dateTimes)
        {
            if (dateTimes == null)
                return null;

            var clonedDateTimes = new DateTime[dateTimes.Length];

            if (dateTimes.Length > 0)
                Array.Copy(dateTimes, clonedDateTimes, dateTimes.Length);

            return clonedDateTimes;
        }
    }
}
