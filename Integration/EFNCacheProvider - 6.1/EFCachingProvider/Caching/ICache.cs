// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.CacheEntry;
using System.Data.Common;

namespace Alachisoft.NCache.Integrations.EntityFramework.Caching
{
    /// <summary>
    /// Interface to be implemented by cache implementations.
    /// </summary>
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Get or set cache id
        /// </summary>
        string CacheId { get; set; }

        /// <summary>
        /// Tries to the get cached entry by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The retrieved value.</param>
        /// <returns>A value of <c>true</c> if entry was found in the cache, <c>false</c> otherwise.</returns>
        bool GetItem(string key, out object value);

        /// <summary>
        /// Adds the specified entry to the cache.
        /// </summary>
        /// <param name="key">The entry key.</param>
        /// <param name="item">The entry value.</param>
        void PutItem(string key, DbResultItem item, DbCommand command);
    }
}
