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
    ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    ///// internal class and must not be used from outside
    ///// </summary>
    class InprocCacheAsyncEventsListener : MarshalByRefObject, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private CacheAsyncEventsListenerBase _parent;


        /// <summary> Underlying implementation of NCache. </summary>
        private Alachisoft.NCache.Caching.Cache _nCache;

        private AsyncOperationCompletedCallback _asyncOperationCompleted;

        private DataSourceUpdatedCallback _dsUpdatedCallback;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="nCache"></param>
        internal InprocCacheAsyncEventsListener(CacheAsyncEventsListenerBase parent,
            Alachisoft.NCache.Caching.Cache nCache)
        {
            _parent = parent;
            _nCache = nCache;

            _asyncOperationCompleted = new AsyncOperationCompletedCallback(this.OnAsyncOperationCompleted);
            _nCache.AsyncOperationCompleted += _asyncOperationCompleted;

            _dsUpdatedCallback = new DataSourceUpdatedCallback(this.OnDSUpdated);
            _nCache.DataSourceUpdated += _dsUpdatedCallback;
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
            try
            {
                _nCache.AsyncOperationCompleted -= _asyncOperationCompleted;
                _nCache.DataSourceUpdated -= _dsUpdatedCallback;
            }
            catch
            {
            }
        }

        #endregion


        public void OnAsyncOperationCompleted(object opCode, object result, EventContext eventContext)
        {
            _parent.OnAsyncOperationCompleted(opCode, result, true);
        }

        public void OnDSUpdated(object result, CallbackEntry cbEntry, OpCode operationCode)
        {
            if (cbEntry != null)
            {
                AsyncCallbackInfo info = cbEntry.WriteBehindOperationCompletedCallback as AsyncCallbackInfo;
                if (info != null && (short)info.Callback != -1)
                {
                    Hashtable resTbl = result as Hashtable;
                    Hashtable newRes = null;
                    if (resTbl != null)
                    {
                        newRes = new Hashtable();
                        IDictionaryEnumerator ide = resTbl.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            object val = ide.Value;
                            if (val != null && val is string)
                            {
                                newRes[ide.Key] = (DataSourceOpResult)Convert.ToInt32(val);
                            }
                            else
                            {
                                newRes[ide.Key] = ide.Value;
                            }
                        }
                    }

                    _parent.OnDataSourceUpdated((short)info.Callback, newRes as Hashtable, operationCode, true);
                }
            }
        }
    }
}
