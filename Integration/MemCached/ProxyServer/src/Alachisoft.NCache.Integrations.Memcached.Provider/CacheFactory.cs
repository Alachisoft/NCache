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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Integrations.Memcached.Provider
{
    public class CacheFactory
    {
        public static IMemcachedProvider CreateCacheProvider(string cacheProviderName)
        {
            lock (typeof(CacheFactory))
            {
                try
                {
                    if (MemcachedProvider.Instance == null)
                    {
                        MemcachedProvider.Instance = new MemcachedProvider();
                        MemcachedProvider.Instance.InitCache(cacheProviderName);
                    }
                }
                catch (Exception)
                {
                    MemcachedProvider.Instance = null;
                    throw;
                }
            }

            return MemcachedProvider.Instance;
        }

        public static void DisposeCacheProvider()
        {
            if (MemcachedProvider.Instance != null)
                MemcachedProvider.Instance.Dispose();
        }
    }


}
