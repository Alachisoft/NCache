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

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Asynchronous clear operation task.
    /// </summary>
    internal class AsyncClear : AsyncProcessor.IAsyncTask
    {
        /// <summary> The cache object </summary>
        private Cache		    _cache = null;
        private Notifications   _cbEntry;
        private BitSet _flagMap;
        private OperationContext _operationContext;
        
        ILogger NCacheLog
        {
            get { return _cache.NCacheLog; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache"></param>
        public AsyncClear(Cache cache, Notifications notification, BitSet flagMap, OperationContext operationContext)
        {
            _cache = cache;
            _cbEntry = notification;
            _flagMap = flagMap;
            _operationContext = operationContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>
        /// 
        void AsyncProcessor.IAsyncTask.Process()
        {
            object result = null;
            try
            {
                _operationContext.Add(OperationContextFieldName.NoGracefulBlock, true);
                _cache.Clear(_flagMap, _cbEntry, _operationContext);
                result = AsyncOpResult.Success;
            }
            catch (Exception e)
            {
                if (NCacheLog != null)  NCacheLog.Error("AsyncClear.Process()", e.Message);
                result = e;
            }
            finally
            {
                if (_cbEntry != null && _cbEntry.AsyncOperationCompleteCallback != null)
                    _cache.OnAsyncOperationCompleted(AsyncOpCode.Clear, new object[] { null, _cbEntry.AsyncOperationCompleteCallback, result });
            }
        }

    }
}