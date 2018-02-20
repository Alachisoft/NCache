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

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.CacheSynchronization
{
    public class CacheSyncDependency: ICompactSerializable
    {
        private string _key;
        private string _cacheId;
        private ISyncCache _cache;

        public CacheSyncDependency(string cacheId, string key, ISyncCache cache)

        {
            _key = key;
            _cacheId = cacheId;
            _cache = cache;

        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string CacheId
        {
            get { return _cacheId != null ? _cacheId.ToLower() : null ; }
            set { _cacheId = value; }
        }

        public ISyncCache SyncCache
        {
            get { return _cache; }
            set { _cache = value; }
        }

    
        internal DependencyStatus GetStatus(CacheSyncManager synchronizer)
        {
            DependencyStatus status = DependencyStatus.Expired;
            if (synchronizer != null)
            {
                status = synchronizer.GetDependencyStatus(new CacheSyncManager.SyncItem(_cacheId, Key));
            }
            return status;
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _cacheId = reader.ReadObject() as string;
            _key = reader.ReadObject() as string;
            _cache = reader.ReadObject() as ISyncCache;
           
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_cacheId);
            writer.WriteObject(_key);
            writer.WriteObject(_cache);
           
        }

        #endregion
    }
}