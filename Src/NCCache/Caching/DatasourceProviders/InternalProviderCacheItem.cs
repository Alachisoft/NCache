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
using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Runtime.Caching;
using System.Collections;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    public class InternalProviderCacheItem : ProviderCacheItem
    {
        private ISyncCache _syncCache;
        private string _username;
        private string _pwd;

        private DateTime _lastModifiedTime;

        private DateTime _creationTime;

        private object _lockId;
        private LockAccessType _accessType;
        private ulong _version;

        public InternalProviderCacheItem(object value) : base(value)
        {
        }

        public ISyncCache SyncCache
        {
            get { return _syncCache; }
            set { _syncCache = value; }
        }

        public string SyncCacheUserName
        {
            get { return _username; }
            set { _username = value; }
        }
        public string SyncCachePassword
        {
            get { return _pwd; }
            set { _pwd = value; }
        }
        public DateTime LastModifiedTime
        {
            get { return _lastModifiedTime; }
            set { _lastModifiedTime = value; }
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }

        public LockAccessType AccessType
        {
            get { return _accessType; }
            set { _accessType = value; }
        }

        public ulong Version
        {
            get { return _version; }
            set { _version = value; }
        }
        public object LockId
        {
            get { return _lockId; }
            set { _lockId = value; }
        }

        public string ClientID { get; set; }

        public Hashtable QueryInfo { get; set; }

        public Common.BitSet Flags { get; set; }

        public int InternalSize { get; set; }

        public Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency SyncDependency { get; set; }
    }
}
