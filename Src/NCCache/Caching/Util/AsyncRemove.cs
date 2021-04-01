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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Asynchronous remove operation task.
    /// </summary>
    internal class AsyncRemove : AsyncProcessor.IAsyncTask
    {
        /// <summary> The cache object </summary>
        private Cache _cache;
        /// <summary> Key to remove from the cache </summary>
        private object _key;

        private OperationContext _operationContext;

        ILogger NCacheLog
        {
            get { return _cache.NCacheLog; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        public AsyncRemove(Cache cache, object key,OperationContext operationContext)
        {
            _cache = cache;
            _key = key;
            _operationContext = operationContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>
        void AsyncProcessor.IAsyncTask.Process()
        {
            object result = null;

            BitSet flagMap = null;
            Notifications notification = null;
            string providerName = null;
            try
            {
                flagMap = BitSet.CreateAndMarkInUse(_cache.Context.FakeObjectPool, NCModulesConstants.AsyncCrud);
                if (_key is object[])
                {
                    object[] package = (object[])_key;
                    _key = package[0];
                    flagMap = package[1] as BitSet;
                    if (package.Length > 2) notification = package[2] as Notifications;
                    if (package.Length == 4) providerName = package[3] as string;
                }
                _operationContext.Add(OperationContextFieldName.NoGracefulBlock, true);
                _cache.Remove(_key as string, flagMap, notification, null, 0, LockAccessType.IGNORE_LOCK, providerName,_operationContext);
                result = AsyncOpResult.Success;
            }
            catch (Exception e)
            {
                if (NCacheLog != null) NCacheLog.Error("AsyncRemove.Process()", e.Message);
                result = e;
            }
            finally
            {
                flagMap?.MarkFree(NCModulesConstants.AsyncCrud); 
                if (notification != null && notification.AsyncOperationCompleteCallback != null)
                    _cache.OnAsyncOperationCompleted(AsyncOpCode.Remove, new object[] { _key, notification.AsyncOperationCompleteCallback, result });
               
            }
        }
    }
}