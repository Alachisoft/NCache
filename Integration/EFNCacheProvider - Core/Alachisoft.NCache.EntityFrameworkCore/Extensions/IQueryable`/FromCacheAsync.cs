// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alachisoft.NCache.EntityFrameworkCore.NCache;

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
#if EF5
    /// <summary>
    ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
    ///     yet, the query is materialized asynchronously and cached before being returned.
    /// </summary>
    /// <typeparam name="T">The generic type of the query.</typeparam>
    /// <param name="query">The query to cache in the QueryCacheManager.</param>
    /// <param name="policy">The policy to use to cache the query.</param>
    /// <param name="tags">
    ///     A variable-length parameters list containing tags to expire cached
    ///     entries.
    /// </param>
    /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, CacheItemPolicy policy, params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var result = Task.Run(() =>
            {
                var item = QueryCacheManager.Cache.Get(key);

                if (item == null)
                {
                    item = query.AsNoTracking().ToList();
                    item = QueryCacheManager.Cache.AddOrGetExisting(key, item, policy) ?? item;
                    QueryCacheManager.AddCacheTag(key, tags);
                }

                return (IEnumerable<T>) item;
            });

            return result;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="absoluteExpiration">The fixed date and time at which the cache entry will expire.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, DateTimeOffset absoluteExpiration, params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var result = Task.Run(() =>
            {
                var item = QueryCacheManager.Cache.Get(key);

                if (item == null)
                {
                    item = query.AsNoTracking().ToList();
                    item = QueryCacheManager.Cache.AddOrGetExisting(key, item, absoluteExpiration) ?? item;
                    QueryCacheManager.AddCacheTag(key, tags);
                }

                return (IEnumerable<T>) item;
            });

            return result;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, params string[] tags) where T : class
        {
            return query.FromCacheAsync(QueryCacheManager.DefaultCacheItemPolicy, tags);
        }
#elif EF6
        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the query.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="policy">The policy to use to cache the query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static async Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, CacheItemPolicy policy, CancellationToken cancellationToken = default(CancellationToken), params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = await query.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
                item = QueryCacheManager.Cache.AddOrGetExisting(key, item, policy) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }

            return (IEnumerable<T>) item;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="policy">The policy to use to cache the query.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, CacheItemPolicy policy, params string[] tags) where T : class
        {
            return query.FromCacheAsync(policy, default(CancellationToken), tags);
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="absoluteExpiration">The fixed date and time at which the cache entry will expire.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static async Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default(CancellationToken), params string[] tags) where T : class
        {
            var key = QueryCacheManager.GetCacheKey(query, tags);

            var item = QueryCacheManager.Cache.Get(key);

            if (item == null)
            {
                item = await query.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
                item = QueryCacheManager.Cache.AddOrGetExisting(key, item, absoluteExpiration) ?? item;
                QueryCacheManager.AddCacheTag(key, tags);
            }

            return (IEnumerable<T>) item;
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="absoluteExpiration">The fixed date and time at which the cache entry will expire.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, DateTimeOffset absoluteExpiration, params string[] tags) where T : class
        {
            return query.FromCacheAsync(absoluteExpiration, default(CancellationToken), tags);
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, params string[] tags) where T : class
        {
            return query.FromCacheAsync(QueryCacheManager.DefaultCacheItemPolicy, default(CancellationToken), tags);
        }

        /// <summary>
        ///     Return the result of the <paramref name="query" /> from the cache. If the query is not cached
        ///     yet, the query is materialized asynchronously and cached before being returned.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="query">The query to cache in the QueryCacheManager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="tags">
        ///     A variable-length parameters list containing tags to expire cached
        ///     entries.
        /// </param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default(CancellationToken), params string[] tags) where T : class
        {
            return query.FromCacheAsync(QueryCacheManager.DefaultCacheItemPolicy, cancellationToken, tags);
        }

#elif EFCORE

        /* ************************************************************************************************************************ */
        /*                                                                                                                          */
        /*                  OUR IMPLEMENTATIONS FOR [FromCache, FromCacheOnly AND LoadIntoCache] ASYNC METHODS                      */
        /*                                                                                                                          */
        /* ************************************************************************************************************************ */

        /// <summary>
        /// Asynchronously Checks if the result set is available in cache or not. If it is available it is fetched from the cache and returned
        /// however if it is not available the query is executed on the database and the result set is stored in cache as well
        /// as returned. The result set is encapsulated in a task and returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query (encapsulated in a task) from cache if available else from the database and stores it in 
        /// the cache.
        /// </returns>
        public static async Task<IEnumerable<T>> FromCacheAsync<T>(this IQueryable<T> query, CachingOptions options) where T : class
        {
            Logger.Log(
                "Async operation requested.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Task<IEnumerable<T>> task = Task.Factory.StartNew(
                () => FromCache(query, options)
            );
            return await task;
        }

        /// <summary>
        /// Executes the query on the cache Asynchronously and returns the result set as generic IEnumerable encapsulated as a task.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed. </param>
        /// <returns>Returns the result set of the query (encapsulated in a task) after executing it in the cache. In case of no result return the
        /// default values.</returns>
        public static async Task<IEnumerable<T>> FromCacheOnlyAsync<T>(this IQueryable<T> query) where T : class
        {
            Logger.Log(
                "Async operation requested.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Task<IEnumerable<T>> task = Task.Factory.StartNew(
                () => FromCacheOnly(query)
            );
            return await task;
        }

        /// <summary>
        /// Executes the query on the database irrespective of the fact that the result set could have been in the cache
        /// and could have been served from there and updates the result set in the cache.
        /// </summary>
        /// <typeparam name="T">The generic type of the collection</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <param name="options">The option that will be used to store the result set.</param>
        /// <returns>Returns the result set of the query (encapsulated in a task) after executing it on the databse and storing it in the cache.
        /// </returns>
        public static async Task<IEnumerable<T>> LoadIntoCacheAsync<T>(this IQueryable<T> query, CachingOptions options) where T : class
        {
            Logger.Log(
                "Async operation requested.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Task<IEnumerable<T>> task = Task.Factory.StartNew(
                () => LoadIntoCache(query, options)
            );
            return await task;
        }
#endif
    }
}
