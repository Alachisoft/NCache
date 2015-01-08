// Copyright (c) 2015 Alachisoft
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
using System.Data;
using System.Collections;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Storage
{
	/// <summary>
	/// Facotry object resonsible for creating mulitple types of stores. Used by the LocalCache.
	/// sample property string for creation is.
	///  
	///		storage
	///		(
	///			scheme=file;
	///			file
	///			(
	///				max-objects=100;
	///				root-dir=�c:\temp\test�;
	///			)
	///		)
	///		
	/// </summary>
	public class CacheStorageFactory
	{

		/// <summary>
		/// Internal method that creates a cache store. A HashMap containing the config parameters 
		/// is passed to this method.
		/// </summary>
		public static ICacheStorage CreateStorageProvider(IDictionary properties, string cacheContext, bool evictionEnabled, ILogger NCacheLog)
		{
			if(properties == null)
				throw new ArgumentNullException("properties");

			StorageProviderBase cacheStorage = null;
			try
			{
                string scheme = "heap";
                IDictionary schemeProps = (IDictionary)properties[scheme];

                if (scheme.CompareTo("heap") == 0)
                {
                    cacheStorage = new ClrHeapStorageProvider(schemeProps, evictionEnabled, NCacheLog);
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
