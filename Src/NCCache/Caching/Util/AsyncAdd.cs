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
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Asynchronous Add operation task.
    /// </summary>
    internal class AsyncAdd : AsyncProcessor.IAsyncTask
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

        private Hashtable _queryInfo;

        private Notifications _callbackEntry;

        private OperationContext _operationContext;

        private string _typeName;

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
		public AsyncAdd(Cache cache, object key, object value,
			            ExpirationHint expiryHint, 
                        EvictionHint evictionHint,
                        string group, string subGroup, BitSet Flag, Hashtable queryInfo, string provider, Notifications notification, OperationContext operationContext, string typeName)
		{
			_cache = cache;
			_key = key;
            _provider = provider;
			_value = value;
			_expiryHint = expiryHint;
			_evictionHint = evictionHint;
			_group = group;
			_subGroup = subGroup;
            _flag = new BitSet() { Data = Flag.Data};
            _queryInfo = queryInfo;
            _callbackEntry = notification;
            _operationContext = operationContext;
            _typeName = typeName;
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
                _cache.Add(_key, _value, _expiryHint, _evictionHint, _group, _subGroup, _queryInfo, _flag, _provider, null, _operationContext, null, _callbackEntry, _typeName);
                result = AsyncOpResult.Success;
            }
            catch (Exception e)
            {
                if(NCacheLog != null) NCacheLog.Error("AsyncAdd.Process()", e.Message);
                result = e;
            }
            finally
            {
                if (_callbackEntry != null && _callbackEntry.AsyncOperationCompleteCallback != null)
                    _cache.OnAsyncOperationCompleted(AsyncOpCode.Add, new object[] { _key, _callbackEntry.AsyncOperationCompleteCallback, result });
            }
		}
	}
}
