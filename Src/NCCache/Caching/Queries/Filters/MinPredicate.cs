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
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class MinPredicate : AggregateFunctionPredicate
    {
        public override bool ApplyPredicate(object o)
        {
            return false;
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            if(ChildPredicate!=null)
                ChildPredicate.Execute(queryContext, nextPredicate);

            queryContext.Tree.Reduce();
            CacheEntry entry = null;

            bool initialized = false;
            IComparable min = null;
            Type type = null;
            foreach (string key in queryContext.Tree.LeftList)
            {
                CacheEntry cacheentry = queryContext.Cache.GetEntryInternal(key, false);
                IComparable current = (IComparable)queryContext.Index.GetAttributeValue(key, AttributeName, cacheentry.IndexInfo);

                if (current != null)
                {
                    type = current.GetType();
                    if (type == typeof(bool))
                        throw new Exception("MIN cannot be applied to Boolean data type.");

                    if (!initialized)
                    {
                        min = current;
                        initialized = true;
                    }
                    if (current.CompareTo(min) < 0)
                        min = current;
                }
            }
            if (type != null)
            {

                if ((type != typeof(DateTime)) && (type != typeof(string)) && (type != typeof(char)))
                {
                    if (min != null)
                    {
                        base.SetResult(queryContext, AggregateFunctionType.MIN, Convert.ToDecimal(min));
                        return;
                    }
                }
            }
            base.SetResult(queryContext, AggregateFunctionType.MIN, min);
        }

        internal override void ExecuteInternal(QueryContext queryContext, ref SortedList list)
        {
        }

        internal override AggregateFunctionType GetFunctionType()
        {
            return AggregateFunctionType.MIN;
        }
    }
}
