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
using System.Threading;

using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Analysis.Generator;

namespace Alachisoft.NCache.Integrations.EntityFramework.Analysis.Generator
{    
    /// <summary>
    /// Generates custom policy element from analysis data
    /// </summary>
    internal sealed class CustomPolicyGenerator :IGenerator<CustomPolicyElement>, IDisposable
    {
        private AnalysisPolicyElement analysisPolicy;
        private Dictionary<string, QueryPolicyGenerator> queries;
        private ReaderWriterLock rwLock;

        private Dictionary<string, QueryPolicyElementGenerator> queriesGen;
        /// <summary>
        /// Create an instance of CustomPolicyGenerator
        /// </summary>
        /// <param name="analysisPolicy">Analysis policy as specified in config</param>
        public CustomPolicyGenerator(AnalysisPolicyElement analysisPolicy)
        {
            this.analysisPolicy = analysisPolicy;            
            if (this.analysisPolicy == null)
            {
                this.analysisPolicy = new AnalysisPolicyElement()
                {
                    CacheEnableThreshold = 0,
                    DbSyncDependency = false,
                    DefaultExpirationType = CachePolicy.Expirations.Absolute,
                    DefualtExpirationTime = 180
                };
            }
            this.queries = new Dictionary<string, QueryPolicyGenerator>();
            this.rwLock = new ReaderWriterLock();
            this.queriesGen = new Dictionary<string, QueryPolicyElementGenerator>();
        }

        /// <summary>
        /// Analyze current query
        /// </summary>
        /// <param name="query"></param>
        //public void AnalyzeQuery(Query query)
        //{
        //    try
        //    {
        //        this.rwLock.AcquireReaderLock(Timeout.Infinite);
                
        //        QueryPolicyGenerator queryGen = null;
        //        if (this.queries.ContainsKey(query.QueryText))
        //        {
        //            queryGen = this.queries[query.QueryText];
        //        }

        //        if (queryGen == null)
        //        {
        //            LockCookie cookies = this.rwLock.UpgradeToWriterLock(Timeout.Infinite);
        //            try
        //            {
        //                if (!this.queries.ContainsKey(query.QueryText))
        //                {
        //                    queryGen = new QueryPolicyGenerator(query, this.analysisPolicy);
        //                    this.queries.Add(query.QueryText, queryGen);
        //                }
        //                else
        //                {
        //                    queryGen = this.queries[query.QueryText];
        //                }
        //            }
        //            finally
        //            {
        //                this.rwLock.DowngradeFromWriterLock(ref cookies);
        //            }
        //        }
        //        queryGen.IncrementCallCount();
        //    }
        //    finally
        //    {
        //        this.rwLock.ReleaseReaderLock();
        //    }            
        //}




        public void AnalyzeQuery(Query query)
        {
            try
            {
                this.rwLock.AcquireReaderLock(Timeout.Infinite);

                QueryPolicyElementGenerator queryGen = null;
                if (this.queriesGen.ContainsKey(query.QueryText))
                {
                    queryGen = this.queriesGen[query.QueryText];
                }

                if (queryGen == null)
                {
                    LockCookie cookies = this.rwLock.UpgradeToWriterLock(Timeout.Infinite);
                    try
                    {
                        if (!this.queriesGen.ContainsKey(query.QueryText))
                        {
                            queryGen = new QueryPolicyElementGenerator(query, this.analysisPolicy);
                            this.queriesGen.Add(query.QueryText, queryGen);
                        }
                        else
                        {
                            queryGen = this.queriesGen[query.QueryText];
                        }
                    }
                    finally
                    {
                        this.rwLock.DowngradeFromWriterLock(ref cookies);
                    }
                }
                queryGen.IncrementCallCount();
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }
        }

        #region IGenerator<string> Members

        /// <summary>
        /// Generates analysis report
        /// </summary>
        /// <returns>Analysis report data</returns>
        //public CustomPolicyElement Generate()
        //{
        //    if (this.queries.Count == 0)
        //    {
        //        return null;
        //    }

        //    try
        //    {
        //        this.rwLock.AcquireWriterLock(Timeout.Infinite);
        //        List<QueryPolicyGenerator> values = this.queries.Values.ToList<QueryPolicyGenerator>();
        //        QueryPolicyComparer comparer = new QueryPolicyComparer(QueryPolicyComparer.Order.Descending);
        //        values.Sort(comparer);

        //        CustomPolicyElement customPolicy = new CustomPolicyElement();
        //        List<QueryElement> queryElements = values.ConvertAll<QueryElement>(qpGen => (QueryElement)qpGen);
        //        customPolicy.Query = queryElements.ToArray<QueryElement>();
        //        return customPolicy;
        //    }
        //    finally
        //    {
        //        this.rwLock.ReleaseWriterLock();
        //    }
        //}


        public CustomPolicyElement Generate()
        {
            if (this.queriesGen.Count == 0)
            {
                return null;
            }

            try
            {
                this.rwLock.AcquireWriterLock(Timeout.Infinite);
                List<QueryPolicyElementGenerator> values = this.queriesGen.Values.ToList<QueryPolicyElementGenerator>();
                QueryPolicyComparer comparer = new QueryPolicyComparer(QueryPolicyComparer.Order.Descending);
                values.Sort(comparer);

                CustomPolicyElement customPolicy = new CustomPolicyElement();
                customPolicy.Query = new QueryPolicyElement[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    customPolicy.Query[i] = values[i].Generate();
                }


                return customPolicy;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }


        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.queries.Clear();
        }

        #endregion
    }
}
