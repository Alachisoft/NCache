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
 
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Toolkit;
using System.Security;

namespace Alachisoft.NCache.Integrations.EntityFramework.Analysis.Generator
{
    /// <summary>
    /// Hold analysis data
    /// </summary>
    public sealed class QueryPolicyGenerator  :IGenerator<QueryElement>, IComparable<QueryPolicyGenerator>
    {
        private AnalysisPolicyElement analysisPolicy;        
        private string queryCommand;
        private int callCount;
        private Query query;
               
        /// <summary>
        /// Create an instance of QueryPolicyGenerator
        /// </summary>
        /// <param name="query">Query command</param>
        /// <param name="analysisPolicy">Analysis policy as specified in config</param>
        public QueryPolicyGenerator(string query, AnalysisPolicyElement analysisPolicy)
        {
            this.analysisPolicy = analysisPolicy;
            this.queryCommand = query;
            this.callCount = 0;
        }

        public QueryPolicyGenerator(Query query, AnalysisPolicyElement analysisPolicy)
        {
            this.analysisPolicy = analysisPolicy;
            this.query = query;
            this.callCount = 0;
        }

        public Query Query
        {
            get { return this.query; }
        }

        /// <summary>
        /// Get query call count
        /// </summary>
        public int CallCount { get { return this.callCount; } }

        /// <summary>
        /// Increment query call count
        /// </summary>
        public void IncrementCallCount()
        {
            Interlocked.Increment(ref this.callCount);
        }

        /// <summary>
        /// Implicit conversion from QueryPolicyGenerator to QueryElement
        /// </summary>
        /// <param name="queryGen"></param>
        /// <returns></returns>
        /// <remarks>Takes care of both implicit and explicit conversion</remarks>
        public static implicit operator QueryElement(QueryPolicyGenerator queryGen)
        {
            return queryGen.Generate();
        }
        
        #region IGenerator<string> Members

        /// <summary>
        /// Generate QueryElement from collected analysis data
        /// </summary>
        /// <returns>QueryElement instance</returns>
        public QueryElement Generate()
        {
            QueryElement queryElement = new QueryElement()
            {
                Comment = "Call count = " + this.CallCount,
                CommentQuery ="sql-query = \""+query.QueryText+"\"",
                //Enabled = this.analysisPolicy.CacheEnableThreshold <= this.CallCount,
                //DbSyncDependency = this.analysisPolicy.DbSyncDependency,
                //ExpirationType = this.analysisPolicy.DefaultExpirationType,
                //ExpirationTime = this.analysisPolicy.DefualtExpirationTime,
                QueryText = SecurityElement.Escape(query.QueryText),
                //CacheableParameterString = query.GetParameterList(),
                
            };

            return queryElement;
        }

        #endregion

        #region IComparable<QueryPolicyGenerator> Members

        /// <summary>
        /// Compare the supplied QueryPolicyGenerator with this instance
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(QueryPolicyGenerator other)
        {
            if (other == null)
            {
                return 1;
            }
            else if(this.callCount < other.callCount)
            {
                return -1;
            }
            else if (this.callCount > other.callCount)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}
