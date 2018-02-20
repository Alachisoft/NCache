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
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.Config;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;
using System.Security;
using Alachisoft.NCache.Integrations.EntityFramework.Util;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    internal sealed class CachePolicyCollection
    {
        private CachePolicyElement cachePolicy;
        private ReaderWriterLock rwLock;

        /// <summary>
        /// Current instance of CachePolicyCollection
        /// </summary>
        public static CachePolicyCollection Instance;

        /// <summary>
        /// Static constructor
        /// </summary>
        static CachePolicyCollection()
        {
            Instance = new CachePolicyCollection();
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        private CachePolicyCollection()
        {
            this.rwLock = new ReaderWriterLock();
            EFCachingConfiguration.Instance.ConfigurationUpdated += new EventHandler<ConfiguraitonUpdatedEventArgs>(Instance_ConfigurationUpdated);
        }

        /// <summary>
        /// Called when configuration is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_ConfigurationUpdated(object sender, ConfiguraitonUpdatedEventArgs e)
        {
            if (e.Configuration != null)
            {
                this.LoadConfig(e.Configuration.CachePolicy);
            }
        }

        /// <summary>
        /// Load the collection with new cache policy
        /// </summary>
        /// <param name="cachePolicy">Cache policy from configuration</param>
        public void LoadConfig(CachePolicyElement cachePolicy)
        {
            try
            {
                this.rwLock.AcquireWriterLock(Timeout.Infinite);
                APILevelCaching.LoadConfig(cachePolicy.APILevelCaching);
                this.cachePolicy = cachePolicy;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }
        
        /// <summary>
        /// Get policy settings for specified query
        /// </summary>
        /// <param name="query">Query command</param>
        /// <param name="absoluteExpiration">Absolute expiration value as specified in config</param>
        /// <param name="slidingExpiration">Sliding expiration value as specified in config</param>
        /// <param name="dbType">Target database type</param>
        /// <param name="dbSyncDependency">Indicate whether database sycn dependency is enabled or not</param>
        /// <returns>True if caching is enabled for the selected policy, false otherwise</returns>
        public bool GetEffectivePolicy(string query, out DateTime absoluteExpiration, out TimeSpan slidingExpiration, out CachePolicyElement.DatabaseType dbType, out bool dbSyncDependency,out List<string> parameterList)
        {
            absoluteExpiration = Cache.NoAbsoluteExpiration;
            slidingExpiration = Cache.NoSlidingExpiration;
            dbType = CachePolicyElement.DatabaseType.None;
            dbSyncDependency = false;
            parameterList = null;
            bool cacheable = false;

            if (query == null)
            {
                return cacheable;
            }

            try
            {
                this.rwLock.AcquireReaderLock(Timeout.Infinite);
 
                Alachisoft.NCache.Integrations.EntityFramework.Config.CachePolicy selected = null;
 
                QueryPolicyElement queryPolicyElement = this.GetQueryElement(this.cachePolicy.Query, query);
                if (queryPolicyElement != null)
                {
                    selected = queryPolicyElement.CachePolicy;
                    if (selected != null)
                        parameterList = ((QueryCachePolicyElement)selected).CacheableParameterList;
                    else

                        Logger.Instance.TraceError("No cache policy found for the query " + query + ". Not Cacheable.");
                }
                 
                if (selected == null)
                {
                    if (this.cachePolicy.CacheAllQueries)
                    {
                        //   absoluteExpiration=this.GetQueryElement.
                        QueryLevelCachePolicyElement queryLevelCachePolicy = this.cachePolicy.APILevelCaching;
                        dbType = this.cachePolicy.Database;
                        cacheable = true;
                        dbSyncDependency = queryLevelCachePolicy.DbSyncDependency;
                        if (queryLevelCachePolicy.ExpirationType == EntityFramework.Config.CachePolicy.Expirations.Absolute)
                            absoluteExpiration = DateTime.Now.AddSeconds(queryLevelCachePolicy.ExpirationTime);
                        else
                            slidingExpiration = new TimeSpan(0, 0, queryLevelCachePolicy.ExpirationTime);
                    }
                    return cacheable;
                }

                dbType = this.cachePolicy.Database;
                cacheable = selected.Enabled;
                dbSyncDependency = selected.DbSyncDependency;
                this.GetExpirations(selected, out absoluteExpiration, out slidingExpiration);
            }
            catch (Exception ex)
            {
                cacheable = false;
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }

            return cacheable;
        }

        public QueryLevelCachePolicy APILevelCaching { get { return QueryLevelCachePolicy.Instance; } }
         
        /// <summary>
        /// Get expirations
        /// </summary>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        private void GetExpirations(Alachisoft.NCache.Integrations.EntityFramework.Config.CachePolicy policy, out DateTime absoluteExpiration, out TimeSpan slidingExpiration)
        {
            
            absoluteExpiration = Cache.NoAbsoluteExpiration;
            slidingExpiration = Cache.NoSlidingExpiration;

            switch (policy.ExpirationType)
            {
                case CachePolicy.Expirations.Absolute:
                    absoluteExpiration = DateTime.Now.AddSeconds(policy.ExpirationTime);
                    break;

                case CachePolicy.Expirations.Sliding:
                    slidingExpiration = new TimeSpan(0, 0, policy.ExpirationTime);
                    break;
            }
        }

        /// <summary>
        /// Get query element for specified query
        /// </summary>
        /// <param name="queries">Array of query elements</param>
        /// <param name="queryText">Query statement</param>
        /// <returns>Query element</returns>

        private QueryPolicyElement GetQueryElement(QueryPolicyElement[] queries, string queryText)
        {
            if (queries != null)
            {
                for (int i = 0; i < queries.Length; i++)
                {
                    QueryElement qelement = queries[i].QueryElement;
                    if (qelement.QueryText.Equals(queryText, StringComparison.OrdinalIgnoreCase))
                    {
                        return queries[i];
                    }
                }
            }
          return null;
        }

        internal CachePolicyElement.DatabaseType DatabaseType
        {
            get
            {
                return this.cachePolicy.Database;
            }
        }

        private QueryElement GetQueryElement(QueryElement[] queries, string queryText)
        {
            
            return queries.FirstOrDefault<QueryElement>(
                    selected =>
                    {
                        if (selected.QueryText == null) return false;
                        return selected.QueryText.Equals(queryText, StringComparison.OrdinalIgnoreCase);
                    });
        }

         
    }
}
