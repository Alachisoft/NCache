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

using Alachisoft.NCache.Integrations.EntityFramework.Analysis;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    public class EFCommandInterceptor : IDbCommandInterceptor
    {
        public EFCommandInterceptor()
        {
            Logger.Initialize();
            Application.Instance.Initialize();
        }
        public void NonQueryExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void NonQueryExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
        }

        public void ReaderExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
        }

        public void ReaderExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
            switch (Application.Instance.Mode)
            {
                case Application.ApplicationMode.Analysis:
                    AnalysisCommandExecuting(command, interceptionContext);
                    break;
                case Application.ApplicationMode.Cache:
                    CachingCommandExecuting(command, interceptionContext);
                    break;
                case Application.ApplicationMode.Passthru:
                    break;
            }
        }

        private void AnalysisCommandExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
            Query query;
            if (command.CommandType == System.Data.CommandType.StoredProcedure)
                query = Query.CreateQuery(command, true);
            else
                query = Query.CreateQuery(command, false);

            AnalysisManager.Instance.AnalyzeQuery(query);
        }

        private void CachingCommandExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
            Query query;
            ICache _cache = Application.Instance.Cache;
            if (_cache == null)
                return;
            if(command.CommandType == System.Data.CommandType.StoredProcedure)
                query = Query.CreateQuery(command, true);
            else
                query = Query.CreateQuery(command, false);

            List<string> parameterList = null;
            DbResultItem resultItem = new DbResultItem();
            resultItem.ConnectionString = command.Connection.ConnectionString;
            bool cacheable = Application.Instance.CachePolicy.GetEffectivePolicy(query.QueryText, out resultItem.AbsoluteExpiration, out resultItem.SlidingExpiration, out resultItem.TargetDatabase, out resultItem.DbSyncDependency, out parameterList);

            if (command.CommandType == System.Data.CommandType.StoredProcedure)
            {
                resultItem.DbSyncDependency = false;
                Logger.Instance.TraceDebug("Database dependency is not supported for store procedure.");
            }

            object value = null;

            string key = query.GetCacheKey(parameterList, command.Parameters);
            if (cacheable)
            {
                try
                {
                    if (_cache.GetItem(key, out value))
                    {
                        interceptionContext.SuppressExecution();
                        interceptionContext.Result = new CachingDataReaderCacheReader((DbQueryResults)value, interceptionContext.CommandBehavior);
                    }
                    else
                    {
                        interceptionContext.SuppressExecution();
                        if (resultItem.TargetDatabase == CachePolicyElement.DatabaseType.None && resultItem.DbSyncDependency)
                        {
                            Logger.Instance.TraceError("Database information must be provided in provider\'s configuration file in cache-policy-configuration element. Inserting item added with no database dependency.");
                            resultItem.DbSyncDependency = false;
                        }
                        interceptionContext.Result = new EFCachingDataReaderCacheWriter(command.ExecuteReader(interceptionContext.CommandBehavior),
                            delegate(DbQueryResults entry)
                            {
                                try
                                {
                                    resultItem.Value = entry;
                                    _cache.PutItem(key, resultItem, command);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Instance.TraceError(exc.Message);
                                }
                            },
                            interceptionContext.CommandBehavior);
                    }
                }
                catch (Exception exc)
                {
                    Logger.Instance.TraceError(exc.Message);
                }
            }
        }

        public void ScalarExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }

        public void ScalarExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
        }

        ~EFCommandInterceptor()
        {
            Application.Instance.Dispose(); 
        }
    }
}
