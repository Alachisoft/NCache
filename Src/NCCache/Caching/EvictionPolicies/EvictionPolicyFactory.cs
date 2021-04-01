//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections;
using System.Threading;
using System.Globalization;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{
	internal class EvictionPolicyFactory
	{
		/// <summary>
		/// Creates and returns a default eviction policy.
		/// </summary>
		/// <returns></returns>
		public static IEvictionPolicy CreateDefaultEvictionPolicy()
		{
			//return new LFUEvictionPolicy();
            return null;
		}

        /// <summary>
        /// Internal method that creates a cache policy. A HashMap containing the config parameters 
        /// is passed to this method.
        /// </summary>
        public static IEvictionPolicy CreateEvictionPolicy(IDictionary properties, ILogger logger)
        {
            if (properties == null)
				throw new ArgumentNullException("properties");

			try
			{
				float evictRatio = 0;
                if (properties.Contains("evict-ratio")) 
                {
                    CultureInfo thisCult = Thread.CurrentThread.CurrentCulture; //get the currently applied culture.
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");//change it to enUS
                    evictRatio = Convert.ToSingle(properties["evict-ratio"]); //get the value out ...
                    Thread.CurrentThread.CurrentCulture = thisCult; //revert back the original culture.
                }
                
                IEvictionPolicy evictionPolicy = null;

                string scheme = Convert.ToString(properties["class"]).ToLower();
                IDictionary schemeProps = (IDictionary)properties[scheme];

                evictionPolicy = new PriorityEvictionPolicy(schemeProps, evictRatio);

                if (evictionPolicy == null)
                    throw new ConfigurationException("Invalid Eviction Policy: " + scheme);

                FeatureUsageCollector.Instance.GetFeature(FeatureEnum.priority_eviction, FeatureEnum.eviction).UpdateUsageTime();
                return evictionPolicy;
			}
			catch(ConfigurationException e)
			{
				throw;
			}			
			catch(Exception e)
			{
                throw new ConfigurationException("EvictionPolicyFactory.CreateEvictionPolicy(): " + e.ToString());
			}
		}
	}
}
