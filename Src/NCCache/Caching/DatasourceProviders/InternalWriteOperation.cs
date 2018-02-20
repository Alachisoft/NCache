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
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    public class InternalWriteOperation : WriteOperation
    {
        public enum WriteOption
        {
            None,
            WriteThru,
            WriteBehind,
        }

        private WriteOption _writeOption;
        private string _providerName;
        private string _clientId;
        private short _clientUpdateCallbackID = -1;
        private short _clientRemoveCallbackID = -1;
        private short _clientDSCallbackID = -1;
        private EventDataFilter _clientUpdateCallbackFilter;
        private EventDataFilter _clientRemoveCallbackFilter;
        private ulong _itemVersion;

        public InternalWriteOperation(string key, ProviderCacheItem cacheItem, WriteOperationType opType, int retryCount) : base(key, cacheItem, opType, retryCount)
        {
        }

        public WriteOption OperationWriteOption
        {
            get { return _writeOption; }
            set { _writeOption = value; }
        }

        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }

        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public short ClientUpdateCallbackId
        {
            get { return _clientUpdateCallbackID; }
            set { _clientUpdateCallbackID = value; }
        }

        public EventDataFilter ClientUpdateCallbackFilter
        {
            get { return _clientUpdateCallbackFilter; }
            set { _clientUpdateCallbackFilter = value; }
        }

        public short ClientRemoveCallbackId
        {
            get { return _clientRemoveCallbackID; }
            set { _clientRemoveCallbackID = value; }
        }

        public EventDataFilter ClientRemoveCallbackFilter
        {
            get { return _clientRemoveCallbackFilter; }
            set { _clientRemoveCallbackFilter = value; }
        }

        public short ClientDsCallbackId
        {
            get { return _clientDSCallbackID; }
            set { _clientDSCallbackID = value; }
        }

        /// <summary>
        /// Version of the item inserted in L2 cache using write-thru
        /// </summary>
        public ulong L2CacheItemVersion
        {
            get { return _itemVersion; }
            set { _itemVersion = value; }
        }
    }

}
