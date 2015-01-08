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
using System.Collections;
using Alachisoft.NCache.Caching.Topologies.Local;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public abstract class Predicate : IPredicate
    {
        private bool inverse;
        
        public bool Inverse { get { return inverse; } }
        public virtual void Invert() { inverse = !inverse; }

        public bool Evaluate(object o)
        {
            return !inverse == ApplyPredicate(o);
        }

        internal virtual ArrayList ReEvaluate(AttributeIndex index, LocalCacheBase cache, IDictionary attributeValues, string cacheContext)
        {
            QueryContext context = new QueryContext(cache);
            context.AttributeValues = attributeValues;
            context.Index = index;
            context.CacheContext = cacheContext;    

            Execute(context, null);

            context.Tree.Reduce();
            return context.Tree.LeftList;
        }

        internal virtual void Execute(QueryContext queryContext, Predicate nextPredicate) { }

        internal virtual void ExecuteInternal(QueryContext queryContext, ref SortedList list) { }

        public abstract bool ApplyPredicate(object o);
    }
}
