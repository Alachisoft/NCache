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
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Runtime.Processor;

namespace Alachisoft.NCache.Web.Caching
{
    internal sealed class InprocCache : CacheImplBase, IEnumerable, IRecordSetLoader
    {
#if DEBUG
        /// <summary> Underlying implementation of NCache. </summary>
        public Alachisoft.NCache.Caching.Cache _nCache;
#else
        /// <summary> Underlying implementation of NCache. </summary>
		internal Alachisoft.NCache.Caching.Cache	_nCache;
#endif

        internal CacheConfig _config;

        /// <summary> Reference count of the cache. </summary>
        internal int _refCount;

        /// <summary> </summary>
        private CompactCacheEntry _entry;

        /// <summary>Serialization context (actually name of the cache.)used for Compact Framework </summary>
        private string _serializationContext;

        /// <summary> Cache event listener object. Implements all events.</summary>
        private InprocCacheEventsListener _listener;

        ///// <summary> Cache event listener object. Implements all events.</summary>
        private InprocCacheAsyncEventsListener _asyncListener;

        /// <summary> Contains callback ids and associated callbacks</summary>
        private ResourcePool _callbackIDsMap;


        /// <summary> Initial values for CustomRemovedCallback and CustomUpdateCallback ids. </summary>
        private short _rcbInitialVal = 0;

        private short _ucbInitialVal = 1000;
        private short _aiacbInitialVal = 2000;
        private short _aiucbInitialVal = 3000;
        private short _aircbInitialVal = 4000;
        private short _acccbInitialVal = 5000;


        private Cache _parent;

        #region	/                 --- Fields & Events ---           /

        /// <summary>
        /// Used in the <paramref name="absoluteExpiration"/> parameter in an Insert method call to 
        /// indicate the item should never expire. This field is read-only.
        /// </summary>
        /// <remarks>When used, this field sets the <paramref name="absoluteExpiration"/> parameter equal 
        /// to <see cref="DateTime.MaxValue"/>, which is a constant representing the largest possible 
        /// <see cref="DateTime"/> value, 12/31/9999 11:59:59 PM.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="NoAbsoluteExpiration"/> field to disable 
        /// absolute expirations when inserting an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.Insert("DSN", connectionString, null, Cache.NoAbsoluteExpiration, TimeSpan.FromSeconds(10));
        /// 
        /// </code>
        /// </example>
        public static readonly DateTime NoAbsoluteExpiration = DateTime.MaxValue.ToUniversalTime();

        /// <summary>
        /// Used as the <paramref name="slidingExpiration"/> parameter in an Insert method call 
        /// to disable sliding expirations. This field is read-only.
        /// </summary>
        /// <remarks>When used, this field sets the <paramref name="slidingExpiration"/> parameter equal to the <see cref="TimeSpan.Zero"/>
        /// field, which has a constant value of zero. The cached item then expires 
        /// in accordance with the <paramref name="absoluteExpiration"/> parameter.</remarks>
        /// <example>The following example demonstrates how to use the Insert method to add an item to the 
        /// <see cref="Cache"/> object using the <see cref="NoSlidingExpiration"/> field.
        /// <code>
        /// 
        ///	NCache.Cache.Insert("DSN", connectionString, null, DateTime.Now.AddMinutes(2), Cache.NoSlidingExpiration);
        /// 
        /// </code>
        /// </example>
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

        #endregion


        private TypeInfoMap _typeMap;

        protected override internal TypeInfoMap TypeMap
        {
            get { return _typeMap; }
            set { _typeMap = value; }
        }


        protected internal override bool SerializationEnabled
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="config"></param>
        internal InprocCache(Alachisoft.NCache.Caching.Cache objectCache, CacheConfig config, Cache parent)
            : base()
        {
            _nCache = objectCache;

            this.TypeMap = _nCache.GetTypeInfoMap();
            _config = config;
            _parent = parent;

            if (_nCache != null)
            {
                _listener = new InprocCacheEventsListener(_parent.EventListener, _nCache);

                _asyncListener = new InprocCacheAsyncEventsListener(_parent.AsyncListener, _nCache);

                _nCache.OnClientConnected(ClientID, _config.CacheId, LocalClientInfo);
                _serializationContext = _nCache.Name; //Sets the serialization context.
            }

            AddRef();
        }

        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~InprocCache()
        {
            Dispose(false);
        }

        internal void AddRef()
        {
            lock (this)
            {
                _refCount++;
            }
        }


        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>The behavior of this method depends upon the cache's sharing mode (inproc/outproc) 
        /// specified in the configuration.
        /// <para>If the application uses the cache in <b>inproc</b> mode, calling Dispose closes the cache 
        /// and releases the resources used by it. If in a cluster the calling node leaves the 
        /// cluster as well. 
        /// </para>
        /// <para>If the application uses the cache in <b>outproc</b> mode, calling Dispose releases the 
        /// reference to the cache object. The cache itself remains operational and cluster remains intact. 
        /// </para>
        /// </remarks>
        private void DisposeInternal(bool disposing)
        {
            lock (this)
            {
                _refCount--;
                if (_refCount > 0) return;
                else if (_refCount < 0)
                    _refCount = 0;

                // remove from the cache table.
                lock (NCache.Caches)
                {
                    if (_config != null)
                        NCache.Caches.Remove(_config.CacheId);
                }
                if (_listener != null) _listener.Dispose();

                if (_config != null && _config.UseInProc)
                {
                    if (_nCache != null) _nCache.Dispose();
                }

                _nCache = null;
                if (disposing) GC.SuppressFinalize(this);
            }
        }


        /// <summary>
        /// Decerements the reference count of the cache and performs application-defined tasks 
        /// associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// The behavior of this method depends upon the cache's sharing mode (inproc/outproc) 
        /// specified in the configuration.
        /// <para>If the application uses the cache in <b>inproc</b> mode, calling Dispose closes the cache 
        /// and releases the resources used by it. If in a cluster the calling node leaves the 
        /// cluster as well. 
        /// </para>
        /// <para>If the application uses the cache in <b>outproc</b> mode, calling Dispose releases the 
        /// reference to the cache object. The cache itself remains operational and cluster remains intact. 
        /// </para>
        /// </remarks>
        public override void Dispose(bool disposing)
        {
            DisposeInternal(true);
        }

        #endregion

        /// <summary>
        /// Acquire a lock on an item in cache.
        /// </summary>
        /// <param name="key">key of cached item to be locked.</param>
        /// <param name="lockTimeout">TimeSpan after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <see cref="Lockhandle"/> that will be filled in with the lock information if lock is acquired successfully.</param>
        /// <returns>Whether or not lock was acquired successfully.</returns>
        /// <example>
        /// Following example demonstrates how to lock a cached item.
        /// <code>
        /// ...
        /// LockHandle lockHandle = new LockHandle();
        /// bool locked = theCache.lock("cachedItemKey", new TimeSpan(0,0,10), out lockHandle);
        /// ...
        /// </code>
        /// </example>
        public override bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            object lockId = null;
            DateTime lockDate = DateTime.Now;
            lockHandle = null;
            if (_nCache != null)
            {
                bool result = _nCache.Lock(key, lockTimeout, out lockId, out lockDate,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
                lockHandle = new LockHandle(lockId == null ? null : lockId as string, lockDate);
                return result;
            }

            return false;
        }

