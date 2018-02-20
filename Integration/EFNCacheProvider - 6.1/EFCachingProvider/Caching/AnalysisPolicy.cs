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

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    internal sealed class AnalysisPolicy
    {
        private AnalysisPolicyElement effectivePolicy;
        private ReaderWriterLock rwLock;        

        /// <summary>
        /// Current instance of CachePolicyCollection
        /// </summary>
        public readonly static AnalysisPolicy Instance;

        /// <summary>
        /// Static constructor
        /// </summary>
        static AnalysisPolicy()
        {
            Instance = new AnalysisPolicy();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private AnalysisPolicy()
        {
            this.rwLock = new ReaderWriterLock();
            
            ///Analysis policy will only be fetched when analysis starts. Once started we will
            ///not consider any policy updates.
            //EFCachingConfiguration.Instance.ConfigurationUpdated += new EventHandler<ConfiguraitonUpdatedEventArgs>(Instance_ConfigurationUpdated);
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
                this.LoadConfig(e.Configuration.AnalysisPolicy);
            }
        }

        /// <summary>
        /// Load analysis configuration by making a copy of orignal config
        /// </summary>
        /// <param name="analysisPolicy"></param>
        public void LoadConfig(AnalysisPolicyElement analysisPolicy)
        {            
            try
            {
                this.rwLock.AcquireWriterLock(Timeout.Infinite);
                this.effectivePolicy = analysisPolicy;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Get a copy of effective analysis policy
        /// </summary>
        /// <returns></returns>
        public AnalysisPolicyElement GetEffectivePolicy()
        {
            try
            {
                this.rwLock.AcquireReaderLock(Timeout.Infinite);
                if (this.effectivePolicy == null)
                {
                    return null;
                }
                return this.effectivePolicy.Clone() as AnalysisPolicyElement;
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }
        }
    }
}
