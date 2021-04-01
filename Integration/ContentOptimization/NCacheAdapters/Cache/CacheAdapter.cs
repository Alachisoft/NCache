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
using System.Collections.Generic;
using Alachisoft.ContentOptimization.Caching;
using System.Threading;
using System.Collections;
using System.Reflection;
using Alachisoft.NCache.ContentOptimization.Diagnostics;
using Alachisoft.ContentOptimization.Diagnostics.Logging;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Common.FeatureUsageData;

namespace Alachisoft.NCache.ContentOptimization.Caching
{
    class CacheAdapter : Alachisoft.ContentOptimization.Caching.ICache
    {
        private const string VIEWSTATE_TAG = "NC_ASP.net_viewstate_data";
        public static Version Version
        {
            get
            {
                AssemblyName name = typeof(Alachisoft.NCache.Client.ICache).Assembly.GetName();
                return name.Version;
            }
        }

        /// <summary>
        /// Class to hold byte[] because NCache throws internal exceptions if value is byte[]
        /// </summary>
        [Serializable]
        class ByteArray
        {
            public byte[] Value { get; private set; }

            public ByteArray(byte[] value)
            {
                this.Value = value;
            }
        }

        Alachisoft.NCache.Client.ICache cache;
        string cacheName;
        string sessionCacheNaame;
        String sessionApppId;

        System.Timers.Timer reloadTimer;
        const string LOCK_GROUP = "NCPStreamLocks";

        public Expiration DefaultExpiration { get; set; }
        /// <summary>
        /// Retry interval in seconds
        /// </summary>
        public int RetryInterval { get; set; }
        public bool Loaded { get; private set; }
        public bool AsyncMode { get; set; }
        /// <summary>
        /// Size of cache stream chunk in KBs
        /// </summary>
        public int StreamBlockSize { get; set; }
        /// <summary>
        /// No. of blocks to read in bulk from Cache
        /// </summary>
        public int BulkGets { get; set; }
        public bool UseRemoteDependency { get; set; }
        public FileBasedTraceProvider TraceProvider{ get; set; }

        public CacheAdapter(string cacheName)
        {
            StreamBlockSize = 64;
            BulkGets = 1;

            this.cacheName = cacheName;

            reloadTimer = new System.Timers.Timer();
            reloadTimer.AutoReset = false;
            reloadTimer.Elapsed += new System.Timers.ElapsedEventHandler(reloadTimer_Elapsed);
        }

        public bool Load()
        {
            reloadTimer.Stop();

            bool loaded = true;
            try
            {
				CacheConnectionOptions cacheConnectionOptions = new CacheConnectionOptions();
                cacheConnectionOptions.AppName= cacheConnectionOptions.AppName = FeatureUsageCollector.FeatureTag + FeatureEnum.view_state;
                cache = CacheManager.GetCache(cacheName, cacheConnectionOptions);
            }
            catch (Exception ex)
            {
                loaded = false;
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not initialize cache due to exception: {0}", ex.Message);
            }
            Loaded = loaded;
            
            if (!loaded)
                ScheduleReload();
            return loaded;
        }

        public bool Contains(string key)
        {
            if (!Loaded)
                return false;

            bool exists;
            try
            {
                exists = cache.Contains(key);
            }
            catch (Exception ex)
            {
                OnError(ex);
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not access cache due to exception: {0}", ex.Message);
                exists = false;
            }

            return exists;
        }

        public bool Insert(string key, object value)
        {
            return Insert(key, value, DefaultExpiration);
        }

        public bool Insert(string key, object value, Expiration expiration)
        {
            CacheItem cacheItem = CreateCacheItem(value, expiration);

            if (!Loaded)
                return false;
            else
            {
                cache.Insert(key, cacheItem);
                return true;
            }
        }

        public bool InsertWithReleaseLock(string key, object value, object lockHandle, Expiration expiration)
        {
            if (!Loaded)
                return false;

            if (value is byte[])
                value = new ByteArray((byte[])value);

            CacheItem item = CreateCacheItem(value, expiration);
            LockHandle handle = lockHandle as LockHandle;
            try
            {
                cache.Insert(key, item, handle, true);
                return true;
            }
            catch(Exception ex)
            {
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not add item to cache due to exception: {0}", ex.Message);
                return false;
            }
        }
		
        public IEnumerable<DictionaryEntry> GetBulk(params string[] keys)
        {
            IDictionary<string,string> items = cache.GetBulk<string>(keys);

            foreach (var entry in items)
                yield return new DictionaryEntry(entry.Key, SafeConvert(entry.Value));
        }

        public object Get(string key)
        {
            if (!Loaded)
                return null;

            object value;
            try
            {
                value = cache.Get<string>(key);
            }
            catch (Exception ex)
            {
                OnError(ex);
                value = null;
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not read item from cache due to exception: {0}", ex.Message);
            }

