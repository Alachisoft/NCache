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
using Alachisoft.NCache.Samples.Utility;

namespace Alachisoft.NCache.Samples.Providers
{
    /// <summary>
    /// Contains methods used to read an object by its key from the master data source. 
    /// </summary>
    public class SqlReadThruProvider : Alachisoft.NCache.Runtime.DatasourceProviders.IReadThruProvider
    {
        private SqlDatasource sqlDatasource;

        /// <summary>
        /// Responsible for loading the object from the external data source. 
        /// Key is passed as parameter.
        /// <param name="key">item identifier; probably a primary key</param>
        /// <param name="exh">Current expiration hint; you can modify the value and attach a new hint</param>
        /// <param name="evh">Current eviction hint; you can modify the value and attach a new hint</param>
        /// <returns></returns>
        public void LoadFromSource(string key, out ProviderCacheItem cacheItem)
        {
            cacheItem = new ProviderCacheItem(sqlDatasource.LoadCustomer(key));
            cacheItem.ResyncItemOnExpiration = true;
            cacheItem.ResyncProviderName = sqlDatasource.ConnString;
        }

        /// <summary>
        /// Perform tasks like allocating resources or acquiring connections
        /// </summary>
        /// <param name="parameters">Startup paramters defined in the configuration</param>
        /// <param name="cacheId">Define for which cache provider is configured</param>
        public void Init(IDictionary parameters, string cacheId)
        {
            object connString = parameters["connstring"];
            sqlDatasource = new SqlDatasource();
            sqlDatasource.Connect(connString == null ? "" : connString.ToString());
        }

        /// <summary>
        ///  Perform tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            sqlDatasource.DisConnect();
        }

        #region IReadThruProvider Members

        public Dictionary<string, ProviderCacheItem> LoadFromSource(string[] keys)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
