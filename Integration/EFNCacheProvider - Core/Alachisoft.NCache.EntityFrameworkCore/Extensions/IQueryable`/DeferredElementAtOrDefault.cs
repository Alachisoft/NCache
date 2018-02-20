// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

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
        /// <seealso cref="System.Linq.Queryable.ElementAtOrDefault{TSource}(IQueryable{TSource}, int)"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "ElementAtOrDefault" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An System.Linq.IQueryable`1 to return an element from.</param>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>default(TSource) if index is outside the bounds of source; otherwise, 
        /// the element at the specified position in source.</returns>
        public static QueryDeferred<TSource> DeferredElementAtOrDefault<TSource>(this IQueryable<TSource> source, int index)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<TSource>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.ElementAtOrDefault, source, index),
                    new[] { source.Expression, Expression.Constant(index) }
                    ));
        }
    }
}
