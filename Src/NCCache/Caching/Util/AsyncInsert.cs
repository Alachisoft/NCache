//  Copyright (c) 2021 Alachisoft
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
using System;
using System.Collections;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Asynchronous update operation task.
    /// </summary>
    internal class AsyncInsert : AsyncProcessor.IAsyncTask
    {
        /// <summary> The cache object </summary>
        private Cache					_cache;
        /// <summary> Key and value to add to the cache </summary>
        private object					_key, _value;
        /// <summary> Expiration hint </summary>
        private ExpirationHint			_expiryHint;
        /// <summary> Eviction hint </summary>
        private EvictionHint			_evictionHint;
        /// <summary>group for data</summary>
        private string					_group;
        /// <summary>sub group of the group</summary>
        private string					_subGroup;
        /// <summary>provider name</summary>
        private string _provider;

        private BitSet                  _flag;
        private Notifications _cbEntry;

        private Hashtable _queryInfo;

        private OperationContext _operationContext;
        private string _type;

        ILogger NCacheLog
        {
            get { return _cache.NCacheLog; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiryHint"></param>
        /// <param name="evictionHint"></param>
        public AsyncInsert(Cache cache, object key, object value, 
            ExpirationHint expiryHint,
            EvictionHint evictionHint,
            string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider, OperationContext operationContext, Notifications notification,  string typeName)
        {
            _cache = cache;
            _key = key;
            _value = value;
            _expiryHint = expiryHint;
            _evictionHint = evictionHint;
            _group = group;
            _subGroup = subGroup;
            _flag = new BitSet() { Data = Flag.Data };
            _queryInfo = queryInfo;
            _provider = provider;
            _cbEntry = notification;
            _operationContext = operationContext;
            _type = typeName;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>
        void AsyncProcessor.IAsyncTask.Process()
        {
            object result = null;
            try
            {
                _operationContext.Add(OperationContextFieldName.NoGracefulBlock, true);
                _cache.Insert(_key, _value, _expiryHint, _evictionHint, _group, _subGroup, _queryInfo, _flag, _operationContext, null, _cbEntry, _type);
                result = NCache.Caching.AsyncOpResult.Success;

            }
            catch (Exception e)
            {
                if (NCacheLog != null)  NCacheLog.Error("AsyncInsert.Process()", e.Message);
                result = e;
            }
            finally
            {
                if (_cbEntry != null && _cbEntry.AsyncOperationCompleteCallback != null)
                    _cache.OnAsyncOperationCompleted(AsyncOpCode.Update, new object[] { _key, _cbEntry.AsyncOperationCompleteCallback, result });
            }
        }
    }
}