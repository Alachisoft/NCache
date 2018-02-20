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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Analysis.Generator;

namespace Alachisoft.NCache.Integrations.EntityFramework.Analysis.Generator
{
    /// <summary>
    /// Implements IComparer and return values to sort list
    /// </summary>
    public sealed class QueryPolicyComparer : IComparer<QueryPolicyElementGenerator>
    {
        public enum Order
        {
            Ascending = 1,
            Descending = -1
        }

        private int order;

        /// <summary>
        /// Create an instance of QueryPolicyComparer
        /// </summary>
        /// <param name="order">Order in which sorting will be done</param>
        public QueryPolicyComparer(Order order)
        {
            this.order = (int)order;
        }

        #region IComparer<QueryPolicyGenerator> Members

        /// <summary>
        /// Compare the two policies/
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(QueryPolicyElementGenerator x, QueryPolicyElementGenerator y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x != null)
            {
                return x.CompareTo(y) * this.order;
            }
            else
            {
                return y.CompareTo(x);
            }
        }

        #endregion
    }
}
