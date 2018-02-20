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

using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    class CachedQueryEnumerator<T> : IEnumerator<T>
    {
        private Action<EnumerableResult> _addToCache;
        private IEnumerator _enumerator;
        private EnumerableResult _enumerableResult;
        private bool _fromCache;

        public CachedQueryEnumerator(IEnumerator enumerator, Action<EnumerableResult> addToCache, bool fromCache)
        {
            _enumerator = enumerator;
            _addToCache = addToCache;
            _enumerableResult = new EnumerableResult();
            _fromCache = fromCache;
        }

        public CachedQueryEnumerator(DbResultItem item)
        {
            _enumerator = Convert(item.Value as EnumerableResult);
            _fromCache = true;
        }

        private IEnumerator Convert(EnumerableResult result)
        {
            return result.Objects as IEnumerator;
        }

        public object Current
        {
            get 
            {
                if (_enumerableResult == null)
                {
                    _enumerableResult = new EnumerableResult();
                }
                object obj = _enumerator.Current;
                if(!_fromCache)
                    _enumerableResult.Add(obj);
                return obj; 
            }
        }

        public bool MoveNext()
        {
            bool moveNext = _enumerator.MoveNext();
            if (!moveNext && !_fromCache)
            {
                _addToCache(_enumerableResult);
                _enumerableResult = null;
            }
            return moveNext;
        }

        public void Reset()
        {
            _enumerableResult.Reset();
            _enumerator.Reset();
        }

        T IEnumerator<T>.Current
        {
            get
            {
                if (_enumerableResult == null)
                {
                    _enumerableResult = new EnumerableResult();
                }
                object obj = _enumerator.Current;
                _enumerableResult.Add(obj);
                return (T)obj;
            }
        }

        public void Dispose()
        {
        }
    }
}
