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
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{int})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Int32 values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<int> DeferredSum(this IQueryable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<int>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{int?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Int32 values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<int?> DeferredSum(this IQueryable<int?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<int?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{long})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Int64 values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<long> DeferredSum(this IQueryable<long> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<long>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{long?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Int64 values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<long?> DeferredSum(this IQueryable<long?> source)
        {
            if (source == null)
                throw Error.ArgumentNull("source");

            return new QueryDeferred<long?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{float})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Single values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<float> DeferredSum(this IQueryable<float> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{float?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Single values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<float?> DeferredSum(this IQueryable<float?> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{double})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Double values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<double> DeferredSum(this IQueryable<double> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{double?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Double values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<double?> DeferredSum(this IQueryable<double?> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{decimal})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of System.Decimal values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<decimal> DeferredSum(this IQueryable<decimal> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum(IQueryable{decimal?})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <param name="source">A sequence of nullable System.Decimal values to calculate the sum of.</param>
        /// <returns>The sum of the values in the sequence.</returns>
        public static QueryDeferred<decimal?> DeferredSum(this IQueryable<decimal?> source)
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
                    GetMethodInfo(Queryable.Sum, source), source.Expression));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<int> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<int>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<int?> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<int?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<long> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<long>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<long?> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
        {
            if (source == null)
                throw Error.ArgumentNull("source");
            if (selector == null)
                throw Error.ArgumentNull("selector");

            return new QueryDeferred<long?>(
#if EF5 || EF6
                source.GetObjectQuery(),
#elif EFCORE 
                source,
#endif
                Expression.Call(
                    null,
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<float> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<float?> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<double> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<double?> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<decimal> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }

        /// <summary>
        /// This method is the deferred implementation of the extension method 
        /// <seealso cref="System.Linq.Queryable.Sum{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}})"/>. 
        /// In EF some linq operations are performed on the client end instead of the server.
        /// Therefore, if we use the regular method it will cache the data from the database before
        /// performing the actual "Sum" operation. To cater to this, we provide with a deferred 
        /// implementation that delays the caching so that only the result can be cached.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values of type TSource.</param>
        /// <param name="selector">A projection function to apply to each element.</param>
        /// <returns>The sum of the projected values.</returns>
        public static QueryDeferred<decimal?> DeferredSum<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
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
                    GetMethodInfo(Queryable.Sum, source, selector),
                    new[] { source.Expression, Expression.Quote(selector) }
                    ));
        }
    }
}
