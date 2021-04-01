//  Copyright (c) 2018 Alachisoft
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

using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using System;

namespace Alachisoft.NCache.Web.Caching.APILogging
{
    internal class APILogItem
    {
        private string _signature = null;

        private string _key = null;

        private int? _noOfObjectsReturned = null;

        /// <summary> Absolute expiration for the object. </summary>
        private DateTime? _abs = null;
        /// <summary> Sliding expiration for the object. </summary>
        private TimeSpan? _sld = null;

        private CacheItemPriority? _p = null;
        private string _group = null;
        private string _subGroup = null;

        private Tag[] _tags = null;
        private NamedTagsDictionary _namedTags = null;
        private int _noOfKeys = -1;

        private CacheDependency _dep;
        private CacheSyncDependency _syncDep;

        private string _providerName = null;
        private string _resyncProviderName = null;
        private bool? _isResyncRequired = null;

        private DSWriteOption? _dsWriteOption = null;
        private DSReadOption? _dsReadOption = null;

        private string _query = null;
        private System.Collections.IDictionary _queryValues = null;

        private ContinuousQuery _cq = null;
        private StreamMode? _streamMode = null;


        private CacheItemVersion _version = null;

        private TimeSpan? _lockTimeout = null;
        private bool? _acquireLock = null;



        private string _exceptionMessage = null;
        private RuntimeAPILogItem _rtAPILogItem = null;
        private DateTime _loggingTime;

        public DateTime LoggingTime
        {
            get { return _loggingTime; }
            set { _loggingTime = value; }
        }


        public RuntimeAPILogItem RuntimeAPILogItem
        {
            get { return _rtAPILogItem; }
            set { _rtAPILogItem = value; }
        }

        public APILogItem()
        {
        }

        public APILogItem(string key, string exceptionMessage)
        {
            _key = key;
            this.ExceptionMessage = exceptionMessage;
        }

        public APILogItem(string key, CacheItem item, string exceptionMessage)
        {
            _key = key;

            _group = item.Group;
            _subGroup = item.SubGroup;
            _tags = item.Tags;
            _namedTags = item.NamedTags;
            _abs = item.AbsoluteExpiration;
            _sld = item.SlidingExpiration;
            _p = item.Priority;
            _dep = item.Dependency;
            _syncDep = item.SyncDependency;
            _resyncProviderName = item.ResyncProviderName;
            _version = item.Version;
            _isResyncRequired = item.IsResyncExpiredItems;
            this.ExceptionMessage = exceptionMessage;
        }

        /// <summary>
        /// Get or set the signature of API call
        /// </summary>
        public string Signature
        {
            get { return _signature; }
            set { _signature = value; }
        }

        /// <summary>
        /// Get or set the key
        /// </summary>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// Get or set the number of keys
        /// </summary>
        public int NoOfKeys
        {
            get { return _noOfKeys; }
            set { _noOfKeys = value; }
        }

        public int? NoOfObjectsReturned
        {
            get { return _noOfObjectsReturned; }
            set { _noOfObjectsReturned = value; }
        }

        /// <summary>
        /// Get or set the name of group
        /// </summary>
        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        /// <summary>
        /// Get or set the name of subgroup
        /// </summary>
        public string SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }

        /// <summary>
        /// Get or set the absolute expiration date and time
        /// </summary>
        public DateTime? AbsolueExpiration
        {
            get { return _abs; }
            set { _abs = value; }
        }

        /// <summary>
        /// Get or set the sliding expiration timespan
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get { return _sld; }
            set { _sld = value; }
        }

        /// <summary>
        /// Get or set the tags
        /// </summary>
        public Tag[] Tags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        /// <summary>
        /// Get or set the named tags
        /// </summary>
        public NamedTagsDictionary NamedTags
        {
            get { return _namedTags; }
            set { _namedTags = value; }
        }

        /// <summary>
        /// Get or set the priority
        /// </summary>
        public CacheItemPriority? Priority
        {
            get { return _p; }
            set { _p = value; }
        }

        public CacheDependency Dependency
        {
            get { return _dep; }
            set { _dep = value; }
        }

        /// <summary>
        /// CacheSyncDependency for this item.
        /// </summary>
        public CacheSyncDependency SyncDependency
        {
            get { return _syncDep; }
            set { _syncDep = value; }
        }

        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }

        public string ResyncProviderName
        {
            get { return _resyncProviderName; }
            set { _resyncProviderName = value; }
        }

        public DSWriteOption? DSWriteOption
        {
            get { return _dsWriteOption; }
            set { _dsWriteOption = value; }
        }

        public DSReadOption? DSReadOption
        {
            get { return _dsReadOption; }
            set { _dsReadOption = value; }
        }


        public ContinuousQuery ContinuousQuery
        {
            get { return _cq; }
            set { _cq = value; }
        }

        public StreamMode? StreamMode
        {
            get { return _streamMode; }
            set { _streamMode = value; }
        }


        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public System.Collections.IDictionary QueryValues
        {
            get { return _queryValues; }
            set { _queryValues = value; }
        }

        public CacheItemVersion CacheItemVersion
        {
            get { return _version; }
            set { _version = value; }
        }

        public TimeSpan? LockTimeout
        {
            get { return _lockTimeout; }
            set { _lockTimeout = value; }
        }

        public bool? AcquireLock
        {
            get { return _acquireLock; }
            set { _acquireLock = value; }
        }



        public bool? IsResyncRequired
        {
            get { return _isResyncRequired; }
            set { _isResyncRequired = value; }
        }

        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set
            {
                _exceptionMessage = value;
                if (_exceptionMessage != null)
                {
                    _exceptionMessage = _exceptionMessage.Replace('\r', ' ');
                    _exceptionMessage = _exceptionMessage.Replace('\n', ' ');
                }
            }
        }
    }
}
