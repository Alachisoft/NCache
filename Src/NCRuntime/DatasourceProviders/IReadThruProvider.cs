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
// limitations under the License

using System;
using System.Collections;
using System.Collections.Generic;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Dependencies;
#else
using Alachisoft.NCache.Runtime.Dependencies;
#endif
#if JAVA
using Alachisoft.TayzGrid.Runtime;
#else
using Alachisoft.NCache.Runtime;
#endif
#if JAVA
using Alachisoft.TayzGrid.Runtime.Caching;
#else
using Alachisoft.NCache.Runtime.Caching;
#endif

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.DatasourceProviders
#else
namespace Alachisoft.NCache.Runtime.DatasourceProviders
#endif
{
	/// <summary>
	/// Contains methods used to read an object by its key from the master data source. 
	/// Must be implemented by read-through components.
    /// </summary>


	public interface IReadThruProvider
    {
		/// <summary>
		/// Perform tasks like allocating resources or acquiring connections etc.
		/// </summary>
		/// <param name="parameters">Startup paramters defined in the configuration</param>
        /// <param name="cacheId">Id of the Cache</param>
        void Init(IDictionary parameters, string cacheId);

		/// <summary>
		/// Responsible for loading the object from the data source. Key is passed as parameter.
		/// </summary>
		/// <param name="key">key used to refernece object</param>
		/// <param name="CacheItem">cache item to be inserted</param>
		/// 
		void LoadFromSource(string key, out ProviderCacheItem cacheItem);
		
        /// <summary>
        /// Responsible for loading array of objects from the data source. Keys are passed as parameter.
        /// </summary>
        /// <param name="keys">array of keys</param>
		/// <param name="CacheItem">cache item to be inserted</param>
		/// 
		Dictionary<string, ProviderCacheItem> LoadFromSource(string[] keys);
		/// <summary>
		/// Perform tasks associated with freeing, releasing, or resetting resources.
		/// </summary>
		void Dispose();

	}
}
