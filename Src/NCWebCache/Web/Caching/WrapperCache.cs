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
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching.APILogging;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Caching;
#if COMMUNITY
using Alachisoft.NCache.Caching.Topologies.Clustered.Results;
using Alachisoft.NCache.Caching.Topologies.Clustered.Operations;
#endif
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
#if COMMUNITY || CLIENT
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using System.IO;
using Alachisoft.NCache.Web.Caching.APILogging;
#endif
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Runtime.Events;

/// <summary>
/// The <see cref="Alachisoft.NCache.Web.Caching"/> namespace provides classes for caching frequently used data 
/// in a cluster This includes the <see cref="Cache"/> class, a dictionary that allows you to store 
/// arbitrary data objects, such as hash tables and data sets. It also provides expiration functionality 
/// for those objects, and methods that allow you to add and removed the objects. You can also add the 
/// objects with a dependency upon other files or cache entries, and perform a callback to notify your 
/// application when an object is removed from the <see cref="Cache"/>.
/// </summary>
namespace Alachisoft.NCache.Web.Caching
{
    //Wraps Cache class instance
    //This class is used to log any public api of Cache called by the user.
    //Note that internal/private methods does not need to be logged. Implementation of internal/private methods if any will just call same function on Cache instance.
    class WrapperCache : Cache
    {
        private Cache _webCache = null;
        private APILogger _apiLogger;

        private DebugAPIConfiguraions _debugConfigurations;

        public WrapperCache(Cache cache)
        {
            _webCache = cache;
            try
            {
                _debugConfigurations = new DebugAPIConfiguraions();
                _apiLogger = new APILogger(cache.CacheId, _debugConfigurations);
            }
            catch (Exception)
            {
            }
        }

        internal override string SerializationContext
        {
            get { return _webCache.SerializationContext; }
            set { _webCache.SerializationContext = value; }
        }


        public override event CacheStoppedCallback CacheStopped
        {
            add { _webCache.CacheStopped += value; }
            remove { _webCache.CacheStopped -= value; }
        }


        public override event CacheClearedCallback CacheCleared
        {
            add { _webCache.CacheCleared += value; }
            remove { _webCache.CacheCleared -= value; }
        }


        public override event CacheItemAddedCallback ItemAdded
        {
            add { _webCache.ItemAdded += value; }
            remove { _webCache.ItemAdded -= value; }
        }


        public override event CacheItemUpdatedCallback ItemUpdated
        {
            add { _webCache.ItemUpdated += value; }
            remove { _webCache.ItemUpdated -= value; }
        }

        public override event CacheItemRemovedCallback ItemRemoved
        {
            add { _webCache.ItemRemoved += value; }
            remove { _webCache.ItemRemoved -= value; }
        }

        public override event CustomEventCallback CustomEvent
        {
            add { _webCache.CustomEvent += value; }
            remove { _webCache.CustomEvent -= value; }
        }

        internal override CacheAsyncEventsListenerBase AsyncListener
        {
            get { return _webCache.AsyncListener; }
        }

        internal override CacheEventsListenerBase EventListener
        {
            get { return _webCache.EventListener; }
        }

        internal override Common.ResourcePool CallbackIDsMap
        {
            get { return _webCache.CallbackIDsMap; }
        }

        internal override Common.ResourcePool CallbacksMap
        {
            get { return _webCache.CallbacksMap; }
        }

        internal override string CacheId
        {
            get { return _webCache.CacheId; }
        }


        internal override CacheImplBase CacheImpl
        {
            get { return _webCache.CacheImpl; }
            set { _webCache.CacheImpl = value; }
        }

        internal override void AddRef()
        {
            _webCache.AddRef();
        }

        internal override EventManager EventManager
        {
            get { return _webCache.EventManager; }
        }


        internal override void AddSecondaryInprocInstance(Cache secondaryInstance)
        {
            _webCache.AddSecondaryInprocInstance(secondaryInstance);
        }


        public override void Dispose()
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Dispose();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Dispose()";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }

                    _apiLogger.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        public override bool ExceptionsEnabled
        {
            get { return _webCache.ExceptionsEnabled; }
            set { _webCache.ExceptionsEnabled = value; }
        }


        public override object this[string key]
        {
            get { return _webCache[key]; }
            set { _webCache[key] = value; }
        }


        public override long Count
        {
            get { return _webCache.Count; }
        }

