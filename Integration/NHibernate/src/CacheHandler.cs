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
using System.Linq;
using System.Text;


namespace Alachisoft.NCache.Integrations.NHibernate.Cache

{
    class CacheHandler
    {

         private Alachisoft.NCache.Web.Caching.Cache _cache;


        private int _refCount = 0;

        public CacheHandler(string cacheName, bool exceptionEnabled)
        {

            _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheName);


            _cache.ExceptionsEnabled = exceptionEnabled;
            _refCount++;
        }


        public Alachisoft.NCache.Web.Caching.Cache Cache

        {
            get { return _cache; }
        }

        public void IncrementRefCount()
        {
            _refCount++;
        }

        public int DecrementRefCount()
        {
            return --_refCount;
        }

        public void DisposeCache()
        {
            _cache.Dispose();
        }
    }
}
