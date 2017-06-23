// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class AggregateFunctionPredicate : Predicate, IComparable
    {
        private string _attributeName;
        private Predicate _childPredicate;

        public Predicate ChildPredicate
        {
            get { return _childPredicate; }
            set { _childPredicate = value; }
        }

        public string AttributeName
        {
            get { return _attributeName; }
            set { _attributeName = value; }
        }

        public override bool ApplyPredicate(object o)
        {
            return false;
        }

        internal void SetResult(QueryContext queryContext, AggregateFunctionType functionType, object result)
        {
            QueryResultSet resultSet = new QueryResultSet();
            resultSet.Type = QueryType.AggregateFunction;
            resultSet.AggregateFunctionType = functionType;
            resultSet.AggregateFunctionResult = new DictionaryEntry(functionType, result);
            queryContext.ResultSet = resultSet;
        }

        internal virtual AggregateFunctionType GetFunctionType()
        {
            return AggregateFunctionType.NOTAPPLICABLE;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            AggregateFunctionPredicate other = obj as AggregateFunctionPredicate;

            if (other != null)
                return ((IComparable)ChildPredicate).CompareTo(other.ChildPredicate);

            return -1;
        }

        #endregion
    }
}
