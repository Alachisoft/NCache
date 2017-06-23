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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Queries;

namespace Alachisoft.NCache.Caching.Queries
{
    internal class QueryContext
    {
        private Topologies.Local.LocalCacheBase _cache;
        private AttributeIndex _index;
        private string _cacheContext;
        private IDictionary _values;
        private bool _populateTree = true;
        private QueryResultSet _resultSet = new QueryResultSet();
        private string _typeName = String.Empty;
        private IQueryResult _internalQueryResult;

        public IQueryResult InternalQueryResult
        {
            get { return _internalQueryResult; }
            set { _internalQueryResult = value; }
        }

        public QueryContext(Topologies.Local.LocalCacheBase cache)
        {
            _cache = cache;
        }

        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        internal QueryResultSet ResultSet
        {
            get { return _resultSet; }
            set { _resultSet = value; }
        }

        public IDictionary AttributeValues
        {
            get { return _values; }
            set { _values = value; }
        }

        public Topologies.Local.LocalCacheBase Cache
        {
            get { return _cache; }
        }

        public string CacheContext
        {
            get{ return _cacheContext; }
            set { _cacheContext = value; }
        }

        public AttributeIndex Index
        {
            get 
            { 
                if (_index == null)
                    _index = IndexManager.IndexMap[TypeName] as AttributeIndex;

                return _index; 
            }

            set { _index = value; }
        }


        public bool PopulateTree
        {
            get { return _populateTree; }
            set { _populateTree = value; }
        }

        public QueryIndexManager IndexManager
        {
            get
            {
                return ((Topologies.Local.IndexedLocalCache)_cache).IndexManager;
            }
        }

        public object Get(object key, OperationContext operationContext)
        {
            CacheEntry entry = Cache.Get(key,operationContext);
            object obj = entry.DeflattedValue(CacheContext);

            return obj;
        }
    }
}
