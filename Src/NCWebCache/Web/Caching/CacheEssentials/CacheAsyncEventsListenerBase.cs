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
using Alachisoft.NCache.Common;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace Alachisoft.NCache.Web.Caching
{
    ///// <summary>
    ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
    ///// internal class and must not be used from outside
    ///// </summary>
    internal class CacheAsyncEventsListenerBase : MarshalByRefObject, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private Cache _parent;


        private AsyncOperationCompletedCallback _asyncOperationCompleted;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        internal CacheAsyncEventsListenerBase(Cache parent)
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
        public virtual void Dispose()
        {
            try
            {
            }
            catch
            {
            }
        }

        #endregion


        public virtual void OnDataSourceUpdated(short callbackId, Hashtable result, OpCode operationType,
            bool notifyAsync)
        {
            int processid = System.Diagnostics.Process.GetCurrentProcess().Id;

            switch (operationType)
            {
                case OpCode.Add:
                    try
                    {
                        if (callbackId != -1)
                        {
                            DataSourceItemsAddedCallback cb =
                                (DataSourceItemsAddedCallback)_parent._asyncCallbackIDsMap.GetResource(callbackId);
                            if (cb != null)
                            {
                                _parent._asyncCallbackIDsMap.RemoveResource(callbackId, result.Count);
                                _parent._asyncCallbacksMap.RemoveResource(
                                    "dsiacb-" + processid + "-" + cb.Method.Name);

                                if (notifyAsync)
                                {
#if !NETCORE
                                    cb.BeginInvoke(result, null, null);
#elif NETCORE
                                        TaskFactory factory = new TaskFactory();
                                        Task task = factory.StartNew(() => cb(result));
#endif
                                }
                                else
                                    cb(result);

                                if (_parent._perfStatsCollector != null)
                                    _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                            }
                        }
                    }
                    catch
                    {
                    }

                    break;
                case OpCode.Update:
                    try
                    {
                        if (callbackId != -1)
                        {
                            DataSourceItemsUpdatedCallback cb =
                                (DataSourceItemsUpdatedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                    callbackId);
                            if (cb != null)
                            {
                                _parent._asyncCallbackIDsMap.RemoveResource(callbackId, result.Count);
                                _parent._asyncCallbacksMap.RemoveResource(
                                    "dsiucb-" + processid + "-" + cb.Method.Name);

                                if (notifyAsync)
                                {
#if !NETCORE
                                    cb.BeginInvoke(result, null, null);
#elif NETCORE
                                        TaskFactory factory = new TaskFactory();
                                        Task task = factory.StartNew(() => cb(result));
#endif
                                }
                                else
                                    cb(result);
                            }
                        }
                    }
                    catch
                    {
                    }

                    break;
                case OpCode.Remove:
                    try
                    {
                        if (callbackId != -1)
                        {
                            DataSourceItemsRemovedCallback cb =
                                (DataSourceItemsRemovedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                    callbackId);
                            if (cb != null)
                            {
                                _parent._asyncCallbackIDsMap.RemoveResource(callbackId, result.Count);
                                _parent._asyncCallbacksMap.RemoveResource(
                                    "dsiucb-" + processid + "-" + cb.Method.Name);

                                if (notifyAsync)
                                {
#if !NETCORE
                                    cb.BeginInvoke(result, null, null);
#elif NETCORE
                                        TaskFactory factory = new TaskFactory();
                                        Task task = factory.StartNew(() => cb(result));
#endif
                                }
                                else
                                    cb(result);
                            }
                        }
                    }
                    catch
                    {
                    }

                    break;
                case OpCode.Clear:
                    try
                    {
                        if (callbackId != -1)
                        {
                            DataSourceClearedCallback cb =
                                (DataSourceClearedCallback)_parent._asyncCallbackIDsMap.GetResource(callbackId);
                            if (cb != null)
                            {
                                _parent._asyncCallbackIDsMap.RemoveResource(callbackId);
                                _parent._asyncCallbacksMap.RemoveResource(
                                    "dsccb-" + processid + "-" + cb.Method.Name);

                                object param = null;
                                foreach (DictionaryEntry entry in result)
                                {
                                    param = entry.Value;
                                }

                                if (notifyAsync)
                                {
#if !NETCORE
                                    cb.BeginInvoke(param, null, null);
#elif NETCORE
                                        TaskFactory factory = new TaskFactory();
                                        Task task = factory.StartNew(() => cb(param));
#endif
                                }
                                else
                                    cb(param);
                            }
                        }
                    }
                    catch
                    {
                    }

                    break;
            }
        }

        public virtual void OnAsyncOperationCompleted(object opCode, object result, bool notifyAsync)
        {
            try
            {
                BitSet flag = new BitSet();
                object[] package = null;

                package = (object[])_parent.SafeDeserialize(result, _parent._serializationContext, flag);

                string key = (string)package[0];
                AsyncCallbackInfo cbInfo = (AsyncCallbackInfo)package[1];
                object res = package[2];

                AsyncOpCode code = (AsyncOpCode)opCode;
                int processid = System.Diagnostics.Process.GetCurrentProcess().Id;

                switch (code)
                {
                    case AsyncOpCode.Add:
                        try
                        {
                            if (cbInfo != null)
                            {
                                AsyncItemAddedCallback cb =
                                    (AsyncItemAddedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                        cbInfo.Callback);
                                if (cb != null)
                                {
                                    _parent._asyncCallbackIDsMap.RemoveResource(cbInfo.Callback);
                                    _parent._asyncCallbacksMap.RemoveResource(
                                        "aiacb-" + processid + "-" + cb.Method.Name);

                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        cb.BeginInvoke(key, res, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => cb(key, res));
#endif
                                    }
                                    else
                                    {
                                        cb(key, res);
                                    }

                                    if (_parent._perfStatsCollector != null)
                                        _parent._perfStatsCollector.IncrementEventsProcessedPerSec();
                                }
                            }
                        }
                        catch
                        {
                        }

                        break;
                    case AsyncOpCode.Update:
                        try
                        {
                            if (cbInfo != null)
                            {
                                AsyncItemUpdatedCallback cb =
                                    (AsyncItemUpdatedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                        cbInfo.Callback);
                                if (cb != null)
                                {
                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        cb.BeginInvoke(key, res, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => cb(key, res));
#endif
                                    }
                                    else
                                        cb(key, res);

                                    _parent._asyncCallbackIDsMap.RemoveResource(cbInfo.Callback);
                                    _parent._asyncCallbacksMap.RemoveResource(
                                        "aiucb-" + processid + "-" + cb.Method.Name);
                                }
                            }
                        }
                        catch
                        {
                        }

                        break;
                    case AsyncOpCode.Remove:
                        try
                        {
                            if (cbInfo != null)
                            {
                                AsyncItemRemovedCallback cb =
                                    (AsyncItemRemovedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                        cbInfo.Callback);
                                if (cb != null)
                                {
                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        cb.BeginInvoke(key, res, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => cb(key, res));
#endif
                                    }
                                    else
                                        cb(key, res);

                                    _parent._asyncCallbackIDsMap.RemoveResource(cbInfo.Callback);
                                    _parent._asyncCallbacksMap.RemoveResource(
                                        "aircb-" + processid + "-" + cb.Method.Name);
                                }
                            }
                        }
                        catch
                        {
                        }

                        break;
                    case AsyncOpCode.Clear:
                        try
                        {
                            if (cbInfo != null)
                            {
                                AsyncCacheClearedCallback cb =
                                    (AsyncCacheClearedCallback)_parent._asyncCallbackIDsMap.GetResource(
                                        cbInfo.Callback);
                                if (cb != null)
                                {
                                    if (notifyAsync)
                                    {
#if !NETCORE
                                        cb.BeginInvoke(res, null, null);
#elif NETCORE
                                            TaskFactory factory = new TaskFactory();
                                            Task task = factory.StartNew(() => cb(res));
#endif
                                    }
                                    else
                                        cb(res);

                                    _parent._asyncCallbackIDsMap.RemoveResource(cbInfo.Callback);
                                    _parent._asyncCallbacksMap.RemoveResource(
                                        "acccb-" + processid + "-" + cb.Method.Name);
                                }
                            }
                        }
                        catch
                        {
                        }

                        break;
                }
            }

            catch
            {
            }
        }
    }
}
