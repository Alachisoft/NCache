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
using System.Collections.Generic;
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{

    public class StringPool
    {
        private static Hashtable _stringPool = new Hashtable();

        private static Dictionary<String, ushort> _providerIDsPool = new Dictionary<String, ushort>();
        private static Dictionary<ushort, String> _providerNamesPool = new Dictionary<ushort, String>();


        private static ushort _providerID = 0;

        private static object _stringLock = new object();
        private static object _providerLock = new object();


        /// <summary>
        /// Gets a string from the pool.
        /// </summary>
        /// <returns></returns>
        public static String PoolString(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            lock (_stringLock)
            {
                if (!_stringPool.Contains(str))
                {
                    _stringPool[str] = str;
                }

                return _stringPool[str] as string;
            }
        }

        /// <summary>
        /// Gets provider from the pool.
        /// </summary>
        /// <returns></returns>
        public static void PoolProviderName(String providerName)
        {
            lock (_providerLock)
            {
                if (!_providerIDsPool.ContainsKey(providerName))
                {
                    _providerNamesPool[++_providerID] = providerName;
                    _providerIDsPool[providerName] = _providerID;
                }               
            }
        }

        /// <summary>
        /// Gets provider from the pool.
        /// </summary>
        /// <returns></returns>
        public static ushort GetProviderID(String providerName)
        {           
            if (!String.IsNullOrEmpty(providerName)&& _providerIDsPool.ContainsKey(providerName))
            {
                return _providerIDsPool[providerName];
            }

            return 0;
        }


        /// <summary>
        /// Gets provider from the pool.
        /// </summary>
        /// <returns></returns>
        public static String GetProviderName(ushort providerID)
        {
            if (_providerNamesPool.ContainsKey(providerID))
            {
                return _providerNamesPool[providerID];
            }

            return String.Empty;
        }


        /// <summary>
        /// Releases all the Strings.
        /// </summary>
        public static void Clear()
        {
            lock (_stringLock)
            {
                _stringPool.Clear();
            }
        }
    }
}
