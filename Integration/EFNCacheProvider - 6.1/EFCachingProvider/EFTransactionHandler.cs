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
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    public class EFTransactionHandler : IDbTransactionInterceptor
    {
        private readonly ICache _cache;
        public EFTransactionHandler(ICache cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");
            _cache = cache;
        }
        public virtual bool GetItem(string key, out object value)
        {
            return _cache.GetItem(key, out value);
        }
        public virtual void PutItem(string key, DbResultItem value, DbCommand command)
        {
            _cache.PutItem(key, value, command);
        }
        public void Committed(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void Committing(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void ConnectionGetting(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext<System.Data.Common.DbConnection> interceptionContext)
        {
        }

        public void ConnectionGot(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext<System.Data.Common.DbConnection> interceptionContext)
        {
        }

        public void Disposed(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void Disposing(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void IsolationLevelGetting(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext<System.Data.IsolationLevel> interceptionContext)
        {
        }

        public void IsolationLevelGot(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext<System.Data.IsolationLevel> interceptionContext)
        {
        }

        public void RolledBack(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void RollingBack(System.Data.Common.DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }
    }
}
