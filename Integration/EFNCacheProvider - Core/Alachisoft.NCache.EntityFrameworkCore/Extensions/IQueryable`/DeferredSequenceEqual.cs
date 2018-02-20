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
        /// <seealso cref="System.Linq.Queryable.SequenceEqual{TSource}(IQueryable{TSource}, IEnumerable{TSource})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "SequenceEqual" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An System.Linq.IQueryable`1 whose elements to compare to those of source2.</param>
        /// <param name="source2">An System.Collections.Generic.IEnumerable`1 whose elements to compare to 
        /// those of the first sequence.</param>
        /// <returns>true if the two source sequences are of equal length and their corresponding elements 
        /// compare equal; otherwise, false.</returns>
        public static QueryDeferred<bool> DeferredSequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2)
        {
            if (source1 == null)
                throw Error.ArgumentNull("source1");
            if (source2 == null)
                throw Error.ArgumentNull("source2");

            return new QueryDeferred<bool>(
#if EF5 || EF6
                source1.GetObjectQuery(),
#elif EFCORE 
                source1,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.SequenceEqual, source1, source2),
                    new[] { source1.Expression, GetSourceExpression(source2) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.SequenceEqual{TSource}(IQueryable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "SequenceEqual" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source1">An System.Linq.IQueryable`1 whose elements to compare to those of source2.</param>
        /// <param name="source2">An System.Collections.Generic.IEnumerable`1 whose elements to compare to 
        /// those of the first sequence.</param>
        /// <param name="comparer">An System.Collections.Generic.IEqualityComparer`1 to use to compare elements.</param>
        /// <returns>true if the two source sequences are of equal length and their corresponding elements 
        /// compare equal; otherwise, false.</returns>
        public static QueryDeferred<bool> DeferredSequenceEqual<TSource>(this IQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
        {
            if (source1 == null)
                throw Error.ArgumentNull("source1");
            if (source2 == null)
                throw Error.ArgumentNull("source2");

            return new QueryDeferred<bool>(
#if EF5 || EF6
                source1.GetObjectQuery(),
#elif EFCORE 
                source1,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.SequenceEqual, source1, source2, comparer),
                    new[]
                    {
                        source1.Expression,
                        GetSourceExpression(source2),
                        Expression.Constant(comparer, typeof (IEqualityComparer<TSource>))
                    }
                    ));
        }
    }
}
