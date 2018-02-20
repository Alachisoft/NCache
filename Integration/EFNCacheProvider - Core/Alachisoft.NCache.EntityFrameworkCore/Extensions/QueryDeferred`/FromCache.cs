// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using Alachisoft.NCache.EntityFrameworkCore.Extensions.QueryDeferred;
using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Alachisoft.NCache.EntityFrameworkCore.NCLinq;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

#if EF5 || EF6
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
        public static T FromCache<T>(this QueryDeferred<T> query, CacheItemPolicy policy, params string[] tags)
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = query.Execute();

                item = QueryCacheManager.Cache.AddOrGetExisting(key, item ?? DBNull.Value, policy) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }
            else
            {
                if (item == DBNull.Value)
                {
                    item = null;
                }    
            }

            return (T) item;
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
        public static T FromCache<T>(this QueryDeferred<T> query, DateTimeOffset absoluteExpiration, params string[] tags)
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = query.Execute();

                item = QueryCacheManager.Cache.AddOrGetExisting(key, item ?? DBNull.Value, absoluteExpiration) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }
            else
            {
                if (item == DBNull.Value)
                {
                    item = null;
                }
            }

            return (T) item;
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
        public static T FromCache<T>(this QueryDeferred<T> query, params string[] tags)
        {
            return query.FromCache(QueryCacheManager.DefaultCacheItemPolicy, tags);
        }
#elif EFCORE
        /// <summary>
        /// Executes the query on the cache and returns the result as generic type.
        /// </summary>
        /// <typeparam name="T">The generic type of the result.</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <returns>Returns the result of the query after executing it in the cache. In case of no result return the
        /// default value.</returns>
        public static T FromCacheOnly<T>(this QueryDeferred<T> query)
        {
            string oqlQuery = OQLBuilder.ExpressionToOQL(query.Query.GetDbContext(), query.Expression, out OQLBuilder builder);

            Logger.Log("OQL Query Generated for deferred operation: " + oqlQuery, Microsoft.Extensions.Logging.LogLevel.Debug);

            IEnumerable<T> enumerable = new NCacheOqlEnumerable<T>(
                NCacheOqlEnumerable<T>.EnumerableType.Deferred,
                QueryCacheManager.Cache.NCacheInstance.ExecuteReader(oqlQuery, new System.Collections.Hashtable(), true)
            );

            string aggregateUsed = builder.GetAggregateUsed();

            if (aggregateUsed != null)
            {
                aggregateUsed = aggregateUsed.Split('(')[0].ToLower();

                Logger.Log("Aggregate function used: " + aggregateUsed, Microsoft.Extensions.Logging.LogLevel.Trace);

                if (aggregateUsed.Equals("count"))
                {
                    dynamic expando = new ExpandoObject();
                    var list = enumerable.ToList();

                    if (list.Count == 1)
                    {
                        expando.Value = list[0];
                    }
                    else
                    {
                        expando.Value = list.Count;
                    }
                    return (T)expando.Value;
                }
            }

            return enumerable.FirstOrDefault();
        }

        /// <summary>
        /// Executes the query on the database irrespective of the fact that the result could have been in the cache
        /// and could have been served from there and updates the result in the cache.
        /// </summary>
        /// <typeparam name="T">The generic type of the result</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result.</param>
        /// <returns>Returns the result of the query after executing it on the databse and storing it in the cache.
        /// </returns>
        public static T LoadIntoCache<T>(this QueryDeferred<T> query, CachingOptions options)
        {
            return query.LoadIntoCache(out string cacheKey, options);
        }

        /// <summary>
        /// Executes the query on the database irrespective of the fact that the result could have been in the cache
        /// and could have been served from there and updates the result set in the cache.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="cacheKey">The key against which the result will be cached is returned as out parameter.</param>
        /// <param name="options">The option that will be used to store the result.</param>
        /// <returns>Returns the result of the query after executing it on the databse and storing it in the cache.
        /// </returns>
        public static T LoadIntoCache<T>(this QueryDeferred<T> query, out string cacheKey, CachingOptions options)
        {
            return FromCacheImplementation<T>(CachingMethod.LoadIntoCache, query, out cacheKey, options);
        }

        /// <summary>
        /// Checks if the result is available in cache or not. If it is available it is fetched from the cache and returned
        /// however if it is not available the query is executed on the database and the result is stored in cache as well
        /// as returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query from cache if available else from the database and stores it in 
        /// the cache.
        /// </returns>
        public static T FromCache<T>(this QueryDeferred<T> query, CachingOptions options)
        {
            return query.FromCache(out string cacheKey, options);
        }

        /// <summary>
        /// Checks if the result is available in cache or not. If it is available it is fetched from the cache and returned
        /// however if it is not available the query is executed on the database and the result is stored in cache as well
        /// as returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the result</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="cacheKey">The key against which the result will be cached is returned as out parameter.</param>
        /// <param name="options">The option that will be used to store the result.</param>
        /// <returns>Returns the result of the query from cache if available else from the database and stores it in 
        /// the cache.
        /// </returns>
        public static T FromCache<T>(this QueryDeferred<T> query, out string cacheKey, CachingOptions options)
        {
            return FromCacheImplementation(CachingMethod.FromCache, query, out cacheKey, options);
        }

        // Main implementation
        private static T FromCacheImplementation<T>(CachingMethod cachingMethod, QueryDeferred<T> query, out string cacheKey, CachingOptions options)
        {
            Logger.Log(
                "Performing " + cachingMethod + " for " + query.ToString() + " with options " + options.ToLog() + ".",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            options = (CachingOptions)options.Clone();
            // Always store as collection
            options.StoreAs = StoreAs.Collection;

            bool cacheHit = false;
            Hashtable cacheResult = default(Hashtable);
            cacheKey = QueryCacheManager.GetQueryCacheKey(query.Query, options.QueryIdentifier);

            // If user has specified tag, leave it as it is
            // Otherwise overwrite it with 'cacheKey'
            options.QueryIdentifier = options.QueryIdentifier ?? new Tag(cacheKey);

            /* NOTE: If user stored result with a tag and is trying to query 
             *       it without the tag, it's a different query so don't 
             *       worry about that.
             */

            // Get result into 'cacheResult' hashtable if it exists
            if (cachingMethod == CachingMethod.FromCache)
            {
                // Get by the tag (more reliable)
                cacheHit = QueryCacheManager.Cache.GetByTags(options.QueryIdentifier, out cacheResult);
            }
            // If result wasn't found OR result was meant to be stored fresh
            if (cachingMethod == CachingMethod.LoadIntoCache || !cacheHit)
            {
                CacheDependency dbDependency = null;

                if (options.CreateDbDependency)
                {
                    IRelationalCommand command = query.Query.CreateCommand(out RelationalQueryContext queryContext);

                    string connectionString = queryContext.Connection.ConnectionString;

                    dbDependency = GetDependency(NCacheConfiguration.DatabaseType, command.CommandText, connectionString);
                }

                object item = query.Execute();

                QueryCacheManager.Cache.SetAsCacheEntry(cacheKey, item ?? Null.Value, options, dbDependency);

                return item == null ? default(T) : (T)item;
            }
            // If result was meant to be fetched instead of stored fresh AND it was found (somewhat)
            else
            {
                object returnVal = default(T);

                if (cacheResult != default(Hashtable))
                {
                    returnVal = cacheResult.Values.Cast<CacheEntry>().FirstOrDefault().Value;
                }
                return returnVal != null ? (returnVal is Null ? default(T) : (T)returnVal) : default(T);
            }
        }
#endif
    }
}
