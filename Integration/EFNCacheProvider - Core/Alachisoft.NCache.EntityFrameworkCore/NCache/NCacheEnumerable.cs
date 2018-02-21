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

using Alachisoft.NCache.Runtime.Dependencies;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal class NCacheEnumerable<T> : IEnumerable<T>
    {
        private IEnumerable<T> _innerEnumerable;
        private NCacheEnumerator<T> _enumeratorWrapper;

        public NCacheEnumerable(string key, IQueryable<T> query, IEnumerable<T> enumerable, CachingOptions options, CacheDependency dbDependency)
        {
            _innerEnumerable = enumerable;
            _enumeratorWrapper = new NCacheEnumerator<T>(key, query, _innerEnumerable, options, dbDependency);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _enumeratorWrapper;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumeratorWrapper;
        }
    }

    internal class NCacheEnumerator<T> : IEnumerator<T>
    {
        private IEnumerable<T> _innerEnumerable;
        private IEnumerator<T> _innerEnumerator;
        private string _queryKey;
        private Dictionary<string, T> _dataEnumerated;
        private CachingOptions _options;
        private CacheDependency _dbDependency;
        private IQueryable<T> _query;
        private bool _isSeperateStorageEligible = false;

        private DbContext _currentContext;

        public NCacheEnumerator(string key, IQueryable<T> query, IEnumerable<T> enumerable, CachingOptions options, CacheDependency dbDependency)
        {
            _queryKey = key;
            _query = query;
            _innerEnumerable = enumerable;
            _options = options;
            _dbDependency = dbDependency;
            _currentContext = query.GetDbContext();

            _innerEnumerator = _innerEnumerable.GetEnumerator();
            _dataEnumerated = new Dictionary<string, T>();

            if (_options.StoreAs == StoreAs.SeperateEntities)
            {
                _isSeperateStorageEligible = QueryHelper.IsSeperateStorageEligible(query, options);
            }
        }

        public T Current => _innerEnumerator.Current;

        object IEnumerator.Current => _innerEnumerator.Current;

        public void Dispose()
        {
            _innerEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            bool result = _innerEnumerator.MoveNext();

            // If enumeration is complete
            if (result == false)
            {
                if (_options.StoreAs == StoreAs.SeperateEntities && _isSeperateStorageEligible)
                {
                    QueryCacheManager.Cache.Set<T>(_queryKey, _dataEnumerated, _options, _dbDependency, StoreAs.SeperateEntities);
                }
                else
                {
                    QueryCacheManager.Cache.Set<T>(_queryKey, _dataEnumerated, _options, _dbDependency, StoreAs.Collection);
                }
            }
            // If enumeration is being done
            else
            {
                // If StoreAs SeperateEntites
                if (_options.StoreAs == StoreAs.SeperateEntities && _isSeperateStorageEligible)
                {
                    // Note that this case will only occur when query will return single item even if the type is IEnumerable<T>
                    // and that the T is a solid entity this extracting the pkValues in the constructor and using them here
                    string entityKey = GetEntityCacheKey(Current);
                    _dataEnumerated.Add(entityKey, Current);
                }
                // If StoreAs Collection || Seperate eligibility fails store as collection
                else
                {
                    _dataEnumerated.Add(_dataEnumerated.Count.ToString(), Current);
                }
            }
            return result;
        }

        public void Reset()
        {
            _innerEnumerator.Reset();
        }

        public string GetEntityCacheKey(object entity)
        {
            NCacheWrapper nCacheW = QueryCacheManager.Cache as NCacheWrapper;
            if (nCacheW != null)
            {
                StringBuilder keyBuilder = new StringBuilder();
                keyBuilder.Append(nCacheW.DefaultKeyGen.GetKey(_currentContext, entity));
                return keyBuilder.ToString();
            }
            // Handle other cases if needed
            throw new Exception("Cache is not NCache.");
        }
    }
}
