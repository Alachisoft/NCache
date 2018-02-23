// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Caching;
using BackingSource.Utility;

namespace Alachisoft.NCache.Samples.Providers
{
    /// <summary>
    /// Contains methods used to read an object by its key from the master data source. 
    /// </summary>
    public class SqliteReadThruProvider : Alachisoft.NCache.Runtime.DatasourceProviders.IReadThruProvider
    {
        SqliteDataSource _source = new SqliteDataSource();

        /// <summary>
        /// Responsible for loading the object from the external data source. 
        /// Key is passed as parameter.
        /// </summary>
        /// <param name="key">item identifier; probably a primary key</param>
        /// <param name="cacheItem">It is used to provide CacheItem information like Cache policies, dependencies, expirations ...</param>
		public void LoadFromSource(string key, out ProviderCacheItem cacheItem)
        {
            cacheItem = new ProviderCacheItem(_source.LoadCustomer(key));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys[]"></param>
        /// <returns></returns>
        public Dictionary<string, ProviderCacheItem> LoadFromSource(string[] keys)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Perform tasks like allocating resources or acquiring connections
        /// </summary>
        /// <param name="parameters">Startup paramters defined in the configuration</param>
        /// <param name="cacheId">Define for which cache provider is configured</param>
        public void Init(IDictionary parameters, string cacheId)
        {
            object connString = parameters["connstring"];
            _source.Connect(connString == null ? "" : connString.ToString());
        }

        /// <summary>
        ///  Perform tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            _source.DisConnect();
        }
    }
}