            value = SafeConvert(value);

            return value;
        }

        public object GetWithLock(string key, int interval, bool acquireLock, out object lochkHandle)
        {
            if (!Loaded)
            {
                lochkHandle = null;
                return null;
            }
            
            DateTime startTime = DateTime.Now;
            object value = null;
            LockHandle handle = null;

            TimeSpan lockInterval = new TimeSpan(0, 0, 0, 0, interval);
            while (startTime.AddMilliseconds(interval) >= DateTime.Now)            
            {
                handle = null; //lochkHandle as LockHandle;
                
                if(cache != null)
                    value = cache.Get<string>(key, true, lockInterval, ref handle);

                if (value != null) break;
                if (value == null && handle == null) break;

                Thread.Sleep(500);
            }
            if (value == null && handle != null)
            {
                if (cache != null)
                {
                    cache.Unlock(key);
                    value = cache.Get<string>(key,true,lockInterval, ref handle);
                }
            }

            if (value != null) lochkHandle = handle;
            else lochkHandle = null;

            return value;
        }


        public bool Remove(string key)
        {
            if (!Loaded)
                return false;

            bool removed = true;
            try
            {
                cache.Remove(key);
            }
            catch (Exception ex)
            {
                OnError(ex);
                removed = false;
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not remove item from cache due to exception: {0}", ex.Message);
            }
            return removed;
        }


        public bool Lock(string key, TimeSpan lockTimeout)
        {
            if (!Loaded)
                return false;

            bool success = true;
            LockHandle handle;
            try
            {
                success = cache.Lock(key, lockTimeout, out handle);
            }
            catch(Exception ex)
            {
                return false;
            }
            return success;
        }


        public void Unlock(string key)
        {
            if (!Loaded)
                return;
            try 
            {
                cache.Unlock(key);
            }
            catch(Exception ex)
            {}
        }

        public void ReleaseLock(string lockId)
        {
            Remove(lockId);
        }

        public Alachisoft.ContentOptimization.Caching.ICache GetSynchronized(ReaderWriterLock syncLock)
        {
           return this;
        }

        public Alachisoft.ContentOptimization.Caching.ICache GetSynchronized()
        {
            return this;
        }

        void OnAsyncItemAdded(string key, object result)
        {
            if (result is Exception)
            {
                var ex = (Exception)result;
                OnError(ex);
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Could not add item to cache due to exception: {0}", ex.Message);
            }
        }

        void reloadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Load();
        }

        static object SafeConvert(object value)
        {
            if (value is ByteArray)
                value = ((ByteArray)value).Value;
            return value;
        }

        CacheItem CreateCacheItem(object value, Expiration expiration)
        {
            CacheItem item = new CacheItem(value);
            if (expiration != null)
            {
                switch (expiration.ExpirationType)
                {
                    case ExpirationType.Absolute:
                        item.Expiration = new Alachisoft.NCache.Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Absolute, TimeSpan.FromMinutes(expiration.Duration));
                        break;

                    case ExpirationType.Sliding:
                        item.Expiration = new Alachisoft.NCache.Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, TimeSpan.FromMinutes(expiration.Duration));
                        break;
                }
            }
            return item;
        }

        void ScheduleReload()
        {
            Loaded = false;

            if (cache != null)
                cache.Dispose();

            cache = null;

            if (RetryInterval > 0)
            {
                reloadTimer.Interval = RetryInterval * 1000; // convert to miliseconds
                reloadTimer.Start();
            }
        }

        void OnError(Exception ex)
        {
            /* TODO: We've requested NCache Team to throw a DependencyKeyNotFoundException so we don't have to compare the text.
                 * This is one of the several possibilities in which cache should not be disposed
                 * This error currently won't arise in Async mode so we've only handled it here
                 * we should improve this.
                 *
                 * PS: When two parallel read requests for same BLOB comes then both start adding the stream to cache
                 * the second request removes the items added by first request and so the dependency key is lost */
            if (ErrorRequiresReload(ex))
                ScheduleReload();
        }

        bool ErrorRequiresReload(Exception ex)
        {
            bool requiresReload = true;

            if (ex is Alachisoft.NCache.Runtime.Exceptions.OperationFailedException && ex.Message == "One of the dependency keys does not exist.")
                requiresReload = false;
            else if (ex is Alachisoft.NCache.Runtime.Exceptions.AggregateException)
            {
                var aggregateEx = (Alachisoft.NCache.Runtime.Exceptions.AggregateException)ex;
                var exRequiresReload = Array.Find(aggregateEx.InnerExceptions, ErrorRequiresReload);
                requiresReload = exRequiresReload != null;
            }

            return requiresReload;
        }

        public void Dispose()
        {
            reloadTimer.Dispose();
            if (cache != null)
                cache.Dispose();
        }
    }
}
