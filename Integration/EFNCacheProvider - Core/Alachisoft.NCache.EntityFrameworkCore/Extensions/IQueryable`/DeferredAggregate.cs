// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using System;
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
        /// <seealso cref="System.Linq.Queryable.Aggregate{TSource}(IQueryable{TSource}, Expression{Func{TSource, TSource, TSource}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Aggregate" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The System.Linq.IQueryable`1 that contains the elements to be counted.</param>
        /// <param name="func">An accumulator function to apply to each element.</param>
        /// <returns>The final accumulator value.</returns>
        public static QueryDeferred<TSource> DeferredAggregate<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, TSource>> func)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (func == null)
                throw Error.ArgumentNull("func");

            return new QueryDeferred<TSource>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Aggregate, source, func),
                    new[] { source.Expression, Expression.Quote(func) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Aggregate{TSource, TAccumulate}(IQueryable{TSource}, TAccumulate, Expression{Func{TAccumulate, TSource, TAccumulate}})"/>.
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Aggregate" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to invoke on each element.</param>
        /// <returns>The final accumulator value.</returns>
        public static QueryDeferred<TAccumulate> DeferredAggregate<TSource, TAccumulate>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (func == null)
                throw Error.ArgumentNull("func");

            return new QueryDeferred<TAccumulate>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Aggregate, source, seed, func),
                    new[] { source.Expression, Expression.Constant(seed), Expression.Quote(func) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Aggregate{TSource, TAccumulate, TResult}(IQueryable{TSource}, TAccumulate, Expression{Func{TAccumulate, TSource, TAccumulate}}, Expression{Func{TAccumulate, TResult}})"/>.
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Aggregate" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">A sequence to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to invoke on each element.</param>
        /// <param name="selector">A function to transform the final accumulator value into the result value.</param>
        /// <returns>The transformed final accumulator value.</returns>
        public static QueryDeferred<TResult> DeferredAggregate<TSource, TAccumulate, TResult>(this IQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (func == null)
                throw Error.ArgumentNull("func");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<TResult>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Aggregate, source, seed, func, selector),
                    source.Expression,
                    Expression.Constant(seed),
                    Expression.Quote(func),
                    Expression.Quote(selector)
                    ));
        }
    }
}
