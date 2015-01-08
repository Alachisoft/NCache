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
using System.Collections;
using Alachisoft.NCache.Parser;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class FunctorLesserGeneratorPredicate : Predicate, IComparable
    {
        IFunctor functor;
        IGenerator generator;

        public FunctorLesserGeneratorPredicate(IFunctor lhs, IGenerator rhs)
        {
            functor = lhs;
            generator = rhs;
        }

        public override bool ApplyPredicate(object o)
        {
            object lhs = functor.Evaluate(o);
            if (Inverse)
                return Comparer.Default.Compare(lhs, generator.Evaluate()) >= 0;
            return Comparer.Default.Compare(lhs, generator.Evaluate()) < 0;
        }

        internal override void ExecuteInternal(QueryContext queryContext, ref SortedList list)
        {
            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);

            if (store != null)
            {
                ArrayList keyList = null;
                if (Inverse)
                    keyList = store.GetData(generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues), ComparisonType.GREATER_THAN_EQUALS);
                else
                    keyList = store.GetData(generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues), ComparisonType.LESS_THAN);

                if (keyList != null)
                    list.Add(keyList.Count, keyList);
            }
            else
            {
                throw new AttributeIndexNotDefined("Index is not defined for attribute '" + ((MemberFunction)functor).MemberName + "'");
            }
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);

            if (store != null)
            {
                ArrayList keyList = null;
                if (Inverse)
                    keyList = store.GetData(generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues), ComparisonType.GREATER_THAN_EQUALS);
                else
                    keyList = store.GetData(generator.Evaluate(((MemberFunction)functor).MemberName, queryContext.AttributeValues), ComparisonType.LESS_THAN);

                if (keyList != null && keyList.Count > 0)
                {
                    IEnumerator keyListEnum = keyList.GetEnumerator();

                    if (queryContext.PopulateTree)
                    {
                        queryContext.Tree.RightList = keyList;

                        queryContext.PopulateTree = false;
                    }
                    else
                    {
                        while (keyListEnum.MoveNext())
                        {
                            if (queryContext.Tree.LeftList.Contains(keyListEnum.Current))
                                queryContext.Tree.Shift(keyListEnum.Current);
                        }
                    }
                }
            }
            else //straight-forward. no index.
            {
                throw new AttributeIndexNotDefined("Index is not defined for attribute '" + ((MemberFunction)functor).MemberName + "'");
            }
        }

        public override string ToString()
        {
            return functor + (Inverse ? " >= " : " < ") + generator;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is FunctorLesserGeneratorPredicate)
            {
                FunctorLesserGeneratorPredicate other = (FunctorLesserGeneratorPredicate)obj;
                if (Inverse == other.Inverse)
                    return ((IComparable)functor).CompareTo(other.functor) == 0 &&
                           ((IComparable)generator).CompareTo(other.generator) == 0 ? 0 : -1;
            }
            return -1;
        }

        #endregion
    }
}
