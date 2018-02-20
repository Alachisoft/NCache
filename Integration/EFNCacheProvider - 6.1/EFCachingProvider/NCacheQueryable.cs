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
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Web.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    public class NCacheQueryable<T> : IQueryable<T>
    {
        private DbResultItem _resultItem;
        private IQueryable<T> _queryable;
        public NCacheQueryable(IQueryable<T> queryable)
        {
            _queryable = queryable;
        }

        public NCacheQueryable(IQueryable<T> queryable, DateTime? absoluteExpiration, TimeSpan? slidingExpiration, bool dbSynDependency)
        {
            _resultItem = new DbResultItem();
            _queryable = queryable;
            if(absoluteExpiration != null)
                _resultItem.AbsoluteExpiration = (DateTime)absoluteExpiration;
            if(slidingExpiration != null)
                _resultItem.SlidingExpiration = (TimeSpan)slidingExpiration;
            _resultItem.DbSyncDependency = dbSynDependency;
            _resultItem.TargetDatabase = Application.Instance.CachePolicy.DatabaseType;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_queryable == null)
            {
                throw new ArgumentNullException("source");
            }

            var objectQuery = TryGetObjectQuery(_queryable) ?? _queryable as ObjectQuery;

            string queryString = objectQuery.ToTraceString().StripTabsAndNewlines();

            string key = "IQueryableExtensionPrefix:" + queryString;

            if(objectQuery != null)
            {
                ICache cache = Application.Instance.Cache;

                object result = null;
                try
                {
                    if (cache.GetItem(key, out result))
                    {
                        IEnumerable<T> enumerable = result as IEnumerable<T>;
                        IEnumerator<T> enumerator = enumerable != null ? enumerable.GetEnumerator() : _queryable.GetEnumerator();
                        return enumerator;
                    }
                    else
                    {
                        if (_resultItem.Equals(null))
                        {
                            _resultItem = new DbResultItem();
                            LoadDefaultSettings();
                        }

                        string connectionString = ((System.Data.Entity.Core.EntityClient.EntityConnection)objectQuery.Context.Connection).StoreConnection.ConnectionString;

                        DbCommand command = null;

                        if (_resultItem.TargetDatabase == CachePolicyElement.DatabaseType.Oracle10i2)
                        {
                            command = new OracleCommand(queryString, new OracleConnection(connectionString));
                        }
                        else if (_resultItem.TargetDatabase == CachePolicyElement.DatabaseType.Sql2005Above)
                        {
                            command = new SqlCommand(queryString, new SqlConnection(connectionString));
                        }
                        else if (_resultItem.DbSyncDependency)
                        {
                            Logger.Instance.TraceError("Database information must be provided in provider\'s configuration file in cache-policy-configuration element. Inserting item with no database dependency.");
                            _resultItem.DbSyncDependency = false;
                        }

                        _resultItem.ConnectionString = connectionString;

                        return new CachedQueryEnumerator<T>(_queryable.GetEnumerator(),
                        delegate(EnumerableResult entry)
                        {
                                _resultItem.Value = entry;
                                cache.PutItem(key, _resultItem, command);
                        }, false);
                    }
                }
                catch (Exception exc)
                {
                     Logger.Instance.TraceError(exc.Message);
                }
            }
            return _queryable.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (_queryable == null)
            {
                throw new ArgumentNullException("source");
            }

            var objectQuery = TryGetObjectQuery(_queryable) ?? _queryable as ObjectQuery;

            string queryString = objectQuery.ToTraceString().StripTabsAndNewlines();

            string key = "IQueryableExtensionPrefix:" + queryString;

            if (objectQuery != null)
            {
                ICache cache = Application.Instance.Cache;

                object result;
                try
                {
                    if (cache.GetItem(key, out result))
                    {
                        IEnumerable<T> enumerable = result as IEnumerable<T>;
                        IEnumerator<T> enumerator = enumerable != null ? enumerable.GetEnumerator() : _queryable.GetEnumerator();
                        return enumerator;
                    }
                    else
                    {
                        if (_resultItem.Equals(null))
                        {
                            _resultItem = new DbResultItem();
                            LoadDefaultSettings();
                        }

                        string connectionString = ((System.Data.Entity.Core.EntityClient.EntityConnection)objectQuery.Context.Connection).StoreConnection.ConnectionString;

                        DbCommand command = null;

                        if (_resultItem.TargetDatabase == CachePolicyElement.DatabaseType.Oracle10i2)
                        {
                            command = new OracleCommand(queryString, new OracleConnection(connectionString));
                        }
                        else if (_resultItem.TargetDatabase == CachePolicyElement.DatabaseType.Sql2005Above)
                        {
                            command = new SqlCommand(queryString, new SqlConnection(connectionString));
                        }
                        else if (_resultItem.DbSyncDependency)
                        {
                            Logger.Instance.TraceError("Database information must be provided in provider\'s configuration file in cache-policy-configuration element. Inserting item with no database dependency.");
                            _resultItem.DbSyncDependency = false;
                        }

                        _resultItem.ConnectionString = connectionString;

                        return new CachedQueryEnumerator<T>(_queryable.GetEnumerator(),
                        delegate(EnumerableResult entry)
                        {
                            _resultItem.Value = entry;
                            cache.PutItem(key, _resultItem, command);
                        }, false);
                    }
                }
                catch (Exception exc)
                {
                    Logger.Instance.TraceError(exc.Message);
                }
            }
            return _queryable.GetEnumerator();
        }

        public Type ElementType
        {
            get { return _queryable.ElementType; }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return _queryable.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _queryable.Provider; }
        }

        private static ObjectQuery TryGetObjectQuery<T>(IQueryable<T> source)
        {
            var dbQuery = source as DbQuery<T>;

            if (dbQuery != null)
            {
                const BindingFlags privateFieldFlags =
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

                var internalQuery =
                    source.GetType().GetProperty("InternalQuery", privateFieldFlags)
                        .GetValue(source, null);

                return
                    (ObjectQuery)internalQuery.GetType().GetProperty("ObjectQuery", privateFieldFlags)
                        .GetValue(internalQuery, null);
            }

            return null;
        }

        private void LoadDefaultSettings()
        {
            QueryLevelCachePolicyElement queryLevelCachePolicy = Application.Instance.CachePolicy.APILevelCaching.GetEffectivePolicy();
            int seconds = queryLevelCachePolicy.ExpirationTime;
            if (queryLevelCachePolicy.ExpirationType == CachePolicy.Expirations.Sliding)
            {
                _resultItem.SlidingExpiration = new TimeSpan(0, 0, seconds);
            }
            else if (queryLevelCachePolicy.ExpirationType == CachePolicy.Expirations.Absolute)
            {
                _resultItem.AbsoluteExpiration = DateTime.Now.AddSeconds(seconds);
            }
            _resultItem.DbSyncDependency = queryLevelCachePolicy.DbSyncDependency;
            _resultItem.TargetDatabase = Application.Instance.CachePolicy.DatabaseType;
        }
    }
}
