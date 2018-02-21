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

using Alachisoft.NCache.Runtime.Caching;
using System;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    /// <summary>
    /// Provides the user to configure different options that can be set while caching a certain item/result set.
    /// </summary>
    public class CachingOptions : ICloneable
    {
        private TimeSpan _SlidingExpTime;
        private DateTime _absoluteExpTime;
        private ExpirationType _expirationType;
        private Tag _queryIdentifier;
        private StoreAs _storeAs;
        private Alachisoft.NCache.Runtime.CacheItemPriority _priority;
        private bool _createDbDependency;

        /// <summary>
        /// Returns the absolute time when the item will expire.
        /// </summary>
        public DateTime AbsoluteExpirationTime
        {
            get => _absoluteExpTime;
        }

        /// <summary>
        /// Returns the sliding expiration time span of the item that will be cached.
        /// </summary>
        public TimeSpan SlidingExpirationTime
        {
            get => _SlidingExpTime;
        }

        /// <summary>
        /// Returns the configured expiration type.
        /// </summary>
        public ExpirationType ExpirationType
        {
            get => _expirationType;
        }

        /// <summary>
        /// Identifier for a query which is added as a <seealso cref="Tag"/> against the result set of 
        /// the query in cache. This MUST be unique for each unique query. QueryIdentifier is used to 
        /// regenerate the result set from the cache upon execution of the same query again. The user 
        /// must maintain a mapping of the query against the query identifier to be used in the future. 
        /// If not specified, NCache adds the query string itself as the tag (making it unique for each 
        /// unique query). For example, a query which returns a Customer is not tagged by the user. The 
        /// next time the same query is executed, the query string is searched as a tag within cache and 
        /// if it exists, the result set against it will be returned.
        /// </summary>
        public Tag QueryIdentifier
        {
            get => _queryIdentifier;
            set => _queryIdentifier = value;
        }

        /// <summary>
        /// Specifies whether the result set should be stored as seperate entities or as a collection.
        /// </summary>
        public StoreAs StoreAs
        {
            get => _storeAs;
            set => _storeAs = value;
        }

        /// <summary>
        /// Specifies the priority of the item. Low priority items are evicted first when eviction triggers.
        /// By default the priority is <seealso cref="Alachisoft.NCache.Runtime.CacheItemPriority.Default"/>.
        /// </summary>
        public Alachisoft.NCache.Runtime.CacheItemPriority Priority
        {
            get => _priority;
            set => _priority = value;
        }

        /// <summary>
        /// Specifies whether to create a database dependency with the result set or not.
        /// </summary>
        public bool CreateDbDependency
        {
            get => _createDbDependency;
            set => _createDbDependency = value;
        }

        /// <summary>
        /// Creates an instance of <see cref="CachingOptions"/> with default values.
        /// </summary>
        public CachingOptions()
        {
            _expirationType = ExpirationType.Absolute;
            _absoluteExpTime = Alachisoft.NCache.Web.Caching.Cache.DefaultAbsolute;
            _SlidingExpTime = Alachisoft.NCache.Web.Caching.Cache.DefaultSliding;
            _queryIdentifier = null;
            _storeAs = StoreAs.Collection;
            _priority = Alachisoft.NCache.Runtime.CacheItemPriority.Default;
            _createDbDependency = false;
        }

        /// <summary>
        /// Sets the absolute expiration time of the caching item.
        /// Only one type of expiration, either absoute expiration or sliding expiration, can be configured at one time.
        /// </summary>
        /// <param name="dateTime">The absolute date time after which the item will be expired from the cache.</param>
        public void SetAbsoluteExpiration(DateTime dateTime)
        {
            _expirationType = ExpirationType.Absolute;
            _absoluteExpTime = dateTime;
        }

        /// <summary>
        /// Sets the sliding expiration time of the caching item.
        /// Only one type of expiration, either absoute expiration or sliding expiration, can be configured at one time.
        /// </summary>
        /// <param name="timespan">The time span in which if the item is not accessed it will be expired from the cache.</param>
        public void SetSlidingExpiration(TimeSpan timespan)
        {
            _expirationType = ExpirationType.Sliding;
            _SlidingExpTime = timespan;
        }

        /// <summary>
        /// Creates a shallow copy of this instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var cachingOptions = new CachingOptions();

            cachingOptions._absoluteExpTime = this._absoluteExpTime;
            cachingOptions._createDbDependency = this.CreateDbDependency;
            cachingOptions._expirationType = this.ExpirationType;
            cachingOptions._priority = this._priority;
            cachingOptions._SlidingExpTime = this._SlidingExpTime;
            cachingOptions._storeAs = this._storeAs;
            if (this._queryIdentifier != null)
                cachingOptions._queryIdentifier = new Tag(this._queryIdentifier.ToString());

            return cachingOptions;
        }

        internal string ToLog()
        {
            return GetType().Name + " = { "
                    + "AbsoluteExpirationTime = '" + AbsoluteExpirationTime + "', "
                    + "SlidingExpirationTime = '" + SlidingExpirationTime + "', "
                    + "ExpirationType = '" + ExpirationType + "', "
                    + "QueryIdentifier = '" + QueryIdentifier + "', "
                    + "StoreAs = '" + StoreAs + "', "
                    + "Priority = '" + Priority + "', "
                    + "CreateDbDependency = '" + CreateDbDependency + "' "
                + "}";
        }
    }
}
