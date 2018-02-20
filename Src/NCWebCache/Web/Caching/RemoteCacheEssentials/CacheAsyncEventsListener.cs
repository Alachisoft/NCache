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
using System.Collections;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Web.Caching
{
    ///// <summary>
    ///// Providers handlers for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    ///// internal class and must not be used from outside
    ///// </summary>
    public class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private CacheAsyncEventsListenerBase _parent;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        internal CacheAsyncEventsListener(CacheAsyncEventsListenerBase parent)
        {
            _parent = parent;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public void Dispose()
        {
        }

        #endregion

        private object PackageResult(string key, short callbackId, object result)
        {
            object[] package = new object[3];
            package[0] = key;

            AsyncCallbackInfo cbEntry = new AsyncCallbackInfo(-1, null, callbackId);
            package[1] = cbEntry;
            package[2] = result;

            return package;
        }

        public void OnAsyncAddCompleted(string key, short callbackId, object result, bool notifyAsync)
        {
            OnAsyncOperationCompleted(AsyncOpCode.Add, PackageResult(key, callbackId, result), notifyAsync);
        }

        public void OnAsyncInsertCompleted(string key, short callbackId, object result, bool notifyAsync)
        {
            OnAsyncOperationCompleted(AsyncOpCode.Update, PackageResult(key, callbackId, result), notifyAsync);
        }

        public void OnAsyncRemoveCompleted(string key, short callbackId, object result, bool notifyAsync)
        {
            OnAsyncOperationCompleted(AsyncOpCode.Remove, PackageResult(key, callbackId, result), notifyAsync);
        }

        public void OnAsyncClearCompleted(short callbackId, object result, bool notifyAsync)
        {
            OnAsyncOperationCompleted(AsyncOpCode.Clear, PackageResult(null, callbackId, result), notifyAsync);
        }

        public void OnAsyncOperationCompleted(object opCode, object result, bool notifyAsync)
        {
            _parent.OnAsyncOperationCompleted(opCode, result, notifyAsync);
        }

        public void OnDataSourceUpdated(short callbackID, Hashtable result, OpCode operationCode, bool notifyAsync)
        {
            _parent.OnDataSourceUpdated(callbackID, result, operationCode, notifyAsync);
        }
    }
}
