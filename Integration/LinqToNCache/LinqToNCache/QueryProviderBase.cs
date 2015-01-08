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
using System.Linq.Expressions;
using System.Reflection;

#if JAVA
namespace Alachisoft.TayzGrid.Linq
#else
namespace Alachisoft.NCache.Linq
#endif

{
    public abstract class QueryProviderBase : IQueryProvider
    {
        protected QueryProviderBase()
        {
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            try
            {
#if JAVA
                return new TayzGridQuery<TElement>(this, expression);
#else
                return new NCacheQuery<TElement>(this, expression);
#endif
            }
            catch (InvalidCastException)
            {
                throw new Exception("Operation not supported");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
#if JAVA
                return (IQueryable)Activator.CreateInstance(typeof(TayzGridQuery<>).MakeGenericType(elementType), new object[] { this, expression });
#else
                return (IQueryable)Activator.CreateInstance(typeof(NCacheQuery<>).MakeGenericType(elementType), new object[] { this, expression });
#endif
                }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            return (TResult)this.Execute(expression);
        }

        object IQueryProvider.Execute(System.Linq.Expressions.Expression expression)
        {
            return this.Execute(expression);
        }

        #endregion
        public abstract object Execute(Expression expression);
    }
}
