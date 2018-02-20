// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    /// <summary>
    /// A class that contains various extension methods from Entity Framework with deferred implementations.
    /// </summary>
    public static partial class QueryDeferredExtensions
    {
        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Contains{TSource}(IQueryable{TSource}, TSource)"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Contains" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An System.Linq.IQueryable`1 in which to locate item.</param>
        /// <param name="item">The object to locate in the sequence.</param>
        /// <returns>true if the input sequence contains an element that has the specified value; otherwise, false.</returns>
        public static QueryDeferred<bool> DeferredContains<TSource>(this IQueryable<TSource> source, TSource item)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<bool>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Contains, source, item),
                    new[] { source.Expression, Expression.Constant(item, typeof(TSource)) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Contains{TSource}(IQueryable{TSource}, TSource, IEqualityComparer{TSource})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Contains" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An System.Linq.IQueryable`1 in which to locate item.</param>
        /// <param name="item">The object to locate in the sequence.</param>
        /// <param name="comparer">An System.Collections.Generic.IEqualityComparer`1 to compare values.</param>
        /// <returns>true if the input sequence contains an element that has the specified value; otherwise, false.</returns>
        public static QueryDeferred<bool> DeferredContains<TSource>(this IQueryable<TSource> source, TSource item, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<bool>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Contains, source, item, comparer),
                    new[] { source.Expression, Expression.Constant(item, typeof(TSource)), Expression.Constant(comparer, typeof(IEqualityComparer<TSource>)) }
                    ));
        }
    }
}
