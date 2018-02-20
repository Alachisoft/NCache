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
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{double})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Double values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{double?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Double values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is 
        /// empty or contains only null values.</returns>
        public static QueryDeferred<double?> DeferredAverage(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{long})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Int64 values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{long?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Int64 values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty 
        /// or contains only null values.</returns>
        public static QueryDeferred<double?> DeferredAverage(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{float})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Single values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<float> DeferredAverage(this IQueryable<float> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<float>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{float?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Single values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty 
        /// or contains only null values.</returns>
        public static QueryDeferred<float?> DeferredAverage(this IQueryable<float?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<float?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{double})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Double values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage(this IQueryable<double> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{double?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Double values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or 
        /// contains only null values.</returns>
        public static QueryDeferred<double?> DeferredAverage(this IQueryable<double?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{decimal})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Decimal values to calculate the average of.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<decimal> DeferredAverage(this IQueryable<decimal> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<decimal>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average(IQueryable{decimal?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Decimal values to calculate the average of.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence is empty or 
        /// contains only null values.</returns>
        public static QueryDeferred<decimal?> DeferredAverage(this IQueryable<decimal?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<decimal?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence 
        /// is empty or contains only null values.</returns>
        public static QueryDeferred<double?> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<float> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<float>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence 
        /// is empty or contains only null values.</returns>
        public static QueryDeferred<float?> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<float?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence 
        /// is empty or contains only null values</returns>
        public static QueryDeferred<double?> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<double> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence 
        /// is empty or contains only null values</returns>
        public static QueryDeferred<double?> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<double?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values.</returns>
        public static QueryDeferred<decimal> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<decimal>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Average{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Average" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to calculate the average of.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The average of the sequence of values, or null if the source sequence 
        /// is empty or contains only null values</returns>
        public static QueryDeferred<decimal?> DeferredAverage<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<decimal?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Average, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }
    }
}
