using Alachisoft.NCache.Web.Caching;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    internal class NCacheOqlEnumerable<T> : IEnumerable<T>
    {
        private NCacheOqlEnumerator<T> enumeratorWrapper;

        internal enum EnumerableType
        {
            Normal,
            Deferred
        };

        internal NCacheOqlEnumerable(EnumerableType type, ICacheReader cacheReader)
        {
            switch (type)
            {
                case EnumerableType.Normal:
                    enumeratorWrapper = new NCacheOqlEnumeratorNormal<T>(cacheReader);
                    break;
                case EnumerableType.Deferred:
                    enumeratorWrapper = new NCacheOqlEnumeratorDeferred<T>(cacheReader);
                    break;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return enumeratorWrapper;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return enumeratorWrapper;
        }
    }

    abstract class NCacheOqlEnumerator<TSource> : IEnumerator<TSource>
    {
        protected ICacheReader reader;

        public TSource Current => RowReturn();

        object IEnumerator.Current => RowReturn();

        protected NCacheOqlEnumerator(ICacheReader cacheReader)
        {
            Logger.Log(
                "Running " + GetType().Name + " enumerator.",
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            reader = cacheReader;
        }

        public void Dispose() => reader.Dispose();

        public void Reset() => throw new Exception("Reset is not supported.");

        public bool MoveNext()
        {
            return reader.Read();
        }

        protected abstract TSource RowReturn();
    }

    class NCacheOqlEnumeratorNormal<TSource1> : NCacheOqlEnumerator<TSource1>
    {
        internal NCacheOqlEnumeratorNormal(ICacheReader cacheReader) : base(cacheReader) { }

        protected override TSource1 RowReturn()
        {
            return (TSource1)reader[reader.FieldCount - 1];
        }
    }

    class NCacheOqlEnumeratorDeferred<TSource2> : NCacheOqlEnumerator<TSource2>
    {
        private int key;
        private Type[] genericTypes;

        internal NCacheOqlEnumeratorDeferred(ICacheReader cacheReader) : base(cacheReader)
        {
            key = 0;
            genericTypes = typeof(TSource2).GetGenericArguments();
        }

        protected override TSource2 RowReturn()
        {
            // When Group By is used
            if (genericTypes.Length > 1)
            {
                var returnVal = (TSource2)Activator.CreateInstance(typeof(Grouping<,>).MakeGenericType(genericTypes), ++key);

                returnVal.GetType().GetMethod("Add").Invoke(returnVal, new object[] { Convert.ToInt32(reader[reader.FieldCount - 1]) });

                return returnVal;
            }

            // When Group By isn't used
            return (TSource2)Convert.ChangeType(reader[reader.FieldCount - 1], typeof(TSource2));
        }
    }
}
