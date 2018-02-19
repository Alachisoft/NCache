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
using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class IsInListPredicate : Predicate, IComparable
    {
        private IFunctor functor;
        private ArrayList members;

        public IsInListPredicate()
        {
            members = new ArrayList();
        }

        public IFunctor Functor
        {
            set { functor = value; }
        }

        public ArrayList Values { get { return members; } }

        public void Append(object item)
        {
            object obj = ((IGenerator)item).Evaluate();
            if (members.Contains(obj)) return;
            members.Add(obj);
            members.Sort();
        }

        public override bool ApplyPredicate(object o)
        {
            object lhs = functor.Evaluate(o);
            if (Inverse)
                return !members.Contains(lhs);
            return members.Contains(lhs);
        }

        internal override void ExecuteInternal(QueryContext queryContext, CollectionOperation mergeType)
        {
            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);

            if (store != null)
            {
                members = queryContext.AttributeValues[((MemberFunction)functor).MemberName] as ArrayList;
                if (members == null)
                {
                    if (queryContext.AttributeValues.Count > 0)
                    {
                        members = new ArrayList();
                        members.Add(queryContext.AttributeValues[((MemberFunction)functor).MemberName]);
                    }
                    else
                    {
                        throw new Exception("Value(s) not specified for indexed attribute " + ((MemberFunction)functor).MemberName + ".");
                    }
                }

                IQueryResult tempResult = queryContext.InternalQueryResult;
                queryContext.InternalQueryResult = new Common.Queries.HashedQueryResult();


                if (!Inverse)
                {
                    for (int i = 0; i < members.Count; i++)
                    {
                        store.GetData(members[i], ComparisonType.EQUALS, queryContext.InternalQueryResult, CollectionOperation.Union);
                    }
                }
                else
                {
                    store.GetData(members[0], ComparisonType.NOT_EQUALS, queryContext.InternalQueryResult, CollectionOperation.Union);
                    if (queryContext.InternalQueryResult != null)
                    {
                        if (queryContext.InternalQueryResult.Count > 0)
                        {
                            for (int i = 1; i < members.Count; i++)
                            {
                                store.GetData(members[i], ComparisonType.EQUALS, queryContext.InternalQueryResult, CollectionOperation.Subtract);
                            }
                        }
                    }
                }
                queryContext.InternalQueryResult.Merge(tempResult, mergeType);
                
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
                members = queryContext.AttributeValues[((MemberFunction)functor).MemberName] as ArrayList;

                if (members == null)
                {
                    if (queryContext.AttributeValues.Count > 0)
                    {
                        members = new ArrayList();
                        members.Add(queryContext.AttributeValues[((MemberFunction)functor).MemberName]);
                    }
                    else
                    {
                        throw new Exception("Value(s) not specified for indexed attribute " + ((MemberFunction)functor).MemberName + ".");
                    }
                }

                ClusteredArrayList keyList = new ClusteredArrayList();
                if (!Inverse)
                {
                    ClusteredArrayList distinctMembers = new ClusteredArrayList();

                    for (int i = 0; i < members.Count; i++)
                    {
                        if (!distinctMembers.Contains(members[i]))
                        {
                            distinctMembers.Add(members[i]);
                            store.GetData(members[i], ComparisonType.EQUALS, queryContext.InternalQueryResult, CollectionOperation.Union);
                        }
                    }
                }
                else
                {
                    ArrayList distinctMembers = new ArrayList();
                    queryContext.InternalQueryResult = new HashedQueryResult();
                    store.GetData(members[0], ComparisonType.NOT_EQUALS, queryContext.InternalQueryResult, CollectionOperation.Union);
                    if (queryContext.InternalQueryResult != null)
                    {
                        if (queryContext.InternalQueryResult.Count > 0)
                        {
                            for (int i = 1; i < members.Count; i++)
                            {
                                if (!distinctMembers.Contains(members[i]))
                                {
                                    distinctMembers.Add(members[i]);
                                    store.GetData(members[i], ComparisonType.EQUALS, queryContext.InternalQueryResult, CollectionOperation.Subtract);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new AttributeIndexNotDefined("Index is not defined for attribute '" + ((MemberFunction)functor).MemberName + "'");
            }
        }
        
        public override string ToString()
        {
            string text = Inverse ? "is not in (" : "is in (";
            for (int i = 0; i < members.Count; i++)
            {
                if (i > 0) text += ", ";
                text += members[i].ToString();
            }
            text += ")";
            return text;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is IsInListPredicate)
            {
                IsInListPredicate other = (IsInListPredicate)obj;
                if (Inverse == other.Inverse)
                {
                    if (members.Count == other.members.Count)
                    {
                        for (int i = 0; i < members.Count; i++)
                            if (members[i] != other.members[i]) return -1;
                        return 0; 
                    }
                }
            }
            return -1;
        }

        #endregion
    }
}
