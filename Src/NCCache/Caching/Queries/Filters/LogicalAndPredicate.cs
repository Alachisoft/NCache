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
using Alachisoft.NCache.Common.Queries;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class LogicalAndPredicate : Predicate, IComparable
    {
        private ArrayList members;

        public LogicalAndPredicate()
        {
            members = new ArrayList();
        }

        public ArrayList Children { get { return members; } }

        public override void Invert()
        {
            base.Invert();
            for (int i = 0; i < members.Count; i++)
                ((Predicate)members[i]).Invert();
        }

        public override bool ApplyPredicate(object o)
        {
            for (int i = 0; i < members.Count; i++)
                if (((Predicate)members[i]).Evaluate(o) == Inverse)
                    return Inverse;
            return !Inverse;
        }

        internal override void ExecuteInternal(QueryContext queryContext, CollectionOperation mergeType)
        {
            bool sortAscending = true;
            ClusteredArrayList keys = new ClusteredArrayList();

            if (Inverse)
                sortAscending = false;

            SortedList tmpList = new SortedList(new QueryResultComparer(sortAscending));

            IQueryResult temp = queryContext.InternalQueryResult;
            queryContext.InternalQueryResult = new Common.Queries.HashedQueryResult();
            
            for (int i = 0; i < members.Count; i++)
            {
                Predicate predicate = (Predicate)members[i];
                CollectionOperation mergeTypeX = (Inverse == true || (queryContext.InternalQueryResult.Count == 0 & i == 0)) ? CollectionOperation.Union : CollectionOperation.Intersection;
                predicate.ExecuteInternal(queryContext, mergeTypeX);
            }
            queryContext.InternalQueryResult.Merge(temp, mergeType);
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            for (int i = 0; i < members.Count; i++)
            {
                Predicate predicate = (Predicate)members[i];
                bool isOfTypePredicate = predicate is IsOfTypePredicate;

                if (isOfTypePredicate)
                {
                    predicate.Execute(queryContext, (Predicate)members[++i]);
                }
                else
                {
                    CollectionOperation mergeType = (Inverse == true || (queryContext.InternalQueryResult.Count == 0 & i == 0)) ? CollectionOperation.Union : CollectionOperation.Intersection;
                    predicate.ExecuteInternal(queryContext, mergeType);
                }
            }
        }

        /// <summary>
        /// handles case for 'OR' condition [Inverse == true]
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private ClusteredArrayList GetUnion(SortedList list)
        {
            HashVector finalTable = new HashVector();

            if (list.Count > 0)
            {
                ClusteredArrayList finalKeys = list.GetByIndex(0) as ClusteredArrayList;
                for (int i = 0; i < finalKeys.Count; i++)
                {
                    finalTable[finalKeys[i]] = null;
                }

                for (int i = 1; i < list.Count; i++)
                {
                    ClusteredArrayList keys = list.GetByIndex(i) as ClusteredArrayList;

                    if (keys != null && keys.Count > 0)
                    {
                        for (int j = 0; j < keys.Count; j++)
                        {
                            finalTable[keys[j]] = null;
                        }
                    }
                }
            }

            return new ClusteredArrayList(finalTable.Keys);
        }

        /// <summary>
        /// handles the case for 'AND' condition
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private ClusteredArrayList GetIntersection(SortedList list)
        {
            HashVector finalTable = new HashVector();

            if (list.Count > 0)
            {
                ClusteredArrayList keys = list.GetByIndex(0) as ClusteredArrayList;

                for (int i = 0; i < keys.Count; i++)
                {
                    finalTable[keys[i]] = null;
                }

                for (int i = 1; i < list.Count; i++)
                {
                    HashVector shiftTable = new HashVector();
                    keys = list.GetByIndex(i) as ClusteredArrayList;

                    if (keys != null)
                    {
                        for (int j = 0; j < keys.Count; j++)
                        {
                            object key = keys[j];

                            if (finalTable.ContainsKey(key))
                                shiftTable[key] = null;
                        }
                    }

                    finalTable = shiftTable;
                }
            }

            return new ClusteredArrayList(finalTable.Keys);
        }

        public override string ToString()
        {
            string text = Inverse ? "(" : "(";
            for (int i = 0; i < members.Count; i++)
            {
                if (i > 0) text += Inverse ? " or " : " and ";
                text += members[i].ToString();
            }
            text += ")";
            return text;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is LogicalAndPredicate)
            {
                LogicalAndPredicate other = (LogicalAndPredicate)obj;
                if (Inverse == other.Inverse)
                {
                    if (members.Count == other.members.Count)
                    {
                        for (int i = 0; i < members.Count; i++)
                            if (members[i] != other.members[i]) return -1;
                        return 0; //members.CompareTo(other.members);
                    }
                }
            }
            return -1;
        }

        #endregion
    }
}