        public override void Clear()
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Clear();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Clear()";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override void Clear(DSWriteOption updateOpt, DataSourceClearedCallback dataSourceClearedCallback)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Clear(updateOpt, dataSourceClearedCallback);
                _webCache.Clear();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Clear(DSWriteOption updateOpt, DataSourceClearedCallback dataSourceClearedCallback)";
                        logItem.DSWriteOption = updateOpt;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override void ClearAsync(AsyncCacheClearedCallback onAsyncCacheClearCallback)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.ClearAsync(onAsyncCacheClearCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "ClearAsync(DSWriteOption updateOpt, AsyncCacheClearedCallback onAsyncCacheClearCallback)";
                        logItem.DSWriteOption = DSWriteOption.None;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void ClearAsync(DSWriteOption updateOpt, AsyncCacheClearedCallback onAsyncCacheClearCallback,
            DataSourceClearedCallback dataSourceClearedCallback)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.ClearAsync(updateOpt, onAsyncCacheClearCallback, dataSourceClearedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "ClearAsync(DSWriteOption updateOpt, AsyncCacheClearedCallback onAsyncCacheClearCallback, DataSourceClearedCallback dataSourceClearedCallback)";
                        logItem.DSWriteOption = DSWriteOption.None;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override bool Contains(string key)
        {
            bool result;
            string exceptionMessage = null;
            try
            {
                result = _webCache.Contains(key);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Contains(string key)";
                        logItem.Key = key;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }


        public override void RaiseCustomEvent(object notifId, object data)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RaiseCustomEvent(notifId, data);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RaiseCustomEvent(object notifId, object data)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                        ;
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        internal override short GetCallbackId(CacheItemRemovedCallback removedCallback)
        {
            return _webCache.GetCallbackId(removedCallback);
        }

        internal override short GetCallbackId(CacheItemUpdatedCallback updateCallback)
        {
            return _webCache.GetCallbackId(updateCallback);
        }


        internal override short GetCallbackId(AsyncItemAddedCallback asyncItemAddCallback)
        {
            return _webCache.GetCallbackId(asyncItemAddCallback);
        }

        internal override short GetCallbackId(AsyncItemUpdatedCallback asyncItemUpdateCallback)
        {
            return _webCache.GetCallbackId(asyncItemUpdateCallback);
        }

        internal override short GetCallbackId(AsyncItemRemovedCallback asyncItemRemoveCallback)
        {
            return _webCache.GetCallbackId(asyncItemRemoveCallback);
        }

        internal override short GetCallbackId(AsyncCacheClearedCallback asyncCacheClearCallback)
        {
            return _webCache.GetCallbackId(asyncCacheClearCallback);
        }

        internal override short GetCallbackId(DataSourceItemsAddedCallback dsItemAddedCallback)
        {
            return _webCache.GetCallbackId(dsItemAddedCallback);
        }

        internal override short GetCallbackId(DataSourceItemsAddedCallback dsItemAddedCallback, int numberOfCallbacks)
        {
            return _webCache.GetCallbackId(dsItemAddedCallback, numberOfCallbacks);
        }

        internal override short GetCallbackId(DataSourceItemsUpdatedCallback dsItemUpdatedCallback)
        {
            return _webCache.GetCallbackId(dsItemUpdatedCallback);
        }

        internal override short GetCallbackId(DataSourceItemsUpdatedCallback dsItemUpdatedCallback,
            int numberOfCallbacks)
        {
            return _webCache.GetCallbackId(dsItemUpdatedCallback, numberOfCallbacks);
        }

        internal override short GetCallbackId(DataSourceItemsRemovedCallback dsItemRemovedCallback)
        {
            return _webCache.GetCallbackId(dsItemRemovedCallback);
        }

        internal override short GetCallbackId(DataSourceItemsRemovedCallback dsItemRemovedCallback,
            int numberOfCallbacks)
        {
            return _webCache.GetCallbackId(dsItemRemovedCallback, numberOfCallbacks);
        }

        internal override short GetCallbackId(DataSourceClearedCallback dsClearedCallback)
        {
            return _webCache.GetCallbackId(dsClearedCallback);
        }

        public override bool AddDependency(string key, Runtime.Dependencies.CacheDependency dependency,
            bool isResyncRequired)
        {
            bool result;
            string exceptionMessage = null;
            try
            {
                result = _webCache.AddDependency(key, dependency, isResyncRequired);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "AddDependency(string key, Runtime.Dependencies.CacheDependency dependency, bool isResyncRequired)";
                        logItem.Dependency = dependency;
                        logItem.IsResyncRequired = isResyncRequired;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return _webCache.AddDependency(key, dependency, isResyncRequired);
        }

        public override bool AddDependency(string key, CacheSyncDependency syncDependency)
        {
            bool result;
            string exceptionMessage = null;
            try
            {
                result = _webCache.AddDependency(key, syncDependency);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "AddDependency(string key, CacheSyncDependency syncDependency)";
                        logItem.SyncDependency = syncDependency;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }


        public override bool SetAttributes(string key, Runtime.Caching.CacheItemAttributes attributes)
        {
            return _webCache.SetAttributes(key, attributes);
        }


        public override CacheItemVersion Add(string key, object value)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, value);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Add(string key, object value)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Add(string key, object value, Runtime.Caching.Tag[] tags)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, value, tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Add(string key, object value, Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Add(string key, object value, Runtime.Caching.NamedTagsDictionary namedTags)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, value, namedTags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Add(string key, object value, Runtime.Caching.NamedTagsDictionary namedTags)";
                        logItem.NamedTags = namedTags;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Insert(string key, object value, Runtime.Caching.NamedTagsDictionary namedTags)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, value, namedTags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Insert(string key, object value, Runtime.Caching.NamedTagsDictionary namedTags)";
                        logItem.NamedTags = namedTags;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Add(string key, object value, string group, string subGroup)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, value, group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Add(string key, object value, string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Add(string key, object value, Runtime.Dependencies.CacheDependency dependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, value, dependency, absoluteExpiration, slidingExpiration, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Add(string key, object value, Runtime.Dependencies.CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.Dependency = dependency;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Add(string key, CacheItem item)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, item);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature = "Add(string key, CacheItem item)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Add(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, item, dsWriteOption, onDataSourceItemAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "Add(string key, CacheItem item, DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemAdded)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Add(string key, CacheItem item, DSWriteOption dsWriteOption,
            string providerName, DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            CacheItemVersion version;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Add(key, item, dsWriteOption, providerName, onDataSourceItemAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "Add(string key, CacheItem item, DSWriteOption dsWriteOption, string providerName, DataSourceItemsAddedCallback onDataSourceItemAdded)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override System.Collections.IDictionary AddBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded)
        {
            IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.AddBulk(keys, items, dsWriteOption, onDataSourceItemsAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "AddBulk(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override System.Collections.IDictionary AddBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, string providerName, DataSourceItemsAddedCallback onDataSourceItemsAdded)
        {
            IDictionary iDict = null;
            string exceptionMessage = null;

            try
            {
                iDict = _webCache.AddBulk(keys, items, dsWriteOption, providerName, onDataSourceItemsAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "AddBulk(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, string providerName, DataSourceItemsAddedCallback onDataSourceItemsAdded)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override void AddAsync(string key, object value, AsyncItemAddedCallback onAsyncItemAddCallback,
            string group, string subGroup)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.AddAsync(key, value, onAsyncItemAddCallback, group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "AddAsync(string key, object value, AsyncItemAddedCallback onAsyncItemAddCallback, string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.AddAsync(key, item, dsWriteOption, onDataSourceItemAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemAdded)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.AddAsync(key, item, dsWriteOption, providerName, onDataSourceItemAdded);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption, string providerName, DataSourceItemsAddedCallback onDataSourceItemAdded)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        internal override System.Collections.IDictionary AddBulkOperation(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded, string providerName,
            ref long[] sizes, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId,
            short dsItemAddedCallbackID, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            bool returnVersions, out IDictionary itemVersions,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.AddBulkOperation(keys, items, dsWriteOption, onDataSourceItemsAdded, providerName,
                    ref sizes, true, clientId, updateCallbackId, removeCallbackId, dsItemAddedCallbackID,
                    updateCallbackFilter, removeCallabackFilter, returnVersions, out itemVersions, callbackType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "AddBulkOperation(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded, string providerName, ref long[] sizes, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackID, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, CallbackType callbackType = CallbackType.PushBasedNotification)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Get("MyTextBox.Value");
        /// 
        /// </code>
        /// </example>
        public override object Get(string key)
        {
            object obj = null;
            try
            {
                obj = _webCache.Get(key);
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Get(string key)";
                        logItem.Key = key;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override object Get(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)
        {
            Object obj = null;

            try
            {
                obj = _webCache.Get(key, lockTimeout, ref lockHandle, acquireLock);
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Get(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)";
                        logItem.Key = key;
                        logItem.LockTimeout = lockTimeout;
                        logItem.AcquireLock = acquireLock;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }


        public override System.Collections.IDictionary GetBulk(string[] keys, DSReadOption dsReadOption)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.GetBulk(keys, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetBulk(string[] keys, DSReadOption dsReadOption)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override System.Collections.IDictionary GetBulk(string[] keys, string providerName,
            DSReadOption dsReadOption)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.GetBulk(keys, providerName, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetBulk(string[] keys, string providerName, DSReadOption dsReadOption)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ProviderName = providerName;
                        logItem.DSReadOption = dsReadOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }


        public override CacheItem GetCacheItem(string key)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheItem(string key)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }


        public override CacheStream GetCacheStream(string key, StreamMode streamMode)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, streamMode);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheStream(string key, StreamMode streamMode)";
                        logItem.StreamMode = streamMode;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheStream GetCacheStream(string key, StreamMode streamMode,
            Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, streamMode, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheStream(string key, StreamMode streamMode, Runtime.CacheItemPriority priority)";
                        logItem.Priority = priority;
                        logItem.StreamMode = streamMode;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheStream GetCacheStream(string key, StreamMode streamMode, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, streamMode, absoluteExpiration, slidingExpiration, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheStream(string key, StreamMode streamMode, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.StreamMode = streamMode;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheStream GetCacheStream(string key, string group, string subgroup, StreamMode streamMode,
            Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, group, subgroup, streamMode, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheStream(string key, string group, string subgroup, StreamMode streamMode, Runtime.CacheItemPriority priority)";
                        logItem.Group = group;
                        logItem.SubGroup = subgroup;
                        logItem.Priority = priority;
                        logItem.StreamMode = streamMode;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheStream GetCacheStream(string key, string group, string subgroup, StreamMode streamMode,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, group, subgroup, streamMode, absoluteExpiration,
                    slidingExpiration, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheStream(string key, string group, string subgroup, StreamMode streamMode, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.Group = group;
                        logItem.SubGroup = subgroup;
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.StreamMode = streamMode;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheStream GetCacheStream(string key, string group, string subGroup, StreamMode streamMode,
            Runtime.Dependencies.CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            CacheStream stream = null;
            try
            {
                stream = _webCache.GetCacheStream(key, group, subGroup, streamMode, dependency, absoluteExpiration,
                    slidingExpiration, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheStream(string key, string group, string subGroup, StreamMode streamMode, Runtime.Dependencies.CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.Dependency = dependency;
                        logItem.StreamMode = streamMode;

                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return stream;
        }

        public override CacheItem GetCacheItem(string key, string group, string subGroup)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;

            try
            {
                cItem = _webCache.GetCacheItem(key, group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheItem(string key, string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }


        public override CacheItem GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle,
            bool acquireLock)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, lockTimeout, ref lockHandle, acquireLock);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)";
                        logItem.LockTimeout = lockTimeout;
                        logItem.AcquireLock = acquireLock;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }

        internal override CacheItem GetCacheItemInternal(string key, string group, string subGroup,
            DSReadOption dsReadOption, ref CacheItemVersion version, LockAccessType accessType, TimeSpan lockTimeout,
            ref LockHandle lockHandle, string providerName)
        {
            return _webCache.GetCacheItemInternal(key, group, subGroup, dsReadOption, ref version, accessType,
                lockTimeout, ref lockHandle, providerName);
        }


        public override System.Collections.ArrayList GetGroupKeys(string group, string subGroup)
        {
            System.Collections.ArrayList groupKeys = null;
            string exceptionMessage = null;
            try
            {
                groupKeys = _webCache.GetGroupKeys(group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetGroupKeys(string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        if (groupKeys != null)
                            logItem.NoOfObjectsReturned = groupKeys.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return groupKeys;
        }

        public override System.Collections.IDictionary GetGroupData(string group, string subGroup)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.GetGroupData(group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetGroupData(string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }


        public override System.Collections.Hashtable GetByTag(Runtime.Caching.Tag tag)
        {
            System.Collections.Hashtable ht = null;
            string exceptionMessage = null;
            try
            {
                ht = _webCache.GetByTag(tag);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetByTag(Runtime.Caching.Tag tag)";
                        logItem.Tags = new Tag[] {tag};
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return ht;
        }

        public override System.Collections.ICollection GetKeysByTag(Runtime.Caching.Tag tag)
        {
            System.Collections.ICollection iCol = null;
            string exceptionMessage = null;
            try
            {
                iCol = _webCache.GetKeysByTag(tag);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetKeysByTag(Runtime.Caching.Tag tag)";
                        logItem.Tags = new Tag[] {tag};
                        if (iCol != null)
                            logItem.NoOfObjectsReturned = iCol.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iCol;
        }

        public override System.Collections.Hashtable GetByAllTags(Runtime.Caching.Tag[] tags)
        {
            System.Collections.Hashtable ht = null;
            string exceptionMessage = null;
            try
            {
                ht = _webCache.GetByAllTags(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetByAllTags(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return ht;
        }

        public override System.Collections.ICollection GetKeysByAllTags(Runtime.Caching.Tag[] tags)
        {
            System.Collections.ICollection iCol = null;
            string exceptionMessage = null;
            try
            {
                iCol = _webCache.GetKeysByAllTags(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetKeysByAllTags(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        if (iCol != null)
                            logItem.NoOfObjectsReturned = iCol.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iCol;
        }

        public override System.Collections.Hashtable GetByAnyTag(Runtime.Caching.Tag[] tags)
        {
            System.Collections.Hashtable ht = null;
            string exceptionMessage = null;
            try
            {
                ht = _webCache.GetByAnyTag(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetByAnyTag(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return ht;
        }

        public override System.Collections.ICollection GetKeysByAnyTag(Runtime.Caching.Tag[] tags)
        {
            System.Collections.ICollection iCol = null;
            string exceptionMessage = null;
            try
            {
                iCol = _webCache.GetKeysByAnyTag(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetKeysByAnyTag(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        if (iCol != null)
                            logItem.NoOfObjectsReturned = iCol.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iCol;
        }

        public override void RemoveByAnyTag(Runtime.Caching.Tag[] tags)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RemoveByAnyTag(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RemoveByAnyTag(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RemoveByAllTags(Runtime.Caching.Tag[] tags)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RemoveByAllTags(tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RemoveByAllTags(Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RemoveByTag(Runtime.Caching.Tag tag)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RemoveByTag(tag);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RemoveByTag(Runtime.Caching.Tag tag)";
                        logItem.Tags = new Tag[] {tag};
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object GetIfNewer(string key, ref CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.GetIfNewer(key, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetIfNewer(string key, ref CacheItemVersion version)";
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override object GetIfNewer(string key, string group, string subGroup, ref CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.GetIfNewer(key, group, subGroup, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetIfNewer(string key, string group, string subGroup, ref CacheItemVersion version)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override object Get(string key, DSReadOption dsReadOption)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Get(string key, DSReadOption dsReadOption)";
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, DSReadOption dsReadOption)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheItem(string key, DSReadOption dsReadOption)";
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }

        public override object Get(string key, string providerName, DSReadOption dsReadOption)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, providerName, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Get(string key, string providerName, DSReadOption dsReadOption)";
                        logItem.ProviderName = providerName;
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, string providerName, DSReadOption dsReadOption)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, providerName, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheItem(string key, string providerName, DSReadOption dsReadOption)";
                        logItem.ProviderName = providerName;
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }


        public override object Get(string key, ref CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Get(string key, ref CacheItemVersion version)";
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, ref CacheItemVersion version)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "GetCacheItem(string key, ref CacheItemVersion version)";
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }


        public override object Get(string key, DSReadOption dsReadOption, ref CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, dsReadOption, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Get(string key, DSReadOption dsReadOption, ref CacheItemVersion version)";
                        logItem.DSReadOption = dsReadOption;
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, DSReadOption dsReadOption, ref CacheItemVersion version)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, dsReadOption, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheItem(string key, DSReadOption dsReadOption, ref CacheItemVersion version)";
                        logItem.DSReadOption = dsReadOption;
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }

        public override object Get(string key, string providerName, DSReadOption dsReadOption,
            ref CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, providerName, dsReadOption, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Get(string key, string providerName, DSReadOption dsReadOption, ref CacheItemVersion version)";
                        logItem.ProviderName = providerName;
                        logItem.DSReadOption = dsReadOption;
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, string providerName, DSReadOption dsReadOption,
            ref CacheItemVersion version)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, providerName, dsReadOption, ref version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheItem(string key, string providerName, DSReadOption dsReadOption, ref CacheItemVersion version)";
                        logItem.ProviderName = providerName;
                        logItem.DSReadOption = dsReadOption;
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }

        public override object Get(string key, string group, string subGroup, DSReadOption dsReadOption)
        {
            object obj = null;
            string exceptionMessage = null;
            try
            {
                obj = _webCache.Get(key, group, subGroup, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Get(string key, string group, string subGroup, DSReadOption dsReadOption)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override CacheItem GetCacheItem(string key, string group, string subGroup, DSReadOption dsReadOption)
        {
            CacheItem cItem = null;
            string exceptionMessage = null;
            try
            {
                cItem = _webCache.GetCacheItem(key, group, subGroup, dsReadOption);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "GetCacheItem(string key, string group, string subGroup, DSReadOption dsReadOption)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.DSReadOption = dsReadOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return cItem;
        }


        internal override object GetInternal(string key, string group, string subGroup, DSReadOption dsReadOption,
            ref CacheItemVersion version, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle,
            string providerName)
        {
            return _webCache.GetInternal(key, group, subGroup, dsReadOption, ref version, accessType, lockTimeout,
                ref lockHandle, providerName);
        }

        public override CacheItemVersion Insert(string key, object value)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, value);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Insert(string key, object value)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Insert(string key, object value, string group, string subGroup)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, value, group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Insert(string key, object value, string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Insert(string key, object value, Runtime.Caching.Tag[] tags)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, value, tags);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Insert(string key, object value, Runtime.Caching.Tag[] tags)";
                        logItem.Tags = tags;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Insert(string key, object value,
            Runtime.Dependencies.CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            Runtime.CacheItemPriority priority)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, value, dependency, absoluteExpiration, slidingExpiration, priority);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Insert(string key, object value, Runtime.Dependencies.CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.Dependency = dependency;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Insert(string key, CacheItem item)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, item);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature = "Insert(string key, CacheItem item)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Insert(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, item, dsWriteOption, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "Insert(string key, CacheItem item, DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }

        public override CacheItemVersion Insert(string key, CacheItem item, DSWriteOption dsWriteOption,
            string providerName, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, item, dsWriteOption, providerName, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "Insert(string key, CacheItem item, DSWriteOption dsWriteOption, string providerName, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override CacheItemVersion Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)
        {
            CacheItemVersion version = null;
            string exceptionMessage = null;
            try
            {
                version = _webCache.Insert(key, item, lockHandle, releaseLock);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return version;
        }


        public override System.Collections.IDictionary InsertBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;

            try
            {
                iDict = _webCache.InsertBulk(keys, items, dsWriteOption, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "InsertBulk(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override System.Collections.IDictionary InsertBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.InsertBulk(keys, items, dsWriteOption, providerName, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "InsertBulk(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, string providerName, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }


        public override void InsertAsync(string key, object value, AsyncItemUpdatedCallback onAsyncItemUpdateCallback,
            string group, string subGroup)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.InsertAsync(key, value, onAsyncItemUpdateCallback, group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "InsertAsync(string key, object value, AsyncItemUpdatedCallback onAsyncItemUpdateCallback, string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void InsertAsync(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.InsertAsync(key, item, dsWriteOption, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "InsertAsync(string key, CacheItem item, DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void InsertAsync(string key, CacheItem item, string providerName, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.InsertAsync(key, item, providerName, dsWriteOption, onDataSourceItemUpdatedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, item, exceptionMessage);
                        logItem.Signature =
                            "InsertAsync(string key, CacheItem item, string providerName, DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }


        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        public virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RegisterKeyNotificationCallback(key, updateCallback, removeCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public virtual void UnRegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterKeyNotificationCallback(key, updateCallback, removeCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "UnRegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal override void RegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RegisterKeyNotificationCallback(keys, updateCallback, removeCallback, notifyOnItemExpiration,
                    callbackType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void UnRegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterKeyNotificationCallback(keys, updateCallback, removeCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "UnRegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback, CacheItemRemovedCallback removeCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override void Unlock(string key)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Unlock(key);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Unlock(string key)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void Unlock(string key, LockHandle lockHandle)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Unlock(key, lockHandle);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Unlock(string key, LockHandle lockHandle)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            bool result;
            string exceptionMessage = null;

            try
            {
                result = _webCache.Lock(key, lockTimeout, out lockHandle);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)";
                        logItem.LockTimeout = lockTimeout;

                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        internal override bool IsLocked(string key, ref LockHandle lockHandle)
        {
            return _webCache.IsLocked(key, ref lockHandle);
        }

        public override object Remove(string key)
        {
            object obj = null;
            string exceptionMessage = null;

            try
            {
                obj = _webCache.Remove(key);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Remove(string key)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return obj;
        }

        public override void Delete(string key)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Delete(key);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Delete(string key)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override object Remove(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            object obj = null;
            string exceptionMessage = null;

            try
            {
                obj = _webCache.Remove(key, dsWriteOption, onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Remove(string key, DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override void Delete(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Delete(key, dsWriteOption, onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Delete(string key, DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;

                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Remove(string key, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            object obj = null;
            string exceptionMessage = null;

            try
            {
                obj = _webCache.Remove(key, dsWriteOption, providerName, onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Remove(string key, DSWriteOption dsWriteOption, string providerName, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ProviderName = providerName;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override void Delete(string key, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Delete(key, dsWriteOption, providerName, onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "Delete(string key, DSWriteOption dsWriteOption, string providerName, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ProviderName = providerName;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        internal override object Remove(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback, LockHandle lockHandle,
            CacheItemVersion version, LockAccessType accessType, string providerName)
        {
            return _webCache.Remove(key, dsWriteOption, onDataSourceItemRemovedCallback, lockHandle, version,
                accessType, providerName);
        }

        internal override void Delete(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback, LockHandle lockHandle,
            CacheItemVersion version, LockAccessType accessType, string providerName)
        {
            _webCache.Delete(key, dsWriteOption, onDataSourceItemRemovedCallback, lockHandle, version, accessType,
                providerName);
        }

        public override object Remove(string key, LockHandle lockHandle)
        {
            object obj = null;
            string exceptionMessage = null;

            try
            {
                obj = _webCache.Remove(key, lockHandle);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Remove(string key, LockHandle lockHandle)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override void Delete(string key, LockHandle lockHandle)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Delete(key, lockHandle);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Delete(string key, LockHandle lockHandle)";
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Remove(string key, CacheItemVersion version)
        {
            object obj = null;
            string exceptionMessage = null;

            try
            {
                obj = _webCache.Remove(key, version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Remove(string key, CacheItemVersion version)";
                        logItem.CacheItemVersion = version;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override void Delete(string key, CacheItemVersion version)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Delete(key, version);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature = "Delete(string key, CacheItemVersion version)";
                        logItem.CacheItemVersion = version;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override System.Collections.IDictionary RemoveBulk(string[] keys, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;

            try
            {
                iDict = _webCache.RemoveBulk(keys, dsWriteOption, onDataSourceItemsRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RemoveBulk(string[] keys, DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override void DeleteBulk(string[] keys, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.DeleteBulk(keys, dsWriteOption, onDataSourceItemsRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "DeleteBulk(string[] keys, DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override System.Collections.IDictionary RemoveBulk(string[] keys, DSWriteOption dsWriteOption,
            string providerName, DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.RemoveBulk(keys, dsWriteOption, providerName, onDataSourceItemsRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RemoveBulk(string[] keys, DSWriteOption dsWriteOption, string providerName, DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ProviderName = providerName;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override void DeleteBulk(string[] keys, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.DeleteBulk(keys, dsWriteOption, providerName, onDataSourceItemsRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "DeleteBulk(string[] keys, DSWriteOption dsWriteOption, string providerName, DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ProviderName = providerName;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback,
            DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RemoveAsync(key, onAsyncItemRemoveCallback, dsWriteOption, onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback, DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;

                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback,
            DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RemoveAsync(key, onAsyncItemRemoveCallback, dsWriteOption, providerName,
                    onDataSourceItemRemovedCallback);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem(key, exceptionMessage);
                        logItem.Signature =
                            "RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback, DSWriteOption dsWriteOption, string providerName, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)";
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.ProviderName = providerName;

                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RemoveGroupData(string group, string subGroup)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RemoveGroupData(group, subGroup);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RemoveGroupData(string group, string subGroup)";
                        logItem.Group = group;
                        logItem.SubGroup = subGroup;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override System.Collections.ICollection Search(string query, System.Collections.IDictionary values)
        {
            System.Collections.ICollection iCol = null;
            string exceptionMessage = null;

            try
            {
                iCol = _webCache.Search(query, values);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Search(string query, System.Collections.IDictionary values)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        if (iCol != null)
                            logItem.NoOfObjectsReturned = iCol.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iCol;
        }

        public override System.Collections.IDictionary SearchEntries(string query,
            System.Collections.IDictionary values)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;

            try
            {
                iDict = _webCache.SearchEntries(query, values);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "SearchEntries(string query, System.Collections.IDictionary values)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        if (iDict != null)
                            logItem.NoOfObjectsReturned = iDict.Count;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        public override void Log(string module, string message)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.Log(module, message);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Log(string module, string message)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        public override System.Collections.IEnumerator GetEnumerator()
        {
            System.Collections.IEnumerator iEnum = null;
            string exceptionMessage = null;

            try
            {
                iEnum = _webCache.GetEnumerator();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetEnumerator()";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iEnum;
        }

        internal override System.Collections.IEnumerator GetEnumerator(string group, string subGroup)
        {
            return _webCache.GetEnumerator(group, subGroup);
        }

        internal override List<Common.DataStructures.EnumerationDataChunk> GetNextChunk(
            List<Common.DataStructures.EnumerationPointer> pointer)
        {
            return _webCache.GetNextChunk(pointer);
        }


        internal override string OpenStream(string key, Common.Enum.StreamModes mode, string group, string subGroup,
            DateTime absExpiration, TimeSpan slidingExpiration, Runtime.Dependencies.CacheDependency dependency,
            Runtime.CacheItemPriority priority)
        {
            return _webCache.OpenStream(key, mode, group, subGroup, absExpiration, slidingExpiration, dependency,
                priority);
        }

        internal override void CloseStream(string key, string lockHandle)
        {
            _webCache.CloseStream(key, lockHandle);
        }

        internal override int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset,
            int streamOffset, int length)
        {
            return _webCache.ReadFromStream(ref buffer, key, lockHandle, offset, streamOffset, length);
        }

        internal override void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset,
            int length)
        {
            _webCache.WriteToStream(key, lockHandle, buffer, srcOffset, dstOffset, length);
        }

        internal override long GetStreamLength(string key, string lockHandle)
        {
            return _webCache.GetStreamLength(key, lockHandle);
        }


        public override void UnRegisterCQ(ContinuousQuery query)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCQ(query);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "UnRegisterCQ(ContinuousQuery query)";
                        logItem.ContinuousQuery = query;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RegisterCQ(ContinuousQuery query)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.RegisterCQ(query);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "RegisterCQ(ContinuousQuery query)";
                        logItem.ContinuousQuery = query;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override System.Collections.ICollection SearchCQ(ContinuousQuery query)
        {
            System.Collections.ICollection iCol = null;
            string exceptionMessage = null;
            try
            {
                iCol = _webCache.SearchCQ(query);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "SearchCQ(ContinuousQuery query)";
                        logItem.ContinuousQuery = query;
                        if (iCol != null)
                            logItem.NoOfObjectsReturned = iCol.Count;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return iCol;
        }

        public override System.Collections.IDictionary SearchEntriesCQ(ContinuousQuery query)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;

            try
            {
                iDict = _webCache.SearchEntriesCQ(query);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "SearchEntriesCQ(ContinuousQuery query)";
                        logItem.ContinuousQuery = query;
                        if (iDict != null)
                            logItem.NoOfObjectsReturned = iDict.Count;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }


        protected internal override void AddAsyncOperation(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, AsyncItemAddedCallback onAsyncItemAddCallback,
            DataSourceItemsAddedCallback onDataSourceItemAdded, bool isResyncExpiredItems, string group,
            string subGroup, Tag[] tags, string providerName, string resyncProviderName, NamedTagsDictionary namedTags,
            CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback,
            Runtime.Events.EventDataFilter itemUpdateDataFilter, Runtime.Events.EventDataFilter itemRemovedDataFilter,
            string clientId, short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackId)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.AddAsyncOperation(key, value, dependency, syncDependency, absoluteExpiration,
                    slidingExpiration, priority, dsWriteOption, onRemoveCallback, onUpdateCallback,
                    onAsyncItemAddCallback, onDataSourceItemAdded, isResyncExpiredItems, group, subGroup, tags,
                    providerName, resyncProviderName, namedTags, cacheItemUdpatedCallback, cacheItemRemovedCallaback,
                    itemUpdateDataFilter, itemRemovedDataFilter, clientId, updateCallbackId, removeCallbackId,
                    dsItemAddedCallbackId);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "AddAsyncOperation(string key, object value, CacheDependency dependency, CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback, AsyncItemAddedCallback onAsyncItemAddCallback, DataSourceItemsAddedCallback onDataSourceItemAdded, bool isResyncExpiredItems, string group, string subGroup, Tag[] tags, string providerName, string resyncProviderName, NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback, CacheDataNotificationCallback cacheItemRemovedCallaback, Runtime.Events.EventDataFilter itemUpdateDataFilter, Runtime.Events.EventDataFilter itemRemovedDataFilter, string clientId, short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackId)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal override object AddOperation(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, DataSourceItemsAddedCallback onDataSourceItemAdded,
            bool isResyncExpiredItems, string group, string subGroup, Tag[] tags,
            string providerName, string resyncProviderName, NamedTagsDictionary namedTags,
            CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback,
            Runtime.Events.EventDataFilter itemUpdateDataFilter,
            Runtime.Events.EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId,
            short updateCallbackID, short removeCallbackID, short dsItemAddedCallbackID)
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.AddOperation(key, value, dependency, syncDependency, absoluteExpiration,
                    slidingExpiration, priority, dsWriteOption, onRemoveCallback, onUpdateCallback,
                    onDataSourceItemAdded, isResyncExpiredItems, group, subGroup, tags, providerName,
                    resyncProviderName, namedTags, cacheItemUdpatedCallback, cacheItemRemovedCallaback,
                    itemUpdateDataFilter, itemRemovedDataFilter, ref size, allowQueryTags, clientId, updateCallbackID,
                    removeCallbackID, dsItemAddedCallbackID);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "AddOperation(AddOperation(string key, object value, CacheDependency dependency, CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback, DataSourceItemsAddedCallback onDataSourceItemAdded, bool isResyncExpiredItems, string group, string subGroup, Tag[] tags, .....)";
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            string exceptionMessage = null;

            try
            {
                result = _webCache.Equals(obj);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Equals(object obj)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }


        public override int ExecuteNonQuery(string query, IDictionary values)
        {
            int result = 0;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteNonQuery(query, values);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteNonQuery(string query, IDictionary values)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        logItem.NoOfKeys = result;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReader(string query, IDictionary values)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReader(query, values);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteReader(string query, IDictionary values)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReader(string query, IDictionary values, bool getData)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReader(query, values, getData);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteReader(string query, IDictionary values, bool getData)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReader(query, values, getData, chunkSize);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)";
                        logItem.Query = query;
                        logItem.QueryValues = values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReaderCQ(ContinuousQuery query)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReaderCQ(query);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteReaderCQ(ContinuousQuery query)";
                        logItem.Query = query.Query;
                        logItem.QueryValues = query.Values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReaderCQ(ContinuousQuery cquery, bool getData)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReaderCQ(cquery, getData);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteReaderCQ(ContinuousQuery query, bool getData)";
                        logItem.Query = cquery.Query;
                        logItem.QueryValues = cquery.Values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override ICacheReader ExecuteReaderCQ(ContinuousQuery query, bool getData, int chunkSize)
        {
            ICacheReader result = null;
            string exceptionMessage = null;

            try
            {
                result = _webCache.ExecuteReaderCQ(query, getData, chunkSize);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteReaderCQ(ContinuousQuery query, bool getData, int chunkSize)";
                        logItem.Query = query.Query;
                        logItem.QueryValues = query.Values;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public override int GetHashCode()
        {
            int result;
            string exceptionMessage = null;

            try
            {
                result = _webCache.GetHashCode();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetHashCode()";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        internal override object GetSerializedObject(string key, DSReadOption dsReadOption, ref ulong v,
            ref BitSet flag, ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration, ref string group,
            ref string subGroup, ref Hashtable queryInfo)
        {
            return _webCache.GetSerializedObject(key, dsReadOption, ref v, ref flag, ref absoluteExpiration,
                ref slidingExpiration, ref group, ref subGroup, ref queryInfo);
        }


        protected internal override void InsertAsyncOperation(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, AsyncItemUpdatedCallback onAsyncItemUpdateCallback,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback, bool isResyncExpiredItems, string group,
            string subGroup, Tag[] tags, string providerName, NamedTagsDictionary namedTags,
            CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback,
            Runtime.Events.EventDataFilter itemUpdateDataFilter, Runtime.Events.EventDataFilter itemRemovedDataFilter,
            string clientId, short updateCallbackID, short removeCallbackId, short dsItemAddedCallbackID)
        {
            _webCache.InsertAsyncOperation(key, value, dependency, syncDependency, absoluteExpiration,
                slidingExpiration, priority, dsWriteOption, onRemoveCallback, onUpdateCallback,
                onAsyncItemUpdateCallback, onDataSourceItemUpdatedCallback, isResyncExpiredItems, group, subGroup, tags,
                providerName, namedTags, cacheItemUdpatedCallback, cacheItemRemovedCallaback, itemUpdateDataFilter,
                itemRemovedDataFilter, clientId, updateCallbackID, removeCallbackId, dsItemAddedCallbackID);
        }

        internal override IDictionary InsertBulkOperation(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback, string providerName, ref long[] sizes,
            bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId,
            short dsItemUpdatedCallbackID, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            bool returnVersions, out IDictionary itemVersions,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.InsertBulkOperation(keys, items, dsWriteOption, onDataSourceItemUpdatedCallback,
                    providerName, ref sizes, true, clientId, updateCallbackId, removeCallbackId,
                    dsItemUpdatedCallbackID, updateCallbackFilter, removeCallabackFilter, returnVersions,
                    out itemVersions, callbackType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "InsertBulkOperation(string[] keys, CacheItem[] items, DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback, string providerName, ref long[] sizes, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId, short dsItemUpdatedCallbackID, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, CallbackType callbackType = CallbackType.PushBasedNotification)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }

                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }


        internal override CacheItemVersion InsertOperation(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback,
            bool isResyncExpiredItems, string group, string subGroup, LockHandle lockHandle, CacheItemVersion version,
            LockAccessType accessType, Tag[] tags, string providerName, string resyncProviderName,
            NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback,
            Runtime.Events.EventDataFilter itemUpdateDataFilter, Runtime.Events.EventDataFilter itemRemovedDataFilter,
            ref long size, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId,
            short dsItemUpdateCallbackId, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.InsertOperation(key, value, dependency, syncDependency, absoluteExpiration,
                    slidingExpiration, priority, dsWriteOption, onRemoveCallback, onUpdateCallback,
                    onDataSourceItemUpdatedCallback, isResyncExpiredItems, group, subGroup, lockHandle, version,
                    accessType, tags, providerName, resyncProviderName, namedTags, cacheItemUdpatedCallback,
                    cacheItemRemovedCallaback, itemUpdateDataFilter, itemRemovedDataFilter, ref size, true, clientId,
                    updateCallbackId, removeCallbackId, dsItemUpdateCallbackId, callbackType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "InsertOperation(string key, object value, CacheDependency dependency, CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback, bool isResyncExpiredItems, string group, string subGroup, LockHandle lockHandle, CacheItemVersion version, LockAccessType accessType, Tag[] tags, string providerName, string resyncProviderName, NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback, CacheDataNotificationCallback cacheItemRemovedCallaback, Runtime.Events.EventDataFilter itemUpdateDataFilter, Runtime.Events.EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId, short updateCallbackId, short removeCallbackId, short dsItemUpdateCallbackId, CallbackType callbackType = CallbackType.PushBasedNotification)";
                        logItem.ProviderName = providerName;
                        logItem.DSWriteOption = dsWriteOption;
                        logItem.RuntimeAPILogItem =
                            (RuntimeAPILogItem) _webCache.APILogHashTable[
                                System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal override void RegisterCacheDataNotificationCallback(string[] key,
            CacheDataNotificationCallback callback, Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PullBasedCallback)
        {
            _webCache.RegisterCacheDataNotificationCallback(key, callback, eventType, datafilter,
                notifyOnItemExpiration, callbackType);
        }


        public override CacheEventDescriptor RegisterCacheNotification(
            CacheDataNotificationCallback cacheDataNotificationCallback, Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter)
        {
            CacheEventDescriptor result = null;
            string exceptionMessage = null;
            try
            {
                result = _webCache.RegisterCacheNotification(cacheDataNotificationCallback, eventType, datafilter);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType, Runtime.Events.EventDataFilter datafilter)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }


        public override void RegisterCacheNotification(string key,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RegisterCacheNotification(key, selectiveCacheDataNotificationCallback, eventType, datafilter);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType, Runtime.Events.EventDataFilter datafilter)";
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.Key = key;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void RegisterCacheNotification(string[] keys,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RegisterCacheNotification(keys, selectiveCacheDataNotificationCallback, eventType,
                    datafilter);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType, Runtime.Events.EventDataFilter datafilter)";
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.NoOfKeys = keys.Length;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal override void RegisterCacheNotificationDataFilter(Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter, short eventSequenceId)
        {
            _webCache.RegisterCacheNotificationDataFilter(eventType, datafilter, eventSequenceId);
        }

        internal override CacheEventDescriptor RegisterCacheNotificationInternal(string key,
            CacheDataNotificationCallback callback, Runtime.Events.EventType eventType,
            Runtime.Events.EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            return _webCache.RegisterCacheNotificationInternal(key, callback, eventType, datafilter,
                notifyOnItemExpiration, callbackType);
        }

        internal override void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration)
        {
            _webCache.RegisterKeyNotificationCallback(key, updateCallback, removeCallback, notifyOnItemExpiration);
        }

        internal override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag)
        {
            return _webCache.SafeDeserialize(serializedObject, serializationContext, flag);
        }

        internal override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            ref long size)
        {
            return _webCache.SafeSerialize(serializableObject, serializationContext, ref flag, ref size);
        }

        public override string ToString()
        {
            return _webCache.ToString();
        }

        public virtual void UnRegisterCacheNotification(CacheEventDescriptor discriptor)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCacheNotification(discriptor);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "UnRegisterCacheNotification(CacheEventDescriptor discriptor)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback,
            Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCacheNotification(key, callback, eventType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)";
                        logItem.Key = key;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override void UnRegisterCacheNotification(string[] key, CacheDataNotificationCallback callback,
            Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCacheNotification(key, callback, eventType);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "UnRegisterCacheNotification(string[] key, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)";
                        logItem.NoOfKeys = key.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal override void SetQueryTypeInfoMap(TypeInfoMap typeMap)
        {
            _webCache.SetQueryTypeInfoMap(typeMap);
        }

        #region MapReduce Methods

        public override ITrackableTask ExecuteTask(MapReduceTask task)
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.ExecuteTask(task);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteTask(MapReduceTask task)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override ITrackableTask ExecuteTask(MapReduceTask task, IKeyFilter keyFilter)
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.ExecuteTask(task, keyFilter);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteTask(MapReduceTask task, IKeyFilter keyFilter)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override ITrackableTask ExecuteTask(MapReduceTask task, string query, Hashtable parameters)
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.ExecuteTask(task, query, parameters);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "ExecuteTask(MapReduceTask task, string query, Hashtable parameters)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override ArrayList GetRunningTasks()
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.GetRunningTasks();
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetRunningTasks()";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override ITrackableTask GetTaskResult(string taskId)
        {
            string exceptionMessage = null;

            try
            {
                return _webCache.GetTaskResult(taskId);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "GetTaskResult(string taskId)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion

        #region Aggregator Methods

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Aggregate(IValueExtractor extractor, IAggregator aggregator)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator, IKeyFilter keyFilter)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator, keyFilter);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Aggregate(IValueExtractor extractor, IAggregator aggregator, IKeyFilter keyFilter)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator, IKeyFilter keyFilter, int timeout)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator, keyFilter, timeout);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Aggregate(IValueExtractor extractor, IAggregator aggregator, IKeyFilter keyFilter, int timeout)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator, int timeout)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator, timeout);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature = "Aggregate(IValueExtractor extractor, IAggregator aggregator, int timeout)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator, string query, Hashtable parameters)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator, query, parameters);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Aggregate(IValueExtractor extractor, IAggregator aggregator, string query, Hashtable parameters)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public override object Aggregate(Runtime.Aggregation.IValueExtractor extractor,
            Runtime.Aggregation.IAggregator aggregator, string query, Hashtable parameters, int timeout)
        {
            string exceptionMessage = null;
            try
            {
                return _webCache.Aggregate(extractor, aggregator, query, parameters, timeout);
            }
            catch (Exception e)
            {
                exceptionMessage = e.Message;
                throw;
            }
            finally
            {
                try
                {
                    if (_debugConfigurations.IsInLoggingInterval())
                    {
                        APILogItem logItem = new APILogItem();
                        logItem.Signature =
                            "Aggregate(IValueExtractor extractor, IAggregator aggregator, string query, Hashtable parameters, int timeout)";
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion

        public override event CacheClientConnectivityChangedCallback CacheClientConnectivityChanged
        {
            add { _webCache.CacheClientConnectivityChanged += value; }
            remove { _webCache.CacheClientConnectivityChanged -= value; }
        }

        public override IList<ClientInfo> GetConnectedClientList()
        {
            return _webCache.GetConnectedClientList();
        }

        public override ClientInfo ClientInfo
        {
            get { return _webCache.ClientInfo; }
        }
    }
}