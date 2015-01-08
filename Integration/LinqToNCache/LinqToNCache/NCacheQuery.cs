// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;
#if JAVA
using Alachisoft.TayzGrid.Web.Caching;
#else
using Alachisoft.NCache.Web.Caching;
#endif

#if JAVA
namespace Alachisoft.TayzGrid.Linq
{
    /// <summary>
    /// Represents an IQueryable instance. Use this class to create a queryable object with the specified 
    /// type 'T'.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TayzGridQuery<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable
    {
        private QueryProviderBase _queryProvider;
        Expression expression;

        public TayzGridQuery(Cache cache)
        {
            _queryProvider = new TayzGrid.Linq.NCacheQueryProvider(cache);
            this.expression = Expression.Constant(this);
        }

        internal TayzGridQuery(QueryProviderBase provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            this._queryProvider = provider;
            this.expression = expression;
        }
#else
namespace Alachisoft.NCache.Linq
{
    /// <summary>
    /// Represents an IQueryable instance. Use this class to create a queryable object with the specified 
    /// type 'T'.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NCacheQuery<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable
    {
         private QueryProviderBase _queryProvider;
        Expression expression;

        public NCacheQuery(Cache cache)
        {
            _queryProvider = new NCache.Linq.NCacheQueryProvider(cache);
            this.expression = Expression.Constant(this);
        }

        internal NCacheQuery(QueryProviderBase provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            this._queryProvider = provider;
            this.expression = expression;
        }
#endif



        #region IQueryable Members
        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree
        /// associated with this instance of System.Linq.IQueryable is executed.
        /// </summary>
        public Type ElementType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
        /// </summary>
        public Expression Expression
        {
            get { return this.expression; }
        }
        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public IQueryProvider Provider
        {
            get { return this._queryProvider; }
        }

        #endregion

        #region IEnumerable Members
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this._queryProvider.Execute(this.expression)).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this._queryProvider.Execute(this.expression)).GetEnumerator();
        }

        #endregion
    }
}