        internal override bool IsLocked(string key, ref LockHandle lockHandle)
        {
            if (_nCache != null)
            {
                object lockId = null;
                DateTime lockDate = new DateTime();
                if (lockHandle == null) lockHandle = new LockHandle();
                bool result = _nCache.IsLocked(key, ref lockId, ref lockDate,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
                lockHandle.LockId = lockId == null ? "" : lockId as string;
                lockHandle.LockDate = lockDate;
                return result;
            }

            return false;
        }

        public override Hashtable GetEncryptionInfo()
        {
            return null;
        }

        /// <summary>
        /// Forcefully unlocks a locked cached item.
        /// </summary>
        /// <param name="key">key of a cached item to be unlocked</param>
        /// <example>
        /// Following example demonstrates how to unlock a cached item.
        /// <code>
        /// ...
        /// theCache.Unlock("cachedItemKey");
        /// ...
        /// </code>
        /// </example>
        public override void Unlock(string key)
        {
            if (_nCache != null)
                _nCache.Unlock(key, null, true,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Unlocks a locked cached item if the correct lock-id is specified.
        /// </summary>
        /// <param name="key">key of a cached item to be unlocked</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> that was generated when lock was acquired.</param>
        /// <example>
        /// Following example demonstrates how to unlock a cached item.
        /// <code>
        /// ...
        /// theCache.Unlock("cachedItemKey", lockHandle);
        /// ...
        /// </code>
        /// </example>
        public override void Unlock(string key, object lockId)
        {
            if (_nCache != null)
                _nCache.Unlock(key, lockId, false,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        #region	/                 --- Indexer ---           /

        /// <summary>
        /// Gets or sets the cache item at the specified key.
        /// </summary>
        /// <param name="key">A <c>string</c> object that represents the key for the cache item.</param>
        /// <value>The specified cache item.</value>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>You can use this property to retrieve the value of a specified 
        /// cache item, or to add an item and a key for it to the cache.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following examples demonstrates using this property to retrieve and insert the values of cached item.
        /// <code>
        /// 
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		txtValue.Text = NCache.Cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			NCache.Cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        /// 
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		txtValue.Text = Cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			Cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        /// 
        /// </code>
        /// </example>
        public override object this[string key]
        {
            get
            {
                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                return Get(key, new BitSet(), null, null, ref version, ref lockHandle, TimeSpan.Zero,
                    LockAccessType.IGNORE_LOCK, null);
            }
            set
            {
            }
        }

        #endregion

        #region	/                 --- Count ---           /

        /// <summary>
        /// Gets the number of items stored in the cache.
        /// </summary>
        /// <remarks>This property can be useful when monitoring your application's performance 
        /// or when using ASP.NET tracing functionality.
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <value>The number of items stored in the cache.</value>
        public override long Count
        {
            get
            {
                if (_nCache != null) return _nCache.Count;
                return 0;
            }
        }

        /// <summary>
        /// Gets the name of the cache.
        /// </summary>
        public override string Name
        {
            get
            {
                if (_nCache != null) return _nCache.Name;
                return null;
            }
        }

        #endregion

        #region	/                 --- Clear ---           /

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.Clear();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Clear();
        /// 
        /// </code>
        /// </example>
        public override void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            if (_nCache == null) return;

            CallbackEntry cbEntry = null;
            if (onDsClearedCallback != -1)
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsClearedCallback, flagMap,
                    EventDataFilter.None, EventDataFilter.DataWithMetadata);
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);

            _nCache.Clear(flagMap, cbEntry, operationContext);
        }


        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.ClearAsync();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync();
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            if (_nCache == null) return;

            CallbackEntry cbEntry = null;
            if (onDsClearedCallback != -1)
            {
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsClearedCallback, flagMap,
                    EventDataFilter.None, EventDataFilter.None);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
            _nCache.ClearAsync(flagMap, cbEntry, operationContext);
        }

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <param name="onAsyncCacheClearedCallback">A delegate that, that can be used to get the result of 
        /// the Asynchronous Clear operation.</param>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        /// void OnAsyncCacheCleared(object result)
        /// {
        /// ...
        /// }
        /// 
        ///	NCache.Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback,
            string providerName)
        {
            if (_nCache == null) return;

            CallbackEntry cbEntry = null;
            if (onAsyncCacheClearCallback != -1 || onDsClearedCallback != -1)
            {
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, onAsyncCacheClearCallback, onDsClearedCallback,
                    flagMap, EventDataFilter.None, EventDataFilter.None);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
            _nCache.ClearAsync(flagMap, cbEntry, operationContext);
        }

        #endregion

