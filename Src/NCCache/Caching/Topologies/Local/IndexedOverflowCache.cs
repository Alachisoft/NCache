//  Copyright (c) 2018 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System;
using System.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager, replicated cache and partitioned cache.
    /// </summary>
    internal class IndexedOverflowCache : OverflowCache
    {
        /// <summary>
        /// Overloaded constructor. Takes the properties as a map.
        /// </summary>
        /// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
        /// <param name="properties">properties collection for this cache.</param>
        /// <param name="listener">cache events listener</param>
        /// <param name="timeSched">scheduler to use for periodic tasks</param>
        public IndexedOverflowCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, ActiveQueryAnalyzer activeQueryAnalyzer )
            : base( cacheClasses, parentCache, properties, listener, context, activeQueryAnalyzer)
        {
        }


        protected override LocalCacheBase CreateLocalCache(CacheBase parentCache, IDictionary cacheClasses, IDictionary schemeProps)
        {
            return new IndexedLocalCache(cacheClasses, parentCache, schemeProps, null, _context, _activeQueryAnalyzer);
        }

        protected override LocalCacheBase CreateOverflowCache(IDictionary cacheClasses, IDictionary schemeProps)
        {
            return new IndexedOverflowCache(cacheClasses, this, schemeProps, null, _context, _activeQueryAnalyzer);
        }


        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion


        #region	/                 --- CacheBase ---           /

        /// <summary>
        /// Returns the TypeInfoMap for queries.
        /// </summary>
        public sealed override TypeInfoMap TypeInfoMap
        {
            get
            {
                return _primary.TypeInfoMap;
            }
        }
        /// <summary>
        /// Retrieve the object from the cache. A string key is passed as parameter.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <returns>cache entry.</returns>
        public override ArrayList GetGroupKeys(string group, string subGroup, OperationContext operationContext)
        {
            ArrayList primaryList = _primary.GetGroupKeys(group, subGroup, operationContext);
            ArrayList secondaryList = _secondary.GetGroupKeys(group, subGroup, operationContext);

            if (primaryList != null)
            {
                if (secondaryList != null)
                {
                    primaryList.AddRange(secondaryList);
                }

                return primaryList;
            }
            else
            {
                return secondaryList;
            }
        }


        /// <summary>
        /// Returns the list of key and value pairs in the group or sub group
        /// </summary>
        /// <param name="group">group for which data is required</param>
        /// <param name="subGroup">sub group within the group</param>
        /// <returns>list of key and value pairs in the group</returns>
        public override HashVector GetGroupData(string group, string subGroup, OperationContext operationContext)
        {
            HashVector primaryTable = _primary.GetGroupData(group, subGroup, operationContext);
            HashVector secondaryTable = _secondary.GetGroupData(group, subGroup, operationContext);

            if (primaryTable != null)
            {
                if (secondaryTable != null)
                {
                    IDictionaryEnumerator ide = secondaryTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        primaryTable.Add(ide.Key, ide.Value);
                    }
                }
                return primaryTable;
            }
            else
            {
                return secondaryTable;
            }
        }

        /// <summary>
        /// Gets the list of data groups in the cache.
        /// </summary>
        public override ArrayList DataGroupList
        {
            get
            {
                ArrayList list = new ArrayList();

                if (_primary != null)
                {
                    ICollection primarylist = _primary.DataGroupList;
                    if (primarylist != null)
                        list.AddRange(primarylist);
                }
                if (_secondary != null)
                {
                    ICollection secondarylist = _secondary.DataGroupList;
                    if (secondarylist != null)
                    {
                        IEnumerator ie = secondarylist.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            if (!list.Contains(ie.Current))
                                list.AddRange(secondarylist);
                        }
                    }
                }
                return list;
            }
        }

        public override GroupInfo GetGroupInfo(object key, OperationContext operationContext)
        {
            CacheEntry entry = Get(key, operationContext);
            GroupInfo info = null;
            if (entry != null)
            {
                if (entry.GroupInfo != null)
                    info = new GroupInfo(entry.GroupInfo.Group, entry.GroupInfo.SubGroup);
                else
                    info = new GroupInfo(null, null);
            }

            return info;
        }

        /// <summary>
        /// Gets the data groups of the items.
        /// </summary>
        /// <param name="keys">Keys of the items</param>
        /// <returns>Hashtable containing key of the item as 'key' and GroupInfo as 'value'</returns>
        public override Hashtable GetGroupInfoBulk(object[] keys, OperationContext operationContext)
        {
            Hashtable infoTahle = new Hashtable();
            HashVector entries = (HashVector)Get(keys, operationContext);
            CacheEntry currentEntry;
            GroupInfo info;
            if (entries != null)
            {
                IDictionaryEnumerator ide = entries.GetEnumerator();
                while (ide.MoveNext())
                {
                    info = null;
                    currentEntry = (CacheEntry)ide.Value;
                    if (currentEntry != null)
                    {
                        info = currentEntry.GroupInfo;
                        if (info == null) info = new GroupInfo(null, null);
                    }
                    infoTahle.Add(ide.Key, info);
                }
            }
            return infoTahle;
        }

        /// <summary>
        /// Remove the objects from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        public override Hashtable Remove(string group, string subGroup, bool notify, OperationContext operationContext)
        {
            ArrayList list = GetGroupKeys(group, subGroup, operationContext);
            object[] keys = MiscUtil.GetArrayFromCollection(list);
            return Remove(keys, ItemRemoveReason.Removed, notify, operationContext);
        }

        internal override QueryContext SearchInternal(Predicate pred, IDictionary values, CancellationToken token, Boolean includeFilters = false)
        {

            return null;
        }

        #endregion
    }
}