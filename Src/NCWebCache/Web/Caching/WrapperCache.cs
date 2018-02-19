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
using Alachisoft.NCache.Runtime.Events;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching.APILogging;
using Alachisoft.NCache.Common;
using System.Collections.Generic;


/// <summary>
/// The <see cref="Alachisoft.NCache.Web.Caching"/> namespace provides classes for caching frequently used data 
/// in a cluster This includes the <see cref="Cache"/> class, a dictionary that allows you to store 
/// arbitrary data objects, such as hash tables and data sets. It also provides expiration functionality 
/// for those objects, and methods that allow you to add and removed the objects. 
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
            { }
        }

        internal override string SerializationContext
        {
            get { return _webCache.SerializationContext; }
            set { _webCache.SerializationContext = value; }
        }

        internal override Cache.CacheAsyncEventsListener AsyncListener
        {
            get { return _webCache.AsyncListener; }
        }

        internal override Cache.CacheEventsListener EventListener
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
            get
            {
                return _webCache.EventManager;
            }
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
                { }
            }
        }

        public override bool ExceptionsEnabled
        {
            get { return _webCache.ExceptionsEnabled; }
            set { _webCache.ExceptionsEnabled = value; }
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
                        _apiLogger.Log(logItem); ;
                    }
                }
                catch (Exception)
                { }
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
                        _apiLogger.Log(logItem); ;
                    }
                }
                catch (Exception)
                { }
            }
            return result;
        }


        public override void Add(string key, object value)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Add(key, value);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override bool SetAttributes(string key, Runtime.Caching.CacheItemAttributes attributes)
        {
            return _webCache.SetAttributes(key, attributes);
        }

        public override void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Add(key, value, absoluteExpiration, slidingExpiration, priority);
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
                        logItem.Signature = "Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)";
                        logItem.AbsolueExpiration = absoluteExpiration;
                        logItem.SlidingExpiration = slidingExpiration;
                        logItem.Priority = priority;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void Add(string key, CacheItem item)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Add(key, item);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override IDictionary AddBulk(string[] keys, CacheItem[] items)
        {
            IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.AddBulk(keys, items);
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
                        logItem.Signature = "AddBulk(string[] keys, CacheItem[] items)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return iDict;
        }

        internal override void MakeTargetCacheActivePassive(bool makeActive)
        {
            _webCache.MakeTargetCacheActivePassive(makeActive);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
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
                        logItem.Signature = "Get(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)";
                        logItem.Key = key;
                        logItem.LockTimeout = lockTimeout;
                        logItem.AcquireLock = acquireLock;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

            return obj;
        }

        public override IDictionary GetBulk(string[] keys)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.GetBulk(keys);
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
                        logItem.Signature = "GetBulk(string[] keys)";
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
            return cItem;
        }

        public override CacheItem GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)
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
                        logItem.Signature = "GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)";
                        logItem.LockTimeout = lockTimeout;
                        logItem.AcquireLock = acquireLock;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
            return cItem;
        }



        internal override CacheItem GetCacheItemInternal(string key, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            return _webCache.GetCacheItemInternal(key, accessType, lockTimeout, ref lockHandle);
        }

        public override void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, Runtime.CacheItemPriority priority)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Insert(key, value, absoluteExpiration, slidingExpiration, priority);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

        }

        internal override object GetInternal(string key, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            return _webCache.GetInternal(key, accessType, lockTimeout, ref lockHandle);
        }

        public override void Insert(string key, object value)
        {

            string exceptionMessage = null;
            try
            {
                _webCache.Insert(key, value);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }

        }

        public override void Insert(string key, CacheItem item)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Insert(key, item);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override void Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.Insert(key, item, lockHandle, releaseLock);
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
                        logItem.Signature = "Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)";
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public override IDictionary InsertBulk(string[] keys, CacheItem[] items)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.InsertBulk(keys, items);
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
                        logItem.Signature = "InsertBulk(string[] keys, CacheItem[] items)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
            return iDict;
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
                { }
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
                { }
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
                { }
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
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
                { }
            }
        }

        internal override object Remove(string key, LockHandle lockHandle, LockAccessType accessType)
        {
            return _webCache.Remove(key, lockHandle, accessType);
        }

        internal override void Delete(string key, LockHandle lockHandle, LockAccessType accessType)
        {
            _webCache.Delete(key, lockHandle, accessType);
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
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
                { }
            }
        }


        public override IDictionary RemoveBulk(string[] keys)
        {
            System.Collections.IDictionary iDict = null;
            string exceptionMessage = null;
            try
            {
                iDict = _webCache.RemoveBulk(keys);
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
                        logItem.Signature = "RemoveBulk(string[] keys)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
            return iDict;
        }

        public override void DeleteBulk(string[] keys)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.DeleteBulk(keys);
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
                        logItem.Signature = "DeleteBulk(string[] keys)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
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
                { }
            }
            return iCol;
        }

        public override System.Collections.IDictionary SearchEntries(string query, System.Collections.IDictionary values)
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
                        logItem.RuntimeAPILogItem = (RuntimeAPILogItem)_webCache.APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId];
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
                _webCache.APILogHashTable.Remove(System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
            return iDict;
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            System.Collections.IEnumerator iEnum = null;
            string exceptionMessage = null;

            try
            {
                _webCache.GetEnumerator();
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
                { }
            }
            return iEnum;
        }

        internal override List<Common.DataStructures.EnumerationDataChunk> GetNextChunk(List<Common.DataStructures.EnumerationPointer> pointer)
        {
            return _webCache.GetNextChunk(pointer);
        }

        internal override void InitializeCompactFramework()
        {
            _webCache.InitializeCompactFramework();
        }

        internal override IDictionary AddBulkOperation(string[] keys, CacheItem[] items, ref long[] sizes, bool allowQueryTags)
        {
            return _webCache.AddBulkOperation(keys, items, ref sizes, true);
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
                { }
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
                { }
            }
            return result;
        }


        internal override IDictionary InsertBulkOperation(string[] keys, CacheItem[] items, ref long[] sizes, bool allowQueryTags)
        {
            return _webCache.InsertBulkOperation(keys, items, ref sizes, true);
        }

        internal override void InsertOperation(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, LockHandle lockHandle, LockAccessType accessType, CacheDataNotificationCallback cacheItemUdpatedCallback, CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter, EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags)
        {
            _webCache.InsertOperation(key, value, absoluteExpiration, slidingExpiration, priority, lockHandle, accessType, cacheItemUdpatedCallback, cacheItemRemovedCallaback, itemUpdateDataFilter, itemRemovedDataFilter, ref size, true);
        }

        public override void RegisterCacheNotification(string key, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RegisterCacheNotificationInternal(key, callback, eventType, EventDataFilter.None, true);
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
                        logItem.Signature = "RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, Runtime.Events.EventType eventType)";
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.Key = key;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
            }
        }

        public override void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCacheNotificationInternal(key, callback, eventType);
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
                        logItem.Signature = "UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)";
                        logItem.Key = key;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
            }
        }

        public override void RegisterCacheNotification(string[] keys, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;
            try
            {
                _webCache.RegisterCacheNotificationInternal(keys, callback, eventType, EventDataFilter.None);
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
                        logItem.Signature = "RegisterCacheNotification(string[] keys, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)";
                        logItem.ExceptionMessage = exceptionMessage;
                        logItem.NoOfKeys = keys.Length;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
            }
        }

        public override void UnRegisterCacheNotification(string[] keys, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)
        {
            string exceptionMessage = null;

            try
            {
                _webCache.UnRegisterCacheNotificationInternal(keys, callback, eventType);
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
                        logItem.Signature = "UnRegisterCacheNotification(string[] keys, CacheDataNotificationCallback callback, Runtime.Events.EventType eventType)";
                        logItem.NoOfKeys = keys.Length;
                        logItem.ExceptionMessage = exceptionMessage;
                        _apiLogger.Log(logItem);
                    }
                }
                catch (Exception)
                { }
            }
        }


        internal override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag)
        {
            return _webCache.SafeDeserialize(serializedObject, serializationContext, flag);
        }

        internal override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, ref long size)
        {
            return _webCache.SafeSerialize(serializableObject, serializationContext, ref flag, ref size);
        }

        public override string ToString()
        {
            return _webCache.ToString();
        }

    }
       
}
