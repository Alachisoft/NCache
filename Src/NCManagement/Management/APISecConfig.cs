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

namespace Alachisoft.NCache.Management
{
    class APISecConfig
    {
        public static Hashtable oldCacheUsers = null;
        private static ArrayList _cacheList = new ArrayList();

        public static void fillCacheList()
        {
            if (oldCacheUsers != null)
            {
                IDictionaryEnumerator ide = oldCacheUsers.GetEnumerator();
                while (ide.MoveNext())
                {
                    string cacheName = ide.Key.ToString();
                    _cacheList.Add(cacheName);
                }
            }
        }

        public static Hashtable UpdateCacheUserList(Hashtable newCacheUsers)
        {
            try
            {
                IDictionaryEnumerator ide = newCacheUsers.GetEnumerator();
                while (ide.MoveNext())
                {
                    string cacheName = ide.Key.ToString();
                    if (_cacheList.Contains(cacheName))
                    {
                        oldCacheUsers[cacheName.ToLower()] = ide.Value;
                    }
                    else
                    {
                        oldCacheUsers.Add(cacheName.ToLower(), ide.Value);
                    }
                }
                return oldCacheUsers;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}