        #region	/                 --- Contains ---           /

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="Cache"/>.</param>
        /// <returns><b>true</b> if the <see cref="Cache"/> contains an element 
        /// with the specified key; otherwise, <b>false</b>.</returns>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	if(NCache.Cache.Contains("MyTextBox.Value"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	if(Cache.Contains("MyTextBox.Value"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// 
        /// </code>
        /// </example>
        public override bool Contains(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            return _nCache.Contains(key,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
            return false;
        }

        #endregion


        /// <summary>
        /// Broadcasts a custom application defined event.
        /// </summary>
        /// <param name="notifId">Application specific notification code/id</param>
        /// <param name="data">Application specific data</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <para><b>Note:</b> Custom event notifications can not be disabled through configuration.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to raise a custom application defined event.
        /// <code>
        /// 
        ///	NCache.Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now));
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now)); 
        /// 
        /// </code>
        /// </example>
        public override void RaiseCustomEvent(object notifId, object data)
        {
            _nCache.SendNotification(notifId, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void ValidateKeyValue(string key, object value)
        {
            Type type = typeof(ICompactSerializable);
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (!key.GetType().IsSerializable && !type.IsAssignableFrom(key.GetType()))
                throw new ArgumentException("key is not serializable");
            if (!value.GetType().IsSerializable && !type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException("value is not serializable");
        }


        /// <summary>
        /// Create a CompactEntry object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dependency"></param>
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        /// <param name="priority"></param>
        /// <param name="onRemoveCallback"></param>
        /// <param name="isResyncExpiredItems"></param>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        private object MakeCompactEntry(string key, object value,
            Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback,
            short onUpdateCallback, short onAsyncOperationCompleteCallback, short DsCallback, bool isResyncExpiredItems,
            string group, string subGroup, Hashtable queryInfo, BitSet Flag, object lockId, ulong version,
            LockAccessType accessType, string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, string clientId)
        {
            //we create a user binary object.
            if (this.SerializationEnabled)
            {
                value = UserBinaryObject.CreateUserBinaryObject((byte[]) value);
            }

            if ((short) onRemoveCallback != -1 || (short) onUpdateCallback != -1 ||
                (short) onAsyncOperationCompleteCallback != -1 || (short) DsCallback != -1)
                value = new CallbackEntry(string.IsNullOrEmpty(clientId) ? ClientID : clientId, -1, value,
                    onRemoveCallback, onUpdateCallback, onAsyncOperationCompleteCallback, DsCallback, Flag,
                    updateCallbackFilter, removeCallabackFilter);

            ////we can not specify both tags and groups for the same cache item.


            byte expType = WebCacheHelper.EvaluateExpirationParameters(absoluteExpiration, slidingExpiration);
            int options = 0;

            if (expType < 2)
            {
                options = expType;
            }

            if (isResyncExpiredItems)
            {
                int isResync = Convert.ToInt32(isResyncExpiredItems);
                isResync = isResync << 1;
                options = options | isResync;
            }

            int prio = Convert.ToInt32(priority);
            prio += 2;
            prio = (prio << 2);
            options = options | prio;

            long expiration = expType == 1 ? absoluteExpiration.Ticks : slidingExpiration.Ticks;

            ExpirationHint hint = (dependency == null) ? null : dependency;

            Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency syncDep = null;
            if (syncDependency != null)
            {
                try
                {
                    Synchronization.SyncCache syncCache = new Synchronization.SyncCache(syncDependency.CacheId,
                        syncDependency.Server, syncDependency.Port, true);
                    syncDep = new Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency(
                        syncDependency.CacheId, syncDependency.Key, syncCache);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            object entry = new CompactCacheEntry(key, value, hint, syncDep,
                expiration, (byte) options, null, group, subGroup, queryInfo, Flag, lockId, version, accessType,
                providerName, resyncProviderName);
            return entry;
        }


        #region	/                 --- Add ---           /

        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException("key");

            if (attributes == null) throw new ArgumentNullException("attributes");
            ExpirationHint hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(
                _nCache.Configuration.ExpirationPolicy, attributes.Dependency, attributes.AbsoluteExpiration,
                Cache.NoSlidingExpiration);

            return _nCache.AddExpirationHint(key, hint,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Add dependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="dependency">CacheDependency to be added</param>
        /// <param name="isResyncRequired">Boolean value indicating wether Resync is required or not</param>
        /// <returns>True if operations successeded else false</returns>
        public override bool AddDependency(string key, CacheDependency dependency, bool isResyncRequired)
        {
            ExpirationHint hint =
                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(dependency);

            if (isResyncRequired && hint != null)
                hint.SetBit(ExpirationHint.NEEDS_RESYNC);

            return _nCache.AddExpirationHint(key, hint,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        public override bool AddDependency(string key, CacheSyncDependency syncDependency)
        {
            Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency syncDep = null;

            if (syncDependency != null)
            {
                try
                {
                    Synchronization.SyncCache syncCache = new Synchronization.SyncCache(syncDependency.CacheId,
                        syncDependency.Server, syncDependency.Port, true);
                    syncDep = new Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency(
                        syncDependency.CacheId, syncDependency.Key, syncCache);
                }
                catch (Exception)
                {
                    return false;
                }

                return _nCache.AddSyncDependency(key, syncDep,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
            }

            return false;
        }

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override object Add(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemAddedCallback,
            bool isResyncExpiredItems, string group, string subGroup,
            Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            ExpirationHint hint =
                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(dependency);

            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime) valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, syncDependency, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, -1, onDsItemAddedCallback, isResyncExpiredItems, group,
                subGroup, queryInfo, flagMap, null, 0, LockAccessType.IGNORE_LOCK, providerName, resyncProviderName,
                updateCallbackFilter, removeCallabackFilter, clientId);

            UInt64 itemVersion = (UInt64) (DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
            _nCache.AddEntry(entry, operationContext);
            itemVersion = (ulong) operationContext.GetValueByField(OperationContextFieldName.ItemVersion);

            return itemVersion;
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">Sub group of the group</param>
        /// <returns>keys that are added or that alredy exists in the cache and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItems.
        /// <code>
        /// string keys = {"ORD_23", "ORD_67"};
        /// CacheItem items = new CacheItem[2]
        /// items[0] = new CacheItem(new Order());
        /// items[0].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[0].Priority = CacheItemPriority.High;
        /// items[0].ItemRemoveCallback = onRemove;
        ///
        /// items[1] = new CacheItem(new Order());
        /// items[1].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[1].Priority = CacheItemPriority.Low;
        /// items[1].ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        ///	NCache.Cache.Add(keys, items, "Customer", "Orders");
        ///
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items, "Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes,
            string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            itemVersions = null;

            if (_nCache == null)
                return null;

            ExpirationHint hint = null;

            object[] entries = new object[items.Length];


            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(item.Dependency);

                if (itemRemovedCallback == -1)
                {
                    if (item.ItemRemoveCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemRemoveCallback);
                    }
                }

                if (itemUpdatedCallback == -1)
                {
                    if (item.ItemUpdateCallback != null)
                    {
                        itemUpdatedCallback = _parent.GetCallbackId(item.ItemUpdateCallback);
                    }
                }

                Hashtable queryInfoDic = item.QueryInfo["query-info"] as Hashtable;
                if (queryInfoDic != null)
                {
                    IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                    while (queryInfoEnum.MoveNext())
                    {
                        ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                        for (int j = 0; j < valuesEnum.Count; j++)
                        {
                            if (valuesEnum[j] is DateTime)
                            {
                                valuesEnum[j] = ((DateTime) valuesEnum[j]).Ticks.ToString();
                            }
                        }
                    }
                }

                entries[i] = MakeCompactEntry(key, item.Value, hint, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                    itemRemovedCallback, itemUpdatedCallback, -1, onDataSourceItemsAdded, item.IsResyncExpiredItems,
                    item.Group, item.SubGroup, item.QueryInfo, item.FlagMap, null, 0, LockAccessType.IGNORE_LOCK,
                    providerName, item.ResyncProviderName, item.ItemUpdatedCallabackDataFilter,
                    item.ItemRemovedCallabackDataFilter, clientId);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.WriteThru,
                items[0].FlagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind,
                items[0].FlagMap.IsBitSet(BitSetConstants.WriteBehind));

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return _nCache.AddEntries(entries, out itemVersions, operationContext);
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override void AddAsync(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onAsyncItemAddCallback,
            short dsItemAddedCallback,
            bool isResyncExpiredItems, string group, string subGroup, Hashtable queryInfo, BitSet flagMap,
            string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, long size, string clientId)
        {
            if (_nCache == null) return;

            ExpirationHint hint =
                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(dependency);

            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime) valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, syncDependency, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, onAsyncItemAddCallback, dsItemAddedCallback,
                isResyncExpiredItems, group, subGroup, queryInfo, flagMap, null, 0, LockAccessType.IGNORE_LOCK,
                providerName, resyncProviderName, updateCallbackFilter, removeCallabackFilter, clientId);

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            UInt64 itemVersion = (UInt64) (DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
            operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
            _nCache.AddAsyncEntry(entry, operationContext);
        }

        #endregion

        #region	/                 --- Get ---           /

        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get("MyTextBox.Value");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get("MyTextBox.Value");
        /// 
        /// </code>
        /// </example>
        //public override CompressedValueEntry Get(string key, BitSet flagMap)
        //{
        //    return Get(key, flagMap, null, null);
        //}
        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read thru the data source,
        /// put is against the given group and sub group.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="group">Group of the object.</param>
        /// <param name="subGroup">Sub group of the group.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="group"/>  contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// <para>
        /// Note: The group and subGroup parameters are used only if the object is read thru the data source. 
        /// Otherwise the object will be returned from the cache whether it belongs to the given group and sub group or not.
        /// </para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Get(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            object lockId = lockHandle == null ? null : lockHandle.LockId;
            DateTime lockDate = new DateTime();
            ulong itemVersion = version == null ? 0 : version.Version;

            if (_nCache != null)
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ReadThru, flagMap.IsBitSet(BitSetConstants.ReadThru));
                if (providerName != null)
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);

                CompressedValueEntry cmpEntry = _nCache.GetGroup(key, flagMap, group, subGroup, ref itemVersion,
                    ref lockId, ref lockDate, lockTimeout, accessType, providerName, operationContext);
                if (cmpEntry != null && cmpEntry.Value != null)
                {
                    //incase of inproc local cache we will get the user object directly...
                    //therefore, we need to confirm if the value is user binary object or not...
                    if (cmpEntry.Value is UserBinaryObject)
                    {
                        if (cmpEntry.Value is UserBinaryObject)
                        {
                            UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                            object deserialized = ubObject.GetFullObject();

                            try
                            {
                                if (deserialized is byte[])
                                {
                                    deserialized = CompactBinaryFormatter.FromByteBuffer((byte[]) deserialized,
                                        this._serializationContext);
                                }
                            }
                            catch (Exception)
                            {
                            }

                            cmpEntry.Value = deserialized;
                        }
                    }
                }

                if (lockHandle == null) lockHandle = new LockHandle();
                lockHandle.LockId = lockId == null ? null : lockId as string;
                lockHandle.LockDate = lockDate;
                version = itemVersion != 0 ? new CacheItemVersion(itemVersion) : null;
                return cmpEntry;
            }

            return null;
        }
        
        public override Hashtable GetByTag(Tag[] tags, TagComparisonType comparisonType)
        {
            if (_nCache == null) return null;

            IDictionary items = _nCache.GetByTag(CacheHelper.GetTags(tags), comparisonType,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
            IDictionaryEnumerator en = items.GetEnumerator();
            Hashtable returnItems = new Hashtable();
            while (en.MoveNext())
            {
                returnItems.Add(en.Key, en.Value);
            }

            if (returnItems != null)
            {
                IDictionaryEnumerator ide = returnItems.GetEnumerator();
                while (ide.MoveNext())
                {
                    CompressedValueEntry cmpEntry = ide.Value as CompressedValueEntry;
                    if (cmpEntry != null && cmpEntry.Value != null)
                    {
                        if (cmpEntry.Value is UserBinaryObject)
                        {
                            UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                            cmpEntry.Value = ubObject.GetFullObject();
                        }
                    }
                }
            }

            return returnItems;
        }

        public override void RemoveByTag(Tag[] tags, TagComparisonType comarisonType)
        {
            if (_nCache == null) return;

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.RemoveByTag(CacheHelper.GetTags(tags), comarisonType, operationContext);
        }

        public override ICollection GetKeysByTag(Tag[] tags, TagComparisonType comparisonType)
        {
            if (_nCache == null) return null;

            return _nCache.GetKeysByTag(CacheHelper.GetTags(tags), comparisonType,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Retrieves the keys of items in a group or sub group.
        /// </summary>
        /// <param name="group">The group whose keys are to be returned.</param>
        /// <param name="subGroup">The sub group of the group foe which keys are to be returned.</param>
        /// <returns>The list of keys of a group or a sub group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="group"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para>
        /// If only group is specified, keys for the group and all the sub groups of the group are returned.
        /// If both the group and sub group are specified. Only the keys related to the sub group are returned.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	ArrayList list = NCache.Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	ArrayList list = Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override ArrayList GetGroupKeys(string group, string subGroup)
        {
            if (_nCache != null)
                return _nCache.GetGroupKeys(group, subGroup,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
            return null;
        }

        /// <summary>
        /// Retrieves the key and value pairs in a group or sub group.
        /// </summary>
        /// <param name="group">The group whose data is to be returned.</param>
        /// <param name="subGroup">The sub group of the group for which data is to be returned.</param>
        /// <returns>The list of key and value pairs of a group or a sub group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="group"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para>
        /// If only group is specified, data for the group and all the sub groups of the group are returned.
        /// If both the group and sub group are specified. Only the data related to the sub group are returned.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	Hashtable table = NCache.Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Hashtable table = Cache.Get("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary GetGroupData(string group, string subGroup)
        {
            Hashtable items = null;
            if (_nCache != null)
            {
                IDictionaryEnumerator en = _nCache.GetGroupData(group, subGroup,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation)).GetEnumerator();
                if (en != null)
                {
                    items = new Hashtable();
                    while (en.MoveNext())
                    {
                        items[en.Key] = en.Value;
                        CompressedValueEntry cmpEntry = en.Value as CompressedValueEntry;
                        if (cmpEntry != null && cmpEntry.Value != null)
                        {
                            if (cmpEntry.Value is UserBinaryObject)
                            {
                                UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                                cmpEntry.Value = ubObject.GetFullObject();
                            }
                        }
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get(keys);
        /// 
        /// </code> 
        /// </example>
        public override IDictionary Get(string[] keys, BitSet flagMap, string providerName)
        {
            if (_nCache != null)
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ReadThru, flagMap.IsBitSet(BitSetConstants.ReadThru));
                if (providerName != null)
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);

                IDictionary items = (IDictionary) _nCache.GetBulk(keys, flagMap, providerName, operationContext);
                Hashtable retItems = null;

                if (items != null)
                {
                    retItems = new Hashtable();
                    IDictionaryEnumerator ide = items.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        retItems[ide.Key] = ide.Value;
                        CompressedValueEntry cmpEntry = ide.Value as CompressedValueEntry;
                        if (cmpEntry != null && cmpEntry.Value != null)
                        {
                            if (cmpEntry.Value is UserBinaryObject)
                            {
                                UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                                cmpEntry.Value = ubObject.GetFullObject();
                            }
                        }
                    }
                }

                return retItems;
            }

            return null;
        }


        public override object GetCacheItem(string key, BitSet flagMap, string group, string subGroup,
            ref CacheItemVersion version, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType,
            string providerName)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;
            if (version == null) version = new CacheItemVersion();
            DateTime lockDate = new DateTime();
            if (String.IsNullOrEmpty(subGroup)) subGroup = null;

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ReadThru, flagMap.IsBitSet(BitSetConstants.ReadThru));
            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            if (providerName != null)
            {
                operationContext.Add(OperationContextFieldName.ReadThruProviderName, providerName);
            }

            ulong ver = version.Version;
            CacheEntry entry = _nCache.GetCacheEntry(key, group, subGroup, ref lockId, ref lockDate, lockTimeout,
                accessType, operationContext, ref ver) as CacheEntry;
            if (entry != null)
            {
                if (entry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = entry.Value as CallbackEntry;
                    if (cbEntry.Value is UserBinaryObject)
                    {
                        cbEntry.Value = ((UserBinaryObject) cbEntry.Value).GetFullObject();
                    }
                }
                else
                {
                    if (entry.Value is UserBinaryObject)
                    {
                        entry.Value = ((UserBinaryObject) entry.Value).GetFullObject();
                    }
                }

                version.Version = entry.Version;
            }

            lockHandle.LockId = lockId == null ? null : lockId as string;
            lockHandle.LockDate = lockDate;

            return entry;
        }

        #endregion

        #region	/                 --- Insert ---           /

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override CacheItemVersion Insert(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback,
            short onUpdateCallback, short onDsItemUpdateCallback, bool isResyncExpiredItems, string group,
            string subGroup, Hashtable queryInfo, BitSet flagMap,
            object lockId, CacheItemVersion version, LockAccessType accessType, string providerName,
            string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, string clientId,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            ExpirationHint hint =
                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(dependency);

            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime) valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, syncDependency, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, -1, onDsItemUpdateCallback, isResyncExpiredItems, group,
                subGroup, queryInfo, flagMap, lockId, version == null ? 0 : version.Version, accessType, providerName,
                resyncProviderName, updateCallbackFilter, removeCallabackFilter, clientId);

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            UInt64 itemVersionOperation;
            if (version == null)
                itemVersionOperation =
                    (UInt64) (DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
            else
                itemVersionOperation = version.Version;
            operationContext.Add(OperationContextFieldName.ItemVersion, itemVersionOperation);
            ulong itemVersion = _nCache.InsertEntry(entry, operationContext);
            return new CacheItemVersion(itemVersion);
        }


        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The group with which this object is associated</param>
        /// <param name="subGroup">The subGroup of the group</param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        ///	<para><b>Note: </b>For more information about how to use this method with the <see cref="CacheItemRemovedCallback"/> 
        ///	delegate, see <see cref="CacheItemRemovedCallback"/>.
        ///	</para>
        /// First create CacheItems.
        /// <code>
        /// string[] keys = {"SQLDSN", "ORADSN"};
        /// CacheItem items[] = new CacheItem[2];
        /// items[0] = new CacheItem(sqlConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.SlidingExpiration = TimeSpan.Zero;
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.SlidingExpiration = TimeSpan.Zero;
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        ///
        ///	NCache.Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Insert(string[] keys, CacheItem[] items,
            short onDsItemsUpdatedCallback, string providerName, long[] sizes,
            string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            bool returnVersions, out IDictionary itemVersions,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            itemVersions = null;

            if (_nCache == null) return null;

            object[] entries = new object[items.Length];

            ExpirationHint hint = null;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(item.Dependency);

                if (itemRemovedCallback == -1)
                {
                    if (item.ItemRemoveCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemRemoveCallback);
                    }
                }

