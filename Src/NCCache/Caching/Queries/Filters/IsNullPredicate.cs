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
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Parser;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class IsNullPredicate : Predicate, IComparable
    {
        private IFunctor functor;

        public IsNullPredicate(IFunctor f)
        {
            functor = f;
        }

        public override bool ApplyPredicate(object o)
        {
            return o == null;
        }

        public override string ToString()
        {
            return Inverse ? "is not null" : "is null";
        }

        internal override void ExecuteInternal(QueryContext queryContext, CollectionOperation mergeType)
        {
            throw new ParserException("Incorrect query format. \'" + this.ToString() + "\' not supported.");
        }

        internal override void Execute(QueryContext queryContext, Predicate nextPredicate)
        {
            throw new ParserException("Incorrect query format. \'" + this.ToString() + "\' not supported.");
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is IsNullPredicate)
                return Inverse.CompareTo(((Predicate)obj).Inverse);
            return -1;
        }

        #endregion
    }
}