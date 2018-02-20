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
using System.Configuration;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent "app-config" element in efcaching config
    /// </summary>
    public sealed class ApplicationConfigurationElement : ICloneable
    {
        /// <summary>
        /// Application modes
        /// </summary>
        public enum AppMode
        {
            /// <summary>
            /// Analyze incomming queries
            /// </summary>
            Analysis,
            /// <summary>
            /// Cache query results
            /// </summary>
            Caching
        }

        private AppMode _mode;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ApplicationConfigurationElement()
        {
            this.Mode = AppMode.Caching;
        }

        [ConfigurationSection("queryable-level-caching")]
        public QueryLevelCachePolicyElement QueryLevelCachePolicy
        { get; set; }

        /// <summary>
        /// Get or set "analysis-policy" configuration element
        /// </summary>
        [ConfigurationSection("analysis-policy")]
        public AnalysisPolicyElement AnalysisPolicy { get; set; }

        /// <summary>
        /// Get or set "cache-policy" configuration element
        /// </summary>
        [ConfigurationSection("cache-policy-configuration")]
        public CachePolicyElement CachePolicy { get; set; }

        /// <summary>
        /// Get or set mode
        /// </summary>
        public AppMode Mode
        {
            get { return this._mode; }
            set { this._mode = value; }
        }

        /// <summary>
        /// Get or set application id
        /// </summary>
        [ConfigurationAttribute("app-id")]
        public string Id { get; set; }

        /// <summary>
        /// Get or set the port at which application's remote server would be running
        /// </summary>
        [ConfigurationAttribute("mode")]
        public string ModeString 
        {
            get { return this._mode.ToString(); }
            set
            {
                this._mode = AppMode.Caching;
                if (value != null)
                {
                    switch (value.ToLower())
                    {
                        case "analysis":
                            this._mode = AppMode.Analysis;
                            break;
                        case "caching":
                        case "":
                            this._mode = AppMode.Caching;
                            break;
                        default:
                            throw new ConfigurationErrorsException("Unknown \"mode\" specified");
                    }
                }
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            return new ApplicationConfigurationElement()
            {                
                CachePolicy = this.CachePolicy != null ? this.CachePolicy.Clone() as CachePolicyElement : null,
                AnalysisPolicy = this.AnalysisPolicy != null ? this.AnalysisPolicy.Clone() as AnalysisPolicyElement : null,
                QueryLevelCachePolicy = this.QueryLevelCachePolicy != null ? this.QueryLevelCachePolicy.Clone() as QueryLevelCachePolicyElement : null,
                Id = this.Id.Clone() as string,
                Mode = this.Mode
            };
        }

        #endregion
    }
}
