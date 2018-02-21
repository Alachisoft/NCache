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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.Common
{
    public static partial class HashUtil
    {
        public static int CombineHashCodes(params int[] hashes)
        {
            int hash = 0;

            for (int index = 0; index < hashes.Length; index++)
            {
                hash = (hash << 5) + hash;
                hash ^= hashes[index];
            }

            return hash;
        }

        static int GetEntryHash(object entry)
        {
            int entryHash = 0x61E04917; // slurped from .Net runtime internals...

            if (entry != null)
            {
                object[] subObjects = entry as object[];

                if (subObjects != null)
                    entryHash = CombineHashCodes(subObjects);
                else
                    entryHash = entry.GetHashCode();
            }

            return entryHash;
        }

        public static int CombineHashCodes(params object[] objects)
        {
            int hash = 0;

            for (int index = 0; index < objects.Length; index++)
            {
                hash = (hash << 5) + hash;
                hash ^= GetEntryHash(objects[index]);
            }

            return hash;
        }

        public static int CombineHashCodes(int hash1, int hash2)
        {
            return ((hash1 << 5) + hash1) ^ hash2;
        }

        public static int CombineHashCodes(int hash1, int hash2, int hash3)
        {
            int hash = CombineHashCodes(hash1, hash2);
            return ((hash << 5) + hash) ^ hash3;
        }

        public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4)
        {
            int hash = CombineHashCodes(hash1, hash2, hash3);
            return ((hash << 5) + hash) ^ hash4;
        }

        public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4, int hash5)
        {
            int hash = CombineHashCodes(hash1, hash2, hash3, hash4);
            return ((hash << 5) + hash) ^ hash5;
        }

        public static int CombineHashCodes(object obj1, object obj2)
        {
            return CombineHashCodes(obj1.GetHashCode(), obj2.GetHashCode());
        }

        public static int CombineHashCodes(object obj1, object obj2, object obj3)
        {
            return CombineHashCodes(obj1.GetHashCode(), 
                                    obj2.GetHashCode(),
                                    obj3.GetHashCode());
        }

        public static int CombineHashCodes(object obj1, object obj2, object obj3, object obj4)
        {
            return CombineHashCodes(obj1.GetHashCode(),
                                    obj2.GetHashCode(),
                                    obj3.GetHashCode(),
                                    obj4.GetHashCode());
        }

        public static int CombineHashCodes(object obj1, object obj2, object obj3, object obj4, object obj5)
        {
            return CombineHashCodes(obj1.GetHashCode(), 
                                    obj2.GetHashCode(), 
                                    obj3.GetHashCode(),
                                    obj4.GetHashCode(),
                                    obj5.GetHashCode());
        }
    }
}
