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
using Alachisoft.NCache.Common.Propagator; 
using Alachisoft.NCache.Runtime.Exceptions;


using Alachisoft.NCache.Common.Logger;



namespace Alachisoft.NCache.Storage

{
	/// <summary>
	/// Facotry object resonsible for creating mulitple types of stores. Used by the LocalCache.
	/// sample property string for creation is.
	/// </summary>
	public class CacheStorageFactory
	{

		/// <summary>
		/// Internal method that creates a cache store. A HashMap containing the config parameters 
		/// is passed to this method.
		/// </summary>
		public static ICacheStorage CreateStorageProvider(IDictionary properties, string cacheContext, bool evictionEnabled, ILogger NCacheLog, IAlertPropagator alertPropagator)
		{
			if(properties == null)
				throw new ArgumentNullException("properties");

			StorageProviderBase cacheStorage = null;
			try
			{
                if(!properties.Contains("class"))
                    throw new ConfigurationException("Missing cache store class.");

				string scheme = Convert.ToString(properties["class"]).ToLower();

                IDictionary schemeProps = (IDictionary)properties[scheme];

                if (scheme.CompareTo("heap") == 0)
                {
                    cacheStorage = new ClrHeapStorageProvider(schemeProps, evictionEnabled, NCacheLog, alertPropagator);
                }
                else if(scheme.CompareTo("memory") == 0)
				{
					cacheStorage = new InMemoryStorageProvider(schemeProps,evictionEnabled);
				}
				else if(scheme.CompareTo("memory-mapped") == 0)
				{
					cacheStorage = new MmfStorageProvider(schemeProps,evictionEnabled);
				}
				else if(scheme.CompareTo("file") == 0)
				{
					cacheStorage = new FileSystemStorageProvider(schemeProps,evictionEnabled);
				}
                else
				{
                    throw new ConfigurationException("Invalid cache store class: " + scheme);
				}
                if (cacheStorage != null) cacheStorage.CacheContext = cacheContext;
			}
			catch(ConfigurationException e)
			{
                Trace.error("CacheStorageFactory.CreateCacheStore()", e.ToString());
				throw;
			}
			catch(Exception e)
			{
                Trace.error("CacheStorageFactory.CreateCacheStore()", e.ToString());
				throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
			}

			return cacheStorage;
		}

	}
}
