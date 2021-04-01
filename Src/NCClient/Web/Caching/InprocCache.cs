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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Client
{
    internal sealed class InprocCache : CacheImplBase, IEnumerable
    {

		/// <summary> Underlying implementation of NCache. </summary>
		internal Alachisoft.NCache.Caching.Cache	_nCache;

        internal CacheConfig _config;

        /// <summary> Reference count of the cache. </summary>
        internal int _refCount;

        /// <summary> </summary>
        private CompactCacheEntry _entry;

        /// <summary>Serialization context (actually name of the cache.)used for Compact Framework </summary>
        private string _serializationContext;

        /// <summary> Cache event listener object. Implements all events.</summary>
        private CacheEventsListener _listener;

        ///// <summary> Cache event listener object. Implements all events.</summary>
        private CacheAsyncEventsListener _asyncListener;

        private ClusterEventsListener _clusterListener;

        /// <summary> Contains callback ids and associated callbacks</summary>
        private ResourcePool _callbackIDsMap;

        /// <summary> Initial values for CustomRemovedCallback and CustomUpdateCallback ids. </summary>
        private short _rcbInitialVal = 0;
        private short _ucbInitialVal = 1000;
        private short _aiacbInitialVal = 2000;
        private short _aiucbInitialVal = 3000;
        private short _aircbInitialVal = 4000;
        private short _acccbInitialVal = 5000;

        private long _compressionThresholdSize;
        private bool _compressionEnabled;

        private string _userId;
        private byte[] _password = null;

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

        protected internal override EventManager EventManager
        {
            get
            {
                return _parent.EventManager;
            }
        }

        protected internal override bool SerializationEnabled
        {
            get
            {
                return false;
            }
        }

        internal sealed override PoolManager PoolManager => _nCache.FakeObjectPoolManager;

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="config"></param>
        internal InprocCache(Alachisoft.NCache.Caching.Cache objectCache, CacheConfig config, Cache parent, string userId, string password)
            : base()
        {
            _nCache = objectCache;

            this.TypeMap = _nCache.GetTypeInfoMap();

            _config = config;
            _parent = parent;
            _parent.SerializationFormat = _nCache.SerializationFormat;

            if (_nCache != null)
            {
                _listener = new CacheEventsListener(_parent.EventListener, _nCache);

                _asyncListener = new CacheAsyncEventsListener(_parent.AsyncListener, _nCache);
                _nCache.OnClientConnected(ClientID, _config.CacheId, LocalClientInfo, 0);
                _serializationContext = _nCache.Name; //Sets the serialization context.
            }

            AddRef();
            _userId = userId;
            if (password != null)
                _password = Alachisoft.NCache.Common.EncryptionUtil.Encrypt(password);


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
                else if (_refCount < 0) _refCount = 0;

                // remove from the cache table.

                lock (CacheManager.Caches)
                {
                    if (_config != null)
                        CacheManager.Caches.Remove(_config.CacheId);
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
                bool result = _nCache.Lock(key, lockTimeout, out lockId, out lockDate, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
                bool result = _nCache.IsLocked(key, ref lockId, ref lockDate, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
            if (_nCache != null) _nCache.Unlock(key, null, true, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
            {
                bool isPreemtive = false;
                if (lockId == null)
                    isPreemtive = true;

                _nCache.Unlock(key, lockId, isPreemtive, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            }
        }

        #region	/                 --- Count ---           /

        /// <summary>
        /// Gets the number of items stored in the cache.
        /// </summary>
        /// <remarks>This property can be useful when monitoring your application's performance 
        /// or when using ASP.NET tracing functionality.
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.Clear();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Clear();
        /// 
        /// </code>
        /// </example>
        public override void Clear(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            if (_nCache == null) return;

            Notifications notification = null;
            if (onDsClearedCallback != -1)
                notification = new Notifications(ClientID, -1, -1, -1, -1, onDsClearedCallback, EventDataFilter.None, EventDataFilter.None);
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            _nCache.Clear(flagMap, notification, operationContext);
        }


        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        ///	NCache.Cache.ClearAsync();
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync();
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onDsClearedCallback, string providerName)
        {
            if (_nCache == null) return;

            Notifications notification = null;
            if (onDsClearedCallback != -1)
            {
                notification = new Notifications(ClientID, -1, -1, -1, -1, onDsClearedCallback, EventDataFilter.None, EventDataFilter.None);
            }
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
            _nCache.ClearAsync(flagMap, notification, operationContext);
        }

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>
        /// </example>
        public override void ClearAsync(BitSet flagMap, short onAsyncCacheClearCallback, short onDsClearedCallback, string providerName)
        {
            if (_nCache == null) return;

            Notifications notification = null;
            if (onAsyncCacheClearCallback != -1 || onDsClearedCallback != -1)
            {
                notification = new Notifications(ClientID, -1, -1, -1, onAsyncCacheClearCallback, onDsClearedCallback, EventDataFilter.None, EventDataFilter.None);
            }
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);
            _nCache.ClearAsync(flagMap, notification, operationContext);
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
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

            Hashtable keysFound = _nCache.Contains(new string[] { key }, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

            IDictionary<string,bool> keysPresent = HashtableUtil.ExtractKeyStatus(keysFound, new string[] { key });

            return keysPresent[key];
        }

        public override IDictionary<string, bool> ContainsBulk(string[] keys)
        {
            if (keys == null) throw new ArgumentNullException("keys");

            Hashtable keysFound = _nCache.Contains(keys, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

            return HashtableUtil.ExtractKeyStatus(keysFound, keys) ;
        }

        #endregion


        /// <summary>
        /// Broadcasts a custom application defined event.
        /// </summary>
        /// <param name="notifId">Application specific notification code/id</param>
        /// <param name="data">Application specific data</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// <para><b>Note:</b> Custom event notifications can not be disabled through configuration.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to raise a custom application defined event.
        /// <code>
        /// 
        ///	NCache.Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now));
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now)); 
        /// 
        /// </code>
        /// </example>
        public override void RaiseCustomEvent(object notifId, object data)
        {
            _nCache.SendNotification(notifId, data, null);
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
            if (!key.GetType().IsSerializable && !type.IsAssignableFrom(key.GetType())) throw new ArgumentException("key is not serializable");
            if (!value.GetType().IsSerializable && !type.IsAssignableFrom(value.GetType())) throw new ArgumentException("value is not serializable");
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
        private object MakeCompactEntry(string key, object value, Alachisoft.NCache.Caching.AutoExpiration.ExpirationHint dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onAsyncOperationCompleteCallback, short DsCallback, bool isResyncExpiredItems, string group, string subGroup, Hashtable queryInfo, BitSet Flag, object lockId, ulong version, LockAccessType accessType, string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, string clientId)
        {
            Notifications notification = null;
            //we create a user binary object.
            if (this.SerializationEnabled)
            {
                value = UserBinaryObject.CreateUserBinaryObject((byte[])value);
            }

            if ((short)onRemoveCallback != -1 || (short)onUpdateCallback != -1 || (short)onAsyncOperationCompleteCallback != -1 || (short)DsCallback != -1)
                notification = new Notifications(string.IsNullOrEmpty(clientId) ? ClientID : clientId, -1, onRemoveCallback, onUpdateCallback, onAsyncOperationCompleteCallback, DsCallback, updateCallbackFilter, removeCallabackFilter);

            ////muds:
            ////we can not specify both tags and groups for the same cache item.


            byte expType = CacheHelper.EvaluateExpirationParameters(absoluteExpiration, slidingExpiration);
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

            long expiration = expType == 1 ? absoluteExpiration.ToUniversalTime().Ticks : slidingExpiration.Ticks;

           
            object entry = new CompactCacheEntry(key, value, null,
                                                 expiration, (byte)options, null, group, subGroup, queryInfo, Flag, lockId, version, accessType, providerName, resyncProviderName, notification);
            return entry;
        }


        #region	/                 --- Add ---           /



        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (attributes == null) throw new ArgumentNullException("attributes");
            ExpirationHint hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager, null, attributes.AbsoluteExpiration, Cache.NoSlidingExpiration);

            return _nCache.AddExpirationHint(key, hint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }
        
        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, short onDsItemAddedCallback, bool isResyncExpiredItems,
            Hashtable queryInfo, BitSet flagMap, string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName)
        {
            ExpirationHint hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager);
            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList)queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime)valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, absoluteExpiration, slidingExpiration,
                                             priority, onRemoveCallback, onUpdateCallback, -1, onDsItemAddedCallback, isResyncExpiredItems, null,null, queryInfo, flagMap, null, 0, LockAccessType.IGNORE_LOCK, providerName, resyncProviderName, updateCallbackFilter, removeCallabackFilter, clientId);
            UInt64 itemVersion = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
            _nCache.AddEntry(entry, operationContext);
            itemVersion = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);

        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">Sub group of the group</param>
        /// <returns>keys that are added or that already exists in the cache and their status.</returns>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items, "Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        public override IDictionary<string, Exception> Add(string[] keys, CacheItem[] items,
            short onDataSourceItemsAdded, string providerName, long[] sizes, bool encryptionEnabled,
            string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
           CallbackType callbackType = CallbackType.PushBasedNotification)
        {

            if (_nCache == null)
                return null;

            ExpirationHint hint = null;

            object[] entries = new object[items.Length];


            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                hint = NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager);

                hint = null;
                if (itemRemovedCallback == -1)
                {
                    if (item.CacheItemRemovedCallback != null)
                    {
                        removeCallabackFilter = item.ItemRemovedDataFilter;
                        var callbackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventTypeInternal.ItemRemoved, removeCallabackFilter, callbackType);

                        if (callbackIds != null && callbackIds.Length > 1)
                            itemRemovedCallback = callbackIds[1];
                    }
                    else if (item.ItemRemoveCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemRemoveCallback);
                        removeCallabackFilter = EventDataFilter.None;
                    }
                }
                if (itemUpdatedCallback == -1)
                {
                    if (item.CacheItemUpdatedCallback != null)
                    {
                        updateCallbackFilter = item.ItemUpdatedDataFilter;
                        var callbackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventTypeInternal.ItemUpdated, updateCallbackFilter, callbackType);

                        if (callbackIds != null && callbackIds.Length > 0)
                            itemUpdatedCallback = callbackIds[0];
                    }
                    else if (item.ItemUpdateCallback != null)
                    {
                        itemUpdatedCallback = _parent.GetCallbackId(item.ItemUpdateCallback);
                        updateCallbackFilter = EventDataFilter.None;
                    }
                }

                Hashtable queryInfoDic = item.QueryInfo["query-info"] as Hashtable;
                if (queryInfoDic != null)
                {
                    IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                    while (queryInfoEnum.MoveNext())
                    {
                        ArrayList valuesEnum = (ArrayList)queryInfoEnum.Value;
                        for (int j = 0; j < valuesEnum.Count; j++)
                        {
                            if (valuesEnum[j] is DateTime)
                            {
                                valuesEnum[j] = ((DateTime)valuesEnum[j]).Ticks.ToString();
                            }
                        }
                    }
                }

                entries[i] = MakeCompactEntry(key, item.GetValue<object>(), hint,
                                              item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                                              itemRemovedCallback, itemUpdatedCallback, -1, onDataSourceItemsAdded, false,
                                             null, item.SubGroup, item.QueryInfo, item.FlagMap, null, 0, LockAccessType.IGNORE_LOCK, providerName,null, item.ItemUpdatedDataFilter, item.ItemRemovedDataFilter, clientId);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.WriteThru, items[0].FlagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, items[0].FlagMap.IsBitSet(BitSetConstants.WriteBehind));

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return HashtableUtil.ToDictionary(_nCache.AddEntries(entries, out var itemVersions, operationContext) as Hashtable);
        }

        #endregion

        #region	/                 --- Get ---           /

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get("MyTextBox.Value", "Customer", null);
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Get<T>(string key, BitSet flagMap, string group, string subGroup, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            object lockId = lockHandle == null ? null : lockHandle.LockId;
            DateTime lockDate = new DateTime();
            ulong itemVersion =  0 ;

            if (_nCache != null)
            {

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CompressedValueEntry cmpEntry = _nCache.GetGroup(key, flagMap, group, subGroup, ref itemVersion, ref lockId, ref lockDate, lockTimeout, accessType, operationContext);
                if (cmpEntry != null && cmpEntry.Value != null)
                {
                    //muds:
                    //in case of inproc local cache we will get the user object directly...
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
                                    deserialized = CompactBinaryFormatter.FromByteBuffer((byte[])deserialized, this._serializationContext);
                                }
                            }
                            catch (Exception) { }

                            cmpEntry.Value = deserialized;
                        }
                    }
                }

                if (lockHandle == null) lockHandle = new LockHandle();
                lockHandle.LockId = lockId == null ? null : lockId as string;
                lockHandle.LockDate = lockDate;
                cmpEntry.Value = CacheHelper.GetObjectOrInitializedCollection<T>(key, cmpEntry.Type, cmpEntry.Value, _parent.GetCacheInstance());
                return cmpEntry;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// 
        ///	NCache.Cache.Get(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Get(keys);
        /// 
        /// </code> 
        /// </example>
        public override IDictionary Get<T>(string[] keys, BitSet flagMap)
        {
            if (_nCache != null)
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                IDictionary items = (IDictionary)_nCache.GetBulk(keys, flagMap,operationContext);
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
                    retItems = CacheHelper.BulkGetObjectOrInitializedCollection<T>(retItems, _parent);
                }
                return retItems;
            }
            return null;
        }


        public override object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;
            DateTime lockDate = new DateTime();
            CacheEntry entry = null;
            try
            {
               
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                ulong ver = 0;
                entry = _nCache.GetCacheEntry(key, null,null, ref lockId, ref lockDate, lockTimeout, accessType, operationContext, ref ver) as CacheEntry;
                if (entry != null)
                {
                    if (entry.Value is UserBinaryObject)
                        entry.Value = ((UserBinaryObject)entry.Value).GetFullObject();

                }

                lockHandle.LockId = lockId == null ? null : lockId as string;
                lockHandle.LockDate = lockDate;

                return entry;
            }
            finally
            {
                if (entry != null)
                    entry.MarkFree(NCModulesConstants.Global);

            }
        }

        public override IDictionary GetCacheItemBulk(string[] keys, BitSet flagMap)
        {
            if (_nCache != null)
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                IDictionary items = _nCache.GetBulkCacheItems(keys, flagMap, operationContext);
                Hashtable retItems = null;
                if (items != null)
                {
                    retItems = new Hashtable();
                   
                    IDictionaryEnumerator ide = items.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        var entry = ide.Value as CacheEntry;
                        retItems[ide.Key] = entry;

                        if (entry != null)
                        {
                            if (entry.Value is UserBinaryObject)
                                entry.Value = ((UserBinaryObject)entry.Value).GetFullObject();
                        }
                        if (entry != null)
                            entry.MarkFree(NCModulesConstants.Global);
                    }
                }

                return retItems;
            }
            return null;
        }

        #endregion

        #region	/                 --- Insert ---           /

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback,
            short onUpdateCallback, short onDsItemUpdateCallback, bool isResyncExpiredItems, Hashtable queryInfo, BitSet flagMap,
            object lockId, LockAccessType accessType, string providerName, string resyncProviderName, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size, bool encryptionEnabled, string clientId, string typeName, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            ExpirationHint hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager);


            Hashtable queryInfoDic = queryInfo["query-info"] as Hashtable;
            if (queryInfoDic != null)
            {
                IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                while (queryInfoEnum.MoveNext())
                {
                    ArrayList valuesEnum = (ArrayList)queryInfoEnum.Value;
                    for (int i = 0; i < valuesEnum.Count; i++)
                    {
                        if (valuesEnum[i] is DateTime)
                        {
                            valuesEnum[i] = ((DateTime)valuesEnum[i]).Ticks.ToString();
                        }
                    }
                }
            }

            object entry = MakeCompactEntry(key, value, hint, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, -1, onDsItemUpdateCallback, isResyncExpiredItems, null,null, queryInfo, flagMap, lockId,  0 , accessType, providerName, resyncProviderName, updateCallbackFilter, removeCallabackFilter, clientId);
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);
            UInt64 itemVersionOperation;
                itemVersionOperation = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
            operationContext.Add(OperationContextFieldName.ItemVersion, itemVersionOperation);
            ulong itemVersion = _nCache.InsertEntry(entry, operationContext);
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
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// </example>
        public override IDictionary<string, Exception> Insert(string[] keys, CacheItem[] items,
            short onDsItemsUpdatedCallback, string providerName, long[] sizes,
            bool encryptionEnabled, string clientId, short itemUpdatedCallback, short itemRemovedCallback,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            CallbackType callbackType = CallbackType.PushBasedNotification)
        {

            if (_nCache == null) return null;

            object[] entries = new object[items.Length];

            ExpirationHint hint = null;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                hint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager);

                if (itemRemovedCallback == -1)
                {
                    if (item.CacheItemRemovedCallback != null)
                    {
                        removeCallabackFilter = item.ItemRemovedDataFilter;
                        var callbackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventTypeInternal.ItemRemoved, removeCallabackFilter, callbackType);

                        if (callbackIds != null && callbackIds.Length > 1)
                            itemRemovedCallback = callbackIds[1];
                    }
                    else if (item.ItemRemoveCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemRemoveCallback);
                        removeCallabackFilter = EventDataFilter.None;
                    }
                }
                if (itemUpdatedCallback == -1)
                {
                    if (item.CacheItemUpdatedCallback != null)
                    {
                        updateCallbackFilter = item.ItemUpdatedDataFilter;
                        var callbackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventTypeInternal.ItemUpdated, updateCallbackFilter, callbackType);

                        if (callbackIds != null && callbackIds.Length > 0)
                            itemUpdatedCallback = callbackIds[0];
                    }
                    else if (item.ItemUpdateCallback != null)
                    {
                        itemRemovedCallback = _parent.GetCallbackId(item.ItemUpdateCallback);
                        updateCallbackFilter = EventDataFilter.None;
                    }
                }

                Hashtable queryInfoDic = item.QueryInfo["query-info"] as Hashtable;
                if (queryInfoDic != null)
                {
                    IDictionaryEnumerator queryInfoEnum = queryInfoDic.GetEnumerator();
                    while (queryInfoEnum.MoveNext())
                    {
                        ArrayList valuesEnum = (ArrayList)queryInfoEnum.Value;
                        for (int j = 0; j < valuesEnum.Count; j++)
                        {
                            if (valuesEnum[j] is DateTime)
                            {
                                valuesEnum[j] = ((DateTime)valuesEnum[j]).Ticks.ToString();
                            }
                        }
                    }
                }

                entries[i] = MakeCompactEntry(key, item.GetValue<object>(), hint,
                    item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                    itemRemovedCallback, itemRemovedCallback, -1, onDsItemsUpdatedCallback, false, null, item.SubGroup, item.QueryInfo, item.FlagMap, null, 0, LockAccessType.IGNORE_LOCK, providerName, null, item.ItemUpdatedDataFilter, item.ItemRemovedDataFilter, clientId);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.WriteThru, items[0].FlagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, items[0].FlagMap.IsBitSet(BitSetConstants.WriteBehind));
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return HashtableUtil.ToDictionary(_nCache.InsertEntries(entries, out var itemVersions, operationContext) as Hashtable);
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override CompressedValueEntry Remove<T>(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId, LockAccessType accessType, string ProviderName)
        {
            if (_nCache != null)
            {
                Notifications notification = null;
                if (onDsItemRemovedCallback != -1)
                    notification = new Notifications(ClientID, -1, -1, -1, -1, onDsItemRemovedCallback, EventDataFilter.None, EventDataFilter.None);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
                operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
                operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
                if (ProviderName != null)
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, ProviderName);

                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                CompressedValueEntry cmpEntry = _nCache.Remove(key, flagMap, notification, lockId,  0 , accessType, ProviderName, operationContext);
                if (cmpEntry != null && cmpEntry.Value != null)
                {
                    if (cmpEntry.Value is UserBinaryObject)
                    {
                        UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                        cmpEntry.Value = ubObject.GetFullObject();
                    }
                    cmpEntry.Value = CacheHelper.GetObjectOrInitializedCollection<T>(key, cmpEntry.Type, cmpEntry.Value, _parent.GetCacheInstance());
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove("timestamp");
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public override void Delete(string key, BitSet flagMap, short onDsItemRemovedCallback, object lockId, LockAccessType accessType)
        {
            if (_nCache != null)
            {
                Notifications notification = null;
                if (onDsItemRemovedCallback != -1)
                    notification = new Notifications(ClientID, -1, -1, -1, -1, onDsItemRemovedCallback, EventDataFilter.None, EventDataFilter.None);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);

                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                _nCache.Delete(key, flagMap, notification, lockId,  0 , accessType, operationContext);
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        ///	NCache.Cache.Remove(keys);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Client.UI.NPage"/> or <see cref="Alachisoft.NCache.Client.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Remove<T>(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {
            Notifications notification = null;
            if (onDsItemsRemovedCallback != -1)
                notification = new Notifications(ClientID, -1, -1, -1, -1, onDsItemsRemovedCallback, EventDataFilter.None, EventDataFilter.None);

            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.WriteThru, flagMap.IsBitSet(BitSetConstants.WriteThru));
            operationContext.Add(OperationContextFieldName.WriteBehind, flagMap.IsBitSet(BitSetConstants.WriteBehind));
            if (providerName != null)
                operationContext.Add(OperationContextFieldName.WriteThruProviderName, providerName);

            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            IDictionary items = _nCache.Remove(baseKeys, flagMap, notification, providerName, operationContext);

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
                items = CacheHelper.BulkGetObjectOrInitializedCollection<T>(items as Hashtable, _parent);
            }

            return items;
        }

        public override void Delete(string[] keys, BitSet flagMap, string providerName, short onDsItemsRemovedCallback)
        {
            Notifications notification = null;
            if (onDsItemsRemovedCallback != -1)
                notification = new Notifications(ClientID, -1, -1, -1, -1, onDsItemsRemovedCallback, EventDataFilter.None, EventDataFilter.None);

            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

            operationContext.Add(OperationContextFieldName.ClientId, this.ClientID);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.Delete(baseKeys, flagMap, notification, providerName, operationContext);
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

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

            _nCache.Remove(group, subGroup, operationContext);
            return;
        }

       


        #endregion

      

        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, CacheImplBase cacheImpl, ref long size, UserObjectType userObjectType,bool isCustomAttributeBaseSerialzed=false)
        {
            return SerializationUtil.SafeSerializeInProc(serializableObject, serializationContext, ref flag, _parent.SerializationFormat, ref size, userObjectType, _nCache.DataFormat == DataFormat.Binary, isCustomAttributeBaseSerialzed);
        }

        public override T SafeDeserialize<T>(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl, UserObjectType userObjectType)
        {
            return SerializationUtil.SafeDeserializeInProc<T>(serializedObject, serializationContext, flag, userObjectType, _nCache.DataFormat == DataFormat.Binary);
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="CacheManager.ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        public override void RegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid, bool notifyOnitemExpiration)
        {
            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key, new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None), new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.None, notifyOnitemExpiration), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
        public override void UnRegisterKeyNotificationCallback(string key, short updateCallbackid, short removeCallbackid)
        {
            if (_nCache != null)
                _nCache.UnregisterKeyNotificationCallback(key, new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None), new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.None), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
        public override void RegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid, string clientId, CallbackType callbackType = CallbackType.PullBasedCallback)
        {
            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(keys,
                    new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None, callbackType),
                    new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.None, callbackType),
                    new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;

            cbUpdate = new CallbackInfo(ClientID, update, datafilter);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                  cbUpdate,
                  cbRemove,
                  new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove, Runtime.Events.EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                  cbUpdate,
                  cbRemove,
                  new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
        public override void UnRegisterKeyNotificationCallback(string[] keys, short updateCallbackid, short removeCallbackid)
        {
            if (_nCache != null)
                _nCache.UnregisterKeyNotificationCallback(keys, new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None), new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.None), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public override void UnRegisterKeyNotificationCallback(string key, short update, short remove, EventTypeInternal eventType)
        {
            this.UnRegisterKeyNotificationCallback(key, update, remove);
        }

        public override void UnRegisterKeyNotificationCallback(string[] key, short update, short remove, EventTypeInternal eventType)
        {
            this.UnRegisterKeyNotificationCallback(key, update, remove);
        }

        public override void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter, callbackType);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration, callbackType);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                  cbUpdate,
                  cbRemove,
                  new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

        }

        public override void RegisterKeyNotificationCallback(string[] key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration, CallbackType callbackType = CallbackType.PushBasedNotification)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter, callbackType);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration, callbackType);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(key,
                  cbUpdate,
                  cbRemove,
                  new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }


        #endregion

      

        #region	/                 --- Nested Classes ---           /

        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        private class ClusterEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>
            private Cache.ClusterEventsListener _listener;

            /// <summary> Underlying implementation of NCache. </summary>
            private Alachisoft.NCache.Caching.Cache _nCache;

            private NodeJoinedCallback _memberJoined;
            private NodeLeftCallback _memberLeft;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="nCache"></param>
            internal ClusterEventsListener(Cache.ClusterEventsListener parent, Alachisoft.NCache.Caching.Cache nCache)
            {
                _listener = parent;
                _nCache = nCache;


                _memberJoined = new NodeJoinedCallback(this.OnMemberJoined);
                _memberLeft = new NodeLeftCallback(this.OnMemberLeft);

                _nCache.MemberJoined += _memberJoined;
                _nCache.MemberLeft += _memberLeft;

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

                    _nCache.MemberJoined -= _memberJoined;
                    _nCache.MemberLeft -= _memberLeft;

                }
                catch { }
            }

            #endregion


            public void OnMemberJoined(object clusterAddress, object serverAddress, bool flag, EventContext eventContext)
            {
                try
                {
                    
                }
                catch { }
            }

            public void OnMemberLeft(object clusterAddress, object serverAddress, EventContext eventContext)
            {
                try
                {
                }
                catch { }
            }

        }

        ///// <summary>
        ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        ///// internal class and must not be used from outside
        ///// </summary>
        private class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Cache.CacheAsyncEventsListener _parent;


            /// <summary> Underlying implementation of NCache. </summary>
            private Alachisoft.NCache.Caching.Cache _nCache;

            private AsyncOperationCompletedCallback _asyncOperationCompleted;

            private DataSourceUpdatedCallback _dsUpdatedCallback;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="nCache"></param>
            internal CacheAsyncEventsListener(Cache.CacheAsyncEventsListener parent, Alachisoft.NCache.Caching.Cache nCache)
            {
                _parent = parent;
                _nCache = nCache;

                _asyncOperationCompleted = new AsyncOperationCompletedCallback(this.OnAsyncOperationCompleted);
                _nCache.AsyncOperationCompleted += _asyncOperationCompleted;

                
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
                catch { }
            }

            #endregion



            public void OnAsyncOperationCompleted(object opCode, object result, EventContext eventContext)
            {
                _parent.OnAsyncOperationCompleted(opCode, result, true);
            }

           

        }

        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        private class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Cache.CacheEventsListener _listener;


            /// <summary> Underlying implementation of NCache. </summary>
            private Alachisoft.NCache.Caching.Cache _nCache;

            private ItemAddedCallback _itemAdded;
            private ItemUpdatedCallback _itemUpdated;
            private ItemRemovedCallback _itemRemoved;
            private Alachisoft.NCache.Caching.CacheClearedCallback _cacheCleared;
            private Alachisoft.NCache.Caching.CustomNotificationCallback _customNotification;
            private CustomRemoveCallback _customRemoveCallback;
            private CustomUpdateCallback _customUpdateCallback;
            private Alachisoft.NCache.Caching.CacheStoppedCallback _cacheStopped;
            private string _serializationContext;
            private PollRequestCallback _pollRequested;

           
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="nCache"></param>
            internal CacheEventsListener(Cache.CacheEventsListener parent, Alachisoft.NCache.Caching.Cache nCache)
            {
                _listener = parent;
                _nCache = nCache;


                _itemAdded = new ItemAddedCallback(this.OnItemAdded);
                _itemUpdated = new ItemUpdatedCallback(this.OnItemUpdated);
                _itemRemoved = new ItemRemovedCallback(this.OnItemRemoved);
         
                _customNotification = new Alachisoft.NCache.Caching.CustomNotificationCallback(this.OnCustomNotification);
               
         
                _pollRequested = OnPollNotified;

                _nCache.ItemAdded += _itemAdded;
                _nCache.ItemUpdated += _itemUpdated;
                _nCache.ItemRemoved += _itemRemoved;
                _nCache.CacheCleared += _cacheCleared;
                _nCache.CustomNotif += _customNotification;
                _nCache.PollRequestCallbackNotif += _pollRequested;

                _customRemoveCallback = new CustomRemoveCallback(this.OnCustomRemoveCallback);
                _customUpdateCallback = new CustomUpdateCallback(this.OnCustomUpdateCallback);


                _nCache.CustomRemoveCallbackNotif += _customRemoveCallback;
                _nCache.CustomUpdateCallbackNotif += _customUpdateCallback;

       


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

                    _nCache.ItemAdded -= _itemAdded;
                    _nCache.ItemUpdated -= _itemUpdated;
                    _nCache.ItemRemoved -= _itemRemoved;
                    _nCache.CacheCleared -= _cacheCleared;
                    _nCache.CustomNotif -= _customNotification;
                    _nCache.CacheStopped -= _cacheStopped;

                    _nCache.CustomRemoveCallbackNotif -= _customRemoveCallback;
                    _nCache.CustomUpdateCallbackNotif -= _customUpdateCallback;
                }
                catch { }
            }

            #endregion


            public void OnItemAdded(object key, EventContext eventContext)
            {
                try
                {
                    BitSet flag = new BitSet(0);
                    EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);

                    if (_listener != null)
                        _listener.OnItemAdded((string)key, true, item, flag);
                }
                catch { }
            }
            public void OnPollNotified(string clientId, short callbackId, EventTypeInternal eventType)
            {
                if (_listener != null)
                    _listener.OnPollNotified(callbackId, eventType);
            }

            public void OnItemUpdated(object key, EventContext eventContext)
            {
                EventCacheItem oldItem = null;
                EventCacheItem item = null;
                try
                {
                    BitSet flag = new BitSet(0);
                    if (eventContext != null)
                    {
                        if (eventContext.OldItem != null)
                            oldItem = EventUtil.ConvertToItem(eventContext.OldItem);
                        if (eventContext.Item != null)
                            item = EventUtil.ConvertToItem(eventContext.Item);

                        if (_listener != null)
                            _listener.OnItemUpdated((string)key, true, item, oldItem, flag);
                    }
                }
                catch { }
            }

          

            public void OnItemRemoved(object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext)
            {
                EventCacheItem item = null;
                try
                {
                    BitSet flag = new BitSet(0);

                    if (eventContext.Item != null)
                        item = EventUtil.ConvertToItem(eventContext.Item);

                    if (_listener != null)
                        _listener.OnItemRemoved((string)key,
                            value,
                            reason,
                            Flag, true, item);

                }
                catch { }
            }

            public void OnCustomNotification(object notifId, object data, EventContext eventContext)
            {
                try
                {
                    if (_listener != null)
                        _listener.OnCustomNotification(notifId, data, true);
                }
                catch { }
            }

           
         

            public void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, BitSet Flag, EventContext eventContext)
            {
                EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);
                CacheItemRemovedReason removeReason = EventUtil.ConvertToCIRemoveReason(reason);

                _listener.OnCustomRemoveCallback((string)key, value, removeReason, Flag, true, item);
            }

            public void OnCustomUpdateCallback(object key, object value, EventContext eventContext)
            {
                EventCacheItem item = EventUtil.ConvertToItem(eventContext.Item);
                EventCacheItem oldItem = EventUtil.ConvertToItem(eventContext.OldItem);
                _listener.OnCustomUpdateCallback((string)key, value, true, item, oldItem, null);
            }

        }

        #endregion

        #region/          ---Serialization Compact Framework---         /
 
        public override Hashtable GetCompactTypes()
        {
            Hashtable types = new Hashtable(new EqualityComparer()); 
            Hashtable javaTypes = new Hashtable(new EqualityComparer());
            if (_config != null && _config.CompactKnownTypes != null)
            {
                if (_config.DataSharingKnownTypes != null)
                {
                    FilterOutDotNetTypes(HashtableUtil.CreateCustomComparerHashtable(_config.DataSharingKnownTypes), types, javaTypes, false);
                }
                FilterOutDotNetTypes(HashtableUtil.CreateCustomComparerHashtable(_config.CompactKnownTypes), types, javaTypes, true);
                types = SerializationUtil.GetCompactTypes(types, false, Name);
            }
            return types;
        }

        private void FilterOutDotNetTypes(Hashtable _cmptKnownTypes, Hashtable cmptKnownTypesdotNet, Hashtable cmptKnownTypesJava, bool compact)
        {
            if (_cmptKnownTypes != null)
            {
                IDictionaryEnumerator ide = _cmptKnownTypes.GetEnumerator();

                if (!compact)
                {
                    while (ide.MoveNext())
                    {
                        Hashtable compactType = (Hashtable)ide.Value;
                        Hashtable classes = (Hashtable)compactType["known-classes"];
                        IDictionaryEnumerator ide2 = classes.GetEnumerator();
                        while (ide2.MoveNext())
                        {
                            Hashtable typeInfo = (Hashtable)ide2.Value;
                            if (typeInfo["type"].ToString().ToLower().Equals("java"))
                            {
                                if (!cmptKnownTypesJava.Contains((String)ide.Key))
                                    cmptKnownTypesJava.Add((String)ide.Key, new Hashtable(new EqualityComparer()));
                                ((Hashtable)cmptKnownTypesJava[((String)ide.Key)]).Add((String)typeInfo["name"], (Hashtable)typeInfo);
                                if (!typeInfo.Contains("portable"))
                                {
                                    typeInfo.Add("portable", compactType["portable"]);
                                }
                            }
                            else if (typeInfo["type"].ToString().ToLower().Equals("net"))
                            {
                                if (!cmptKnownTypesdotNet.Contains((String)ide.Key))
                                    cmptKnownTypesdotNet.Add((String)ide.Key, new Hashtable(new EqualityComparer()));
                                ((Hashtable)cmptKnownTypesdotNet[((String)ide.Key)]).Add((String)typeInfo["name"], (Hashtable)typeInfo);
                                if (!typeInfo.Contains("portable"))
                                {
                                    typeInfo.Add("portable", compactType["portable"]);
                                }
                            }
                        }

                        if (Convert.ToBoolean(compactType["portable"]))
                        {
                            if (cmptKnownTypesJava.Count > 0 && (Hashtable)cmptKnownTypesJava[((String)ide.Key)] != null)
                            {
                                ((Hashtable)cmptKnownTypesJava[((String)ide.Key)]).Add((String)"Alachisoft.NCache.AttributeUnion", compactType["attribute-union-list"]);
                            }
                            if (cmptKnownTypesdotNet.Count > 0 && (Hashtable)cmptKnownTypesdotNet[((String)ide.Key)] != null)
                            {
                                ((Hashtable)cmptKnownTypesdotNet[((String)ide.Key)]).Add((String)"Alachisoft.NCache.AttributeUnion", compactType["attribute-union-list"]);
                            }
                        }
                    }
                }
                else
                {
                    while (ide.MoveNext())
                    {
                        Hashtable compactType = (Hashtable)ide.Value;
                        if (compactType["type"] != null)
                        {
                            if (compactType["type"].ToString().ToLower().Equals("java"))
                            {
                                if (!cmptKnownTypesJava.Contains((String)ide.Key))
                                    cmptKnownTypesJava.Add((String)ide.Key, new Hashtable(new EqualityComparer()));
                                ((Hashtable)cmptKnownTypesJava[((String)ide.Key)]).Add((String)compactType["name"], (Hashtable)compactType);
                            }
                            else if (compactType["type"].ToString().ToLower().Equals("net"))
                            {
                                if (compactType.Contains("arg-types"))
                                {
                                    compactType["arg-types"] = FilterOutNestedGenerics((Hashtable)compactType["arg-types"]);
                                }
                                if (!cmptKnownTypesdotNet.Contains((String)ide.Key))
                                    cmptKnownTypesdotNet.Add((String)ide.Key, new Hashtable(new EqualityComparer()));
                                ((Hashtable)cmptKnownTypesdotNet[((String)ide.Key)]).Add((String)compactType["name"], (Hashtable)compactType);
                            }
                        }
                    }
                }

            }
        }
        private Hashtable FilterOutNestedGenerics(Hashtable htArgTypes)
        {
            Hashtable htArgTypes2 = null;
            if (htArgTypes != null && htArgTypes.Count > 0)
            {
                htArgTypes2 = new Hashtable(new EqualityComparer());
                IDictionaryEnumerator ide11 = htArgTypes.GetEnumerator();
                while (ide11.MoveNext())
                {
                    Hashtable innerGenericType = new Hashtable(new EqualityComparer());
                    htArgTypes2.Add(ide11.Key.ToString(), innerGenericType);
                    Hashtable argInstances = (Hashtable)ide11.Value;
                    IDictionaryEnumerator ide12 = argInstances.GetEnumerator();
                    while (ide12.MoveNext())
                    {
                        Hashtable instanceArgType = (Hashtable)ide12.Value;
                        if (instanceArgType.Contains("arg-types"))
                        {
                            instanceArgType["arg-types"] = FilterOutNestedGenerics((Hashtable)instanceArgType["arg-types"]);
                        }
                        Hashtable innerGenericTypeDetail = new Hashtable(new EqualityComparer());
                        innerGenericTypeDetail.Add(instanceArgType["name"].ToString(), instanceArgType);
                        innerGenericType.Add(ide12.Key.ToString(), innerGenericTypeDetail);
                    }
                }
            }
            return htArgTypes2;
        }

        #endregion

        #region /           --- Stream Operations ---                      /

        public  string OpenStream(string key, StreamModes mode, string group, string subGroup, DateTime absExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            ExpirationHint expirationHint = null;

            if (absExpiration != null)
                expirationHint = Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetExpirationHint(PoolManager, null, absExpiration, slidingExpiration);


            EvictionHint evictionHint = PriorityEvictionHint.Create(PoolManager);
            return _nCache.OpenStream(key, mode, group, subGroup, expirationHint, evictionHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public  void CloseStream(string key, string lockHandle)
        {
            _nCache.CloseStream(key, lockHandle, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public  int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset, int streamOffset, int length)
        {
            VirtualArray vBuffer = null;
            int bytesRead = _nCache.ReadFromStream(ref vBuffer, key, lockHandle, streamOffset, length, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            if (bytesRead > 0 && vBuffer != null)
            {
                return vBuffer.CopyData(buffer, offset, length);
            }
            return 0;
        }

        public  void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset, int length)
        {
            UserBinaryObject ub = UserBinaryObject.CreateUserBinaryObject(buffer);
            VirtualArray vBuffer = new VirtualArray(ub.Data);
            _nCache.WriteToStream(key, lockHandle, vBuffer, srcOffset, dstOffset, length, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public  long GetStreamLength(string key, string lockHandle)
        {
            return _nCache.GetStreamLength(key, lockHandle, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        #endregion



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
                        //Forcing a deep copy instead of shallow one to avoid ToLower() on the tag list at user's end
                        ArrayList list = new ArrayList((ArrayList)dicEnum.Value);
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (!(list[i] == null))
                            {
                                if (list[i] is System.String)
                                    list[i] = (object)(list[i].ToString().ToLower());
                            }
                            else
                            {
                                throw new ArgumentNullException("NCache query does not support null values", (System.Exception)null);
                            }
                        }
                        tempValues.Add(dicEnum.Key, new ArrayList(list));
                    }
                    else
                    {
                        if (!(dicEnum.Value == null))
                        {
                            if (dicEnum.Value is System.String)
                                tempValues.Add(dicEnum.Key, (object)(dicEnum.Value.ToString().ToLower()));
                            else
                                tempValues.Add(dicEnum.Key, dicEnum.Value);
                        }
                        else
                        {
                            throw new ArgumentNullException("NCache query does not support null values", (System.Exception)null);
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
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            _nCache.Touch(keys, operationContext);
        }


        #region                                         ------------------ Messaging ------------------------


        internal override bool GetOrCreate(string topicName, TopicOperationType type)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            TopicOperation topicOperation = new TopicOperation(topicName, type);
            return _nCache.TopicOpertion(topicOperation, operationContext);

        }

        internal override bool Subscribe(string topicName, string subscriptionName, Common.Enum.SubscriptionType pubSubType,long creatioTime,long expirationTime,SubscriptionPolicyType subscriptionPolicy=SubscriptionPolicyType.NonDurableExclusiveSubscription)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            SubscriptionInfo subInfo = new SubscriptionInfo() { SubscriptionId = subscriptionName, ClientId = ClientID, Type = pubSubType, SubPolicyType = subscriptionPolicy };
            var topicOperation = new SubscriptionOperation(topicName, TopicOperationType.Subscribe, subInfo);

            return _nCache.TopicOpertion(topicOperation, operationContext);

        }

        internal override bool UnSubscribe(string topicName, string recepientId, SubscriptionPolicyType subscriptionPolicy, Common.Enum.SubscriptionType pubSubType,bool dispose)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            SubscriptionInfo subInfo = new SubscriptionInfo() { SubscriptionId = recepientId, ClientId = ClientID, Type = pubSubType,SubPolicyType=subscriptionPolicy };
            var topicOperation = new SubscriptionOperation(topicName, TopicOperationType.UnSubscribe, subInfo,dispose);

            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override bool RemoveTopic(string topicName, bool forcefully)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            var topicOperation = new TopicOperation(topicName, TopicOperationType.Remove);

            return _nCache.TopicOpertion(topicOperation, operationContext);
        }

        internal override void PublishMessage(string messageId, object payLoad, long creationTime, long expirationTime, Hashtable metadata, BitSet flagMap)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            _nCache.PublishMessage(messageId, payLoad, creationTime, expirationTime, metadata, flagMap, operationContext);
        }

        internal override object GetMessageData(BitSet flagMap)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            MessageResponse response = _nCache.GetAssignedMessages(new SubscriptionInfo() { ClientId = this.ClientID }, operationContext);
            ReceivedMessages receivedMessages = new ReceivedMessages();
            receivedMessages.AssignedMessages = GetMessageItems(response.AssignedMessages);
            return receivedMessages;
        }

        internal override void AcknowledgeMessageReceipt(IDictionary<string, IList<string>> topicWiseMessageIds)
        {
            var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            _nCache.AcknowledgeMessageReceipt(ClientID, topicWiseMessageIds, operationContext);
        }

        private IDictionary<string, IList<MessageItem>> GetMessageItems(IDictionary<string, IList<object>> messages)
        {
            IDictionary<string, IList<MessageItem>> messageItems = new HashVector<string, IList<MessageItem>>(messages.Count);

            foreach (var pair in messages)
            {
                if (pair.Value.Count > 0)
                {
                    IList<MessageItem> items = new ClusteredList<MessageItem>(pair.Value.Count);

                    foreach (var message in pair.Value)
                    {
                        var cachingMessage = (Alachisoft.NCache.Caching.Messaging.Message)message;

                        var messageItem = new MessageItem();
                        messageItem.Flag = cachingMessage.FlagMap;
                        messageItem.MessageId = cachingMessage.MessageId;

                        if (cachingMessage.PayLoad != null)
                            messageItem.Payload = cachingMessage.PayLoad;
                        else
                        {
                            if (cachingMessage is EventMessage)
                                messageItem.Payload = CreateMessageEventItems(message as Alachisoft.NCache.Caching.Messaging.EventMessage);

                          
                        }

                        messageItem.CreationTime = cachingMessage.CreationTime;
                        messageItem.DeliveryOption = cachingMessage.MessageMetaData.DeliveryOption;
                        messageItem.SubscriptionType = cachingMessage.MessageMetaData.SubscriptionType;
                        messageItem.MessageFailureReason = cachingMessage.MessageMetaData.MessgeFailureReason;
                        messageItem.ExpirationTime = new TimeSpan(cachingMessage.MessageMetaData.ExpirationTime);

                        if (cachingMessage.MessageMetaData.RecepientList != null)
                            messageItem.RecipientList = new HashSet<string>(cachingMessage.MessageMetaData.RecepientList);
                        if (cachingMessage.MessageMetaData.SubscriptionIdentifierList != null)
                            messageItem.SubscriptionIdentifierList = new List<SubscriptionIdentifier>(cachingMessage.MessageMetaData.SubscriptionIdentifierList);
                        items.Add(messageItem);
                    }
                    messageItems.Add(pair.Key, items);
                }
            }
            return messageItems;
        }

        private MessageEventItem[] CreateMessageEventItems(Alachisoft.NCache.Caching.Messaging.EventMessage eventMessage)
        {
            var eventItems = new ClusteredList<MessageEventItem>();

            if (eventMessage.CallbackInfos != null)
            {
                foreach (CallbackInfo cbInfo in eventMessage.CallbackInfos)
                {
                    if (cbInfo.Client == this.ClientID)
                    {
                        var messageEventItem = new MessageEventItem();
                        messageEventItem.Callback = cbInfo.Callback;
                        messageEventItem.DataFilter = cbInfo.DataFilter;

                        messageEventItem.Key = eventMessage.Key;
                        messageEventItem.EventType = eventMessage.EventID.EventType;
                        messageEventItem.Item = EventUtil.ConvertToItem(eventMessage.Item);
                        messageEventItem.OldItem = EventUtil.ConvertToItem(eventMessage.OldItem);
                        messageEventItem.Reason = EventUtil.ConvertToCIRemoveReason(eventMessage.RemoveReason);

                        eventItems.Add(messageEventItem);
                    }
                }
            }
         
            else
            {
                 var messageEventItem = new MessageEventItem();
                 messageEventItem.Key = eventMessage.Key;
                if (eventMessage.EventID.EventType == Persistence.EventType.ITEM_ADDED_EVENT)
                {
                   
                    messageEventItem.Item= EventUtil.ConvertToItem(eventMessage.Item);
                    messageEventItem.EventType = eventMessage.EventID.EventType;
                }
                else if (eventMessage.EventID.EventType == Persistence.EventType.ITEM_UPDATED_EVENT)
                {
                    messageEventItem.Item = EventUtil.ConvertToItem(eventMessage.Item);
                    messageEventItem.OldItem = EventUtil.ConvertToItem(eventMessage.OldItem);
                    messageEventItem.EventType = eventMessage.EventID.EventType;
                  
                }
                else if (eventMessage.EventID.EventType == Persistence.EventType.ITEM_REMOVED_EVENT)
                {
                    messageEventItem.Item = EventUtil.ConvertToItem(eventMessage.Item);
                    messageEventItem.EventType = eventMessage.EventID.EventType;
                    messageEventItem.Reason = EventUtil.ConvertToCIRemoveReason(eventMessage.RemoveReason);
                }
                eventItems.Add(messageEventItem);
            }
            return eventItems.ToArray();
        }


       
        #endregion

        

    }
}
