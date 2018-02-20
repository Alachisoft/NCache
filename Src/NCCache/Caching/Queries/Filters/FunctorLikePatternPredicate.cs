// Copyright (c) 2018 Alachisoft
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
using System.Collections;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class FunctorLikePatternPredicate : Predicate, IComparable
    {
        IFunctor functor;
        IGenerator generator;
        Regex regex;
        string pattern;

        public FunctorLikePatternPredicate(IFunctor lhs, IGenerator rhs)
        {
            functor = lhs;
            generator = rhs;
        }

        public override bool ApplyPredicate(object o)
        {
            object lhs = functor.Evaluate(o);
            if (Inverse)
                return !regex.Match(lhs.ToString()).Success;
            return regex.Match(lhs.ToString()).Success;
        }

        internal override void ExecuteInternal(QueryContext queryContext, CollectionOperation mergeType)
        {
            ClusteredArrayList keyList = null;
            pattern = (string)generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues);
            pattern = pattern.Trim('\'');

            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);
            if (store != null)
            {
                if (Inverse)
                    store.GetData(pattern, ComparisonType.NOT_LIKE, queryContext.InternalQueryResult, mergeType);
                else
                    store.GetData(pattern, ComparisonType.LIKE, queryContext.InternalQueryResult, mergeType);
            }
            else
            {
                throw new AttributeIndexNotDefined("Index is not defined for attribute '" + ((MemberFunction)functor).MemberName + "'");
            }
        }

        /// <summary>
        /// See attribute-level indexes can't be used in this predicate.
        /// </summary>
        /// <param name="queryContext"></param>
        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            ClusteredArrayList keyList = null;
            pattern = (string)generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues);
            pattern = pattern.Trim('\'');

            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);

            if (store != null)
            {
                if (Inverse)
                    store.GetData(pattern, ComparisonType.NOT_LIKE, queryContext.InternalQueryResult, CollectionOperation.Union);
                else
                    store.GetData(pattern, ComparisonType.LIKE, queryContext.InternalQueryResult, CollectionOperation.Union);

            }
            else
            {
                throw new AttributeIndexNotDefined("Index is not defined for attribute '" + ((MemberFunction)functor).MemberName + "'");
            }
        }

        public override string ToString()
        {
            return functor + (Inverse ? " not like " : " like ") + pattern;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            return -1;
        }

        #endregion
    }
}