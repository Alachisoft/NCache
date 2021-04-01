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
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Enumeration
{
    /// <summary>
    /// A singleton Class that is responsible for all the management of snaphot pool for Enumeration.
    /// This class can be used to get snaphot from snaphot pool for enumeration based on size of data that snaphot will return and is configurable in service config.
    /// </summary>
    ///

    internal class CacheSnapshotPool
    {
        static readonly CacheSnapshotPool instance = new CacheSnapshotPool();

        /// <summary>
        /// Contains the mapping between cache id and pool specific to that cache.
        /// </summary>
        Hashtable _cachePoolMap;

        static CacheSnapshotPool()
        {

        }

        CacheSnapshotPool()
        {
            _cachePoolMap = new Hashtable();
        }

        public static CacheSnapshotPool Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Get a snaphot from the pool 
        /// </summary>
        /// <param name="pointerID"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public Array GetSnaphot(string pointerID, CacheBase cache)
        {
            CachePool pool = null;

            if (_cachePoolMap.Contains(cache.Context.CacheRoot.Name))
            {
                pool = _cachePoolMap[cache.Context.CacheRoot.Name] as CachePool;
                return pool.GetSnaphotInPool(pointerID, cache);
            }
            else
            {
                pool = new CachePool();
                _cachePoolMap.Add(cache.Context.CacheRoot.Name, pool);

            }
            return pool.GetSnaphotInPool(pointerID, cache);
        }

        /// <summary>
        /// Get a snaphot from the pool for a particular cache.
        /// </summary>        
        public void DiposeSnapshot(string pointerID, CacheBase cache)
        {
            CachePool pool = null;

            if (_cachePoolMap.Contains(cache.Context.CacheRoot.Name))
            {
                pool = _cachePoolMap[cache.Context.CacheRoot.Name] as CachePool;
                pool.DiposeSnapshotInPool(pointerID);
            }

        }

        /// <summary>
        /// Dispose the pool created for a cache when the cache is disposed.
        /// </summary>
        /// <param name="cacheId"></param>
        public void DisposePool(string cacheId)
        {
            _cachePoolMap.Remove(cacheId);
        }


        class CachePool
        {
            /// <summary>
            /// The time on which a new snapshot was created and added to Snaphot Pool
            /// </summary>
            private DateTime _lastSnaphotCreationTime = DateTime.MinValue;

            /// <summary>
            /// The pool containing all the available snaphots.
            /// </summary>
            private Dictionary<string, Array> _pool = new Dictionary<string, Array>();

            /// <summary>
            /// Contains the mapping between pointer and its snaphot. tells which pointer is using which snapshot in pool.
            /// </summary>
            private Dictionary<string, string> _enumeratorSnaphotMap = new Dictionary<string, string>();

            /// <summary>
            /// Contains the map for each snapshot and number of enumerators on it. Tells how many emumerators a references
            /// a particluar snaphot.
            /// </summary>
            private Dictionary<string, int> _snapshotRefCountMap = new Dictionary<string, int>();

            /// <summary>
            /// holds the id of current usable snaphot of the pool
            /// </summary>
            private string _currentUsableSnapshot;

            /// <summary>
            /// Returns a unique GUID that is assigned to the new snaphot added to the pool
            /// </summary>
            /// <returns></returns>
            private string GetNewUniqueID()
            {
                return Guid.NewGuid().ToString();
            }


            internal CachePool()
            {
            }

            /// <summary>
            /// Return a snaphot from the snaphot pool to be used by current enumerator
            /// </summary>
            /// <param name="pointerID">unque id of the enumeration pointer being used by current enumerator.</param>
            /// <param name="cache">underlying cache from which snapshot has to be taken.</param>
            /// <returns>snaphot as an array.</returns>
            public Array GetSnaphotInPool(string pointerID, CacheBase cache)
            {
                string uniqueID = string.Empty;

                if (cache.Count < ServiceConfiguration.EnableSnapshotPoolingCacheSize)
                {
                    return cache.Keys;
                }
                else
                {
                    if (_pool.Count == 0)
                    {
                        uniqueID = GetNewUniqueID();
                        _pool.Add(uniqueID, cache.Keys);
                        _lastSnaphotCreationTime = DateTime.Now;
                        _currentUsableSnapshot = uniqueID;
                    }
                    else if (_pool.Count < ServiceConfiguration.SnapshotPoolSize)
                    {
                        TimeSpan elapsedTime = DateTime.Now.Subtract(_lastSnaphotCreationTime);
                        if (elapsedTime.TotalSeconds >= ServiceConfiguration.SnapshotCreationThreshold)
                        {
                            uniqueID = GetNewUniqueID();
                            _pool.Add(uniqueID, cache.Keys);
                            _lastSnaphotCreationTime = DateTime.Now;
                            _currentUsableSnapshot = uniqueID;
                        }
                    }

                    if (!_enumeratorSnaphotMap.ContainsKey(pointerID))
                    {
                        _enumeratorSnaphotMap.Add(pointerID, uniqueID);

                        if (!_snapshotRefCountMap.ContainsKey(uniqueID))
                        {
                            _snapshotRefCountMap.Add(uniqueID, 1);
                        }
                        else
                        {
                            int refCount = _snapshotRefCountMap[uniqueID];
                            refCount++;
                            _snapshotRefCountMap[uniqueID] = refCount;
                        }
                    }

                    return _pool[_currentUsableSnapshot];
                }
            }

            /// <summary>
            /// Dispose a snaphot from the pool that is not being used by any emumerator.
            /// </summary>
            /// <param name="pointerID">unque id of the enumeration pointer being used by current enumerator.</param>
            public void DiposeSnapshotInPool(string pointerID)
            {
                if (_enumeratorSnaphotMap.ContainsKey(pointerID))
                {
                    string snapshotID = _enumeratorSnaphotMap[pointerID];
                    if (!string.IsNullOrEmpty(snapshotID))
                    {
                        if (_snapshotRefCountMap.ContainsKey(snapshotID))
                        {
                            int refCount = _snapshotRefCountMap[snapshotID];
                            refCount--;
                            if (refCount == 0)
                                _pool.Remove(snapshotID);
                            else
                                _snapshotRefCountMap[snapshotID] = refCount;
                        }
                    }
                }
            }

        }

    }

}
