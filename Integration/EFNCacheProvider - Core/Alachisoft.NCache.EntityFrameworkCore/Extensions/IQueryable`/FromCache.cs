// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Alachisoft.NCache.EntityFrameworkCore.NCLinq;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

#if EF5 || EF6
using System;
using System.Data.Entity;
using System.Runtime.Caching;

#elif EFCORE
#endif

namespace Alachisoft.NCache.EntityFrameworkCore
{
    /// <summary>
    /// A static class that contains extension methods for caching entity framework query result sets.
    /// </summary>
    public static partial class QueryCacheExtensions
    {
#if EF5 || EF6
        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized and cached before being returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the query.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="policy">The policy to use to cache the query.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<T> FromCache<T>(this IQueryable<T> query, CacheItemPolicy policy, params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = query.AsNoTracking().ToList();
                item = QueryCacheManager.Cache.AddOrGetExisting(key, item, policy) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }

            return (IEnumerable<T>) item;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="absoluteExpiration">The fixed date and time at which the cache entry will expire.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<T> FromCache<T>(this IQueryable<T> query, DateTimeOffset absoluteExpiration, params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = query.AsNoTracking().ToList();
                item = QueryCacheManager.Cache.AddOrGetExisting(key, item, absoluteExpiration) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }

            return (IEnumerable<T>) item;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<T> FromCache<T>(this IQueryable<T> query, params string[] tags) where T : class
        {
            return query.FromCache(QueryCacheManager.DefaultCacheItemPolicy, tags);
        }
#elif EFCORE

        /// <summary>
        /// Executes the query on the cache and returns the result as generic IEnumerable.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed. </param>
        /// <returns>Returns the result set of the query after executing it in the cache. In case of no result return the
        /// default values.</returns>
        public static IEnumerable<T> FromCacheOnly<T>(this IQueryable<T> query) where T : class
        {
            Logger.Log(
                "Performing FromCacheOnly for " + query.ToString() + ".", LogLevel.Trace
            );

            string oqlQuery = OQLBuilder.ExpressionToOQL(query.GetDbContext(), query.Expression, out OQLBuilder oqlBuilder);

            Logger.Log("OQL Query Generated: " + oqlQuery, LogLevel.Debug);

            return new NCacheOqlEnumerable<T>(
                NCacheOqlEnumerable<T>.EnumerableType.Normal,
                QueryCacheManager.Cache.NCacheInstance.ExecuteReader(oqlQuery, new Hashtable(), true)
            );
        }

        /// <summary>
        /// Executes the query on the database irrespective of the fact that the result set could have been in the cache
        /// and could have been served from there and updates the result set in the cache.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query after executing it on the databse and storing it in the cache.
        /// </returns>
        public static IEnumerable<T> LoadIntoCache<T>(this IQueryable<T> query, CachingOptions options) where T : class
        {
            string str;
            return query.LoadIntoCache(out str, options);
        }

        /// <summary>
        /// Executes the query on the database irrespective of the fact that the result set could have been in the cache
        /// and could have been served from there and updates the result set in the cache.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="cacheKey">The key against which the result set will be cached is returned as out parameter.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query after executing it on the databse and storing it in the cache.
        /// </returns>
        public static IEnumerable<T> LoadIntoCache<T>(this IQueryable<T> query, out string cacheKey, CachingOptions options) where T : class
        {
            return FromCacheImplementation(CachingMethod.LoadIntoCache, query, out cacheKey, options);
        }

        /// <summary>
        /// Checks if the result set is available in cache or not. If it is available it is fetched from the cache and returned
        /// however if it is not available the query is executed on the database and the result set is stored in cache as well
        /// as returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query from cache if available else from the database and stores it in 
        /// the cache.
        /// </returns>
        public static IEnumerable<T> FromCache<T>(this IQueryable<T> query, CachingOptions options) where T : class
        {
            string str;
            return query.FromCache(out str, options);
        }

        /// <summary>
        /// Checks if the result set is available in cache or not. If it is available it is fetched from the cache and returned
        /// however if it is not available the query is executed on the database and the result set is stored in cache as well
        /// as returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="cacheKey">The key against which the result set will be cached is returned as out parameter.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query from cache if available else from the database and stores it in 
        /// the cache.
        /// </returns>
        public static IEnumerable<T> FromCache<T>(this IQueryable<T> query, out string cacheKey, CachingOptions options) where T : class
        {
            return FromCacheImplementation(CachingMethod.FromCache, query, out cacheKey, options);
        }

        // Main implementation
        private static IEnumerable<T> FromCacheImplementation<T>(CachingMethod cachingMethod, IQueryable<T> query, out string cacheKey, CachingOptions options) where T : class
        {
            Logger.Log(
                "Performing " + cachingMethod + " for " + query.ToString() + " with options " + options.ToLog() + ".", LogLevel.Trace
            );

            // Create NCache entry options
            CachingOptions optionsCloned = (CachingOptions)options.Clone();

            cacheKey = null;
            string queryStoreKey = null;

            if (cachingMethod != CachingMethod.LoadIntoCache)
            {
                // Verify if query can be fetched seperately
                string pkCacheKey;
                if (QueryHelper.CanDirectPkFetch(query, optionsCloned, out pkCacheKey))
                {
                    object pkItem;
                    if (QueryCacheManager.Cache.TryGetValue(pkCacheKey, out pkItem))
                    {
                        List<T> resultSetPk = new List<T>();
                        List<T> resultSetPkTracked = new List<T>();
                        var stateManagerPk = query.GetStateManager();

                        resultSetPk.Add((T)pkItem);

                        foreach (var entity in resultSetPk)
                        {
                            resultSetPkTracked.Add(((StateManager)stateManagerPk).GetRefValue(entity));
                        }
                        return resultSetPkTracked;
                    }
                }
            }

            bool cacheHit = false;
            Hashtable cacheResult = null;

            if (optionsCloned.StoreAs == StoreAs.Collection || optionsCloned.QueryIdentifier == null)
            {
                queryStoreKey = QueryCacheManager.GetQueryCacheKey(query, optionsCloned.QueryIdentifier);
                if (optionsCloned.StoreAs == StoreAs.Collection)
                    cacheKey = queryStoreKey;
                if (optionsCloned.QueryIdentifier == null)
                    optionsCloned.QueryIdentifier = new Tag(queryStoreKey);
                else
                    optionsCloned.QueryIdentifier = new Tag(query.ElementType.FullName + ";" + optionsCloned.QueryIdentifier);
            }

            // Check in cache
            if (cachingMethod != CachingMethod.LoadIntoCache)
            {
                cacheHit = QueryCacheManager.Cache.GetByTags(optionsCloned.QueryIdentifier, out cacheResult);
            }

            // If not found in cache go for db
            if (!cacheHit)
            {
                var enumerableSet = query.AsEnumerable<T>();

                CacheDependency dbDependency = null;
                if (optionsCloned.CreateDbDependency)
                {
                    RelationalQueryContext queryContext = null;
                    IRelationalCommand command = query.CreateCommand(out queryContext);
                    string connectionString = queryContext.Connection.ConnectionString;
                    dbDependency = GetDependency(NCacheConfiguration.DatabaseType, command.CommandText, connectionString);
                }
                return new NCacheEnumerable<T>(queryStoreKey, query, enumerableSet, optionsCloned, dbDependency);
            }
            // data is found in cache return result set
            else
            {
                // Assume its a collection
                if (cacheResult.Count == 1)
                {
                    foreach (var item in cacheResult.Values)
                    {
                        CacheEntry entry = item as CacheEntry;
                        if (entry != null)
                        {
                            // Confirmed stored as collection just return the value after casting
                            IEnumerable<T> resultSetC = (IEnumerable<T>)entry.Value;
                            // [Umer] i know this tracking is costly but there is no other solution
                            var resultSetCTracked = new List<T>();
                            var stateManagerC = query.GetStateManager();
                            foreach (var entity in resultSetC)
                            {
                                resultSetCTracked.Add(((StateManager)stateManagerC).GetRefValue(entity));
                            }
                            return resultSetCTracked;
                        }
                        break;
                    }
                }

                var resultSetSE = cacheResult.Values.Cast<T>();
                // [Umer] i know this tracking is costly but there is no other solution
                var resultSetSETracked = new List<T>();
                var stateManagerSE = query.GetStateManager();
                foreach (var entity in resultSetSE)
                {
                    resultSetSETracked.Add(((StateManager)stateManagerSE).GetRefValue(entity));
                }
                return resultSetSETracked;
            }
        }
#endif
        #region Helper Methods
        private static CacheDependency GetDependency(DependencyType targetDatabase, string commandText, string connectionString)
        {
            DbCommand dbCommand;
            switch (targetDatabase)
            {
                case DependencyType.SqlServer:
                    dbCommand = new SqlCommand(commandText, new SqlConnection(connectionString));
                    return CreateSqlDepenedency(connectionString, dbCommand);

                case DependencyType.Oracle:
                    dbCommand = new SqlCommand(commandText, new SqlConnection(connectionString));
                    return CreateOracleDepenedency(connectionString, dbCommand);
                default:
                    return null;
            }
        }

        private static CacheDependency CreateOracleDepenedency(string connectionString, DbCommand dbCommand)
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

                    dependencyParameter.Value = parameter.Value;

                    depenedency.CommandParams.Add(parameter.ParameterName, dependencyParameter);
                }
            }

            return depenedency;
        }

        private static SqlCacheDependency CreateSqlDepenedency(string conString, DbCommand dbCommand)
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
                        switch (parameter.Direction)
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

                        dependencyParameter.UdtTypeName = parameter.DbType.ToString();
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
        #endregion
    }
}
