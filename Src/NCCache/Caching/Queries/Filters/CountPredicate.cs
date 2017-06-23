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

using System.Collections;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Queries;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class CountPredicate : AggregateFunctionPredicate
    {
        public override bool ApplyPredicate(object o)
        {
            return false;
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            if(ChildPredicate!=null)
                ChildPredicate.Execute(queryContext, nextPredicate);

            decimal count = queryContext.InternalQueryResult.Count;
            base.SetResult(queryContext, AggregateFunctionType.COUNT, count);
        }

        internal override void ExecuteInternal(QueryContext queryContext, CollectionOperation mergeType)
        {
        }

        internal override AggregateFunctionType GetFunctionType()
        {
            return AggregateFunctionType.COUNT;
        }
    }
}
