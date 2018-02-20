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

namespace Alachisoft.NCache.Web.Caching.RemoteCacheDependency
{
    [Serializable]
    internal class RemoteCacheKeyDependencyManager
    {
        static IDictionary<string, RemoteCache> s_cacheList = new Dictionary<string, RemoteCache>();
        static object s_lock_object = new object();

        public static bool RegisterRemoteCacheDependency(RemoteCacheKeyDependency dependency)
        {
            if (dependency != null)
            {
                try
                {
                    RemoteCache remoteCache;
                    lock (s_lock_object)
                    {
                        if (!s_cacheList.ContainsKey(dependency.RemoteCacheID))
                        {
                            remoteCache = new RemoteCache(dependency.RemoteCacheID);
                            if (remoteCache != null)
                            {
                                remoteCache.Intialize();
                                s_cacheList.Add(dependency.RemoteCacheID, remoteCache);
                            }
                        }
                        else
                            remoteCache = s_cacheList[dependency.RemoteCacheID];
                    }

                    if (remoteCache != null)
                        remoteCache.RegisterRemoteCacheDependency(dependency.RemoteCacheKey);

                    return true;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            return false;
        }

        public static void UnregisterRemoteCacheDependency(RemoteCacheKeyDependency dependency)
        {
            if (dependency != null)
            {
                try
                {
                    RemoteCache remoteCache;
                    bool found = s_cacheList.TryGetValue(dependency.RemoteCacheID, out remoteCache);
                    if (found)
                    {
                        remoteCache.UnregisterRemoteCacheDependency(dependency.RemoteCacheKey);
                        lock (s_lock_object)
                        {
                            if (remoteCache.GetCacheKeyCount() == 0)
                            {
                                s_cacheList.Remove(dependency.RemoteCacheID);
                                remoteCache.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        public static bool HasExpired(RemoteCacheKeyDependency depndency)
        {
            if (depndency != null)
            {
                if (s_cacheList.ContainsKey(depndency.RemoteCacheID))
                {
                    RemoteCache remoteCache = s_cacheList[depndency.RemoteCacheID];

                    if (remoteCache != null)
                        return remoteCache.CheckExpiration(depndency.RemoteCacheKey);

                    return false;
                }
            }

            return false;
        }

        public static bool RemoveCacheOnCacheClear(RemoteCache remoteCache)
        {
            return s_cacheList.Values.Remove(remoteCache);
        }
    }
}