// Copyright (c) 2015 Alachisoft
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
using System.Collections;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    /// <summary>
    /// This class provides the local storage options i.e. the actual storage of objects. It is used 
    /// by the Cache Manager and partitioned cache.
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
        public IndexedOverflowCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context)
            : base( cacheClasses, parentCache, properties, listener, context)
        {
        }


        protected override LocalCacheBase CreateLocalCache(CacheBase parentCache, IDictionary cacheClasses, IDictionary schemeProps)
        {
            return new IndexedLocalCache(cacheClasses, parentCache, schemeProps, null, _context);
        }

        protected override LocalCacheBase CreateOverflowCache(IDictionary cacheClasses, IDictionary schemeProps)
        {
            return new IndexedOverflowCache(cacheClasses, this, schemeProps, null, _context);
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
        
        internal override QueryContext SearchInternal(Predicate pred, IDictionary values)
        {
            return null;
        }

        #endregion
    }
}
