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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;
using Alachisoft.NCache.Runtime.Dependencies;

using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
 
namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    public class NCache : ICache
    {
        private Cache cache = null;

        /// <summary>
        /// Create a new NCache instance and initialize cache with specified cache id
        /// </summary>
        /// <param name="cacheId">Cache Id to initialize</param>
        public NCache(string cacheId)
        {
            Logger.Instance.TraceDebug("Initializing cache \"" + cacheId + "\"");
            this.CacheId = cacheId;
            this.cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId);
            if (this.cache != null)
            {
                Logger.Instance.TraceDebug("Initialized cache \"" + cacheId + "\"");
            }
            else
                Logger.Instance.TraceDebug("Cache \"" + cacheId + "\" not initialized");
        }
        
        #region ICache Members

        /// <summary>
        /// Get or set cache id
        /// </summary>
        public string CacheId { get; set; }

        /// <summary>
        /// Tries to the get cached entry by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The retrieved value.</param>
        /// <returns>
        /// A value of <c>true</c> if entry was found in the cache, <c>false</c> otherwise.
        /// </returns>
        public bool GetItem(string key, out object value)
        {
            Logger.Instance.TraceDebug("Retrieving cached results against key \"" + key + "\"");
            value = null;
            if (this.cache == null)
            {
                return false;
            }

            value = this.cache.Get(key);
            if(value != null)
                Logger.Instance.TraceDebug("Retrieved cached results against key \"" + key + "\"");
            else
                Logger.Instance.TraceDebug("Item not found from cache.");


            return value != null;
        }

        /// <summary>
        /// Adds the specified entry to the cache.
        /// </summary>
        /// <param name="key">The entry key.</param>
        /// <param name="resultItem">The entry value.</param>
        public void PutItem(string key, DbResultItem resultItem,DbCommand dbCommand)
        {
            Logger.Instance.TraceDebug("Caching results against key \"" + key + "\"");
            if (this.cache == null)
            {
                return;
            }

            CacheItem cacheItem = new CacheItem(resultItem.Value);

            if (resultItem.DbSyncDependency)
            {
                cacheItem.Dependency = this.GetDependency(resultItem.TargetDatabase, key, resultItem.ConnectionString, dbCommand);
            }
            
            cacheItem.AbsoluteExpiration = resultItem.AbsoluteExpiration;
            cacheItem.SlidingExpiration = resultItem.SlidingExpiration;
            
           CacheItemVersion ver = cache.Insert(key, cacheItem);
           ulong v= ver.Version;

           Logger.Instance.TraceDebug("Cached results against key \"" + key + "\"");
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (this.cache != null)
            {
                try
                {
                    this.cache.Dispose();
                }
                catch (Exception) { }
            }
        }

        #endregion
        
        /// <summary>
        /// Get the cache dependecy for the database specified in config
        /// </summary>
        private CacheDependency GetDependency(CachePolicyElement.DatabaseType targetDatabase, string command, string connectionString,DbCommand dbCommand)
        {
            switch (targetDatabase)
            {
                case CachePolicyElement.DatabaseType.Sql2005Above:
                    return CreateSqlDepenedency(connectionString, dbCommand);//new SqlCacheDependency(connectionString, command);

                case CachePolicyElement.DatabaseType.Oracle10i2:
                    return CreateOracleDepenedency(connectionString, dbCommand);//new OracleCacheDependency(connectionString, command);

                default:
                    return null;
            }
        }

        private CacheDependency CreateOracleDepenedency(string connectionString, DbCommand dbCommand)
        {
            OracleCacheDependency depenedency = new OracleCacheDependency(connectionString, dbCommand.CommandText);
           
                 
                if (dbCommand.Parameters.Count > 0)
                {
                    foreach (DbParameter parameter in dbCommand.Parameters)
                    {
                        OracleCmdParams dependencyParameter = new OracleCmdParams();
                        dependencyParameter.Direction = (OracleParameterDirection)((int)parameter.Direction - 1);
                        switch (parameter.Direction)
                        {
                            case ParameterDirection.Input:
                                dependencyParameter.Direction = OracleParameterDirection.Input;
                                break;
                            case ParameterDirection.Output:
                                dependencyParameter.Direction = OracleParameterDirection.Output;
                                break;
                        }

                      //  dependencyParameter.Type = (OracleCmdParamsType)parameter.DbType;
                        dependencyParameter.Value = parameter.Value;
                        
                        depenedency.CommandParams.Add(parameter.ParameterName, dependencyParameter); 
                    }
                }

            return depenedency;
        }

        private SqlCacheDependency CreateSqlDepenedency(string conString, DbCommand dbCommand)
        {
            SqlCommand sqlCmd = dbCommand as SqlCommand;
            SqlCacheDependency depenedency = null;
            if (sqlCmd != null)
            {
                depenedency = new SqlCacheDependency(conString, sqlCmd.CommandText.StripTabsAndNewlines());
                if (sqlCmd.Parameters != null && sqlCmd.Parameters.Count > 0)
                {
                    foreach (SqlParameter parameter in sqlCmd.Parameters)
                    {
                        SqlCmdParams dependencyParameter = new SqlCmdParams();
                        //dependencyParameter.Direction = (SqlParamDirection)((int)parameter.Direction - 1);
                        switch(parameter.Direction)
                        {
                            case ParameterDirection.Input:
                                dependencyParameter.Direction = SqlParamDirection.Input;
                                break;
                            case ParameterDirection.InputOutput:
                                dependencyParameter.Direction = SqlParamDirection.InputOutput;
                                break;
                            case ParameterDirection.Output:
                                dependencyParameter.Direction = SqlParamDirection.Output;
                                break;
                            case ParameterDirection.ReturnValue:
                                dependencyParameter.Direction = SqlParamDirection.ReturnValue;
                                break;
                        }
                        dependencyParameter.IsNullable = parameter.IsNullable;
                        dependencyParameter.LocaleID = parameter.LocaleId;
                        dependencyParameter.Offset = parameter.Offset;
                        dependencyParameter.Precision = parameter.Precision;
                        dependencyParameter.Scale = parameter.Scale;
                        dependencyParameter.Size = parameter.Size;
                        dependencyParameter.SourceColumn = parameter.SourceColumn;
                        dependencyParameter.SourceColumnNullMapping = parameter.SourceColumnNullMapping;
                        dependencyParameter.SourceVersion = (SqlDataRowVersion)parameter.SourceVersion;
                        dependencyParameter.SqlValue = parameter.SqlValue;
                        dependencyParameter.UdtTypeName = parameter.UdtTypeName;
                        dependencyParameter.SourceColumn = parameter.SourceColumn;
                        dependencyParameter.Type = (CmdParamsType)parameter.SqlDbType;
                        dependencyParameter.TypeName = parameter.TypeName;
                        dependencyParameter.SourceColumn = parameter.SourceColumn;
                        dependencyParameter.Value = parameter.Value;

                        
                        depenedency.CommandParams.Add(parameter.ParameterName, dependencyParameter); 
                    }
                }

            }
            return depenedency;
        }
    }
}
