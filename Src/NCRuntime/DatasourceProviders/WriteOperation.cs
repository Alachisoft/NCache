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

using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Runtime.DatasourceProviders
{
    public class WriteOperation
    {
        private string _key;
        private ProviderCacheItem _cacheItem;
        private WriteOperationType _opType;
        private int _retryCount;
        private int _methodOverload;

        public WriteOperation(string key, ProviderCacheItem cacheItem, WriteOperationType opType, int retryCount)
        {
            this._key = key;
            this._cacheItem = cacheItem;
            this._opType = opType;
            this._retryCount = retryCount;
        }

        /// <summary>
        /// Gets the key of cache item.
        /// </summary>
        public string Key
        {
            get { return _key; }
        }
        /// <summary>
        /// Gets/Sets the cache item.
        /// </summary>
        public ProviderCacheItem ProviderCacheItem
        {
            get { return _cacheItem; }
            set { _cacheItem = value; }
        }
        /// <summary>
        /// Gets the type of Write operation.
        /// </summary>
        public WriteOperationType OperationType
        {
            get { return _opType; }
        }
        
        /// <summary>
        /// Specify number of retries in case of data source operation failure.
        /// </summary>
        public int RetryCount
        {
            get { return _retryCount; }
        }

        public int MethodOverlaod
        {
            get { return _methodOverload; }
            set { _methodOverload = value; }
        }
    }
}