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

using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    internal sealed class QueryLevelCachePolicy
    {
        private QueryLevelCachePolicyElement _defaultPolicy;
        private ReaderWriterLock _rwLock;

        public readonly static QueryLevelCachePolicy Instance;

        static QueryLevelCachePolicy()
        {
            Instance = new QueryLevelCachePolicy();
        }

        private QueryLevelCachePolicy()
        {
            this._rwLock = new ReaderWriterLock();
        }

        public void LoadConfig(QueryLevelCachePolicyElement qLCPolicy)
        {
            try
            {
                this._rwLock.AcquireWriterLock(Timeout.Infinite);
                this._defaultPolicy = qLCPolicy;
            }
            finally
            {
                this._rwLock.ReleaseWriterLock();
            }
        }

        public QueryLevelCachePolicyElement GetEffectivePolicy()
        {
            try
            {
                this._rwLock.AcquireReaderLock(Timeout.Infinite);
                if (this._defaultPolicy == null)
                {
                    return null;
                }
                return this._defaultPolicy.Clone() as QueryLevelCachePolicyElement;
            }
            finally
            {
                this._rwLock.ReleaseReaderLock();
            }
        }
    }
}
