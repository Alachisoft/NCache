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

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    public class CompositeFunction : IFunctor, IComparable
    {
        IFunctor func;
        IFunctor func2;

        public CompositeFunction(IFunctor func, IFunctor func2)
        {
            this.func = func;
            this.func2 = func2;
        }

        public object Evaluate(object o)
        {
            return func.Evaluate(func2.Evaluate(o));
        }

        public override string ToString()
        {
            return func + "." + func2;
        }
        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj is CompositeFunction)
            {
                CompositeFunction other = (CompositeFunction)obj;
                return (((IComparable)func).CompareTo(other.func) == 0)
                       && ((IComparable)func2).CompareTo(other.func2) == 0 ? 0 : -1;
            }
            return -1;
        }

        #endregion
    }
}
