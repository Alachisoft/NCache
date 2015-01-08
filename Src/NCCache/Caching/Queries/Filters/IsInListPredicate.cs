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

        internal override void ExecuteInternal(QueryContext queryContext, ref SortedList list)
        {
            AttributeIndex index = queryContext.Index;
            IIndexStore store = ((MemberFunction)functor).GetStore(index);

            ArrayList keyList = new ArrayList();

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

                if (!Inverse)
                {
                    for (int i = 0; i < members.Count; i++)
                    {
                        ArrayList temp = store.GetData(members[i], ComparisonType.EQUALS);
                        if (temp != null)
                            if (temp.Count > 0)
                                keyList.AddRange(temp);
                    }
                }
                else
                {
                    ArrayList temp = store.GetData(members[0], ComparisonType.NOT_EQUALS);
                    if (temp != null)
                    {
                        if (temp.Count > 0)
                        {
                            for (int i = 1; i < members.Count; i++)
                            {
                                ArrayList extras = store.GetData(members[i], ComparisonType.EQUALS);
                                if (extras != null)
                                {
                                    IEnumerator ie = extras.GetEnumerator();
                                    if (ie != null)
                                    {
                                        while (ie.MoveNext())
                                        {
                                            if (temp.Contains(ie.Current))
                                                temp.Remove(ie.Current);
                                        }
                                    }
                                }
                            }

                            keyList.AddRange(temp);
                        }
                    }
                }

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

                ArrayList keyList = new ArrayList();

                if (!Inverse)
                {
                    ArrayList distinctMembers = new ArrayList();

                    for (int i = 0; i < members.Count; i++)
                    {
                        if (!distinctMembers.Contains(members[i]))
                        {
                            distinctMembers.Add(members[i]);
                            ArrayList temp = store.GetData(members[i], ComparisonType.EQUALS);
                            if (temp != null)
                                if (temp.Count > 0)
                                    keyList.AddRange(temp);
                        }
                    }
                }
                else
                {
                    ArrayList distinctMembers = new ArrayList();

                    ArrayList temp = store.GetData(members[0], ComparisonType.NOT_EQUALS);
                    if (temp != null)
                    {
                        if (temp.Count > 0)
                        {
                            for (int i = 0; i < members.Count; i++)
                            {
                                if (!distinctMembers.Contains(members[i]))
                                {
                                    distinctMembers.Add(members[i]);         
                                    ArrayList extras = store.GetData(members[i], ComparisonType.EQUALS);
                                    if (extras != null)
                                    {
                                        IEnumerator ie = extras.GetEnumerator();
                                        if (ie != null)
                                        {
                                            while (ie.MoveNext())
                                            {
                                                if (temp.Contains(ie.Current))
                                                    temp.Remove(ie.Current);
                                            }
                                        }
                                    }
                                }
                                keyList = temp;
                            }
                        }
                    }
                }

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