                if (itemUpdatedCallback == -1)
                {
                    if (item.ItemUpdateCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemUpdateCallback);
                    }
                }

                Hashtable queryInfoDic = item.QueryInfo["query-info"] as Hashtable;
                if (queryInfoDic != null)
                {
                    IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                    while (queryInfoEnum.MoveNext())
                    {
                        ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                        for (int j = 0; j < valuesEnum.Count; j++)
                        {
                            if (valuesEnum[j] is DateTime)
                            {
                                valuesEnum[j] = ((DateTime) valuesEnum[j]).Ticks.ToString();
                            }
                        }
                    }
                }

                entries[i] = MakeCompactEntry(key, item.Value, hint,
                    item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                    itemRemovedCallback, itemRemovedCallback, -1, onDsItemsUpdatedCallback, item.IsResyncExpiredItems,
                    item.Group, item.SubGroup, item.QueryInfo, item.FlagMap, null, 0, LockAccessType.IGNORE_LOCK,
                    providerName, item.ResyncProviderName, item.ItemUpdatedCallabackDataFilter,
                    item.ItemRemovedCallabackDataFilter, clientId);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return _nCache.InsertEntries(entries, out itemVersions, operationContext);
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override void InsertAsync(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onAsyncItemUpdateCallback,
            short onDsItemsUpdatedCallback, bool isResyncExpiredItems, string group, string subGroup,
            Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallbackFilter, long size, string clientId)
        {
            ExpirationHint hint =
                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(dependency);

            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList) queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime) valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, syncDependency, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, onAsyncItemUpdateCallback, onDsItemsUpdatedCallback,
                isResyncExpiredItems, group, subGroup, queryInfo, flagMap, null, 0, LockAccessType.IGNORE_LOCK,
                providerName, resyncProviderName, updateCallbackFilter, removeCallbackFilter, clientId);

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            _nCache.InsertAsyncEntry(entry, operationContext);
        }

        #endregion

        #region	/                 --- Remove ---           /

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Remove(string key, BitSet flagMap, short onDsItemRemovedCallback,
            object lockId, CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
            if (_nCache != null)
            {
                CallbackEntry cbEntry = null;
                if (onDsItemRemovedCallback != -1)
                    cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsItemRemovedCallback, flagMap,
                        EventDataFilter.None, EventDataFilter.None);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation);

                operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
                operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
                operationContext.Add(OperationContextFieldName.WriteBehind,
                    flagMap.IsBitSet(BitSetConstants.WriteBehind));
                if (ProviderName != null)
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, ProviderName);

                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                CompressedValueEntry cmpEntry = _nCache.Remove(key, flagMap, cbEntry, lockId,
                    version == null ? 0 : version.Version, accessType, ProviderName, operationContext);
                if (cmpEntry != null && cmpEntry.Value != null)
                {
                    if (cmpEntry.Value is UserBinaryObject)
                    {
                        UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                        cmpEntry.Value = ubObject.GetFullObject();
                    }
                }

                return cmpEntry;
            }

            return null;
        }


        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void Delete(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId,
            CacheItemVersion version, LockAccessType accessType, string ProviderName)
        {
            if (_nCache != null)
            {
                CallbackEntry cbEntry = null;
                if (onDsItemRemovedCallback != -1)
                    cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsItemRemovedCallback, flagMap,
                        EventDataFilter.None, EventDataFilter.None);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation);

                operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);

                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                _nCache.Delete(key, flagMap, cbEntry, lockId, version == null ? 0 : version.Version, accessType,
                    ProviderName, operationContext);
            }
        }


        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Remove(string[] keys, BitSet flagMap, string providerName,
            short onDsItemsRemovedCallback)
        {
            CallbackEntry cbEntry = null;
            if (onDsItemsRemovedCallback != -1)
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsItemsRemovedCallback, flagMap,
                    EventDataFilter.None, EventDataFilter.None);

            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            IDictionary items = _nCache.Remove(baseKeys, flagMap, cbEntry, providerName, operationContext);

            if (items != null)
            {
                IDictionaryEnumerator ide = items.GetEnumerator();
                while (ide.MoveNext())
                {
                    CompressedValueEntry cmpEntry = ide.Value as CompressedValueEntry;
                    if (cmpEntry != null && cmpEntry.Value != null)
                    {
                        if (cmpEntry.Value is UserBinaryObject)
                        {
                            UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                            cmpEntry.Value = ubObject.GetFullObject();
                        }
                    }
                }
            }

            return items;
        }

        public override void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {
            CallbackEntry cbEntry = null;
            if (onDsItemsRemovedCallback != -1)
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsItemsRemovedCallback, flagMap,
                    EventDataFilter.None, EventDataFilter.None);

            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.Delete(baseKeys, flagMap, cbEntry, providerName, operationContext);
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Remove"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemRemoved"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.RemoveAsync("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.RemoveAsync("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void RemoveAsync(string key, BitSet flagMap, short onDsItemRemovedCallback)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (_nCache == null) return;

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            if (onDsItemRemovedCallback != -1)
            {
                CallbackEntry entry = new CallbackEntry(ClientID, -1, null, -1, -1, -1, onDsItemRemovedCallback,
                    flagMap, EventDataFilter.None, EventDataFilter.None);
                _nCache.RemoveAsync(new object[] {key, flagMap, entry, null}, operationContext);
            }
            else
            {
                _nCache.RemoveAsync(new object[] {key, flagMap, null, null}, operationContext);
            }
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="onAsyncItemRemoveCallback">The delegate that can be used by the client application to get the
        /// result of the Asynchronous Remove operation.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Remove"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemRemoved"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnAsyncItemRemoved(string key, object result)
        /// {
        /// ...
        /// }
        /// 
        ///	NCache.Cache.RemoveAsync("timestamp", new AsyncItemRemovedCallback(OnAsyncItemRemoved));
        /// 
        ///	Cache.RemoveAsync("timestamp", new AsyncItemRemovedCallback(OnAsyncItemRemoved));
        /// 
        /// </code>
        /// </example>
        public override void RemoveAsync(string key, BitSet flagMap, short onAsyncItemRemoveCallback,
            short onDsItemRemovedCallback, string providerName)
        {
            CallbackEntry cbEntry = null;

            if (onAsyncItemRemoveCallback != -1 || onDsItemRemovedCallback != -1)
            {
                cbEntry = new CallbackEntry(ClientID, -1, null, -1, -1, onAsyncItemRemoveCallback,
                    onDsItemRemovedCallback, flagMap, EventDataFilter.None, EventDataFilter.None);
            }

            if (_nCache == null) return;

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.RemoveAsync(new object[] {key, flagMap, cbEntry, providerName}, operationContext);
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        public override void Remove(string group, string subGroup)
        {
            if (group == null) throw new ArgumentNullException("group");
            if (_nCache == null) return;

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.Remove(group, subGroup, operationContext);
            return;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax t oquery objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <returns>Returns a list of cache keys</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// Hashtable values = new Hashtable();
        /// values.add("Name", "Paul Jones");
        /// "select Test.Application.Employee where this.Name = ?"
        /// 
        /// values.add("Salary", 2000);
        /// "select Test.Application.Employee where this.Salary > ?"
        /// 
        /// values.Add("Name", "Paul jones");
        /// values.Add("Salary", 2000);
        /// "select Test.Application.Employee where this.Name = ? and this.Salary > ?"
        /// 
        /// values.Add("Name", "Paul Jones");
        /// values.Add("Salary", 2000);
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet Search(string query, IDictionary values)
        {
            if (_nCache == null)
                return null;

            Hashtable tempValues = GetValues(values);

            Alachisoft.NCache.Caching.Queries.QueryResultSet resultSet = _nCache.Search(query, tempValues,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));

            return resultSet;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax t oquery objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <returns>Returns a dictionary containing cache keys and associated objects</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// Hashtable values = new Hashtable();
        /// values.add("Name", "Paul Jones");
        /// "select Test.Application.Employee where this.Name = ?"
        /// 
        /// values.add("Salary", 2000);
        /// "select Test.Application.Employee where this.Salary > ?"
        /// 
        /// values.Add("Name", "Paul jones");
        /// values.Add("Salary", 2000);
        /// "select Test.Application.Employee where this.Name = ? and this.Salary > ?"
        /// 
        /// values.Add("Name", "Paul Jones");
        /// values.Add("Salary", 2000);
        /// "select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override QueryResultSet SearchEntries(string query, IDictionary values)
        {
            if (_nCache == null)
                return null;

            Hashtable tempValues = GetValues(values);

            Alachisoft.NCache.Caching.Queries.QueryResultSet resultSet = _nCache.SearchEntries(query, tempValues,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));

            return resultSet;
        }

        #endregion

        #region ------------ Delete/RemoveQuery -----------

        /// <summary>
        /// Remove on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax t oquery objects from cache</param>
        /// <returns>Returns a dictionary containing removed cache keys and associated objects</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , <> , < , > , <=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// "delete Test.Application.Employee where this.Name = 'Paul Jones'"
        /// "delete Test.Application.Employee where this.Salary > 2000"
        /// "delete Test.Application.Employee where this.Name = 'Paul Jones' and this.Salary > 2000"
        /// "delete Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)"
        /// 
        /// </code>
        /// </example>
        public override int ExecuteNonQuery(string query, IDictionary values)
        {
            if (_nCache == null)
                return 0;

            Hashtable tempValues = GetValues(values);

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.RemoveQueryOperation, true);

            int affectedkeys = _nCache.RemoveQuery(query, tempValues, operationContext);

            return affectedkeys;
        }

        #endregion

        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            CacheImplBase cacheImpl, ref long size)
        {
            object serializableObjectUnser = serializableObject;
            if (size <= 0)
            {
                Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[]) serializableObject).Length : 0;
                    return serializableObject;
                }

                serializableObject = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
                size = serializableObject is byte[] ? ((byte[]) serializableObject).Length : 0;
            }

            return serializableObjectUnser;
        }

        public override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag,
            CacheImplBase cacheImpl)
        {
            return serializedObject;
        }

        #region	/                 --- GetEnumerator ---           /

        /// <summary>
        /// Retrieves a dictionary enumerator used to iterate
        /// through the key settings and their values 
        /// contained in the cache.
        /// </summary>
        /// <remarks>
        /// If items are added or removed from the cache while enumerating through the items
        /// the behavior is not predictable. It is therefore advised not to update the cache keys
        /// while enumerating.
        /// <para><b>Note:</b> Just like <see cref="Cache.Count"/> in a cluster especially partitioned 
        /// this operation is an expensive one and may require network calls. It is therefore advised to use 
        /// this method only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <returns>An enumerator to iterate through the <see cref="Cache"/> object.</returns>
        public override IEnumerator GetEnumerator()
        {
            return null;
        }

        public override EnumerationDataChunk GetNextChunk(EnumerationPointer pointer)
        {
            EnumerationDataChunk nextChunk = null;

            if (_nCache != null)
            {
                nextChunk = _nCache.GetNextChunk(pointer, new OperationContext());
            }

            return nextChunk;
        }

        #endregion

        #region /               --- Key based notification registration ---     /

        /// <summary>
        /// Registers the CacheItemUpdatedCallback and/or CacheItemRemovedCallback for the specified key.
        /// </summary>
        /// <remarks>
        /// <see cref="CacheItemUpdatedCallback"/> and/or <see cref="CacheItemRemovedCallback"/> provided
        /// this way are very useful because a client application can show interest in any item already present
        /// in the cache. As soon as the item is updated or removed from the cache, the client application is notified
        /// and actions can be taken accordingly.
        /// </remarks>
        /// <param name="key">The cache key used to reference the cache item.</param>
        /// <param name="updateCallback">The CacheItemUpdatedCallback that is invoked if the item
        /// with the specified key is updated in the cache.</param>
        /// <param name="removeCallback">The CacheItemRemovedCallback is invoked when the item with
        /// the specified key is removed from the cache.</param>
        public override void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid,
            bool notifyOnitemExpiration)
        {
            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                    new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None),
                    new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.DataWithMetadata,
                        notifyOnitemExpiration),
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }


        /// <summary>
        /// Unregisters the <see cref="CacheItemUpdatedCallback"/> and/or <see cref="CacheItemRemovedCallback"/> already registered
        /// for the specified key.
        /// </summary>
        /// <param name="key">The cache key used to reference the cache item.</param>
        /// <param name="updateCallback">CacheItemUpdatedCallback that is invoked when the item 
        /// with the specified key is updated in the cache.</param>
        /// <param name="removeCallback">CacheItemRemovedCallback that is invoked when the item
        /// with the key is removed from the cache.</param>
        public override void UnRegisterKeyNotificationCallback(string key, short updateCallbackid,
            short removeCallbackid)
        {
            if (_nCache != null)
                _nCache.UnregisterKeyNotificationCallback(key,
                    new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None),
                    new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.DataWithMetadata),
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }


        /// <summary>
        /// Registers the CacheItemUpdatedCallback and/or CacheItemRemovedCallback for the list of specified keys.
        /// </summary>
        /// <remarks>
        /// <see cref="CacheItemUpdatedCallback"/> and/or <see cref="CacheItemRemovedCallback"/> provided
        /// this way are very useful because a client application can show interest in any item already present
        /// in the cache. As soon as the item is updated or removed from the cache, the client application is notified
        /// and actions can be taken accordingly.
        /// </remarks>
        /// <param name="keys">The list of the cache keys used to reference the cache items.</param>
        /// <param name="updateCallback">The CacheItemUpdatedCallback that is invoked if the item
        /// with the specified key is updated in the cache.</param>
        /// <param name="removeCallback">The CacheItemRemovedCallback is invoked when the item with
        /// the specified key is removed from the cache.</param>
        public override void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback)
        {
            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(keys,
                    new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None, callbackType),
                    new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.DataWithMetadata, callbackType),
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                    cbUpdate,
                    cbRemove,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove,
            Runtime.Events.EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                    cbUpdate,
                    cbRemove,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        /// <summary>
        /// Unregisters the <see cref="CacheItemUpdatedCallback"/> and/or <see cref="CacheItemRemovedCallback"/> already registered
        /// for the specified list of keys.
        /// </summary>
        /// <param name="keys">The cache key used to reference the cache item.</param>
        /// <param name="updateCallback">CacheItemUpdatedCallback that is invoked when the item 
        /// with the specified key is updated in the cache.</param>
        /// <param name="removeCallback">CacheItemRemovedCallback that is invoked when the item
        /// with the key is removed from the cache.</param>
        public override void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid,
            short removeCallbackid)
        {
            if (_nCache != null)
                _nCache.UnregisterKeyNotificationCallback(keys,
                    new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None),
                    new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.None),
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        public override void UnRegisterKeyNotificationCallback(string key, short update, short remove,
            EventType eventType)
        {
            this.UnRegisterKeyNotificationCallback(key, update, remove);
        }

        public override void UnRegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventType eventType)
        {
            this.UnRegisterKeyNotificationCallback(key, update, remove);
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter, callbackType);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration, callbackType);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                    cbUpdate,
                    cbRemove,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove,
            EventDataFilter datafilter, bool notifyOnItemExpiration,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter, callbackType);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration, callbackType);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                    cbUpdate,
                    cbRemove,
                    new OperationContext(OperationContextFieldName.OperationType,
                        OperationContextOperationType.CacheOperation));
        }

        #endregion

        #region   /           --- Stream Operations ---                      /

        public override string OpenStream(string key, StreamModes mode, string group, string subGroup,
            DateTime absExpiration, TimeSpan slidingExpiration, CacheDependency dependency, CacheItemPriority priority)
        {
            ExpirationHint expirationHint = null;
            if (absExpiration != null)
                expirationHint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(
                    _nCache.Configuration.ExpirationPolicy, dependency, absExpiration, slidingExpiration);

            EvictionHint evictionHint = new PriorityEvictionHint();
            return _nCache.OpenStream(key, mode, group, subGroup, expirationHint, evictionHint,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        public override void CloseStream(string key, string lockHandle)
        {
            _nCache.CloseStream(key, lockHandle,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        public override int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset,
            int streamOffset, int length)
        {
            VirtualArray vBuffer = null;
            int bytesRead = _nCache.ReadFromStream(ref vBuffer, key, lockHandle, streamOffset, length,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
            if (bytesRead > 0 && vBuffer != null)
            {
                return vBuffer.CopyData(buffer, offset, length);
            }

            return 0;
        }

        public override void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset,
            int length)
        {
            UserBinaryObject ub = UserBinaryObject.CreateUserBinaryObject(buffer);
            VirtualArray vBuffer = new VirtualArray(ub.Data);
            _nCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        public override long GetStreamLength(string key, string lockHandle)
        {
            return _nCache.GetStreamLength(key, lockHandle,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
        }

        #endregion


        #region Map Reduce Methods

        public override void ExecuteMapReduceTask(Runtime.MapReduce.MapReduceTask task, string taskId,
            MapReduce.MROutputOption option, short callbackId, IKeyFilter keyFilter, string query, Hashtable parameters)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override void RegisterMapReduceCallbackListener(short callbackId, string taskId)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override Runtime.MapReduce.TaskStatus GetTaskProgress(string taskId)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override void CancelTask(string taskId)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override ArrayList GetRunningTasks()
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override IDictionaryEnumerator GetTaskEnumerator(string taskId, short callbackId)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override Common.MapReduce.TaskEnumeratorResult NextRecord(string serverAddress,
            Common.MapReduce.TaskEnumeratorPointer pointer)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        public override void Dispose(string serverAddress)
        {
            throw new NotSupportedException("Operation not supported in InProc.");
        }

        #endregion

        #region Entry Processor Methods

        public override Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string defaultReadThru, string defaultWriteThru, params object[] arguments)
        {
            return InvokeEntryProcessor(keys, entryProcessor, defaultReadThru, new BitSet(), defaultWriteThru,
                new BitSet(), arguments);
        }

        internal override Hashtable InvokeEntryProcessor(string[] keys, IEntryProcessor entryProcessor,
            string readThruProviderName, BitSet dsReadOptionFlag, string writeThruProviderName,
            BitSet dsWriteOptionFlag, params object[] arguments)
        {
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ReadThru,
                dsReadOptionFlag.IsBitSet(BitSetConstants.ReadThru));
            if (readThruProviderName != null)
            {
                operationContext.Add(OperationContextFieldName.ReadThruProviderName, readThruProviderName);
            }

            return _nCache.InvokeEntryProcessor(keys, entryProcessor, arguments, dsWriteOptionFlag,
                writeThruProviderName, operationContext);
        }

        #endregion

        public override string RegisterCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            Hashtable tempValues = GetValues(query.Values);
            QueryDataFilters datafilters = new QueryDataFilters((int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated));
            return _nCache.RegisterCQ(query.Query, tempValues, clientUniqueId, this.ClientID, notifyAdd, notifyUpdate,
                notifyRemove,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation), datafilters);
        }

        public override QueryResultSet SearchCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            Hashtable tempValues = GetValues(query.Values);
            QueryDataFilters datafilters = new QueryDataFilters((int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated));
            return _nCache.SearchCQ(query.Query, tempValues, clientUniqueId, this.ClientID, notifyAdd, notifyUpdate,
                notifyRemove,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation), datafilters);
        }

        public override QueryResultSet SearchEntriesCQ(ContinuousQuery query, string clientUniqueId, bool notifyAdd,
            bool notifyUpdate, bool notifyRemove)
        {
            Hashtable tempValues = GetValues(query.Values);
            QueryDataFilters datafilters = new QueryDataFilters((int) query.MaxFilter(EventType.ItemAdded)
                , (int) query.MaxFilter(EventType.ItemRemoved)
                , (int) query.MaxFilter(EventType.ItemUpdated));
            return _nCache.SearchEntriesCQ(query.Query, tempValues, clientUniqueId, this.ClientID, notifyAdd,
                notifyUpdate, notifyRemove,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation), datafilters);
        }

        public override void UnRegisterCQ(string serverUniqueId, string clientUniqueId)
        {
            _nCache.UnRegisterCQ(serverUniqueId, clientUniqueId, this.ClientID);
        }

        public override List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointers)
        {
            EnumerationPointer pointer = null;
            List<EnumerationDataChunk> chunks = new List<EnumerationDataChunk>();
            if (pointers.Count > 0)
            {
                pointer = pointers[0];
                chunks.Add(_nCache.GetNextChunk(pointer, new OperationContext()));
            }

            return chunks;
        }


        private Hashtable GetValues(IDictionary values)
        {
            IDictionaryEnumerator dicEnum = values.GetEnumerator();
            Hashtable tempValues = new Hashtable();
            while (dicEnum.MoveNext())
            {
                try
                {
                    if (dicEnum.Value is ArrayList)
                    {
                        ArrayList list = new ArrayList((ArrayList) dicEnum.Value);
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (!(list[i] == null))
                            {
                                if (list[i] is System.String)
                                    list[i] = (object) (list[i].ToString().ToLower());
                            }
                            else
                            {
                                throw new ArgumentNullException("NCache query does not support null values",
                                    (System.Exception) null);
                            }
                        }

                        tempValues.Add(dicEnum.Key, new ArrayList(list));
                    }
                    else
                    {
                        if (!(dicEnum.Value == null))
                        {
                            if (dicEnum.Value is System.String)
                                tempValues.Add(dicEnum.Key, (object) (dicEnum.Value.ToString().ToLower()));
                            else
                                tempValues.Add(dicEnum.Key, dicEnum.Value);
                        }
                        else
                        {
                            throw new ArgumentNullException("NCache query does not support null values",
                                (System.Exception) null);
                        }
                    }
                }
                catch (ArgumentNullException ane)
                {
                    throw ane;
                }
            }

            return tempValues;
        }

        #region Polling

        internal override Common.Events.PollingResult Poll()
        {
            OperationContext context = new OperationContext();
            return _nCache.Poll(context);
        }

        public override void RegisterPollingNotification(short pollingCallbackId)
        {
            _nCache.RegisterPollingNotification(pollingCallbackId, new OperationContext());
        }

        #endregion

        #region ---------------------- Cache Data Reader ----------------------

        /// <summary>
        /// Provide data reader on <see cref="Cache"/> based on the query specified.
        /// </example> 
        public override IRecordSetEnumerator ExecuteReader(string query, IDictionary values, bool getData,
            int chunkSize)
        {
            if (_nCache == null)
                return null;
            Hashtable tempValues = GetValues(values);
            ClusteredList<ReaderResultSet> readerResultSet = _nCache.ExecuteReader(query, tempValues, getData,
                chunkSize, true,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation));
            return GetReader(readerResultSet);
        }

        public override IRecordSetEnumerator ExecuteReaderCQ(ContinuousQuery continuousQuery, bool getData,
            int chunkSize, string clientUniqueId, bool notifyAdd, bool notifyUpdate, bool notifyRemove)
        {
            if (_nCache == null)
                return null;
            List<ReaderResultSet> readerResultSet = _nCache.ExecuteReaderCQ(continuousQuery.Query,
                continuousQuery.Values, getData, chunkSize, continuousQuery.ClientUniqueId, this.ClientID, notifyAdd,
                notifyUpdate, notifyRemove,
                new OperationContext(OperationContextFieldName.OperationType,
                    OperationContextOperationType.CacheOperation),
                new QueryDataFilters((int) continuousQuery.MaxFilter(EventType.ItemAdded),
                    (int) continuousQuery.MaxFilter(EventType.ItemUpdated),
                    (int) continuousQuery.MaxFilter(EventType.ItemRemoved)));
            return GetReader(readerResultSet);
        }


        private IRecordSetEnumerator GetReader(IList<ReaderResultSet> readerResultSet)
        {
            IRecordSetEnumerator result = null;

            if (readerResultSet != null && readerResultSet.Count > 0 && readerResultSet[0].RecordSet != null)
            {
                IRecordSetEnumerator rse = readerResultSet[0].RecordSet.GetEnumerator();
                result = new PartitionRSEnumerator(rse, readerResultSet[0].ReaderID, readerResultSet[0].NodeAddress,
                    readerResultSet[0].NextIndex, this);
            }

            return result;
        }

        #region ---------------------- IRecordSetLoader ----------------------

        public ReaderResultSet GetRecordSet(string readerID, int nextIndex, string nodeIP, string reciepent)
        {
            ReaderResultSet nextChunk = null;

            if (_nCache != null)
            {
                nextChunk = _nCache.GetReaderChunk(readerID, nextIndex, true, new OperationContext());
            }

            return nextChunk;
        }

        /// <summary>
        /// Dispose reader resources on node specified
        /// </example>
        public void DisposeReader(string readerId, string nodeIp)
        {
            if (_nCache != null)
                _nCache.DisposeReader(readerId, new OperationContext());
        }

        #endregion

        #endregion


        public override void RegisterCacheClientConnectivityEvent()
        {
            //Inproc does not raise this event
        }

        public override void UnregisterCacheClientConnectivityEvent()
        {
            //inproc does not raise this event
        }


        internal override void Touch(List<string> keys)
        {
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            _nCache.Touch(keys, operationContext);
        }


        #region                                         ------------------ Messaging ------------------------

        internal override bool GetOrCreate(string topicName, TopicOperationType type)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            TopicOperation topicOperation = new TopicOperation(topicName, type);
            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override bool Subscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            SubscriptionInfo subInfo =
                new SubscriptionInfo() {SubscriptionId = recepientId, ClientId = ClientID, Type = pubSubType};
            var topicOperation = new SubscriptionOperation(topicName, TopicOperationType.Subscribe, subInfo);

            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override bool UnSubscribe(string topicName, string recepientId, SubscriptionType pubSubType)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            SubscriptionInfo subInfo =
                new SubscriptionInfo() {SubscriptionId = recepientId, ClientId = ClientID, Type = pubSubType};
            var topicOperation = new SubscriptionOperation(topicName, TopicOperationType.UnSubscribe, subInfo);

            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override bool RemoveTopic(string topicName, bool forcefully)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            var topicOperation = new TopicOperation(topicName, TopicOperationType.Remove);

            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime,
            Hashtable metadata, BitSet flagMap)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            _nCache.PublishMessage(messageId, payLoad, creationTime, expirationTime, metadata, flagMap,
                operationContext);
        }

        internal override object GetMessageData(BitSet flagMap)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            IDictionary<string, IList<object>> messages =
                _nCache.GetAssignedMessages(new SubscriptionInfo() {ClientId = this.ClientID}, operationContext);
            return GetMessageItems(messages);
        }

        internal override void AcknowledgeMessageReceipt(IDictionary<string, IList<string>> topicWiseMessageIds)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType,
                OperationContextOperationType.CacheOperation);
            _nCache.AcknowledgeMessageReceipt(ClientID, topicWiseMessageIds, operationContext);
        }

        private IDictionary<string, IList<MessageItem>> GetMessageItems(IDictionary<string, IList<object>> messages)
        {
            IDictionary<string, IList<MessageItem>> messageItems =
                new HashVector<string, IList<MessageItem>>(messages.Count);

            foreach (var pair in messages)
            {
                if (pair.Value.Count > 0)
                {
                    IList<MessageItem> items = new ClusteredList<MessageItem>(pair.Value.Count);
                    foreach (var message in pair.Value)
                    {
                        var entry = (Alachisoft.NCache.Caching.Messaging.Message) message;
                        var messageItem = new MessageItem();
                        messageItem.MessageId = entry.MessageId;
                        messageItem.Flag = entry.FlagMap;
                        ;
                        messageItem.Payload = entry.PayLoad;
                        messageItem.CreationTime = entry.CreationTime;
                        messageItem.ExpirationTime = new TimeSpan(entry.MessageMetaData.ExpirationTime);
                        messageItem.DeliveryOption = entry.MessageMetaData.DeliveryOption;
                        messageItem.SubscriptionType = entry.MessageMetaData.SubscriptionType;
                        messageItem.MessageFailureReason = entry.MessageMetaData.MessgeFailureReason;

                        if (entry.MessageMetaData.RecepientList != null)
                            messageItem.RecipientList = new HashSet<string>(entry.MessageMetaData.RecepientList);

                        items.Add(messageItem);
                    }

                    messageItems.Add(pair.Key, items);
                }
            }

            return messageItems;
        }

        #endregion
    }
}