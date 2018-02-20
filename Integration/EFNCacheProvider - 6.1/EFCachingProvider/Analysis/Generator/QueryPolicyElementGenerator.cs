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
using Alachisoft.NCache.Integrations.EntityFramework.Analysis.Generator;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using System.Threading;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching.Analysis.Generator
{
    public sealed class QueryPolicyElementGenerator : IGenerator<QueryPolicyElement>,IComparable<QueryPolicyElementGenerator>
    {
        private QueryPolicyGenerator queryPolGen;
        private QueryCachePolicyGenerator cachePolicyGen;

        private ReaderWriterLock rwLock;


        public QueryPolicyElementGenerator(Query query, AnalysisPolicyElement analysisPolicy)
        {
            queryPolGen = new QueryPolicyGenerator(query, analysisPolicy);
            this.rwLock = new ReaderWriterLock();
            cachePolicyGen = new QueryCachePolicyGenerator(query, analysisPolicy);
        }

        public QueryPolicyElement Generate()
        {
            if (this.queryPolGen == null || cachePolicyGen ==null)
            {
                return null;
            }

            try
            {
                this.rwLock.AcquireWriterLock(Timeout.Infinite);
                QueryPolicyElement queryPolicy = new QueryPolicyElement();
                queryPolicy.QueryElement = queryPolGen.Generate();
                queryPolicy.CachePolicy = cachePolicyGen.Generate();

                return queryPolicy;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }

        internal void IncrementCallCount()
        {
            if (queryPolGen != null)
            {
                queryPolGen.IncrementCallCount();
                cachePolicyGen.IncrementCallCount();
            }
        }

        public int CompareTo(QueryPolicyElementGenerator other)
        {
            if (other == null)
            {
                return 1;
            }
            else if (this.cachePolicyGen.CallCount < other.cachePolicyGen.CallCount)
            {
                return -1;
            }
            else if (this.cachePolicyGen.CallCount > other.cachePolicyGen.CallCount)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
