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

        internal override void ExecuteInternal(QueryContext queryContext, ref SortedList list)
        {
            bool sortAscending = true;
            ArrayList keys = new ArrayList();

            if (Inverse)
                sortAscending = false;

            SortedList tmpList = new SortedList(new QueryResultComparer(sortAscending));

            for (int i = 0; i < members.Count; i++)
            {
                Predicate predicate = (Predicate)members[i];
                predicate.ExecuteInternal(queryContext, ref tmpList);
            }

            if (Inverse)
                keys = GetUnion(tmpList);
            else
                keys = GetIntersection(tmpList);

            if (keys != null)
                list.Add(keys.Count, keys);
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            bool sortAscending = true;
            bool normalizePredicates = true;

            if (Inverse)
                sortAscending = false;

            SortedList list = new SortedList(new QueryResultComparer(sortAscending));

            for (int i = 0; i < members.Count; i++)
            {
                Predicate predicate = (Predicate)members[i];
                bool isOfTypePredicate = predicate is IsOfTypePredicate;

                if (isOfTypePredicate)
                {
                    predicate.Execute(queryContext, (Predicate)members[++i]);
                    normalizePredicates = false;
                }
                else
                {
                    predicate.ExecuteInternal(queryContext, ref list);
                }
            }

            if (normalizePredicates)
            {
                if (Inverse)
                    queryContext.Tree.RightList = GetUnion(list);
                else
                    queryContext.Tree.RightList = GetIntersection(list);
            }
        }

        /// <summary>
        /// handles case for 'OR' condition [Inverse == true]
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private ArrayList GetUnion(SortedList list)
        {
            Hashtable finalTable = new Hashtable();

            if (list.Count > 0)
            {
                ArrayList finalKeys = list.GetByIndex(0) as ArrayList;
                for (int i = 0; i < finalKeys.Count; i++)
                {
                    finalTable[finalKeys[i]] = null;
                }

                for (int i = 1; i < list.Count; i++)
                {
                    ArrayList keys = list.GetByIndex(i) as ArrayList;

                    if (keys != null && keys.Count > 0)
                    {
                        for (int j = 0; j < keys.Count; j++)
                        {
                            finalTable[keys[j]] = null;
                        }
                    }
                }
            }

            return new ArrayList(finalTable.Keys);
        }

        /// <summary>
        /// handles the case for 'AND' condition
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private ArrayList GetIntersection(SortedList list)
        {
            Hashtable finalTable = new Hashtable();

            if (list.Count > 0)
            {
                ArrayList keys = list.GetByIndex(0) as ArrayList;

                for (int i = 0; i < keys.Count; i++)
                {
                    finalTable[keys[i]] = null;
                }

                for (int i = 1; i < list.Count; i++)
                {
                    Hashtable shiftTable = new Hashtable();
                    keys = list.GetByIndex(i) as ArrayList;

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

            return new ArrayList(finalTable.Keys);
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
