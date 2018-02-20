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
using System.Reflection;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Web.RemoteClient.Config;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching.APILogging;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Web.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
#if COMMUNITY|| CLIENT
using Alachisoft.NCache.Caching.Queries;
#endif
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Web.MapReduce;
using Alachisoft.NCache.Runtime.Aggregation;
using Alachisoft.NCache.Web.Aggregation;
using Alachisoft.NCache.Runtime.Processor;
#if NETCORE
using System.Threading.Tasks;
#endif

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
    /// <summary>
    /// Implements the clustered cache for an application
    /// </summary>
    /// <remarks>
    /// One instance of this class is created per application domain, and it remains 
    /// valid as long as the application domain remains active. This object is accessible through the 
    /// <see cref="NCache.Cache"/> property of the <see cref="NCache"/> object.
    /// </remarks>
    /// <example>
    /// To create an instance of Cache class you can use code as follows:
    /// <code>
    /// Cache cache = NCache.InitializeCache("myReplicatedCache");
    /// Application[�CacheHandle�] = cache;
    /// </code>
    /// </example>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>    
    public partial class Cache : IEnumerable, IDisposable
    {
        /// <summary> Underlying implementation of NCache. </summary>
        private CacheImplBase _cacheImpl;

        private CacheConfig _config;
        private string _cacheId;


        private string _targetCacheUniqueID;

        /// <summary> Reference count of the cache. </summary>
        private int _refCount;

        /// <summary>Serialization context (actually name of the cache.)used for Compact Framework </summary>
        internal string _serializationContext;

        internal virtual string SerializationContext
        {
            get { return _serializationContext; }
            set { _serializationContext = value; }
        }

        internal virtual EventManager EventManager
        {
            get { return _eventManager; }
        }

        private bool s_exceptions = true;

        /// <summary> Contains callback ids and associated callbacks</summary>
        private ResourcePool _callbackIDsMap = new ResourcePool();

        /// <summary> Contains callbacks and associated callback ids. Actually reverse of the above pool.</summary>
        private ResourcePool _callbacksMap = new ResourcePool();

        /// <summary> Initial values for CustomRemovedCallback and CustomUpdateCallback ids. </summary>
        private short _rcbInitialVal = 0;

        private short _ucbInitialVal = 1000;

        internal event CacheClearedCallback _cacheCleared;
        private CacheClearedCallback _cqCacheClearCallback;
        internal event CacheStoppedCallback _cacheStopped;
        private int _refCacheStoppedCount = 0;
        private int _refClearCount = 0;
        private CacheAsyncEventsListenerBase _asyncListener;
        private CacheEventsListenerBase _listener;

        private EventManager _eventManager;

        internal PerfStatsCollector2 _perfStatsCollector;


        ///// <summary> Cache event listener object. Implements all events.</summary>
        internal ResourcePool _asyncCallbackIDsMap = new ResourcePool();
        internal ResourcePool _asyncCallbacksMap = new ResourcePool();

        private short _aiacbInitialVal = 2000;
        private short _aiucbInitialVal = 3000;
        private short _aircbInitialVal = 4000;
        private short _acccbInitialVal = 5000;
        private short _dsiacbInitialVal = 6000;
        private short _dsiucbInitialVal = 7000;
        private short _dsircbInitialVal = 8000;
        private short _dsccbInitialVal = 9000;


        private int _refAddCount = 0;
        private int _refUpdateCount = 0;
        private int _refRemoveCount = 0;
        private int _refCustomCount = 0;
        private GeneralDataNotificationWrapper _notificationWrapper;
        internal event CacheItemAddedCallback _itemAdded;
        internal event CacheItemUpdatedCallback _itemUpdated;
        internal event CacheItemRemovedCallback _itemRemoved;
        internal event CustomEventCallback _customEvent;


        private ArrayList _secondaryInprocInstances;
        private string _defaultReadThruProvider;
        private string _defaultWriteThruProvider;

        private string _cacheAlias;

        internal string CacheAlias
        {
            get { return _cacheAlias; }
            set { _cacheAlias = value; }
        }

        internal MessagingService _messagingService;

        private Hashtable _apiLogHastable = Hashtable.Synchronized(new Hashtable());

        internal Hashtable APILogHashTable
        {
            get { return _apiLogHastable; }
        }

        /// <summary>
        /// This flag is internally used by client cache when connected through backing source provider to Level 2 cache.
        /// When client cache is outproc and has binary data format, serialization is disabled so that object put/fetched from
        /// level 2 cache are not serialized/deserialized and put in client cache as is.
        /// </summary>
        internal bool InternalSerializationEnabled
        {
            get { return _serializationEnabled; }
            set { _serializationEnabled = value; }
        }


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
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString, null, Cache.NoAbsoluteExpiration, TimeSpan.FromSeconds(10));
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
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString, null, DateTime.Now.AddMinutes(2), Cache.NoSlidingExpiration);
        /// 
        /// </code>
        /// </example>
        public static readonly TimeSpan NoSlidingExpiration = TimeSpan.Zero;

        /// <summary>
        /// Used as the <paramref name="lockTimeout"/> parameter in Lock method call 
        /// to disable locking expirations. This field is read-only.
        /// </summary>
        /// <remarks>When used, this field sets the <paramref name="lockTimeout"/> parameter equal to the <see cref="TimeSpan.Zero"/>
        /// field, which has a constant value of zero. Then the cache item remains locked untill explicitly released.</remarks>
        /// <example>The following example demonstrates how to use the Lock method to add an item to the 
        /// <see cref="Cache"/> object using the <see cref="NoLockExpiration"/> field.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// LockHandle lockHandle = new LockHandle();
        ///	cache.Lock("DSN", Cache.NoLockExpiration, out lockHandle);
        /// 
        /// </code>
        /// </example>
        public static readonly TimeSpan NoLockExpiration = TimeSpan.Zero;

        /// <summary>
        /// A read only field, used to set expiration for cache entry while adding data to cache.
        /// Item will be expired from the cache within DefaultAbsolute value + cache clean up interval.
        /// The value for this field is given from NCache Manager.
        /// Minimum value for DefaultAbsolute expiration is 5s.
        ///</summary>
        /// <b>Note:</b> if DefaultAbsolute expiration is not enabled and given through API than item will be added with no expiration.
        /// <example>The following example demonstrates how to use DefaultAbsolute to add an item to the cache. 
        /// <code>
        ///Product product = new Product();
        ///product.ProductID = 1001;
        ///product.ProductName = "Chai";
        ///string key = "Product:" + product.ProductID;
        ///cache.Add(key, product, null, Cache.DefaultAbsolute, Cache.NoSlidingExpiration, CacheItemPriority.Default);
        /// </code>
        /// </example>
        public static readonly DateTime DefaultAbsolute = DateTime.MinValue.AddYears(1);

        /// <summary>
        /// A read only field, used to set expiration for cache entry while adding data to cache.
        /// Item will be expired from the cache after DefaultAbsoluteLonger value + cache clean up interval.
        /// The value for this field is given from NCache Manager.
        /// Minimum value for DefaultAbsoluteLonger expiration is 5s.
        ///</summary>
        /// <b>Note:</b> if DefaultAbsoluteLonger expiration is not enabled and given through API than item will be added with no expiration.
        /// <example>The following example demonstrates how to use DefaultAbsoluteLonger to add an item to the cache. 
        /// <code>
        ///Product product = new Product();
        ///product.ProductID = 1001;
        ///product.ProductName = "Coffee";
        ///string key = "Product:" + product.ProductID;
        ///cache.Add(key, product, null, Cache.DefaultAbsoluteLonger, Cache.NoSlidingExpiration, CacheItemPriority.Default);
        /// </code>
        /// </example>
        public static readonly DateTime DefaultAbsoluteLonger = DateTime.MinValue.AddYears(2);

        /// <summary>
        /// A read only field, used to set expiration for cache entry while adding data to cache.
        /// Item will be expired from the cache within DefaultSliding value + cache clean up interval, iff there is no fetching of that particular item.
        /// The value for this field is given from NCache Manager. 
        /// Minimum value for DefaultSliding expiration is 5s.
        ///</summary>
        /// <b>Note:</b> if DefaultSliding expiration is not enabled and given through API than item will be added with no expiration.
        /// <example>The following example demonstrates how to use DefaultSliding to add an item to the cache. 
        /// <code>
        ///Product product = new Product();
        ///product.ProductID = 1001;
        ///product.ProductName = "Tea";
        ///string key = "Product:" + product.ProductID;
        ///cache.Add(key, product, null, Cache.NoAbsoluteExpiration, Cache.DefaultSliding,  CacheItemPriority.Default);
        /// </code>
        /// </example>
        public static readonly TimeSpan DefaultSliding = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 1));

        /// <summary>
        /// A read only field, used to set expiration for cache entry while adding data to cache.
        /// Item will be expired from the cache within DefaultSlidingLonger value + cache clean up interval, iff there is no fetching of that particular item.
        /// The value for this field is given from NCache Manager. 
        /// Minimum value for DefaultSlidingLonger expiration is 5s.
        ///</summary>
        /// <b>Note:</b> if DefaultSlidingLonger expiration is not enabled and given through API than item will be added with no expiration.
        /// <example>The following example demonstrates how to use DefaultSlidingLonger to add an item to the cache. 
        /// <code>
        ///Product product = new Product();
        ///product.ProductID = 1001;
        ///product.ProductName = "Tea";
        ///string key = "Product:" + product.ProductID;
        ///cache.Add(key, product, null, Cache.NoAbsoluteExpiration, Cache.DefaultSlidingLonger,  CacheItemPriority.Default);
        /// </code>
        /// </example>
        public static readonly TimeSpan DefaultSlidingLonger = TimeSpan.MinValue.Add(new TimeSpan(0, 0, 2));

        private CacheEventDescriptor _addDiscriptor;
        private CacheEventDescriptor _updateDiscriptor;
        private CacheEventDescriptor _removeDiscriptor;
        internal CacheClientConnectivityChangedCallback _cacheClientConnectivityChanged;
        private bool _serializationEnabled = true;
        private TypeInfoMap _queryTypeMap;

        /// <summary>
        /// Occurs after the cache has been stopped.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform the tasks when a the cache has been stopped.
        /// <para>This callback is invoked when the cache has either been stopped intentionally or 
        /// connection with the server has been lost due to some reason so that you can connect to another 
        /// server.
        /// </para>
        /// For more information on how to use this callback see the documentation for <see cref="CacheStoppedCallback"/>.
        /// </remarks>
        public virtual event CacheStoppedCallback CacheStopped
        {
            add
            {
                _cacheStopped += value;
                if (CacheImpl != null && ++_refCacheStoppedCount == 1) CacheImpl.RegisterCacheStoppedEvent();
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_cacheStopped != null)
                    {
                        beforeLength = _cacheStopped.GetInvocationList().Length;
                        _cacheStopped -= value;

                        if (_cacheStopped != null)
                            afterLength = _cacheStopped.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refCacheStoppedCount == 0)
                                CacheImpl.UnregisterCacheStoppedEvent();
                    }
                }
            }
        }

        /// <summary>
        /// Occurs after the <see cref="Cache"/> is cleared.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform tasks when the <see cref="Cache"/> is cleared.
        /// <para>Since this callback is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
        /// of processing inside the callback might have an impact on the performance of the cache and cluster. It
        /// is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// For more information on how to use this callback see the documentation for <see cref="CacheClearedCallback"/>.
        /// </remarks>
        public virtual event CacheClearedCallback CacheCleared
        {
            add
            {
                _cacheCleared += value;
                if (CacheImpl != null && ++_refClearCount == 1) CacheImpl.RegisterClearEvent();
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_cacheCleared != null)
                    {
                        beforeLength = _cacheCleared.GetInvocationList().Length;
                        _cacheCleared -= value;

                        if (_cacheCleared != null)
                            afterLength = _cacheCleared.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refClearCount == 0)
                                CacheImpl.UnregisterClearEvent();
                    }
                }
            }
        }

        /// <summary>
        /// Event raised when a cache client connets to or disconnect from cache. (Raised only for outproc caches)
        /// </summary>
        public virtual event CacheClientConnectivityChangedCallback CacheClientConnectivityChanged
        {
            add
            {
                bool register = false;
                lock (this)
                {
                    _cacheClientConnectivityChanged += value;
                    register = _cacheClientConnectivityChanged.GetInvocationList().Length == 1;
                }

                if (CacheImpl != null && register) CacheImpl.RegisterCacheClientConnectivityEvent();
            }
            remove
            {
                bool unregister = false;
                lock (this)
                {
                    _cacheClientConnectivityChanged -= value;
                    unregister = _cacheClientConnectivityChanged == null ||
                                 _cacheClientConnectivityChanged.GetInvocationList().Length == 0;
                }

                if (CacheImpl != null && unregister) CacheImpl.UnregisterCacheClientConnectivityEvent();
            }
        }


        /// <summary>
        /// Occurs after an item has been added to the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform tasks when an item is added to the <see cref="Cache"/>.
        /// <para>Since this callback is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
        /// of processing inside the callback might have an impact on the performance of the cache and cluster. It
        /// is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// For more information on how to use this callback see the documentation for <see cref="CacheItemAddedCallback"/>.
        /// </remarks>
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(CacheDataNotificationCallback cacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)",
            false)]
        public virtual event CacheItemAddedCallback ItemAdded
        {
            add
            {
                _itemAdded += value;

                if (CacheImpl != null && ++_refAddCount == 1)
                {
                    CacheDataNotificationCallback wrappedNotification =
                        new CacheDataNotificationCallback(_notificationWrapper.OnCacheDataNotification);
                    _addDiscriptor = this.RegisterCacheNotification(wrappedNotification, EventType.ItemAdded,
                        EventDataFilter.None);
                }
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_itemAdded != null)
                    {
                        beforeLength = _itemAdded.GetInvocationList().Length;
                        _itemAdded -= value;

                        if (_itemAdded != null)
                            afterLength = _itemAdded.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refAddCount == 0)
                            {
                                if (_addDiscriptor != null) UnRegisterCacheNotification(_addDiscriptor);
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs after an item has been updated in the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform tasks when an item is updated in the <see cref="Cache"/>.
        /// <para>Since this callback is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
        /// of processing inside the callback might have an impact on the performance of the cache and cluster. It
        /// is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// For more information on how to use this callback see the documentation for <see cref="CacheItemUpdatedCallback"/>.
        /// </remarks>
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(CacheDataNotificationCallback cacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)",
            false)]
        public virtual event CacheItemUpdatedCallback ItemUpdated
        {
            add
            {
                _itemUpdated += value;
                if (CacheImpl != null && ++_refUpdateCount == 1)
                {
                    CacheDataNotificationCallback wrappedNotification =
                        new CacheDataNotificationCallback(_notificationWrapper.OnCacheDataNotification);
                    _updateDiscriptor = this.RegisterCacheNotification(wrappedNotification, EventType.ItemUpdated,
                        EventDataFilter.None);
                }
            }
            remove
            {
                int beforeLength, afterlength = 0;
                lock (this)
                {
                    if (_itemUpdated != null)
                    {
                        beforeLength = _itemUpdated.GetInvocationList().Length;
                        _itemUpdated -= value;

                        if (_itemUpdated != null)
                            afterlength = _itemUpdated.GetInvocationList().Length;

                        if (beforeLength - afterlength == 1)
                            if (CacheImpl != null && --_refUpdateCount == 0)
                            {
                                if (_updateDiscriptor != null) this.UnRegisterCacheNotification(_updateDiscriptor);
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs after an has been removed from the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>
        /// You can use this event to perform tasks when an item is removed from the <see cref="Cache"/>.
        /// <para>Since this callback is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
        /// of processing inside the callback might have an impact on the performance of the cache and cluster. It
        /// is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// <para><b>Note: </b>If an item is removed from the <see cref="Cache"/> for which a callback
        /// is supplied as parameter to <see cref="Add"/> or <see cref="Insert"/> method; that callback
        /// will be invoked instead of this callback.
        /// </para>
        /// For more information on how to use this callback see the documentation for <see cref="CacheItemRemovedCallback"/>.
        /// </remarks>
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(CacheDataNotificationCallback cacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)",
            false)]
        public virtual event CacheItemRemovedCallback ItemRemoved
        {
            add
            {
                _itemRemoved += value;
                if (CacheImpl != null && ++_refRemoveCount == 1)
                {
                    CacheDataNotificationCallback wrappedNotification =
                        new CacheDataNotificationCallback(_notificationWrapper.OnCacheDataNotification);
                    _removeDiscriptor = this.RegisterCacheNotification(wrappedNotification, EventType.ItemRemoved,
                        EventDataFilter.DataWithMetadata);
                }
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_itemRemoved != null)
                    {
                        beforeLength = _itemRemoved.GetInvocationList().Length;
                        _itemRemoved -= value;

                        if (_itemRemoved != null)
                            afterLength = _itemRemoved.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refRemoveCount == 0)
                            {
                                if (_removeDiscriptor != null) this.UnRegisterCacheNotification(_removeDiscriptor);
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs in response to a <see cref="Cache.RaiseCustomEvent"/> method call.
        /// </summary>
        /// <remarks>
        /// You can use this event to handle custom application defined event notifications.
        /// <para>Doing a lot of processing inside the handler might have an impact on the performance 
        /// of the cache and cluster. It is therefore advisable to do minimal processing inside the handler.
        /// </para>
        /// For more information on how to use this callback see the documentation 
        /// for <see cref="CustomEventCallback"/>.
        /// </remarks>
        public virtual event CustomEventCallback CustomEvent
        {
            add
            {
                _customEvent += value;
                if (CacheImpl != null && ++_refCustomCount == 1) CacheImpl.RegisterCustomEvent();
            }
            remove
            {
                int beforeLength, afterLength = 0;
                lock (this)
                {
                    if (_customEvent != null)
                    {
                        beforeLength = _customEvent.GetInvocationList().Length;
                        _customEvent -= value;

                        if (_customEvent != null)
                            afterLength = _customEvent.GetInvocationList().Length;

                        if (beforeLength - afterLength == 1)
                            if (CacheImpl != null && --_refCustomCount == 0)
                                CacheImpl.UnregisterCustomEvent();
                    }
                }
            }
        }


        internal virtual CacheAsyncEventsListenerBase AsyncListener
        {
            get { return _asyncListener; }
        }

        internal virtual CacheEventsListenerBase EventListener
        {
            get { return _listener; }
        }

        internal virtual ResourcePool CallbackIDsMap
        {
            get { return _callbackIDsMap; }
        }

        internal virtual ResourcePool CallbacksMap
        {
            get { return _callbacksMap; }
        }

        /// <summary>
        /// Get cache id.
        /// </summary>
        internal virtual string CacheId
        {
            get { return _cacheId; }
        }


        internal virtual CacheImplBase CacheImpl
        {
            get { return _cacheImpl; }
            set
            {
                _cacheImpl = value;
                if (_cacheImpl != null)
                {
                    _cacheId = _serializationContext = _cacheImpl.Name;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual IMessagingService MessagingService
        {
            get { return _messagingService; }
        }

        #endregion

        #region [Event Handles]

        internal CacheStoppedCallback CacheStoppedEventHandle
        {
            get
            {
                return _cacheStopped;
            }
        }

        internal CacheClearedCallback CacheClearedEventHandle
        {
            get
            {
                return _cacheCleared;
            }
        }

        internal CacheItemAddedCallback ItemAddedEventHandle
        {
            get
            {
                return _itemAdded;
            }
        }

        internal CacheItemUpdatedCallback ItemUpdatedEventHandle
        {
            get
            {
                return _itemUpdated;
            }
        }

        internal CacheItemRemovedCallback ItemRemovedEventHandle
        {
            get
            {
                return _itemRemoved;
            }
        }

        internal CustomEventCallback CustomEventHandle
        {
            get
            {
                return _customEvent;
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        internal Cache()
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);
            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListenerBase(this, _eventManager);
        }


        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="config"></param>
        internal Cache(CacheImplBase objectCache, CacheConfig config)
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);

            CacheImpl = objectCache;
            _config = config;
            _cacheId = config.CacheId;

            if (CacheImpl != null)
            {
                _serializationContext = CacheImpl.Name; //Sets the serialization context.
                _cacheId = CacheImpl.Name;
            }

            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListenerBase(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListenerBase(this);

            LoadRWTrhuSettings();
            AddRef();
            _messagingService = new MessagingService(_eventManager, null);
        }

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="cacheId"></param>        
        internal Cache(CacheImplBase objectCache, string cacheId, PerfStatsCollector2 perfStatsCollector)
        {
            _notificationWrapper = new GeneralDataNotificationWrapper(this);

            CacheImpl = objectCache;
            _cacheId = cacheId;
            if (CacheImpl != null)
            {
                _serializationContext = CacheImpl.Name; //Sets the serialization context.
            }

            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListenerBase(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListenerBase(this);

            _perfStatsCollector = perfStatsCollector;


            LoadRWTrhuSettings();
            AddRef();
            _messagingService = new MessagingService(_eventManager, perfStatsCollector);
        }

        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        ~Cache()
        {
            Dispose(false);
        }

        internal virtual void AddRef()
        {
            lock (this)
            {
                _refCount++;
            }
        }

        private void LoadRWTrhuSettings()
        {
            ClientConfiguration config = new ClientConfiguration(this._cacheId);
            config.LoadConfiguration();
            _defaultWriteThruProvider = config.DefaultWriteThru;
            _defaultReadThruProvider = config.DefaultReadThru;
        }


        internal virtual void AddSecondaryInprocInstance(Cache secondaryInstance)
        {
            if (_secondaryInprocInstances == null)
                _secondaryInprocInstances = new ArrayList();

            _secondaryInprocInstances.Add(secondaryInstance);
        }

        internal void SetMessagingServiceCacheImpl(CacheImplBase cacheImpl)
        {
            _messagingService.PubSubManager.CacheImpl = cacheImpl;
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
        private void Dispose(bool disposing)
        {
            try
            {
                lock (this)
                {
                    _refCount--;
                    if (_refCount > 0) return;
                    else if (_refCount < 0)
                        _refCount = 0;

                    // remove from the cache table.
                    string cacheIdWithAlias =
                        _cacheId + (string.IsNullOrEmpty(_cacheAlias) ? "" : "(" + _cacheAlias + ")");

                    lock (NCache.Caches)
                    {
                        if (cacheIdWithAlias != null)
                            NCache.Caches.Remove(cacheIdWithAlias);
                    }

                    if (_listener != null) _listener.Dispose();

                    if (CacheImpl != null) CacheImpl.Dispose(disposing);
                    CacheImpl = null;

                    if (_secondaryInprocInstances != null)
                    {
                        foreach (Cache cache in _secondaryInprocInstances)
                        {
                            cache.Dispose();
                        }
                    }

                    if (_perfStatsCollector != null)
                        _perfStatsCollector.Dispose();

                    if (disposing) GC.SuppressFinalize(this);
                }
            }
            catch (Exception ex)
            {
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
        public virtual void Dispose()
        {
            Dispose(true);
        }

#endregion

        /// <summary>
        /// Flag that indicates whether exceptions are enabled or not.
        /// </summary>
        /// <remarks>
        /// If this property is set the <see cref="Alachisoft.NCache.Web.Caching.Cache"/> object
        /// throws exceptions from public operations. If not set no exception is thrown and the
        /// operation fails silently. Setting this flag is especially helpful during 
        /// development phase of application since exceptions provide more information about
        /// the specific causes of failure. 
        /// </remarks>
        /// <value>true if exceptions are enabled, otherwise false.</value>
        /// <example> This sample shows how to set the <see cref="ExceptionsEnabled"/> property.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.ExceptionsEnabled = true;
        ///      
        /// </code>
        /// </example>
        public virtual bool ExceptionsEnabled
        {
            get
            {
                lock (this)
                {
                    return s_exceptions;
                }
            }
            set
            {
                lock (this)
                {
                    s_exceptions = value;
                }
            }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following examples demonstrates using this property to retrieve and insert the values of cached item.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// 
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{   
        ///		txtValue.Text = cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        ///		
        ///	void cmdReset_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		txtValue.Text = cache[txtName.Text].ToString();
        ///	}
        ///	
        ///	void cmdAdd_Click(object objSender, EventArgs objArgs)
        ///	{
        ///		if (txtName.Text != "")
        ///		{
        ///			// Add this item to the cache.
        ///			cache[txtName.Text] = txtValue.Text;
        ///		}
        ///	}
        /// 
        /// </code>
        /// </example>
        public virtual object this[string key]
        {
            get { return Get(key); }
            set { Insert(key, value); }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <value>The number of items stored in the cache.</value>

        [TargetMethod(1)]
        public virtual long Count
        {
            get
            {
                try
                {
                    if (TargetMethodAttribute.MethodOverload == 0)
                        TargetMethodAttribute.MethodOverload = 1;
                    if (CacheImpl != null) return CacheImpl.Count;
                }
                catch (Exception)
                {
                    if (ExceptionsEnabled) throw;
                }
                finally
                {
                    TargetMethodAttribute.MethodOverload = 0;
                }

                return 0;
            }
        }

#endregion

#region	/                 --- Clear ---           /

        /// <summary>
        /// Removes all elements from the <see cref="Cache"/>.
        /// </summary>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Clear();
        /// 
        /// </code>        
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void Clear()
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;

                Clear(DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes all elements from the client cache.
        /// </summary>                
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this function throws exception in case of failure.</para>        
        /// <example>The following example demonstrates how to clear the client cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.ClearClientCache();
        /// 
        /// </code>
        /// </example>
        /// <summary>
        /// Removes all elements from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="updateOpt">Options regarding updating data source</param>
        /// <param name="dataSourceClearedCallback">A delegate that, if provided, is called when
        /// data source is cleared.</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        /// OnDataSourceCleared(object result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Clear(DSWriteOption.WriteBehind, new DataSourceClearedCallback(OnDataSourceCleared));
        /// 
        /// </code>        
        /// </example>
        /// 
        [TargetMethod(2)]
        [Obsolete("This method is deprecated. 'Please use Clear()'", false)]
        public virtual void Clear(DSWriteOption updateOpt, DataSourceClearedCallback dataSourceClearedCallback)
        {
            if (CacheImpl == null) return;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                string providerName = null;

                //clear api not supported in DS providers
                updateOpt = DSWriteOption.None;

                BitSet flagMap = new BitSet();
                flagMap = this.SetDSUpdateOptBit(flagMap, updateOpt);

                short dsClearedCallbackId = -1;
                if (dataSourceClearedCallback != null)
                    dsClearedCallbackId = GetCallbackId(dataSourceClearedCallback);

                CacheImpl.Clear(flagMap, dsClearedCallbackId, providerName);

                if (_callbackIDsMap != null)
                    _callbackIDsMap.RemoveAllResources();

                if (_callbacksMap != null)
                    _callbacksMap.RemoveAllResources();
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Removes all elements from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// This is similar to <see cref="Clear()"/> except that the operation is performed asynchronously.
        /// A <see cref="CacheCleared"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// <param name="dataSourceClearedCallback">A delegate that, if provided, is called when
        /// data source is cleared.</param>
        /// <param name="onAsyncCacheClearCallback">A delegate that, that can be used to get the result of 
        /// the Asynchronous Clear operation.</param>
        /// <param name="updateOpt">Options regarding updating data source</param>
        /// </remarks>
        /// <example>The following example demonstrates how to clear the <see cref="Cache"/>.
        /// <code>
        /// 
        /// void OnAsyncCacheCleared(object result)
        /// {
        /// ...
        /// }
        /// Cache _cache = NCache.InitializeCache("myCache");
        ///	_cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
        /// 
        /// </code>        
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void ClearAsync(AsyncCacheClearedCallback onAsyncCacheClearCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                ClearAsync(DSWriteOption.None, onAsyncCacheClearCallback, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        [TargetMethod(2)]
        [Obsolete("This method is deprecated. 'Please use Clear()'", false)]
        public virtual void ClearAsync(DSWriteOption updateOpt, AsyncCacheClearedCallback onAsyncCacheClearCallback,
            DataSourceClearedCallback dataSourceClearedCallback)
        {
            if (CacheImpl == null) return;

            short asyncOperationCallbackID = -1;
            short dsClearedCallbackId = -1;
            string providerName = null;
            try
            {
                //clear api not supported in DS providers
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                updateOpt = DSWriteOption.None;

                if (_callbackIDsMap != null)
                    _callbackIDsMap.RemoveAllResources();

                if (_callbacksMap != null)
                    _callbacksMap.RemoveAllResources();

                if (onAsyncCacheClearCallback != null)
                {
                    asyncOperationCallbackID = GetCallbackId(onAsyncCacheClearCallback);
                }

                if (dataSourceClearedCallback != null)
                {
                    dsClearedCallbackId = GetCallbackId(dataSourceClearedCallback);
                }

                BitSet flagMap = new BitSet();
                flagMap = this.SetDSUpdateOptBit(flagMap, updateOpt);

                CacheImpl.ClearAsync(flagMap, asyncOperationCallbackID, dsClearedCallbackId, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="Cache"/>.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	if(cache.Contains("MyTextBox.Value"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// 
        /// </code>        
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual bool Contains(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            if (CacheImpl == null) return false;
            try
            {
                return CacheImpl.Contains(key);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return false;
        }

#endregion

        /// <summary>
        /// Broadcasts a custom application defined event.
        /// </summary>
        /// <param name="notifId">Application specific notification code/id</param>
        /// <param name="data">Application specific data</param>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// <para><b>Note:</b> Custom event notifications can not be disabled through configuration.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to raise a custom application defined event.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.RaiseCustomEvent(MyNotificationCodes.ConsumeItem,
        ///	      new ItemData(DateTime.Now));
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(1)]
        public virtual void RaiseCustomEvent(object notifId, object data)
        {
            BitSet flag = null;
            ValidateKeyValue(notifId, data);
            long size = 0;

            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                CacheImpl.RaiseCustomEvent(SafeSerialize(notifId, _serializationContext, ref flag, ref size),
                    SafeSerialize(data, _serializationContext, ref flag, ref size));
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void ValidateKeyValue(object key, object value)
        {
            Type type = typeof(ICompactSerializable);
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (key is string && (string) key == string.Empty)
                throw new ArgumentException("key cannot be empty string");
            if (!key.GetType().IsSerializable && !type.IsAssignableFrom(key.GetType()))
                throw new ArgumentException("key is not serializable");
        }

        /// <summary>
        /// produces a unique callback id for CacheItemRemovedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="removedCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(CacheItemRemovedCallback removedCallback)
        {
            if (removedCallback == null)
                return -1;

            return _eventManager.RegisterSelectiveCallback(removedCallback);
        }

        /// <summary>
        /// produces a unique callback id for CacheItemUpdatedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="updateCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(CacheItemUpdatedCallback updateCallback)
        {
            if (updateCallback == null)
                return -1;

            return _eventManager.RegisterSelectiveCallback(updateCallback);
        }


        /// <summary>
        /// produces a unique callback id for AsyncItemAddedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="asyncItemAddCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(AsyncItemAddedCallback asyncItemAddCallback)
        {
            if (asyncItemAddCallback == null)
                return -1;


            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(asyncItemAddCallback) == null)
                {
                    _aiacbInitialVal++;
                    _asyncCallbacksMap.AddResource(asyncItemAddCallback, _aiacbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_aiacbInitialVal, asyncItemAddCallback);
                    return _aiacbInitialVal;
                }
                else
                {
                    try
                    {
                        //get existing id from the table.
                        short aiacbId = (short) _asyncCallbacksMap.GetResource(asyncItemAddCallback);

                        //add it again into the table for updating ref count.
                        _asyncCallbacksMap.AddResource(asyncItemAddCallback, aiacbId);
                        _asyncCallbackIDsMap.AddResource(aiacbId, asyncItemAddCallback);
                        return aiacbId;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// produces a unique callback id for AsyncItemUpdatedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="asyncItemUpdateCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(AsyncItemUpdatedCallback asyncItemUpdateCallback)
        {
            if (asyncItemUpdateCallback == null)
                return -1;


            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(asyncItemUpdateCallback) == null)
                {
                    _aiucbInitialVal++;
                    _asyncCallbacksMap.AddResource(asyncItemUpdateCallback, _aiucbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_aiucbInitialVal, asyncItemUpdateCallback);
                    return _aiucbInitialVal;
                }
                else
                {
                    try
                    {
                        //get the existing id from the table.
                        short aiucbId = (short) _asyncCallbacksMap.GetResource(asyncItemUpdateCallback);

                        //add it again to update the ref count.
                        _asyncCallbacksMap.AddResource(asyncItemUpdateCallback, aiucbId);
                        _asyncCallbackIDsMap.AddResource(aiucbId, asyncItemUpdateCallback);
                        return aiucbId;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// produces a unique callback id for AsyncItemUpdatedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="asyncItemRemoveCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(AsyncItemRemovedCallback asyncItemRemoveCallback)
        {
            if (asyncItemRemoveCallback == null)
                return -1;


            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(asyncItemRemoveCallback) == null)
                {
                    _aircbInitialVal++;
                    _asyncCallbacksMap.AddResource(asyncItemRemoveCallback, _aircbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_aircbInitialVal, asyncItemRemoveCallback);
                    return _aircbInitialVal;
                }
                else
                {
                    try
                    {
                        //get existing id from the table.
                        short aircbId = (short) _asyncCallbacksMap.GetResource(asyncItemRemoveCallback);

                        //add it again to update the ref count.
                        _asyncCallbacksMap.AddResource(asyncItemRemoveCallback, aircbId);
                        _asyncCallbackIDsMap.AddResource(aircbId, asyncItemRemoveCallback);
                        return aircbId;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// produces a unique callback id for AsyncItemUpdatedCallback and
        /// stores the callback in the callback pools.
        /// </summary>
        /// <param name="asyncCacheClearCallback"></param>
        /// <returns>returns back the newly generated id.</returns>
        internal virtual short GetCallbackId(AsyncCacheClearedCallback asyncCacheClearCallback)
        {
            if (asyncCacheClearCallback == null)
                return -1;


            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(asyncCacheClearCallback) == null)
                {
                    _acccbInitialVal++;
                    _asyncCallbacksMap.AddResource(asyncCacheClearCallback, _acccbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_acccbInitialVal, asyncCacheClearCallback);
                    return _acccbInitialVal;
                }
                else
                {
                    try
                    {
                        //get existing id from the table.
                        short acccbId = (short) _asyncCallbacksMap.GetResource(asyncCacheClearCallback);

                        //add it again for updating the ref count.
                        _asyncCallbacksMap.AddResource(asyncCacheClearCallback, acccbId);
                        _asyncCallbackIDsMap.AddResource(acccbId, asyncCacheClearCallback);
                        return acccbId;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        internal virtual short GetCallbackId(DataSourceItemsAddedCallback dsItemAddedCallback)
        {
            if (dsItemAddedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemAddedCallback) == null)
                {
                    _dsiacbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemAddedCallback, _dsiacbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_dsiacbInitialVal, dsItemAddedCallback);
                    return _dsiacbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemAddedCallback);
                        _asyncCallbacksMap.AddResource(dsItemAddedCallback, dsiacb);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemAddedCallback);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        internal virtual short GetCallbackId(DataSourceItemsAddedCallback dsItemAddedCallback, int numberOfCallbacks)
        {
            if (dsItemAddedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemAddedCallback) == null)
                {
                    _dsiacbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemAddedCallback, _dsiacbInitialVal, numberOfCallbacks);
                    _asyncCallbackIDsMap.AddResource(_dsiacbInitialVal, dsItemAddedCallback, numberOfCallbacks);
                    return _dsiacbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemAddedCallback);
                        _asyncCallbacksMap.AddResource(dsItemAddedCallback, dsiacb, numberOfCallbacks);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemAddedCallback, numberOfCallbacks);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }
        
        internal virtual short GetCallbackId(DataSourceItemsUpdatedCallback dsItemUpdatedCallback)
        {
            if (dsItemUpdatedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemUpdatedCallback) == null)
                {
                    _dsiucbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemUpdatedCallback, _dsiucbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_dsiucbInitialVal, dsItemUpdatedCallback);
                    return _dsiucbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemUpdatedCallback);
                        _asyncCallbacksMap.AddResource(dsItemUpdatedCallback, dsiacb);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemUpdatedCallback);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        internal virtual short GetCallbackId(DataSourceItemsUpdatedCallback dsItemUpdatedCallback,
            int numberOfCallbacks)
        {
            if (dsItemUpdatedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemUpdatedCallback) == null)
                {
                    _dsiucbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemUpdatedCallback, _dsiucbInitialVal, numberOfCallbacks);
                    _asyncCallbackIDsMap.AddResource(_dsiucbInitialVal, dsItemUpdatedCallback, numberOfCallbacks);
                    return _dsiucbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemUpdatedCallback);
                        _asyncCallbacksMap.AddResource(dsItemUpdatedCallback, dsiacb, numberOfCallbacks);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemUpdatedCallback, numberOfCallbacks);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }
        
        internal virtual short GetCallbackId(DataSourceItemsRemovedCallback dsItemRemovedCallback)
        {
            if (dsItemRemovedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemRemovedCallback) == null)
                {
                    _dsircbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemRemovedCallback, _dsircbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_dsircbInitialVal, dsItemRemovedCallback);
                    return _dsircbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemRemovedCallback);
                        _asyncCallbacksMap.AddResource(dsItemRemovedCallback, dsiacb);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemRemovedCallback);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        internal virtual short GetCallbackId(DataSourceItemsRemovedCallback dsItemRemovedCallback,
            int numberOfCallbacks)
        {
            if (dsItemRemovedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsItemRemovedCallback) == null)
                {
                    _dsircbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsItemRemovedCallback, _dsircbInitialVal, numberOfCallbacks);
                    _asyncCallbackIDsMap.AddResource(_dsircbInitialVal, dsItemRemovedCallback, numberOfCallbacks);
                    return _dsircbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsItemRemovedCallback);
                        _asyncCallbacksMap.AddResource(dsItemRemovedCallback, dsiacb, numberOfCallbacks);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsItemRemovedCallback, numberOfCallbacks);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }
        
        internal virtual short GetCallbackId(DataSourceClearedCallback dsClearedCallback)
        {
            if (dsClearedCallback == null) return -1;

            if (_asyncCallbackIDsMap == null)
                _asyncCallbackIDsMap = new ResourcePool();

            if (_asyncCallbacksMap == null)
                _asyncCallbacksMap = new ResourcePool();

            while (true)
            {
                if (_asyncCallbacksMap.GetResource(dsClearedCallback) == null)
                {
                    _dsccbInitialVal++;
                    _asyncCallbacksMap.AddResource(dsClearedCallback, _dsccbInitialVal);
                    _asyncCallbackIDsMap.AddResource(_dsccbInitialVal, dsClearedCallback);
                    return _dsccbInitialVal;
                }
                else
                {
                    try
                    {
                        short dsiacb = (short) _asyncCallbacksMap.GetResource(dsClearedCallback);
                        _asyncCallbacksMap.AddResource(dsClearedCallback, dsiacb);
                        _asyncCallbackIDsMap.AddResource(dsiacb, dsClearedCallback);
                        return dsiacb;
                    }
                    catch (NullReferenceException)
                    {
                        continue;
                    }
                }
            }
        }

        internal virtual TypeInfoMap GetQueryTypeMap()
        {
            return CacheImpl != null ? CacheImpl.TypeMap : null;
        }

        internal virtual void SetQueryTypeInfoMap(TypeInfoMap typeMap)
        {
            _queryTypeMap = typeMap;
        }

        private Hashtable GetQueryInfo(Object value)
        {
            Hashtable queryInfo = null;

            TypeInfoMap typeMap = _queryTypeMap != null ? _queryTypeMap : CacheImpl.TypeMap;

            if (typeMap == null)
                return null;

            try
            {
                string typeName = value.GetType().FullName;
                typeName = typeName.Replace("+", ".");

                int handleId = typeMap.GetHandleId(typeName);
                if (handleId != -1)
                {
                    queryInfo = new Hashtable();
                    Type valType = null; // (Cattering Case-InSensetive string comparisons.
                    ArrayList attribValues = new ArrayList();
                    ArrayList attributes = typeMap.GetAttribList(handleId);
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        PropertyInfo propertyAttrib = value.GetType().GetProperty((string) attributes[i]);
                        if (propertyAttrib != null)
                        {
                            Object attribValue = propertyAttrib.GetValue(value, null);
                            //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                            attribValues.Add(attribValue);
                        }
                        else
                        {
                            FieldInfo fieldAttrib = value.GetType().GetField((string) attributes[i],
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                BindingFlags.FlattenHierarchy);
                            if (fieldAttrib != null)
                            {
                                Object attribValue = fieldAttrib.GetValue(value);
                                //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                                attribValues.Add(attribValue);
                            }
                            else
                            {
                                throw new Exception("Unable extracting query information from user object.");
                            }
                        }
                    }

                    queryInfo.Add(handleId, attribValues);
                }
            }
            catch (Exception)
            {
            }

            return queryInfo;
        }

        private BitSet SetDSUpdateOptBit(BitSet flagMap, DSWriteOption dsWriteOption)
        {
            if (flagMap == null) flagMap = new BitSet();
            switch (dsWriteOption)
            {
                case DSWriteOption.WriteBehind:
                    flagMap.SetBit(BitSetConstants.WriteBehind);
                    break;
                case DSWriteOption.WriteThru:
                    flagMap.SetBit(BitSetConstants.WriteThru);
                    break;
            }

            return flagMap;
        }

        private BitSet SetDSReadOptBit(BitSet flagMap, DSReadOption dsReadOption)
        {
            if (flagMap == null) flagMap = new BitSet();
            if (dsReadOption == DSReadOption.ReadThru) flagMap.SetBit(BitSetConstants.ReadThru);
            return flagMap;
        }

        private Hashtable GetTagInfo(object value, Tag[] tags)
        {
            if (tags == null
                || tags.Length == 0)
                return null;


            Hashtable tagInfo = new Hashtable();
            ArrayList tagsList = new ArrayList();
            foreach (Tag tag in tags)
            {
                if (tag == null)
                {
                    throw new ArgumentNullException("Tag cannot be null.");
                }

                if (tag.TagName == null)
                {
                    throw new ArgumentNullException("Tag cannot be null.");
                }

                else if (!tagsList.Contains(tag.TagName))
                {
                    tagsList.Add(tag.TagName);
                }
            }

            string typeName = value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            tagInfo["type"] = typeName;
            tagInfo["tags-list"] = tagsList;


            return tagInfo;
        }

#region	/                 --- Add ---           /

        /// <summary>
        /// Add dependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="dependency">CacheDependency to be added</param>
        /// <param name="isResyncRequired">If set, then at the time of expiration, a fresh copy of the 
        /// item is fetched from the master datasource provided that the read-thru is enabled. Otherwise it is 
        /// expired as normal.</param>
        /// <returns>True if the operation succeeded otherwise false</returns>
        [TargetMethod(1)]
        public virtual bool AddDependency(string key, CacheDependency dependency, bool isResyncRequired)
        {
            if (key == null) throw new ArgumentNullException();

            if (dependency == null) throw new ArgumentNullException();
            if (!dependency.GetType().IsSerializable)
                throw new ArgumentException("dependency is not serializable");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return CacheImpl.AddDependency(key, dependency, isResyncRequired);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add CacheSyncDependency to the cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="syncDependency">CacheSyncDependency to be added</param>
        /// <returns>True if the operation succeeded otherwise false</returns>
        [TargetMethod(2)]
        [Obsolete(
            "This method is deprecated. 'Please use AddDependency(string key, CacheDependency dependency, bool isResyncRequired)'",
            false)]
        public virtual bool AddDependency(string key, CacheSyncDependency syncDependency)
        {
            if (key == null) throw new ArgumentNullException("key");

            if (syncDependency == null) throw new ArgumentNullException("syncDependency");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            try
            {
                return CacheImpl.AddDependency(key, syncDependency);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Add Attribute existing cache item.
        /// </summary>
        /// <param name="key">key used to reference the required object</param>
        /// <param name="attributes">Set of attribute to be added</param>
        /// <returns>True if the operation succeeded otherwise false</returns>
        [TargetMethod(1)]
        public virtual bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException();

            if (attributes == null) throw new ArgumentNullException();
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return CacheImpl.SetAttributes(key, attributes);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Adds an item into the Cache object with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("DSN", connectionString);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(1)]
        public virtual CacheItemVersion Add(string key, object value)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                object itemVersion = AddOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, null, null, null, null,
                    null, null, null, null, EventDataFilter.None, EventDataFilter.None,
                    ref size, true, null, -1, -1, -1);

                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }

        /// <summary>
        /// Adds an item into the Cache object with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration. It also 
        /// enables the associating tags with the object.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name=
        /// >An array of <see cref="Tag"/> to associate with the object.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[1];
        /// tags[0] = new Tag("tag-name");
        ///	cache.Add("DSN", connectionString, tags);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(2)]
        public virtual CacheItemVersion Add(string key, object value, Tag[] tags)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                object itemVersion = AddOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, null, null, tags,
                    null, null, null, null, null, EventDataFilter.None,
                    EventDataFilter.None, ref size, true, null, -1, -1, -1);
                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary>
        /// Adds an item into the Cache object with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration. It also 
        /// enables associating named tags with the object.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="namedTags">A <see cref="NamedTagsDictionary"/> to associate with the object.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// NamedTagsDictionary namedTags = new NamedTagsDictionary();
        /// namedTags.Add("Attribute", "value");
        ///	cache.Add("DSN", connectionString, namedTags);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(3)]
        public virtual CacheItemVersion Add(string key, object value, NamedTagsDictionary namedTags)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;
                object itemVersion = AddOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None, null, null,
                    null, false, null, null, null, null, null, namedTags, null, null,
                    EventDataFilter.None, EventDataFilter.None, ref size, true, null, -1, -1, -1);
                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary> Adds a value to the cache </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The value that is to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">The data group of the item</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <example>
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("DSN", value, "CONNECTIONS", null);
        /// 
        /// </code>        
        /// </example>    
        [TargetMethod(4)]
        public virtual CacheItemVersion Add(string key, object value, string group, string subGroup)

        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 4;
                object itemVersion = AddOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, group, subGroup,
                    null, null, null, null, null, null,
                    EventDataFilter.None, EventDataFilter.None,
                    ref size, true, null, -1, -1, -1);
                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary>
        /// When any dependency changes, the object becomes invalid and is removed from 
        /// the cache. If there are no dependencies, this parameter contains a null 
        /// reference (Nothing in Visual Basic).</param>
        /// <param name="absoluteExpiration">The time at which the added object expires 
        /// and is removed from the cache.</param>
        /// <param name="slidingExpiration">The interval between the time the added object was last 
        /// accessed and when that object expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// Calls to this method will fail if an item with the same key parameter is 
        /// already in the Cache. To overwrite an existing Cache item using the 
        /// same key parameter, use the Insert method.
        /// <para><b>Note:</b> If the key or value parameter is null, an <see cref="ArgumentNullException"/> is 
        /// thrown. If you set the <paramref name="slidingExpiration"/> parameter to less than TimeSpan.Zero, 
        /// or the equivalent of more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown. 
        /// You cannot set both sliding and absolute expirations on the same cached item. 
        /// If you do so, an <see cref="ArgumentException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item to the cache with no absolute 
        /// expiration, a sliding expiration of 5 minutes and with a priority of 
        /// high.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("timestamp", timeStamp, null, Cache.NoAbsoluteExpiration, new TimeSpan(0,5,0), CacheItemPriority.High);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(5)]
        public virtual CacheItemVersion Add(string key, object value, CacheDependency dependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                object itemVersion = AddOperation(key, value, dependency, null,
                    absoluteExpiration, slidingExpiration, priority,
                    DSWriteOption.None, null, null, null, false,
                    null, null, null, null, null, null, null, null,
                    EventDataFilter.None, EventDataFilter.None,
                    ref size, true, null, -1, -1, -1);

                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }

        /// <summary>
        /// <param name="key">Key of the item to be added</param>
        /// <param name="value">value of the item to be added</param>
        /// <param name="absoluteExpiration">The time at which the added object expires 
        /// and is removed from the cache.</param>
        /// <param name="slidingExpiration">The interval between the time the added object was last 
        /// accessed and when that object expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// Calls to this method will fail if an item with the same key parameter is 
        /// already in the Cache. To overwrite an existing Cache item using the 
        /// same key parameter, use the Insert method.
        /// <para><b>Note:</b> If the key or value parameter is null, an <see cref="ArgumentNullException"/> is 
        /// thrown. If you set the <paramref name="slidingExpiration"/> parameter to less than TimeSpan.Zero, 
        /// or the equivalent of more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown. 
        /// You cannot set both sliding and absolute expirations on the same cached item. 
        /// If you do so, an <see cref="ArgumentException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item to the cache with no absolute 
        /// expiration, a sliding expiration of 5 minutes and with a priority of 
        /// high.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("timestamp", timeStamp, Cache.NoAbsoluteExpiration, new TimeSpan(0,5,0), CacheItemPriority.High);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(6)]
        public virtual CacheItemVersion Add(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 6;

                return Add(key, value, null, absoluteExpiration, slidingExpiration, priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to add an item to the cache with an absolute 
        /// expiration of 2 minutes from now, a priority of high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// object someData = new object();
        /// CacheItem item = new CacheItem(someData);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("someData", item);
        ///
        /// </code>        
        /// </example>
        [TargetMethod(7)]
        public virtual CacheItemVersion Add(string key, CacheItem item)

        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 7;
                return Add(key, item, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemAdded">A delegate that, if provided, is called when
        /// item is added to data source.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to add an item to the cache with an absolute 
        /// expiration of 2 minutes from now, a priority of high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// object someData = new object();
        /// CacheItem item = new CacheItem(someData);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("someData", item, DSWriteOption.WriteBehind, new DataSourceItemsAddedCallback(OnDataSourceItemAdded));
        ///
        /// </code>        
        /// </example>
        [TargetMethod(8)]
        public virtual CacheItemVersion Add(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            try
            {
                long size = 0;

                if (item == null)
                    throw new ArgumentNullException("CacheItem");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 8;

                object itemVersion = AddOperation(key, item.Value, item.Dependency,
                    item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    onDataSourceItemAdded,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags, null, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="providerName">A unique identifier for the data source</param>
        /// <param name="onDataSourceItemAdded">A delegate that, if provided, is called when
        /// item is added to data source</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        [TargetMethod(9)]
        public virtual CacheItemVersion Add(string key, CacheItem item, DSWriteOption dsWriteOption,
            string providerName, DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            try
            {
                long size = 0;

                if (item == null)
                    throw new ArgumentNullException("CacheItem");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 9;

                object itemVersion = AddOperation(key, item.Value, item.Dependency,
                    item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    onDataSourceItemAdded,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags, providerName, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
                return new CacheItemVersion((ulong) itemVersion);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsAdded">A delegate that, if provided, is called when
        /// item is added to data source.</param>
        /// <returns>keys that are added or that already exists in the cache and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItems.
        /// <code>
        /// string keys = {"First", "Second"};
        /// CacheItem items = new CacheItem[2]
        /// items[0] = new CacheItem(firstTimeStamp);
        /// items[0].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[0].Priority = CacheItemPriority.High;
        /// items[0].ItemRemoveCallback = onRemove;
        ///
        /// items[1] = new CacheItem(secondTimeStamp);
        /// items[1].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[1].Priority = CacheItemPriority.Low;
        /// items[1].ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add(keys, items, DSWriteOption.WriteBehind, new DataSourceItemsAddedCallback(OnDataSourceItemAdded));
        ///
        /// </code>        
        /// </example>
        [TargetMethod(1)]
        public virtual IDictionary AddBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded)
        {
            long[] sizes = new long[items.Length];
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;

                IDictionary itemVersions = null;

                return AddBulkOperation(keys, items, dsWriteOption, onDataSourceItemsAdded,
                    null, ref sizes, true, null, -1, -1, -1,
                    EventDataFilter.None, EventDataFilter.None, false, out itemVersions,
                    Runtime.Events.CallbackType.PushBasedNotification);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>keys that are added or that already exists in the cache and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItems.
        /// <code>
        /// string keys = {"First", "Second"};
        /// CacheItem items = new CacheItem[2]
        /// items[0] = new CacheItem(firstTimeStamp);
        /// items[0].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[0].Priority = CacheItemPriority.High;
        /// items[0].ItemRemoveCallback = onRemove;
        ///
        /// items[1] = new CacheItem(secondTimeStamp);
        /// items[1].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// items[1].Priority = CacheItemPriority.Low;
        /// items[1].ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add(keys, items);
        ///
        /// </code>        
        /// </example>
        /// 
        [TargetMethod(2)]
        public virtual IDictionary AddBulk(string[] keys, CacheItem[] items)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                return AddBulk(keys, items, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="providerName">A unique identifier for the data source</param>
        /// <param name="onDataSourceItemsAdded">A delegate that, if provided, is called when
        /// item is added to data source</param>
        /// <returns>keys that are added or that already exists in the cache and their status.</returns>
        [TargetMethod(3)]
        public virtual IDictionary AddBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, string providerName, DataSourceItemsAddedCallback onDataSourceItemsAdded)
        {
            try
            {
                long[] sizes = new long[items.Length];
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;

                IDictionary itemVersions = null;

                return AddBulkOperation(keys, items, dsWriteOption,
                    onDataSourceItemsAdded, providerName, ref sizes, true, null, -1, -1, -1,
                    EventDataFilter.None, EventDataFilter.None, false, out itemVersions,
                    Runtime.Events.CallbackType.PushBasedNotification);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add the object to the cache asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The value that is to be stored</param>
        /// <param name="onAsyncItemAddCallback">A delegate that, if provided, is called to 
        /// inform about the status of the operation.</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">The subGroup of the item</param>
        /// <example>The following example demonstrates how to add an object to the cache with 
        /// group, subGroup and a delegate that notifies the application about the operation status.
        /// 
        /// First create the AsyncItemAddedCallback.
        /// <code>
        /// AsyncItemAddedCallback asyncItemAdded = new AsyncItemAddedCallback(OnAsyncItemAdded);
        /// 
        /// OnAsyncItemAdded(string key, object result)
        /// {
        /// ...
        /// }
        /// </code>
        /// 
        /// Then add the object to the cache.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.AddAsync("timestamp", timestamp, asyncItemAdded, "group-name", "subGroup-name");
        ///
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual void AddAsync(string key, object value, AsyncItemAddedCallback onAsyncItemAddCallback,
            string group, string subGroup)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;

                AddAsyncOperation(key, value,
                    null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default,
                    DSWriteOption.None,
                    null, null,
                    onAsyncItemAddCallback,
                    null, false,
                    group, subGroup, null,
                    null, null, null,
                    null, null,
                    EventDataFilter.None, EventDataFilter.None,
                    null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache asynchronously
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemAdded">A delegate that, if provided, is called when
        /// item is added to data source.</param>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to add an item to the cache with a sliding expiration of 5 minutes, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// CacheItem item = new CacheItem(timeStamp);
        /// item.SlidingExpiration = new TimeSpan(0,5,0);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// 
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.AddAsync("timestamp", item, DSWriteOption.WriteBehind, new DataSourceItemsAddedCallback(OnDataSourceItemAdded));
        ///
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual void AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            try
            {
                if (item == null)
                    throw new ArgumentNullException("CacheItem");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                AddAsyncOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    item.AsyncItemAddCallback, onDataSourceItemAdded,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags, null, item.ResyncProviderName,
                    item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="providerName">A unique identifier for the data source</param>
        /// <param name="onDataSourceItemAdded">A delegate that, if provided, is called when
        /// item is added to data source.</param></param>
        /// <returns></returns>
        [TargetMethod(3)]
        public virtual void AddAsync(string key, CacheItem item, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsAddedCallback onDataSourceItemAdded)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    if (TargetMethodAttribute.MethodOverload == 0)
                        TargetMethodAttribute.MethodOverload = 3;
                AddAsyncOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    item.AsyncItemAddCallback,
                    onDataSourceItemAdded,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags,
                    providerName,
                    item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        internal virtual object AddOperation(string key, object value, CacheDependency dependency,
            CacheSyncDependency syncDependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption, CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback, DataSourceItemsAddedCallback onDataSourceItemAdded,
            bool isResyncExpiredItems, string group, string subGroup, Tag[] tags, string providerName,
            string resyncProviderName, NamedTagsDictionary namedTags,
            CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter,
            EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId,
            short updateCallbackID, short removeCallbackID, short dsItemAddedCallbackID)

        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");

            Hashtable queryInfo = null;
            BitSet flagMap = new BitSet();

            ValidateKeyValue(key, value);


            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }


            if (dependency != null && !dependency.GetType().IsSerializable)
                throw new ArgumentException("dependency is not serializable");

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();
            if (!string.IsNullOrEmpty(resyncProviderName))
                resyncProviderName = resyncProviderName.ToLower();

            UsageStats stats = new UsageStats();
            stats.BeginSample();


            if (queryInfo == null)
            {
                queryInfo = new Hashtable();
                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(value);
                    queryInfo["named-tag-info"] = GetNamedTagsInfo(value, namedTags);
                }

                if (allowQueryTags)
                    queryInfo["tag-info"] = GetTagInfo(value, tags);
            }


            try
            {
                long objectSize = 0;
                long encryptedObjectSize = 0;
                long compressedObjectSize = 0;


                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[]) value).Length : 0;


                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) value).Length);


                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                if (DebugAPIConfiguraions.LoggingEnabled)
                {
                    LogSizeInfo(false, objectSize);
                }

                if (removeCallbackID == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback,
                            EventType.ItemRemoved, itemRemovedDataFilter);
                        removeCallbackID = callabackIds[1];
                    }
                    else if (onRemoveCallback != null && cacheItemRemovedCallaback == null)
                    {
                        removeCallbackID = GetCallbackId(onRemoveCallback);
                        //old notification expects data
                        itemRemovedDataFilter = EventDataFilter.DataWithMetadata;
                    }
                }

                if (updateCallbackID == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback,
                            EventType.ItemUpdated, itemUpdateDataFilter);
                        updateCallbackID = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackID = GetCallbackId(onUpdateCallback);
                        itemUpdateDataFilter = EventDataFilter.None;
                    }
                }

                if (dsItemAddedCallbackID == -1)
                {
                    if (onDataSourceItemAdded != null)
                        dsItemAddedCallbackID = GetCallbackId(onDataSourceItemAdded);
                }

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration &&
                    slidingExpiration != null && slidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both absolute and sliding expirations on a single item");
                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration)
                    absoluteExpiration = ToUTC(absoluteExpiration);

                object itemVersion = CacheImpl.Add(key, value,
                    dependency, syncDependency, absoluteExpiration,
                    slidingExpiration, priority, removeCallbackID,
                    updateCallbackID, dsItemAddedCallbackID,
                    isResyncExpiredItems, group, subGroup,
                    queryInfo, flagMap, providerName,
                    resyncProviderName, itemUpdateDataFilter,
                    itemRemovedDataFilter, size, clientId);


                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerAddSample(stats.Current);
                    _perfStatsCollector.IncrementAddPerSecStats();
                }


                return itemVersion;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
        }

        /// <summary>
        /// Converts provided DateTime to UTC; All clients and server will talk in UTC to avoid geographically seperated servers and clients
        /// 
        /// <para>MSDN Note: On Windows XP systems, the ToLocalTime method recognizes only the current adjustment rule when converting from UTC to local time.
        ///As a result, conversions for periods before the current adjustment rule came into effect may not accurately reflect the difference between UTC and local time.
        ///Link: http://stackoverflow.com/questions/4960303/datetime-tolocaltime-on-windows-xp </para>
        /// </summary>
        /// <param name="date">DateTime to change to UTC</param>
        /// <returns>UTC timeone DateTime</returns>
        private DateTime ToUTC(DateTime date)
        {
            if (date != null && date != Cache.NoAbsoluteExpiration)
                return date.ToUniversalTime();
            return date;
        }

        private object GetNamedTagsInfo(object value, NamedTagsDictionary namedTags)
        {
            CheckDuplicateIndexName(value, namedTags);

            if (namedTags == null || namedTags.Count == 0)
                return null;

            Hashtable tagInfo = new Hashtable();
            Hashtable tagsList = new Hashtable();

            foreach (DictionaryEntry nameValue in namedTags)
            {
                if (nameValue.Key != null && string.IsNullOrEmpty(nameValue.Key.ToString().Trim()))
                    throw new ArgumentException("Named Tag key cannot be null or empty");

                if (nameValue.Value == null)
                {
                    throw new ArgumentNullException("Named Tag value cannot be null");
                }

                tagsList.Add(nameValue.Key, nameValue.Value);
            }

            string typeName = value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            tagInfo["type"] = typeName;
            tagInfo["named-tags-list"] = tagsList;

            return tagInfo;
        }

        private void CheckDuplicateIndexName(object value, NamedTagsDictionary namedTags)
        {
            TypeInfoMap typeMap = _queryTypeMap != null ? _queryTypeMap : CacheImpl.TypeMap;

            if (namedTags == null || value == null || typeMap == null)
            {
                return;
            }

            string typeName = value.GetType().FullName;
            typeName = typeName.Replace("+", ".");

            int handleId = typeMap.GetHandleId(typeName);
            if (handleId != -1)
            {
                ArrayList attributes = typeMap.GetAttribList(handleId);
                foreach (string name in attributes)
                {
                    if (namedTags.Contains(name)) // whether this should be case insensitive
                    {
                        throw new Exception(
                            "Key in named tags conflicts with the indexed attribute name of the specified object.");
                    }
                }
            }
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">Sub group of the group</param>
        /// <param name="dsUpdateOpts">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsAdded">A delegate that, if provided, is called when
        /// item is added to data source.</param>
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
        /// OnDataSourceItemAdded(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add(keys, items, "Customer", "Orders", DataSourceUpdateOpt.WriteBehind, new DataSourceItemsAddedCallback(OnDataSourceItemAdded));
        ///
        /// </code>        
        /// </example>
        internal virtual IDictionary AddBulkOperation(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsAddedCallback onDataSourceItemsAdded,
            string providerName, ref long[] sizes, bool allowQueryTags, string clientId,
            short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackID,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter,
            bool returnVersions, out IDictionary itemVersions,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (items == null) throw new ArgumentNullException("items");
            if (keys.Length != items.Length) throw new ArgumentException("keys count is not equals to items count");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            if (FindDupliate(keys)) throw new ArgumentException("Duplicate keys found in provided 'key' array.");

            itemVersions = null;

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            CacheItem[] clonedItems = new CacheItem[items.Length];

            UsageStats stats = new UsageStats();
            stats.BeginSample();


            long sumObjectSize = 0;

            for (int i = 0; i < items.Length; i++)
            {
                if (keys[i] == null)
                {
                    throw new ArgumentException("Key cannot be null");
                }

                if (items[i] == null)
                {
                    throw new ArgumentException("CacheItem cannot be null");
                }

                if (items[i].SubGroup != null && items[i].Group == null)
                {
                    throw new ArgumentException("group must be specified for sub group");
                }

                string key = keys[i];
                BitSet flagMap = new BitSet();

                long size = 0;
                if (sizes[i] > 0)
                    size = sizes[i];

                CacheItem cloned = items[i].Clone() as CacheItem;
                if (cloned == null) throw new ArgumentNullException("items[" + i + "]");

                Hashtable queryInfo = null;


                ValidateKeyValue(key, cloned.Value);

                if (queryInfo == null)
                {
                    queryInfo = new Hashtable();
                    if (allowQueryTags)
                    {
                        queryInfo["query-info"] = GetQueryInfo(cloned.Value);
                        queryInfo["tag-info"] = GetTagInfo(cloned.Value, cloned.Tags);
                        queryInfo["named-tag-info"] = GetNamedTagsInfo(cloned.Value, cloned.NamedTags);
                    }
                }

                cloned.QueryInfo = queryInfo;

                cloned.Value = SafeSerialize(cloned.Value, _serializationContext, ref flagMap, ref size);
                sizes[i] = size;
                if (DebugAPIConfiguraions.LoggingEnabled)
                    sumObjectSize += cloned.Value is byte[] ? ((byte[]) cloned.Value).Length : 0;


                if (_perfStatsCollector != null)
                {
                    if (cloned.Value != null && cloned.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[]) cloned.Value).Length);
                }


                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);
                cloned.FlagMap = flagMap;
                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != Cache.NoAbsoluteExpiration &&
                    cloned.SlidingExpiration != null && cloned.SlidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both absolute and sliding expirations on a single item");
                cloned.AbsoluteExpiration = ToUTC(cloned.AbsoluteExpiration);

                clonedItems[i] = cloned;
            }

            if (DebugAPIConfiguraions.LoggingEnabled)
                LogSizeInfo(true, sumObjectSize);

            if (dsItemAddedCallbackID == -1)
            {
                if (onDataSourceItemsAdded != null)
                    dsItemAddedCallbackID = GetCallbackId(onDataSourceItemsAdded, keys.Length);
            }

            try
            {
                IDictionary result = CacheImpl.Add(keys, clonedItems, dsItemAddedCallbackID,
                    providerName, sizes, clientId, updateCallbackId, removeCallbackId,
                    updateCallbackFilter, removeCallabackFilter, returnVersions, out itemVersions, callbackType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerAddBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByAddPerSecStats(keys.Length);
                }


                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
        }

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        protected internal virtual void AddAsyncOperation(string key, object value,
            CacheDependency dependency, CacheSyncDependency syncDependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, DSWriteOption dsWriteOption,
            CacheItemRemovedCallback onRemoveCallback,
            CacheItemUpdatedCallback onUpdateCallback,
            AsyncItemAddedCallback onAsyncItemAddCallback,
            DataSourceItemsAddedCallback onDataSourceItemAdded,
            bool isResyncExpiredItems, string group, string subGroup,
            Tag[] tags, string providerName, string resyncProviderName,
            NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback,
            EventDataFilter itemUpdateDataFilter, EventDataFilter itemRemovedDataFilter, string clientId,
            short updateCallbackId, short removeCallbackId, short dsItemAddedCallbackId)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");

            ValidateKeyValue(key, value);

            if (dependency != null && !dependency.GetType().IsSerializable)
                throw new ArgumentException("dependency is not serializable");

            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();
            if (!string.IsNullOrEmpty(resyncProviderName))
                resyncProviderName = resyncProviderName.ToLower();
            // to get the query information from the value before serializing it.
            Hashtable queryInfo = new Hashtable();
            queryInfo["query-info"] = GetQueryInfo(value);
            queryInfo["tag-info"] = GetTagInfo(value, tags);
            queryInfo["named-tag-info"] = GetNamedTagsInfo(value, namedTags);

            short asyncCallbackId = -1;
            long size = 0;

            BitSet flagMap = new BitSet();
            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                long objectSize = 0;
                long encryptedObjectSize = 0;
                long compressedObjectSize = 0;

                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[]) value).Length : 0;


                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(false, objectSize);

                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                if (removeCallbackId == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback,
                            EventType.ItemRemoved, itemRemovedDataFilter);
                        removeCallbackId = callabackIds[1];
                    }
                    else if (onRemoveCallback != null)
                    {
                        removeCallbackId = GetCallbackId(onRemoveCallback);
                        itemRemovedDataFilter = EventDataFilter.DataWithMetadata;
                    }
                }

                if (updateCallbackId == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback,
                            EventType.ItemUpdated, itemUpdateDataFilter);
                        updateCallbackId = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackId = GetCallbackId(onUpdateCallback);
                        itemUpdateDataFilter = EventDataFilter.None;
                    }
                }

                if (onAsyncItemAddCallback != null)
                    asyncCallbackId = GetCallbackId(onAsyncItemAddCallback);

                if (dsItemAddedCallbackId == -1)
                {
                    if (onDataSourceItemAdded != null)
                        dsItemAddedCallbackId = GetCallbackId(onDataSourceItemAdded);
                }

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration &&
                    slidingExpiration != null && slidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both absolute and sliding expirations on a single item");
                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration)
                    absoluteExpiration = ToUTC(absoluteExpiration);

                CacheImpl.AddAsync(key, value, dependency, syncDependency,
                    absoluteExpiration, slidingExpiration, priority,
                    removeCallbackId, updateCallbackId,
                    asyncCallbackId, dsItemAddedCallbackId,
                    isResyncExpiredItems, group, subGroup, queryInfo,
                    flagMap, providerName, resyncProviderName, itemUpdateDataFilter,
                    itemRemovedDataFilter, size, clientId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Get("MyTextBox.Value");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(7)]
        public virtual object Get(string key)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 7;
                return Get(key, DSReadOption.None);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object if it is not already locked. Otherwise returns null (Nothing in Visual Basic).
        /// This is different from the normal Get operation where an item is returned ignoring the lock
        /// altogether.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="lockTimeout">The TimeSpan after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> to hold the lock information.</param>
        /// <param name="acquireLock">A flag to determine whether to acquire a lock or not.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is is an empty string.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the cached value and acquire a lock at the same time.
        /// <code>
        /// 
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// theCache.Add("cachedItemKey", new CacheItem("cachedItemValue"));
        ///
        /// LockHandle lockHandle = new LockHandle();
        ///  
        /// object cachedItem = theCache.Get("cachedItemKey", Cache.NoLockingExpiration, ref lockHandle, true);
        /// 
        /// </code>
        /// </example>   
        [TargetMethod(2)]
        public virtual object Get(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
                CacheItemVersion version = null;
                return GetInternal(key, null, null, DSReadOption.None, ref version, accessType, lockTimeout,
                    ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	IDictionary items = cache.Get(keys, DSReadOption.ReadThru);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual IDictionary GetBulk(string[] keys, DSReadOption dsReadOption)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            RemoveDuplicateKeys(ref keys);
            string providerName = null;
            if (dsReadOption == DSReadOption.ReadThru)
            {
                providerName = _defaultReadThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();
            try
            {
                BitSet flagMap = new BitSet();
                flagMap = this.SetDSReadOptBit(flagMap, dsReadOption);

                UsageStats stats = new UsageStats();
                stats.BeginSample();

                IDictionary table = (IDictionary) CacheImpl.Get(keys, flagMap, null);

                long sumObjectSize = 0;
                long sumCompressedObjectSize = 0;
                long sumEncryptedObjectSize = 0;
                int noOfObjects = 0;

                if (table != null)
                {
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        noOfObjects = table.Count;
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = table[ie.Current] as CompressedValueEntry;
                        if (result != null)
                        {
                            if (result.Value is CallbackEntry)
                            {
                                CallbackEntry e = result.Value as CallbackEntry;
                                result.Value = e.Value;
                            }


                            if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                                _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);

                            if (DebugAPIConfiguraions.LoggingEnabled)
                                sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                            table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                        }
                    }
                }

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByGetPerSecStats(keys.Length);
                }

                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize);

                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	IDictionary items = cache.Get(keys);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(2)]
        public virtual IDictionary GetBulk(string[] keys)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                return GetBulk(keys, DSReadOption.None);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <param name="providerName">The datasource provider name for read-through operation</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache items.</returns>
        [TargetMethod(3)]
        public virtual IDictionary GetBulk(string[] keys, string providerName, DSReadOption dsReadOption)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 3;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            RemoveDuplicateKeys(ref keys);
            if (dsReadOption == DSReadOption.ReadThru && providerName == null)
            {
                providerName = _defaultReadThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            try
            {
                BitSet flagMap = new BitSet();
                flagMap = this.SetDSReadOptBit(flagMap, dsReadOption);

                UsageStats stats = new UsageStats();
                stats.BeginSample();

                IDictionary table = null;
                if (keys.Length > 0)
                    table = (IDictionary) CacheImpl.Get(keys, flagMap, providerName);


                long sumObjectSize = 0;

                int noOfObjects = 0;

                if (table != null)
                {
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = table[ie.Current] as CompressedValueEntry;
                        if (result != null)
                        {
                            if (result.Value is CallbackEntry)
                            {
                                CallbackEntry e = result.Value as CallbackEntry;
                                result.Value = e.Value;
                            }

                            if (DebugAPIConfiguraions.LoggingEnabled)
                                sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                            table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                        }
                    }
                }

                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementByGetPerSecStats(keys.Length);
                }

                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }


        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The key used to reference the stream.</param>
        /// <param name="acquireLock">A flag to determine whether to acquire a lock on stream or not.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <returns>An instance of CacheStream</returns>
        [TargetMethod(1)]
        public virtual CacheStream GetCacheStream(string key, StreamMode streamMode)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                return GetCacheStream(key, streamMode, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The key used to reference the stream.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of CacheStream</returns>
        /// 
        [TargetMethod(2)]
        public virtual CacheStream GetCacheStream(string key, StreamMode streamMode, CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                return GetCacheStream(key, streamMode, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The key used to reference the stream.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <param name="absoluteExpiration">The time at which the added stream expires 
        /// and is removed from the cache. If absolute expiration is not desired, specify
        /// Cache.NoAbsoluteExpiration </param>
        /// <param name="slidingExpiration">The interval between the time the added stream was last 
        /// accessed and when it expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. If sliding expiration is not desired, specify
        /// Cache.NoSlidingExpiration </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of CacheStream</returns>
        /// 
        [TargetMethod(3)]
        public virtual CacheStream GetCacheStream(string key, StreamMode streamMode, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;
                return GetCacheStream(key, null, null, streamMode, null, absoluteExpiration, slidingExpiration,
                    priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The key used to reference the stream.</param>
        /// <param name="group">The group whose keys are to be returned.</param>
        /// <param name="subGroup">The sub group of the group foe which keys are to be returned.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of CacheStream</returns>
        [TargetMethod(4)]
        public virtual CacheStream GetCacheStream(string key, string group, string subgroup, StreamMode streamMode,
            CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 4;
                return GetCacheStream(key, group, subgroup, streamMode, null, Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration, priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The key used to reference the stream.</param>
        /// <param name="group">The group whose keys are to be returned.</param>
        /// <param name="subGroup">The sub group of the group foe which keys are to be returned.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <param name="absoluteExpiration">The time at which the added stream expires 
        /// and is removed from the cache. If absolute expiration is not desired, specify
        /// Cache.NoAbsoluteExpiration </param>
        /// <param name="slidingExpiration">The interval between the time the added stream was last 
        /// accessed and when it expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. If sliding expiration is not desired, specify
        /// Cache.NoSlidingExpiration </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of CacheStream</returns>
        [TargetMethod(5)]
        public virtual CacheStream GetCacheStream(string key, string group, string subgroup, StreamMode streamMode,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                return GetCacheStream(key, group, subgroup, streamMode, null, absoluteExpiration, slidingExpiration,
                    priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Gets an instance of the CacheStream class.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="group">The group whose keys are to be returned.</param>
        /// <param name="subGroup">The sub group of the group foe which keys are to be returned.</param>
        /// <param name="streamMode">Enumeration to specify the desired mode to open the stream.</param>
        /// <param name="dependency">CacheDependency to be added. When any dependency changes, the object 
        /// becomes invalid and is removed from the cache. If there are no dependencies, this parameter 
        /// contains a null reference.</param>
        /// <param name="absoluteExpiration">The time at which the added stream expires 
        /// and is removed from the cache. If absolute expiration is not desired, specify
        /// Cache.NoAbsoluteExpiration </param>
        /// <param name="slidingExpiration">The interval between the time the added stream was last 
        /// accessed and when it expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. If sliding expiration is not desired, specify
        /// Cache.NoSlidingExpiration </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <returns>An instance of CacheStream</returns>
        [TargetMethod(6)]
        public virtual CacheStream GetCacheStream(string key, string group, string subGroup, StreamMode streamMode,
            CacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentNullException("key");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 6;
                if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
                if (group == null && subGroup != null)
                    throw new ArgumentException("group must be specified for sub group");

                if (dependency != null && !dependency.GetType().IsSerializable)
                    throw new ArgumentException("dependency is not serializable");

                CacheStream stream = new CacheStream(this);
                if (absoluteExpiration != Cache.DefaultAbsolute || absoluteExpiration != Cache.DefaultAbsoluteLonger)
                    absoluteExpiration = ToUTC(absoluteExpiration);
                stream.OpenStream(key, streamMode, group, subGroup, absoluteExpiration, slidingExpiration, dependency,
                    priority);

                return stream;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Get the cache item stored in cache.
        /// </summary>
        /// <param name="key">Key used to reference the desired object</param>        
        /// <returns>CacheItem</returns>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	CacheItem item = cache.GetCacheItem(key);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual CacheItem GetCacheItem(string key)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return GetCacheItem(key, DSReadOption.None);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Get the cache item stored in cache.
        /// </summary>
        /// <param name="key">Key used to reference the desired object</param>
        /// <param name="group">The group of the cache item. Items with the same group are logically grouped together.</param>
        /// <param name="subGroup">The sub-group within a group.</param>
        /// <returns>CacheItem</returns>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	CacheItem item = cache.GetCacheItem(key, "group-name", "subgroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        [Obsolete("Use GetCacheItem(string key, string group, string subGroup, DSReadOption dsReadOption)")]
        public virtual CacheItem GetCacheItem(string key, string group, string subGroup)
        {
            try
            {
                if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

                if (key == null) throw new ArgumentNullException("keys");
                if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
                if (group == null && subGroup != null)
                    throw new ArgumentException("group must be specified for sub group");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                return GetCacheItemInternal(key, group, subGroup, DSReadOption.None, ref version,
                    LockAccessType.IGNORE_LOCK, Cache.NoLockExpiration, ref lockHandle, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Get the cache item stored in cache.
        /// </summary>
        /// <param name="key">Key used to reference the desired object</param>
        /// <param name="lockTimeout">The TimeSpan after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> to hold the lock information.</param>
        /// <param name="acquireLock">A flag to determine whether to acquire a lock or not.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.
        /// </returns>
        /// <remarks>
        /// /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// LockHandle lockHandle = new LockHandle();
        ///	CacheItem item = cache.GetCacheItem("cachedItemKey",TimeSpan.FromSeconds(30) , ref lockHandle, true);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual CacheItem GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle,
            bool acquireLock)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;
                LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
                CacheItemVersion version = null;
                return GetCacheItemInternal(key, null, null, DSReadOption.None, ref version, accessType, lockTimeout,
                    ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        internal virtual CacheItem GetCacheItemInternal(string key, string group, string subGroup,
            DSReadOption dsReadOption, ref CacheItemVersion version, LockAccessType accessType, TimeSpan lockTimeout,
            ref LockHandle lockHandle, string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("keys");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (group == null && subGroup != null)
                throw new ArgumentException("group must be specified for sub group"); 


            try
            {
                CacheItem item = new CacheItem();
                Alachisoft.NCache.Caching.CacheEntry entry = null;


                if (dsReadOption == DSReadOption.ReadThru && providerName == null)
                {
                    providerName = _defaultReadThruProvider;
                }


                if (!string.IsNullOrEmpty(providerName))
                    providerName = providerName.ToLower();

                BitSet flagMap = new BitSet();


                flagMap = this.SetDSReadOptBit(flagMap, dsReadOption);

                UsageStats stats = new UsageStats();
                stats.BeginSample();

                object value = CacheImpl.GetCacheItem(key, flagMap, group, subGroup, ref version, ref lockHandle,
                    lockTimeout, accessType, providerName);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementGetPerSecStats();
                }

                long objectSize = 0;

                if (value == null) return null;
                if (value is CacheItem)
                {
                    item = (CacheItem) value;


                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = ((byte[]) item.Value).Length;

                    if (_serializationEnabled)
                        item.Size = ((byte[]) ((CacheItem) value).Value).Length;

                    item.Value = SafeDeserialize(item.Value, _serializationContext, item.FlagMap);
                    if (item.AbsoluteExpiration != Cache.NoAbsoluteExpiration)
                    {
                        item.AbsoluteExpiration = item.AbsoluteExpiration.ToLocalTime();
                    }

                    if (DebugAPIConfiguraions.LoggingEnabled)

                        LogSizeInfo(false, objectSize);


                    return item;
                }

                entry = (Alachisoft.NCache.Caching.CacheEntry) value;
                item.FlagMap = entry.Flag;

                if (entry.Value is CallbackEntry)
                {
                    CallbackEntry cb = (CallbackEntry) entry.Value;
                    if (cb.ItemRemoveCallbackListener != null && cb.ItemRemoveCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemRemoveCallbackListener)
                        {
                            if (cbInfo.Client == CacheImpl.ClientID)
                            {
                                item.ItemRemoveCallback =
                                    (CacheItemRemovedCallback) _callbackIDsMap.GetResource(cbInfo.Callback);
                                break;
                            }
                        }
                    }

                    if (cb.ItemUpdateCallbackListener != null && cb.ItemUpdateCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemUpdateCallbackListener)
                        {
                            if (cbInfo.Client == CacheImpl.ClientID)
                            {
                                item.ItemUpdateCallback =
                                    (CacheItemUpdatedCallback) _callbackIDsMap.GetResource(cbInfo.Callback);
                                break;
                            }
                        }
                    }

                    item.Value = cb.Value;
                }
                else
                {
                    item.Value = entry.Value;


                    if (_perfStatsCollector != null && item.Value != null && item.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[]) item.Value).Length);

                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = ((byte[]) entry.Value).Length;
                    item.Value = SafeDeserialize(entry.Value, _serializationContext, item.FlagMap);
                }

                if (DebugAPIConfiguraions.LoggingEnabled)

                    LogSizeInfo(false, objectSize);


                if (entry != null)
                {
                    item.Priority = (CacheItemPriority) entry.Priority;
                }

                ExpirationHint hint = entry.ExpirationHint;

                if (hint != null)
                    item.IsResyncExpiredItems = hint.NeedsReSync;

                DateTime absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
                TimeSpan slidingExpiration = TimeSpan.Zero;

                item.Dependency =
                    Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetCacheDependency(hint,
                        ref absoluteExpiration, ref slidingExpiration);

                if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                {
                    item.AbsoluteExpiration = absoluteExpiration.ToLocalTime();
                }

                item.SlidingExpiration = slidingExpiration;


                if (entry.GroupInfo != null)
                {
                    item.Group = entry.GroupInfo.Group;
                    item.SubGroup = entry.GroupInfo.SubGroup;
                }

                item.Version = new CacheItemVersion(entry.Version);
                item._creationTime = entry.CreationTime;
                item._lastModifiedTime = entry.LastModifiedTime;

                if (entry.QueryInfo != null)
                {
                    if (entry.QueryInfo["tag-info"] != null)
                    {
                        Hashtable tagInfo = entry.QueryInfo["tag-info"] as Hashtable;
                        ArrayList tagsList = tagInfo["tags-list"] as ArrayList;
                        Tag[] tags = new Tag[tagsList.Count];
                        int i = 0;
                        foreach (string tag in tagsList)
                        {
                            tags[i++] = new Tag(tag);
                        }

                        item.Tags = tags;
                    }

                    if (entry.QueryInfo["named-tag-info"] != null)
                    {
                        Hashtable tagInfo = entry.QueryInfo["named-tag-info"] as Hashtable;
                        Hashtable tagsList = tagInfo["named-tags-list"] as Hashtable;

                        NamedTagsDictionary namedTags = new NamedTagsDictionary();

                        foreach (DictionaryEntry tag in tagsList)
                        {
                            Type tagType = tag.Value.GetType();
                            string tagKey = tag.Key.ToString();

                            if (tagType == typeof(int))
                            {
                                namedTags.Add(tagKey, (int) tag.Value);
                            }
                            else if (tagType == typeof(long))
                            {
                                namedTags.Add(tagKey, (long) tag.Value);
                            }
                            else if (tagType == typeof(float))
                            {
                                namedTags.Add(tagKey, (float) tag.Value);
                            }
                            else if (tagType == typeof(double))
                            {
                                namedTags.Add(tagKey, (double) tag.Value);
                            }
                            else if (tagType == typeof(decimal))
                            {
                                namedTags.Add(tagKey, (decimal) tag.Value);
                            }
                            else if (tagType == typeof(bool))
                            {
                                namedTags.Add(tagKey, (bool) tag.Value);
                            }
                            else if (tagType == typeof(char))
                            {
                                namedTags.Add(tagKey, (char) tag.Value);
                            }
                            else if (tagType == typeof(string))
                            {
                                namedTags.Add(tagKey, (string) tag.Value);
                            }
                            else if (tagType == typeof(DateTime))
                            {
                                namedTags.Add(tagKey, (DateTime) tag.Value);
                            }
                        }

                        item.NamedTags = namedTags;
                    }
                }

                return item;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	ArrayList list = (ArrayList) cache.GetGroupKeys("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual ArrayList GetGroupKeys(string group, string subGroup)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (group == null) throw new ArgumentNullException("group");
            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return CacheImpl.GetGroupKeys(group, subGroup);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	Hashtable table = (Hashtable) cache.GetGroupData("Customer", "Orders");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual IDictionary GetGroupData(string group, string subGroup)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (group == null) throw new ArgumentNullException("group");
            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");

            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                UsageStats stats = new UsageStats();
                stats.BeginSample();

                Hashtable table = CacheImpl.GetGroupData(group, subGroup) as Hashtable;

                long sumObjectSize = 0;
                long sumCompressedObjectSize = 0;
                long sumEncryptedObjectSize = 0;
                int noOfObjects = 0;

                if (table != null)
                {
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = (CompressedValueEntry) table[ie.Current];
                        if (result.Value is CallbackEntry)
                        {
                            CallbackEntry e = result.Value as CallbackEntry;
                            result.Value = e.Value;
                        }


                        if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                            _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);

                        if (DebugAPIConfiguraions.LoggingEnabled)
                            sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                        table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                    }

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerGetBulkSample(stats.Current);
                        _perfStatsCollector.IncrementByGetPerSecStats(table.Count);
                    }
                }

                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize);
                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }

        /// <summary>
        /// Gets all the cached objects with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to search with.</param>
        /// <returns>Returns a dictionary containing the cache keys and associated objects.</returns>
        /// <example>The following example demonstrates how to get the objects with the specified tag.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag tag = new Tag("Sports");
        ///	Hashtable table = cache.GetByTag(tag);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual Hashtable GetByTag(Tag tag)
        {
            if (tag == null || tag.TagName == null) throw new ArgumentException("Tag cannot be null.");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            UsageStats stats = new UsageStats();
            stats.BeginSample();
            try
            {
                Hashtable table = CacheImpl.GetByTag(new Tag[] {tag}, TagComparisonType.BY_TAG);

                long sumObjectSize = 0;
                long sumCompressedObjectSize = 0;
                long sumEncryptedObjectSize = 0;
                int noOfObjects = 0;

                if (table != null)
                {
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        noOfObjects = table.Count;
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = (CompressedValueEntry) table[ie.Current];

                        if (result.Value is CallbackEntry)
                        {
                            CallbackEntry e = result.Value as CallbackEntry;
                            result.Value = e.Value;
                        }


                        if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                            _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                        if (DebugAPIConfiguraions.LoggingEnabled)
                            sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                        table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                    }

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerGetBulkSample(stats.Current);
                        _perfStatsCollector.IncrementByGetPerSecStats(table.Count);
                    }
                }

                if (DebugAPIConfiguraions.LoggingEnabled)

                    LogSizeInfo(true, sumObjectSize);


                return table;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Gets all the keys with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to search with.</param>
        /// <returns>Returns collection containing the cache keys.</returns>
        /// <example>The following example demonstrates how to get the keys with the specified tag.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag tag = new Tag("Sports");
        ///	ICollection keys = cache.GetKeysByTag(tag);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual ICollection GetKeysByTag(Tag tag)
        {
            if (tag == null || tag.TagName == null) throw new ArgumentException("Tag cannot be null.");
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                return CacheImpl.GetKeysByTag(new Tag[] {tag}, TagComparisonType.BY_TAG);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Returns the cached objects that have all the same tags in common. (Returns the Intersection set.) 
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <returns>A dictionary containing cache keys and associated objects.</returns>
        /// <example>The following example demonstrates how to get the objects that have all the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	Hashtable table = cache.GetByAllTags(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual Hashtable GetByAllTags(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag[] cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);

            UsageStats stats = new UsageStats();
            stats.BeginSample();
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                Hashtable table = CacheImpl.GetByTag(tags, TagComparisonType.ALL_MATCHING_TAGS);

                long sumObjectSize = 0;
                long sumCompressedObjectSize = 0;
                long sumEncryptedObjectSize = 0;
                int noOfObjects = 0;
                if (table != null)
                {
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        noOfObjects = table.Count;
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = (CompressedValueEntry) table[ie.Current];

                        if (result.Value is CallbackEntry)
                        {
                            CallbackEntry e = result.Value as CallbackEntry;
                            result.Value = e.Value;
                        }


                        if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                            _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                        if (DebugAPIConfiguraions.LoggingEnabled)
                            sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                        table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                    }

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerGetBulkSample(stats.Current);
                        _perfStatsCollector.IncrementByGetPerSecStats(table.Count);
                    }
                }

                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize);


                return table;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Returns keys that have all the same tags in common. (Returns the Intersection set.) 
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <returns>A collection containing cache keys.</returns>
        /// <example>The following example demonstrates how to get the keys that have all the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	ICollection keys = cache.GetKeysByAllTags(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual ICollection GetKeysByAllTags(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag[] cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return CacheImpl.GetKeysByTag(tags, TagComparisonType.ALL_MATCHING_TAGS);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Returns the cached objects that have any of the same tags in common. (Returns the Union set.) 
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <returns>A dictionary containing cache keys and associated objects.</returns>
        /// <example>The following example demonstrates how to get the objects that have any of the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	Hashtable table = cache.GetByAnyTag(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual Hashtable GetByAnyTag(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag[] cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);

            UsageStats stats = new UsageStats();
            stats.BeginSample();
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                Hashtable table = CacheImpl.GetByTag(tags, TagComparisonType.ANY_MATCHING_TAG);

                long sumObjectSize = 0;
                long sumCompressedObjectSize = 0;
                long sumEncryptedObjectSize = 0;
                int noOfObjects = 0;

                if (table != null)
                {
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        noOfObjects = table.Count;
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = (CompressedValueEntry) table[ie.Current];

                        if (result.Value is CallbackEntry)
                        {
                            CallbackEntry e = result.Value as CallbackEntry;
                            result.Value = e.Value;
                        }


                        if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                            _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                        if (DebugAPIConfiguraions.LoggingEnabled)
                            sumObjectSize += result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                        table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                    }

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerGetBulkSample(stats.Current);
                        _perfStatsCollector.IncrementByGetPerSecStats(table.Count);
                    }
                }

                return table;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Returns keys that have any of the same tags in common. (Returns the Union set.) 
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <returns>A collection containing cache keys.</returns>
        /// <example>The following example demonstrates how to get the keys that have any of the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	ICollection keys = cache.GetKeysByAnyTag(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual ICollection GetKeysByAnyTag(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag[] cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                return CacheImpl.GetKeysByTag(tags, TagComparisonType.ANY_MATCHING_TAG);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the cached objects that have any of the same tags in common.
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <example>The following example demonstrates how to remove the objects that have any of the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	cache.RemoveByTag(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void RemoveByAnyTag(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                CacheImpl.RemoveByTag(tags, TagComparisonType.ANY_MATCHING_TAG);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the cached objects that have any of the same tags in common. (Returns the Union set.) 
        /// </summary>
        /// <param name="tags">An array of Tag to search with.</param>
        /// <example>The following example demonstrates how to remove the objects that have any of the specified tags
        /// in common.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[2];
        /// tags[0] = new Tag("Alpha");
        /// tags[1] = new Tag("Beta");
        ///	cache.RemoveByTag(tags);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void RemoveByAllTags(Tag[] tags)
        {
            if (tags == null) throw new ArgumentException("Tag cannot be null.");
            if (tags.Length == 0) throw new ArgumentException("Atleast one Tag required.");
            RemoveDuplicateTags(ref tags);
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                CacheImpl.RemoveByTag(tags, TagComparisonType.ALL_MATCHING_TAGS);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the cached objects with the specified tag.
        /// </summary>
        /// <param name="tag">A Tag to search with.</param>
        /// <example>The following example demonstrates how to remove the objects with the specified tag.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag tag = new Tag("Alpha");
        ///	cache.RemoveByTag(tag);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void RemoveByTag(Tag tag)
        {
            if (tag == null || tag.TagName == null) throw new ArgumentException("Tag cannot be null.");

            string[] tags = new string[1];
            tags[0] = tag.TagName;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                CacheImpl.RemoveByTag(new Tag[] {tag}, TagComparisonType.BY_TAG);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Gets an object from the cache only if a newer version of the object exists in cache.
        /// </summary>
        /// <param name="key">key used to reference the desired object</param>
        /// <param name="version">The version of the desired object passed by reference.</param>
        /// <returns>If a newer object exists in the cache, the object is returned. Otherwise, null is returned.</returns>
        /// <example>The following example demonstrates how to get a newer version of the item from cache if it exists.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.GetIfNewer("key", ref itemVersion);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual object GetIfNewer(string key, ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                return GetIfNewer(key, null, null, ref version);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Gets an object from the cache only if a newer version of the object exists in cache.
        /// </summary>
        /// <param name="key">key used to reference the desired object</param>
        /// <param name="group">The group of the cached object</param>
        /// <param name="subGroup">The subGroup of the cached object</param>
        /// <param name="version">The version of the desired object passed by reference.</param>
        /// <returns>If a newer object exists in the cache, the object is returned. Otherwise, null is returned.</returns>
        /// <example>The following example demonstrates how to get a newer version of the item from cache if it exists.
        /// <code>
        /// 
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.GetIfNewer("key", "Alpha", null, ref itemVersion);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual object GetIfNewer(string key, string group, string subGroup, ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                LockHandle lockHandle = null;
                return GetInternal(key, group, subGroup, DSReadOption.None, ref version, LockAccessType.COMPARE_VERSION,
                    Cache.NoLockExpiration, ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", DSReadOption.ReadThru);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(8)]
        public virtual object Get(string key, DSReadOption dsReadOption)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                CacheItemVersion version = null;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 8;
                return GetInternal(key, null, null, dsReadOption, ref version, accessType, Cache.NoLockExpiration,
                    ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", DSReadOption.ReadThru);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(4)]
        public virtual CacheItem GetCacheItem(string key, DSReadOption dsReadOption)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                CacheItemVersion version = null;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 4;
                return GetCacheItemInternal(key, null, null, dsReadOption, ref version, accessType,
                    Cache.NoLockExpiration, ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="providerName">A specific name for the data source</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        [TargetMethod(1)]
        public virtual object Get(string key, string providerName, DSReadOption dsReadOption)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                CacheItemVersion version = null;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                return GetInternal(key, null, null, dsReadOption, ref version, accessType, Cache.NoLockExpiration,
                    ref lockHandle, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="providerName">A specific name for the data source</param>
        /// <param name="dsReadOption">Options regarding reading from data source</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        [TargetMethod(5)]
        public virtual CacheItem GetCacheItem(string key, string providerName, DSReadOption dsReadOption)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                CacheItemVersion version = null;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                return GetCacheItemInternal(key, null, null, dsReadOption, ref version, accessType,
                    Cache.NoLockExpiration, ref lockHandle, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Get's client cache compatible expiration
        /// </summary>
        /// <param name="slidingExpiration"></param>
        /// <returns></returns>
        /// 
        internal static DateTime GetCompatibleExpiration(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                return absoluteExpiration;
            if (slidingExpiration == Cache.NoSlidingExpiration)
                return Cache.NoAbsoluteExpiration;
            return absoluteExpiration;
        }

        /// <summary>
        /// this method is used from the sync cache to synchronize the L1 cache with L2.
        /// getting a serialized object reduces the cost as we need to put the same object again in the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dsReadOption"></param>
        /// <returns></returns>
        internal virtual object GetSerializedObject(string key, DSReadOption dsReadOption, ref ulong v, ref BitSet flag,
            ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration, ref string group, ref string subGroup,
            ref Hashtable queryInfo)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");

            CacheItem result = null;
            try
            {
                BitSet flagMap = new BitSet();
                flagMap = this.SetDSReadOptBit(flagMap, dsReadOption);

                group = subGroup = null;
                queryInfo = null;

                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                result = CacheImpl.GetCacheItem(key, flagMap, null, null, ref version, ref lockHandle, TimeSpan.Zero,
                    LockAccessType.GET_VERSION, null) as CacheItem;

                //set the version...
                if (version != null) v = version.Version;

                if (result == null) return null;
                // set the flag
                flag.Data = result.FlagMap.Data;

                // set groups
                group = result.Group;
                subGroup = result.SubGroup;
                queryInfo = result.QueryInfo;

                if (result != null && result.Value != null)
                {
                    absoluteExpiration = GetCompatibleExpiration(result.AbsoluteExpiration, result.SlidingExpiration);

                    slidingExpiration = Cache.NoSlidingExpiration;

                    if (result.Value is CallbackEntry)
                    {
                        CallbackEntry e = result.Value as CallbackEntry;
                        result.Value = e.Value;
                    }


                    return result;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                else return null;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", ref version);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(4)]
        public virtual object Get(string key, ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 4;
                return Get(key, DSReadOption.None, ref version);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", ref version);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(6)]
        public virtual CacheItem GetCacheItem(string key, ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 6;
                return GetCacheItem(key, DSReadOption.None, ref version);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it in the cache. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", DSReadOption.ReadThru, ref version);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual object Get(string key, DSReadOption dsReadOption, ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;
                LockAccessType accessType = LockAccessType.DEFAULT;
                LockHandle lockHandle = null;
                accessType = (version == null ? LockAccessType.GET_VERSION : LockAccessType.MATCH_VERSION);
                return GetInternal(key, null, null, dsReadOption, ref version, accessType, Cache.NoLockExpiration,
                    ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it in the cache. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", DSReadOption.ReadThru, ref version);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(5)]
        public virtual CacheItem GetCacheItem(string key, DSReadOption dsReadOption, ref CacheItemVersion version)
        {
            try
            {
                LockAccessType accessType = LockAccessType.MATCH_VERSION;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                accessType = (version == null ? LockAccessType.GET_VERSION : LockAccessType.MATCH_VERSION);
                return GetCacheItemInternal(key, null, null, dsReadOption, ref version, accessType,
                    Cache.NoLockExpiration, ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it in the cache. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="providerName">A specific name for the data source</param>
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        [TargetMethod(5)]
        public virtual object Get(string key, string providerName, DSReadOption dsReadOption,
            ref CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                LockAccessType accessType = LockAccessType.DEFAULT;
                LockHandle lockHandle = null;
                accessType = (version == null ? LockAccessType.GET_VERSION : LockAccessType.MATCH_VERSION);
                return GetInternal(key, null, null, dsReadOption, ref version, accessType, Cache.NoLockExpiration,
                    ref lockHandle, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it in the cache. It accepts the <see cref="CacheItemVersion"/> by reference.
        /// If null is passed for CacheItemVersion, then the version of the object from the cache is returned. If non-null
        /// CacheItemVersion is passed, then object is returned from the cache only if that is the current version of the object
        /// in the cache.
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>
        /// <param name="providerName">A specific name for the data source</param>
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <param name="version">The version of the object.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// 
        [TargetMethod(7)]
        public virtual CacheItem GetCacheItem(string key, string providerName, DSReadOption dsReadOption,
            ref CacheItemVersion version)
        {
            try
            {
                LockAccessType accessType = LockAccessType.DEFAULT;
                LockHandle lockHandle = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 7;
                accessType = (version == null ? LockAccessType.GET_VERSION : LockAccessType.MATCH_VERSION);
                return GetCacheItemInternal(key, null, null, dsReadOption, ref version, accessType,
                    Cache.NoLockExpiration, ref lockHandle, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it against the given group and sub group. 
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="group">The name of the group which the item belongs to.</param>
        /// <param name="subGroup">The name of the subGroup within a group.</param>
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentNullException"><paramref name="group"/>  contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", "Customer", null, DSReadOption.ReadThru);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(6)]
        public virtual object Get(string key, string group, string subGroup, DSReadOption dsReadOption)
        {
            try
            {
                if (group == null) throw new ArgumentNullException("group");
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 6;
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                return GetInternal(key, group, subGroup, dsReadOption, ref version, accessType, Cache.NoLockExpiration,
                    ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read through the data source,
        /// put it against the given group and sub group. 
        /// </summary>
        /// <param name="key">The identifier for the cache item to retrieve.</param>  
        /// <param name="group">The name of the group which the item belongs to.</param>
        /// <param name="subGroup">The name of the subGroup within a group.</param>
        /// <param name="dsReadOption">Options regarding reading from data source.</param>
        /// <returns>The retrieved cache item, or a null reference (Nothing 
        /// in Visual Basic) if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentNullException"><paramref name="group"/>  contains a null reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control. The version of the cached object is also received.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheItemVersion version = null;
        ///	string textBoxValue = (string) cache.Get("MyTextBox.Value", "Customer", null, DSReadOption.ReadThru);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(8)]
        public virtual CacheItem GetCacheItem(string key, string group, string subGroup, DSReadOption dsReadOption)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                LockHandle lockHandle = null;
                CacheItemVersion version = null;
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 8;
                return GetCacheItemInternal(key, group, subGroup, dsReadOption, ref version, accessType,
                    Cache.NoLockExpiration, ref lockHandle, null);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        internal virtual object GetInternal(string key, string group, string subGroup, DSReadOption dsReadOption,
            ref CacheItemVersion version, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle,
            string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (version == null && accessType == LockAccessType.COMPARE_VERSION)
                throw new ArgumentNullException("version");
            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");


            if (dsReadOption == DSReadOption.ReadThru && providerName == null)
            {
                providerName = _defaultReadThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            long objectSize = 0;
            long encryptedObjectSize = 0;
            long compressedObjectSize = 0;

            CompressedValueEntry result = null;
            try
            {
                BitSet flagMap = new BitSet();


                flagMap = this.SetDSReadOptBit(flagMap, dsReadOption);

                UsageStats stats = new UsageStats();
                stats.BeginSample();

                result = CacheImpl.Get(key, flagMap, group, subGroup, ref version, ref lockHandle, lockTimeout,
                    accessType, providerName);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerGetSample(stats.Current);
                    _perfStatsCollector.IncrementGetPerSecStats();
                }

                if (result != null && result.Value != null)
                {
                    if (result.Value is CallbackEntry)
                    {
                        CallbackEntry e = result.Value as CallbackEntry;
                        result.Value = e.Value;
                    }


                    if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = result.Value is byte[] ? ((byte[]) result.Value).Length : 0;

                    result.Value = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                    if (DebugAPIConfiguraions.LoggingEnabled)

                        LogSizeInfo(false, objectSize);


                    return result.Value;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                else return null;
            }

            return null;
        }

#endregion

#region	/                 --- Insert ---           /

        /// <summary>
        /// Inserts an item into the Cache object with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// This method will overwrite an existing <see cref="Cache"/> item with the same <paramref name="key"/> parameter.
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to insert an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual CacheItemVersion Insert(string key, object value)
        {
            long size = 0;

            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 2;
                return InsertOperation(key, value,
                    null, null, Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, null, null, null, null,
                    LockAccessType.IGNORE_LOCK, null, null, null, null,
                    null, null, EventDataFilter.None, EventDataFilter.None,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary> Insert a value to the cache </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The value that is to be stored</param>
        /// <param name="group">The data group of the item</param>
        /// <param name="subGroup">The data group of the item</param>
        /// <example>
        /// 
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", value, "CONNECTIONS", null);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual CacheItemVersion Insert(string key, object value, string group, string subGroup)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 3;
                return InsertOperation(key, value,
                    null, null, Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, group, subGroup, null, null,
                    LockAccessType.IGNORE_LOCK, null, null, null,
                    null, null, null, EventDataFilter.None, EventDataFilter.None,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Inserts an item into the Cache with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration. It also 
        /// enables associating named tags with the object.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="namedTags">A <see cref="NamedTagsDictionary"/> to associate with the object.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// NamedTagsDictionary namedTags = new NamedTagsDictionary();
        /// namedTags.Add("Attribute", "value");
        ///	cache.Insert("DSN", connectionString, namedTags);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(1)]
        public virtual CacheItemVersion Insert(string key, object value, NamedTagsDictionary namedTags)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;

                return InsertOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, null, null, null, null,
                    LockAccessType.IGNORE_LOCK, null, null, null,
                    namedTags, null, null, EventDataFilter.None,
                    EventDataFilter.None, ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Inserts an item into the Cache with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration. It also 
        /// enables the associating tags with the object.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="tags">An array of <see cref="Tag"/> to associate with the object.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// Tag[] tags = new Tag[1];
        /// tags[0] = new Tag("tag-name");
        ///	cache.Insert("DSN", connectionString, tags);
        /// 
        /// </code>        
        /// </example>
        [TargetMethod(4)]
        public virtual CacheItemVersion Insert(string key, object value, Tag[] tags)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 4;
                return InsertOperation(key, value, null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, null, false, null, null, null, null,
                    LockAccessType.IGNORE_LOCK, tags, null, null,
                    null, null, null, EventDataFilter.None,
                    EventDataFilter.None, ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        //muds:
        //newly added method for NCache Lite Edition
        /// <summary>
        /// Inserts an object into the <see cref="Cache"/> with dependencies, expiration and 
        /// priority policies.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="dependency">The file dependencies for the 
        /// inserted object. When any dependency changes, the object becomes invalid and is 
        /// removed from the cache. If there are no dependencies, this parameter contains 
        /// a null reference (Nothing in Visual Basic).</param>
        /// <param name="absoluteExpiration">The time at which the added object expires 
        /// and is removed from the cache.</param>
        /// <param name="slidingExpiration">The interval between the time the added object was last 
        /// accessed and when that object expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null, 
        /// an <see cref="ArgumentNullException"/> is thrown. If you set the <paramref name="slidingExpiration"/> 
        /// parameter to less than <see cref="TimeSpan.Zero"/>, or the equivalent of 
        /// more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown. You cannot set 
        /// both sliding and absolute expirations on the same cached item. If you do so, an 
        /// <see cref="ArgumentException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString, null, DateTime.Now.AddMinutes(2), TimeSpan.Zero, CacheItemPriority.High);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(5)]
        public virtual CacheItemVersion Insert(string key, object value, CacheDependency dependency,
            DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 5;
                return InsertOperation(key, value, dependency, null,
                    absoluteExpiration, slidingExpiration, priority,
                    DSWriteOption.None, null, null, null, false,
                    null, null, null, null, LockAccessType.IGNORE_LOCK,
                    null, null, null, null, null, null, EventDataFilter.None,
                    EventDataFilter.None, ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Inserts an object into the <see cref="Cache"/> with dependencies, expiration and 
        /// priority policies.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="absoluteExpiration">The time at which the added object expires 
        /// and is removed from the cache.</param>
        /// <param name="slidingExpiration">The interval between the time the added object was last 
        /// accessed and when that object expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. </param>
        /// <param name="priority">The relative cost of the object, as expressed by 
        /// the <see cref="CacheItemPriority"/> enumeration. The cache uses this value when it 
        /// evicts objects; objects with a lower cost are removed from the cache 
        /// before objects with a higher cost.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null, 
        /// an <see cref="ArgumentNullException"/> is thrown. If you set the <paramref name="slidingExpiration"/> 
        /// parameter to less than <see cref="TimeSpan.Zero"/>, or the equivalent of 
        /// more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown. You cannot set 
        /// both sliding and absolute expirations on the same cached item. If you do so, an 
        /// <see cref="ArgumentException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString, null, DateTime.Now.AddMinutes(2), TimeSpan.Zero, CacheItemPriority.High);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(6)]
        public virtual CacheItemVersion Insert(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 6;
                return Insert(key, value, null, absoluteExpiration, slidingExpiration, priority);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add an item to the cache with a sliding expiration of 5 minutes and a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// CacheItem item = new CacheItem(timeStamp);
        /// item.SlidingExpiration = new TimeSpan(0,5,0);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("timestamp", item);
        ///
        /// </code>
        /// </example>
        [TargetMethod(7)]
        public virtual CacheItemVersion Insert(string key, CacheItem item)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 7;
                return InsertOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority, DSWriteOption.None,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    null, item.IsResyncExpiredItems,
                    item.Group, item.SubGroup, null, item.Version,
                    item.Version == null ? LockAccessType.IGNORE_LOCK : LockAccessType.COMPARE_VERSION,
                    item.Tags, null, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
        /// <returns>An instance of <see cref="CacheItemVersion"/></returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to add an item to the cache with a sliding expiration of 5 minutes, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// CacheItem item = new CacheItem(timeStamp);
        /// item.SlidingExpiration = new TimeSpan(0,5,0);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then add CacheItem to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("timestamp", item, DSWriteOption.WriteThru, new DataSourceItemsUpdatedCallback(OnDSItemsUpdated));
        ///
        /// </code>
        /// </example>
        [TargetMethod(8)]
        public virtual CacheItemVersion Insert(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            long size = 0;
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 8;
                return InsertOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    onDataSourceItemUpdatedCallback,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    null, item.Version,
                    item.Version == null ? LockAccessType.IGNORE_LOCK : LockAccessType.COMPARE_VERSION,
                    item.Tags, null, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        [TargetMethod(9)]
        public virtual CacheItemVersion Insert(string key, CacheItem item, DSWriteOption dsWriteOption,
            string providerName, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 9;
                return InsertOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    onDataSourceItemUpdatedCallback,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    null, item.Version,
                    item.Version == null ? LockAccessType.IGNORE_LOCK : LockAccessType.COMPARE_VERSION,
                    item.Tags, providerName, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Inserts a <see cref="CacheItem"/> to the cache if not already existing.
        /// Otherwise updates an existing item if it not already locked or if the correct lock-id is specified.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/>. If the item is locked, then it can be updated only if the correct lockHandle is specified.</param>
        /// <param name="releaseLock">A flag to determine whether or not release lock after operation is performed.</param>
        /// <returns>The object item added to the Cache.</returns>
        /// <remarks>If lockId does not match with the lockId associated with cached item,
        /// an exception will be thrown.</remarks>		
        /// <example>The following example demonstrates how to update a locked item in the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// </code>
        /// Add an item int the cache.
        /// <code>
        /// theCache.Add("cachedItemKey", new CacheItem("cachedItemValue"));
        /// </code>
        /// Create the objects for lockid and lockdate.
        /// <code>
        /// LockHandle lockHandle = new LockHandle();
        /// </code>    
        /// Get the added item from cache and acquire a lock.
        /// <code>
        /// object cachedItem = theCache.Get("cachedItemKey", Cache.NoLockingExpiration, ref lockHandle, true);
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///         theCache.Insert("cachedItemKey", new CacheItem("someothervalue"), lockHandle, true);
        ///         string cachedValue = (string)theCache.Get("cachedItemKey");
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(10)]
        public virtual CacheItemVersion Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)

        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");

            LockAccessType accessType = releaseLock ? LockAccessType.RELEASE : LockAccessType.DONT_RELEASE;
            long size = 0;
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 10;
                return InsertOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    DSWriteOption.None,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    null, item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    lockHandle, null, accessType,
                    item.Tags, null, item.ResyncProviderName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    ref size, true, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
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
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        /// CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        ///	cache.InsertBulk(keys, items, DSWriteOption.WriteThru, new DataSourceItemsUpdatedCallback(onDSItemsUpdated));
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual IDictionary InsertBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            long[] sizes = new long[items.Length];
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                IDictionary itemVersions = null;

                return InsertBulkOperation(keys, items, dsWriteOption,
                    onDataSourceItemUpdatedCallback, null, ref sizes, true, null, -1, -1, -1,
                    EventDataFilter.None, EventDataFilter.None,
                    false, out itemVersions, Runtime.Events.CallbackType.PushBasedNotification);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        /// First create CacheItems.
        /// <code>
        /// string[] keys = {"SQLDSN", "ORADSN"};
        /// CacheItem items[] = new CacheItem[2];
        /// items[0] = new CacheItem(sqlConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        /// CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        ///	cache.InsertBulk(keys, items);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual IDictionary InsertBulk(string[] keys, CacheItem[] items)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            try
            {
                return InsertBulk(keys, items, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="providerName">A unique identifier for the data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
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
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        /// CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        ///	cache.InsertBulk(keys, items, DSWriteOption.WriteThru, providerName, new DataSourceItemsUpdatedCallback(onDSItemsUpdated));
        /// 
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual IDictionary InsertBulk(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 3;
            long[] sizes = new long[items.Length];
            try
            {
                IDictionary itemVersions = null;

                return InsertBulkOperation(keys, items, dsWriteOption,
                    onDataSourceItemUpdatedCallback, providerName, ref sizes, true, null, -1, -1, -1,
                    EventDataFilter.None, EventDataFilter.None, false, out itemVersions,
                    Runtime.Events.CallbackType.PushBasedNotification);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
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
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// 
        /// items[1] = new CacheItem(oraConnectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        /// item.Priority = CacheItemPriority.Low;
        /// item.ItemRemoveCallback = onRemove;
        /// </code>
        /// 
        /// Then insert CacheItems to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[] { "myItem1", "myItem2" };
        /// CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        ///	cache.Insert(keys, items, "Connection", null);
        /// 
        /// </code>
        /// </example>        
        internal virtual IDictionary InsertBulkOperation(string[] keys, CacheItem[] items,
            DSWriteOption dsWriteOption, DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback,
            string providerName, ref long[] sizes, bool allowQueryTags, string clientId, short updateCallbackId,
            short removeCallbackId, short dsItemUpdatedCallbackID,
            EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter, bool returnVersions,
            out IDictionary itemVersions, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (items == null) throw new ArgumentNullException("items");
            if (keys.Length != items.Length) throw new ArgumentException("keys count is not equals to items count");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            if (FindDupliate(keys)) throw new ArgumentException("Duplicate keys found in provided 'key' array.");
            CacheItem[] clonedItems = new CacheItem[items.Length];

            itemVersions = null;

            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            int noOfObjects = 0;
            long sumObjectSize = 0;
            long sumEncryptedObjectSize = 0;
            long sumCompressedObjectSize = 0;

            UsageStats stats = new UsageStats();
            stats.BeginSample();

            if (DebugAPIConfiguraions.LoggingEnabled)
                noOfObjects = items.Length;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                BitSet flagMap = new BitSet();
                if (items[i] == null)
                    throw new Exception("CacheItem cannot be null");

                if (items[i].SubGroup != null && items[i].Group == null)
                {
                    throw new ArgumentException("group must be specified for sub group");
                }

                CacheItem cloned = items[i].Clone() as CacheItem;

                long size = 0;
                if (sizes[i] > 0)
                    size = sizes[i];
                Hashtable queryInfo = null;


                ValidateKeyValue(key, cloned.Value);

                if (queryInfo == null)
                {
                    queryInfo = new Hashtable();
                    if (allowQueryTags)
                    {
                        queryInfo["query-info"] = GetQueryInfo(cloned.Value);
                        queryInfo["tag-info"] = GetTagInfo(cloned.Value, cloned.Tags);
                        queryInfo["named-tag-info"] = GetNamedTagsInfo(cloned.Value, cloned.NamedTags);
                    }
                }

                cloned.QueryInfo = queryInfo;

                cloned.Value = SafeSerialize(cloned.Value, _serializationContext, ref flagMap, ref size);
                sizes[i] = size;

                if (DebugAPIConfiguraions.LoggingEnabled)
                    sumObjectSize += cloned.Value is byte[] ? ((byte[]) cloned.Value).Length : 0;


                if (_perfStatsCollector != null)
                {
                    if (cloned.Value != null) _perfStatsCollector.IncrementAvgItemSize(((byte[]) cloned.Value).Length);
                }


                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);
                cloned.FlagMap = flagMap;

                if (cloned.Dependency != null && !cloned.Dependency.GetType().IsSerializable)
                    throw new ArgumentException("dependency is not serializable");
                if (cloned.AbsoluteExpiration != null && cloned.AbsoluteExpiration != Cache.NoAbsoluteExpiration &&
                    cloned.SlidingExpiration != null && cloned.SlidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both sliding and absolute expirations on a single item");
                cloned.AbsoluteExpiration = ToUTC(cloned.AbsoluteExpiration);

                clonedItems[i] = cloned;
            }

            if (DebugAPIConfiguraions.LoggingEnabled)
                LogSizeInfo(true, sumObjectSize, noOfObjects);

            if (dsItemUpdatedCallbackID == -1)
            {
                if (onDataSourceItemUpdatedCallback != null)
                    dsItemUpdatedCallbackID = GetCallbackId(onDataSourceItemUpdatedCallback, keys.Length);
            }

            try
            {
                IDictionary result = CacheImpl.Insert(keys,
                    clonedItems, dsItemUpdatedCallbackID,
                    providerName, sizes, clientId, updateCallbackId,
                    removeCallbackId, updateCallbackFilter, removeCallabackFilter, returnVersions, out itemVersions,
                    callbackType);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerUpdBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByUpdPerSecStats(keys.Length);
                }

                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
        }

        /// <summary>
        /// Inserts an object into the <see cref="Cache"/> asynchronously with dependencies, expiration and 
        /// priority policies, and a delegate you can use to notify your application 
        /// when the inserted item is removed from the Cache.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>
        /// <param name="group">An string that logically groups the data togehter.</param>
        /// <param name="subGroup">The name of the subGroup within the group.</param>
        /// <param name="onAsyncItemUpdateCallback">A delegate that can be used to get the result of the Asynchronous 
        /// update operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Insert(string, object)"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemUpdated"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to insert an item into the cache asynchronously with associated 
        /// group and subGroup. It also registers a delegate to get the result of the asynchronous operation.
        /// it into your application's <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.InsertAsync("DSN", connectionString, new AsyncItemUpdatedCallback(onAsyncItemUpdated), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual void InsertAsync(string key, object value,
            AsyncItemUpdatedCallback onAsyncItemUpdateCallback, string group, string subGroup)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                InsertAsyncOperation(key, value,
                    null, null,
                    Cache.DefaultAbsolute, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, DSWriteOption.None,
                    null, null, onAsyncItemUpdateCallback, null,
                    false, group, subGroup, null, null,
                    null, null, null, EventDataFilter.None,
                    EventDataFilter.None, null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Insert a <see cref="CacheItem"/> to the cache asynchoronously
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
        /// <remarks> If CacheItem contains invalid values the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        /// <example>The following example demonstrates how to insert an item into the cache asynchronously.
        /// It also provides the option to udpate the data source and registers a delegate to get the result of
        /// the data source operation.
        /// <code>
        /// CacheItem item = new CacheItem(connectionString);
        /// item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        /// item.SlidingExpiration = TimeSpan.Zero;
        /// item.Priority = CacheItemPriority.High;
        /// item.ItemRemoveCallback = onRemove;
        /// item.AsyncItemUpdateCallback = OnAsyncItemUpdated;
        /// </code>
        /// 
        /// Then insert CacheItem to the cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.InsertAsync("DSN", item, DSWriteOption.WriteThru, new DataSourceItemsUpdatedCallback(onDSItemsUpdated));
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual void InsertAsync(string key, CacheItem item, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            try
            {
                InsertAsyncOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    item.AsyncItemUpdateCallback, onDataSourceItemUpdatedCallback,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags, null, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Insert a <see cref="CacheItem"/> to the cache asynchoronously
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="providerName">The datasource name to use for write-through operation.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemUpdatedCallback">A delegate that, if provided, is called when
        /// item is updated in data source.</param>
        [TargetMethod(3)]
        public virtual void InsertAsync(string key, CacheItem item, string providerName, DSWriteOption dsWriteOption,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 3;
            try
            {
                InsertAsyncOperation(key, item.Value,
                    item.Dependency, item.SyncDependency,
                    item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    dsWriteOption,
                    item.ItemRemoveCallback, item.ItemUpdateCallback,
                    item.AsyncItemUpdateCallback,
                    onDataSourceItemUpdatedCallback,
                    item.IsResyncExpiredItems,
                    item.Group, item.SubGroup,
                    item.Tags, providerName, item.NamedTags,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter,
                    null, -1, -1, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        internal virtual CacheItemVersion InsertOperation(string key, object value,
            CacheDependency dependency, CacheSyncDependency syncDependency, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, DSWriteOption dsWriteOption,
            CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback, bool isResyncExpiredItems,
            string group, string subGroup, LockHandle lockHandle, CacheItemVersion version,
            LockAccessType accessType, Tag[] tags, string providerName, string resyncProviderName,
            NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter,
            EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags, string clientId,
            short updateCallbackId, short removeCallbackId, short dsItemUpdateCallbackId,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");

            Hashtable queryInfo = null;
            BitSet flagMap = new BitSet();


            ValidateKeyValue(key, value);

            UsageStats stats = new UsageStats();
            stats.BeginSample();

            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }


            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();

            if (!string.IsNullOrEmpty(resyncProviderName))
                resyncProviderName = resyncProviderName.ToLower();


            if (dependency != null && !dependency.GetType().IsSerializable)
                throw new ArgumentException("dependency is not serializable");


            object lockId = (lockHandle == null) ? null : lockHandle.LockId;

            if (queryInfo == null)
            {
                queryInfo = new Hashtable();
                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(value);
                    queryInfo["named-tag-info"] = GetNamedTagsInfo(value, namedTags);
                }

                if (allowQueryTags)
                    queryInfo["tag-info"] = GetTagInfo(value, tags);
            }


            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                long objectSize = 0;

                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[]) value).Length : 0;


                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) value).Length);


                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(false, objectSize);

                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                if (dsItemUpdateCallbackId == -1)
                {
                    if (onDataSourceItemUpdatedCallback != null)
                        dsItemUpdateCallbackId = GetCallbackId(onDataSourceItemUpdatedCallback);
                }

                if (removeCallbackId == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback,
                            EventType.ItemRemoved, itemRemovedDataFilter);
                        removeCallbackId = callabackIds[1];
                    }
                    else if (onRemoveCallback != null)
                    {
                        removeCallbackId = GetCallbackId(onRemoveCallback);
                    }
                }

                if (updateCallbackId == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback,
                            EventType.ItemUpdated, itemUpdateDataFilter);
                        updateCallbackId = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackId = GetCallbackId(onUpdateCallback);
                    }
                }

                if (lockId != null && ((string) lockId) != string.Empty)
                    flagMap.SetBit(BitSetConstants.LockedItem);
                else
                    flagMap.UnsetBit(BitSetConstants.LockedItem);

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration &&
                    slidingExpiration != null && slidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both sliding and absolute expirations on a single item");

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration)
                    absoluteExpiration = absoluteExpiration.ToUniversalTime();


                CacheItemVersion itemVersion = CacheImpl.Insert(key,
                    value, dependency, syncDependency,
                    absoluteExpiration, slidingExpiration,
                    priority, removeCallbackId,
                    updateCallbackId, dsItemUpdateCallbackId,
                    isResyncExpiredItems, group, subGroup,
                    queryInfo, flagMap, lockId, version,
                    accessType, providerName, resyncProviderName,
                    itemUpdateDataFilter, itemRemovedDataFilter,
                    size, clientId, callbackType);


                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerUpdSample(stats.Current);
                    _perfStatsCollector.IncrementUpdPerSecStats();
                }

                return itemVersion;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        protected internal virtual void InsertAsyncOperation(string key, object value,
            CacheDependency dependency, CacheSyncDependency syncDependency, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, DSWriteOption dsWriteOption,
            CacheItemRemovedCallback onRemoveCallback, CacheItemUpdatedCallback onUpdateCallback,
            AsyncItemUpdatedCallback onAsyncItemUpdateCallback,
            DataSourceItemsUpdatedCallback onDataSourceItemUpdatedCallback,
            bool isResyncExpiredItems, string group, string subGroup, Tag[] tags, string providerName,
            NamedTagsDictionary namedTags, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter,
            EventDataFilter itemRemovedDataFilter, string clientId, short updateCallbackID, short removeCallbackId,
            short dsItemAddedCallbackID)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");

            ValidateKeyValue(key, value);

            if (dependency != null && !dependency.GetType().IsSerializable)
                throw new ArgumentException("dependency is not serializable");

            Hashtable queryInfo = new Hashtable();
            queryInfo["query-info"] = GetQueryInfo(value);
            queryInfo["tag-info"] = GetTagInfo(value, tags);
            queryInfo["named-tag-info"] = GetNamedTagsInfo(value, namedTags);

            short asyncCallbackId = -1;
            BitSet flagMap = new BitSet();
            long size = 0;

            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }

            if (!string.IsNullOrEmpty(providerName))
                providerName = providerName.ToLower();
            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                long objectSize = 0;
                long encryptedObjectSize = 0;
                long compressedObjectSize = 0;
                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[]) value).Length : 0;


                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                if (removeCallbackId == -1)
                {
                    if (cacheItemRemovedCallaback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback,
                            EventType.ItemRemoved, itemRemovedDataFilter);
                        removeCallbackId = callabackIds[1];
                    }
                    else if (onRemoveCallback != null)
                    {
                        removeCallbackId = GetCallbackId(onRemoveCallback);
                        itemRemovedDataFilter = EventDataFilter.DataWithMetadata;
                    }
                }

                if (updateCallbackID == -1)
                {
                    if (cacheItemUdpatedCallback != null)
                    {
                        short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback,
                            EventType.ItemUpdated, itemUpdateDataFilter);
                        updateCallbackID = callabackIds[0];
                    }
                    else if (onUpdateCallback != null)
                    {
                        updateCallbackID = GetCallbackId(onUpdateCallback);
                        itemUpdateDataFilter = EventDataFilter.None;
                    }
                }

                if (onAsyncItemUpdateCallback != null)
                    asyncCallbackId = GetCallbackId(onAsyncItemUpdateCallback);

                if (dsItemAddedCallbackID == -1)
                {
                    if (onDataSourceItemUpdatedCallback != null)
                        dsItemAddedCallbackID = GetCallbackId(onDataSourceItemUpdatedCallback);
                }

                if (absoluteExpiration != null && absoluteExpiration != Cache.NoAbsoluteExpiration &&
                    slidingExpiration != null && slidingExpiration != Cache.NoSlidingExpiration)
                    throw new ArgumentException(
                        "You can not set both sliding and absolute expirations on a single item");

                absoluteExpiration = ToUTC(absoluteExpiration);

                CacheImpl.InsertAsync(key, value, dependency, syncDependency,
                    absoluteExpiration, slidingExpiration, priority, removeCallbackId,
                    updateCallbackID, asyncCallbackId, dsItemAddedCallbackID,
                    isResyncExpiredItems, group, subGroup, queryInfo, flagMap,
                    providerName, null, itemUpdateDataFilter, itemRemovedDataFilter,
                    size, clientId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

#endregion

#region /                --- Key based notifications registration ---       /

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
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        public virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)

        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0)
                    TargetMethodAttribute.MethodOverload = 1;
                RegisterKeyNotificationCallback(key, updateCallback, removeCallback, true);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Registers ItemAdded, ItemUpdate or ItemRemoved events with cache
        /// </summary>
        /// <remarks>
        /// Client application can show interest in receiving events if an item is added, update or removed from the cache.
        /// As soon as the item is added, updated or removed from the cache, the client application is notified
        /// and actions can be taken accordingly.
        /// </remarks>
        /// <param name="cacheDataNotificationCallback">the CacheDataNotificationCallback that is invoked when an item is added, updated or removed from the cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on Item Added, Updated or Removed</param>
        /// <param name="datafilter">Tells whether to receive metadata, data with metadata or none when a notification is triggered</param>
        /// <example>
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Cache Notification
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheEventDescriptor descriptor=cache.RegisterCacheNotification(new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded, EventDataFilter.None);
        /// 
        /// </code>
        /// </example>
        public virtual CacheEventDescriptor RegisterCacheNotification(
            CacheDataNotificationCallback cacheDataNotificationCallback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (cacheDataNotificationCallback == null)
                throw new ArgumentException("cacheDataNotificationCallback");

            return RegisterCacheNotificationInternal(null, cacheDataNotificationCallback, eventType, datafilter, true);
        }

        /// <summary>
        /// Registers the ItemUpdate or ItemRemoved events for the specified key.
        /// </summary>
        /// <param name="key">The cache key used to reference the cache item.</param>
        /// <param name="selectiveCacheDataNotificationCallback">The CacheDataNotificationCallback that is invoked when an item is added, updated or removed from the cache</param>
        /// <param name="eventType">Tells whether the event is to be raised on Item Added, Updated or Removed</param>
        /// <example>
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated);
        /// 
        /// </code>
        /// </example>
        public void RegisterCacheNotification(string key,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType)
        {
            RegisterCacheNotification(key, selectiveCacheDataNotificationCallback, eventType, EventDataFilter.None);
        }

        /// <summary>
        /// Registers the ItemUpdate or ItemRemoved events for the specified key.
        /// </summary>
        /// <param name="key">The cache key used to reference the cache item.</param>
        /// <param name="selectiveCacheDataNotificationCallback">The CacheDataNotificationCallback that is invoked when an item is added, updated or removed from the cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on Item Added, Updated or Removed</param>
        /// <param name="datafilter">Tells whether to receive metadata, data with metadata or none when a notification is triggered</param>
        /// <example>
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated, EventDataFilter.None);
        /// 
        /// </code>
        /// </example>
        public virtual void RegisterCacheNotification(string key,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentNullException("key");

            if (selectiveCacheDataNotificationCallback == null)
                throw new ArgumentException("selectiveCacheDataNotificationCallback");

            RegisterCacheNotificationInternal(key, selectiveCacheDataNotificationCallback, eventType, datafilter, true);
        }

        internal virtual void RegisterCacheNotification(string key,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType,
            EventDataFilter datafilter, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentNullException("key");

            if (selectiveCacheDataNotificationCallback == null)
                throw new ArgumentException("selectiveCacheDataNotificationCallback");

            RegisterCacheNotificationInternal(key, selectiveCacheDataNotificationCallback, eventType, datafilter, true,
                callbackType);
        }

        internal virtual void RegisterPollingNotification(PollNotificationCallback callback, EventType eventType)
        {
            if (callback != null)
            {
                short callbackId = _eventManager.RegisterPollingEvent(callback, eventType);
            }
        }

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
        internal virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration,
            CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback);

                CacheImpl.RegisterKeyNotificationCallback(key, updateCallbackid, removeCallbackid,
                    Runtime.Events.EventDataFilter.None, notifyOnItemExpiration, callbackType);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal virtual CacheEventDescriptor RegisterCacheNotificationInternal(string key,
            CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter,
            bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)

        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            CacheEventDescriptor discriptor = null;

            try
            {
                if (key != null)
                {
                    short[] callbackRefs = _eventManager.RegisterSelectiveEvent(callback, eventType, datafilter);
                    CacheImpl.RegisterKeyNotificationCallback(key, callbackRefs[0], callbackRefs[1], datafilter,
                        notifyOnItemExpiration, callbackType);
                }
                else
                {
                    discriptor = _eventManager.RegisterGeneralEvents(callback, eventType, datafilter);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return discriptor;
        }

        internal virtual void RegisterCacheNotificationDataFilter(EventType eventType, EventDataFilter datafilter,
            short eventSequenceId)
        {
            if (CacheImpl != null)
            {
                CacheImpl.RegisterGeneralNotification(eventType, datafilter, eventSequenceId);
            }
        }

        /// <summary>
        /// Registers the ItemUpdate or ItemRemoved events for the specified keys.
        /// </summary>
        /// <param name="keys">An array of cache keys used to reference the cache items.</param>
        /// <param name="selectiveCacheDataNotificationCallback">The CacheDataNotificationCallback that is invoked when an item is added, updated or removed from the cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on item updated or removed</param>
        /// <example>
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys=new string[size];
        /// cache.RegisterCacheNotification(keys,new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated);
        /// 
        /// </code>
        /// </example>
        public void RegisterCacheNotification(string[] keys,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType)
        {
            RegisterCacheNotification(keys, selectiveCacheDataNotificationCallback, eventType, EventDataFilter.None);
        }

        /// <summary>
        /// Registers the ItemUpdate or ItemRemoved events for the specified keys.
        /// </summary>
        /// <param name="keys">An array of cache keys used to reference the cache items.</param>
        /// <param name="selectiveCacheDataNotificationCallback">The CacheDataNotificationCallback that is invoked when an item is added, updated or removed from the cache.</param>
        /// <param name="eventType">Tells whether the event is to be raised on item updated or removed</param>
        /// <param name="datafilter">This enum is to describe when registering an event, upon raise how much data is 
        /// retrieved from cache when the event is raised</param>
        /// <example>
        /// First create an ItemCallback
        /// <code>
        /// ItemCallback(string key, CacheEventArg e)
        /// {
        ///    ...
        /// }
        /// </code>
        /// Then register the Key Notification
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys=new string[size];
        /// cache.RegisterCacheNotification(keys,new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated, EventDataFilter.None);
        /// 
        /// </code>
        /// </example>
        public virtual void RegisterCacheNotification(string[] keys,
            CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentNullException("key");
            for (int i = 0; i < keys.Length; i++)
            {
                if (String.IsNullOrEmpty(keys[i]))
                    throw new ArgumentNullException("key can't be null or empty");
            }

            if (selectiveCacheDataNotificationCallback == null)
                throw new ArgumentException("selectiveCacheDataNotificationCallback");

            RegisterCacheDataNotificationCallback(keys, selectiveCacheDataNotificationCallback, eventType, datafilter,
                true);
        }

        internal virtual void RegisterCacheDataNotificationCallback(string[] key,
            CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter,
            bool notifyOnItemExpiration, CallbackType callbackType = Runtime.Events.CallbackType.PushBasedNotification)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            try
            {
                if (key != null)
                {
                    short[] callbackRefs = _eventManager.RegisterSelectiveEvent(callback, eventType, datafilter);
                    CacheImpl.RegisterKeyNotificationCallback(key, callbackRefs[0], callbackRefs[1], datafilter,
                        notifyOnItemExpiration, callbackType);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal void RegisterSurrogateKeyNotificationCallback(string[] keys, short update, short remove,
            string clientId, CallbackType callbackType = Runtime.Events.CallbackType.PullBasedCallback)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            try
            {
                if (keys != null)
                {
                    CacheImpl.RegisterKeyNotificationCallback(keys, update, remove, clientId, callbackType);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        internal short RegisterUpdateSorrogateCallback(CacheItemUpdatedCallback onUpdateCallback,
            CacheDataNotificationCallback cacheItemUpdatedCallback, EventDataFilter eventDataFilter)
        {
            short updateCallbackId = -1;
            try
            {
                if (cacheItemUpdatedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUpdatedCallback,
                        EventType.ItemUpdated, eventDataFilter);
                    updateCallbackId = callabackIds[0];
                }
                else if (onUpdateCallback != null)
                {
                    updateCallbackId = GetCallbackId(onUpdateCallback);
                }
            }
            catch
            {
            }

            return updateCallbackId;
        }

        internal short RegisterRemoveSorrogateCallback(CacheItemRemovedCallback onRemovedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallback, EventDataFilter eventDataFilter)
        {
            short removeCallbackId = -1;
            try
            {
                if (cacheItemRemovedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallback,
                        EventType.ItemRemoved, eventDataFilter);
                    removeCallbackId = callabackIds[1];
                }
                else if (onRemovedCallback != null)
                {
                    removeCallbackId = GetCallbackId(onRemovedCallback);
                }
            }
            catch
            {
            }

            return removeCallbackId;
        }


        /// <summary>
        /// Unregisters event that may have been registered against a specific key
        /// </summary>
        /// <param name="key">The cache key used to reference the cache item</param>
        /// <param name="callback">The CacheDataNotificationCallback that was specified while registering the event.</param>
        /// <param name="eventType">Type of the event to unregister</param>
        /// <example>
        /// Let us consider you registered an event against a key
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded, EventDataFilter.None);
        /// </code>
        /// Now, Unregister this event by providing the key, callback and eventtype
        /// <code>
        /// cache.UnRegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded);
        /// </code>
        /// </example>
        public virtual void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback,
            EventType eventType)
        {
            if (CacheImpl == null)
                if (CacheImpl == null)
                    throw new OperationFailedException("Cache is not initialized");
            if (key == null)
                throw new ArgumentNullException("key");
            if (callback == null)
                throw new ArgumentNullException("CacheDataNotificationCallback");

            try
            {
                short[] value = this._eventManager.UnregisterSelectiveNotification(callback, eventType);

                short update = value[0];
                short remove = value[1];

                CacheImpl.UnRegisterKeyNotificationCallback(key, update, remove);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        /// <summary>
        /// Unregisters any event that may have been registered by the user against multiple cache keys
        /// </summary>
        /// <param name="key">An array of cache keys used to reference the cache itemst</param>
        /// <param name="callback">The CacheDataNotificationCallback that was specified while registering the event.</param>
        /// <param name="eventType">Type of event to unregister</param>
        /// <example>
        /// Let us consider you registered an event against a bulk of keys
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys=new string[size];
        /// cache.RegisterCacheNotification(keys, new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded, EventDataFilter.None);
        /// </code>
        /// Now, Unregister this event by providing the key, callback and eventtype
        /// <code>
        /// cache.UnRegisterCacheNotification(keys, new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded);
        /// </code>
        /// </example>
        public virtual void UnRegisterCacheNotification(string[] key, CacheDataNotificationCallback callback,
            EventType eventType)

        {
            if (CacheImpl == null)
                if (CacheImpl == null)
                    throw new OperationFailedException("Cache is not initialized");
            if (key == null)
                throw new ArgumentNullException("key");
            if (callback == null)
                throw new ArgumentNullException("CacheDataNotificationCallback");

            try
            {
                short[] value = this._eventManager.UnregisterSelectiveNotification(callback, eventType);

                short update = value[0];
                short remove = value[1];

                CacheImpl.UnRegisterKeyNotificationCallback(key, update, remove);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        /// <summary>
        /// Unregisters a cache level event that may have been registered
        /// </summary>
        /// <param name="discriptor">The descriptor returned when the general event was registered</param>
        /// <example>
        /// Let us consider you registered an event against a cache
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// CacheEventDescriptor eDescriptor=cache.RegisterCacheNotification(new CacheDataNotificationCallback(ItemCallback), EventType.ItemAdded, EventDataFilter.None);
        /// </code>
        /// Now, Unregister this event by using the CacheEventDescriptor returned by regitering the event
        /// <code>
        /// cache.UnRegisterCacheNotification(eDescriptor);
        /// </code>
        /// </example>
        public virtual void UnRegisterCacheNotification(CacheEventDescriptor discriptor)
        {
            if (CacheImpl == null)
                if (CacheImpl == null)
                    throw new OperationFailedException("Cache is not initialized");
            if (discriptor == null)
                throw new ArgumentNullException("CacheEventDiscriptor");

            if (!discriptor.IsRegistered)
                return;

            _eventManager.UnregisterDiscriptor(discriptor);
        }

        internal virtual void UnregiserGeneralCacheNotification(EventType eventType)
        {
            if (CacheImpl != null)
            {
                CacheImpl.UnRegisterGeneralNotification(eventType, -1);
            }
        }

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
        internal virtual void RegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnItemExpiration)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback);

                CacheImpl.RegisterKeyNotificationCallback(key, updateCallbackid, removeCallbackid,
                    notifyOnItemExpiration);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
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
        public virtual void UnRegisterKeyNotificationCallback(string key, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)
        {
            try
            {
                TargetMethodAttribute.MethodOverload = 1;
                if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

                if (key == null) throw new ArgumentNullException("key");
                if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

                short updateCallbackId = EventManager.UnRegisterSelectiveCallback(updateCallback);
                short removeCallbackId = EventManager.UnRegisterSelectiveCallback(removeCallback);
                if (updateCallbackId == -1 && removeCallbackId == -1) return;

                try
                {
                    CacheImpl.UnRegisterKeyNotificationCallback(key, updateCallbackId, removeCallbackId);
                }
                catch (Exception)
                {
                    if (ExceptionsEnabled) throw;
                }
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
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
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        internal virtual void RegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnExpiration)
        {
            RegisterKeyNotificationCallback(keys, updateCallback, removeCallback, true,
                Runtime.Events.CallbackType.PushBasedNotification);
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
        [Obsolete(
            "This method is deprecated. 'Please use RegisterCacheNotification(string[] keys, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        internal virtual void RegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback, bool notifyOnExpiration, CallbackType callbackType)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");

            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            try
            {
                short updateCallbackid = -1;
                short removeCallbackid = -1;

                if (updateCallback != null) updateCallbackid = GetCallbackId(updateCallback);
                if (removeCallback != null) removeCallbackid = GetCallbackId(removeCallback);

                CacheImpl.RegisterKeyNotificationCallback(keys, updateCallbackid, removeCallbackid,
                    EventDataFilter.DataWithMetadata, notifyOnExpiration, callbackType);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
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
        public virtual void UnRegisterKeyNotificationCallback(string[] keys, CacheItemUpdatedCallback updateCallback,
            CacheItemRemovedCallback removeCallback)

        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("Keys count can not be zero");
            if (updateCallback == null && removeCallback == null) throw new ArgumentNullException();

            short updateCallbackId = EventManager.UnRegisterSelectiveCallback(updateCallback);
            short removeCallbackId = EventManager.UnRegisterSelectiveCallback(removeCallback);
            if (updateCallbackId == -1 && removeCallbackId == -1) return;

            try
            {
                CacheImpl.UnRegisterKeyNotificationCallback(keys, updateCallbackId, removeCallbackId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

#endregion

#region /                 --- Lock/Unlock ---       /

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
        [TargetMethod(1)]
        public virtual void Unlock(string key)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                CacheImpl.Unlock(key);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
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
        [TargetMethod(2)]
        public virtual void Unlock(string key, LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");
            object lockId = (lockHandle == null) ? null : lockHandle.LockId;
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            try
            {
                CacheImpl.Unlock(key, lockId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


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
        [TargetMethod(1)]
        public virtual bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");

            lockHandle = null;
            bool lockAcquired = false;
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                lockAcquired = CacheImpl.Lock(key, lockTimeout, out lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return lockAcquired;
        }


        /// <summary>
        /// Determines if item in cache is locked.
        /// </summary>
        /// <param name="key">key of item to check for lock</param>
        /// <param name="lockHandle">lockHandle will be set if item is locked</param>
        /// <returns>true if the item is locked, false otherwise</returns>
        internal virtual bool IsLocked(string key, ref LockHandle lockHandle)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");

            try
            {
                return CacheImpl.IsLocked(key, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return false;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual object Remove(string key)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                return Remove(key, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Delete("timestamp");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void Delete(string key)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                Delete(key, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating the data source.</param>
        /// <param name="onDataSourceItemRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed from the cache.</param>
        /// <param name="subGroup">The subGroup of the item to be removed from the cache/</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp", DSWriteOption.None, null, "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual object Remove(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
                return Remove(key, dsWriteOption, onDataSourceItemRemovedCallback, null, null,
                    LockAccessType.IGNORE_LOCK, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating the data source.</param>
        /// <param name="onDataSourceItemRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed from the cache.</param>
        /// <param name="subGroup">The subGroup of the item to be removed from the cache/</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp", DSWriteOption.None, null, "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual void Delete(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
                Delete(key, dsWriteOption, onDataSourceItemRemovedCallback, null, null, LockAccessType.IGNORE_LOCK,
                    null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating the data source.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="onDataSourceItemRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp", DSWriteOption.None, null, "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual object Remove(string key, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
                return Remove(key, dsWriteOption, onDataSourceItemRemovedCallback, null, null,
                    LockAccessType.IGNORE_LOCK, providerName);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Removes the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating the data source.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="onDataSourceItemRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp", DSWriteOption.None, null, "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(3)]
        public virtual void Delete(string key, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
                Delete(key, dsWriteOption, onDataSourceItemRemovedCallback, null, null, LockAccessType.IGNORE_LOCK,
                    providerName);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        internal virtual object Remove(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback, LockHandle lockHandle,
            CacheItemVersion version, LockAccessType accessType, string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");


            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }

            long objectSize = 0;
            long encryptedObjectSize = 0;
            long compressedObjectSize = 0;

            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();


                object lockId = (lockHandle == null) ? null : lockHandle.LockId; 


                BitSet flagMap = new BitSet();

                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                short dsItemRemovedCallbackId = -1;


                if (onDataSourceItemRemovedCallback != null)
                    dsItemRemovedCallbackId = GetCallbackId(onDataSourceItemRemovedCallback);


                CompressedValueEntry result = CacheImpl.Remove(key, flagMap, dsItemRemovedCallbackId, lockId, version,
                    accessType, providerName);


                if (result != null && result.Value != null)
                {
                    if (result.Value != null && result.Value is CallbackEntry)
                    {
                        result.Value = ((CallbackEntry) result.Value).Value;
                    }


                    if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = result.Value is byte[] ? ((byte[]) result.Value).Length : 0;
                    result.Value = SafeDeserialize(result.Value, _serializationContext, result.Flag);

                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerDelSample(stats.Current);
                        _perfStatsCollector.IncrementDelPerSecStats();
                    }

                    if (DebugAPIConfiguraions.LoggingEnabled)

                        LogSizeInfo(false, objectSize);


                    return result.Value;
                }
                else
                {
                    if (_perfStatsCollector != null)
                    {
                        stats.EndSample();
                        _perfStatsCollector.IncrementMsecPerDelSample(stats.Current);
                        _perfStatsCollector.IncrementDelPerSecStats();
                    }
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }

            return null;
        }


        internal virtual void Delete(string key, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback, LockHandle lockHandle,
            CacheItemVersion version, LockAccessType accessType, string providerName)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");


            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }


            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();


                object lockId = (lockHandle == null) ? null : lockHandle.LockId; 


                BitSet flagMap = new BitSet();


                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);


                short dsItemRemovedCallbackId = -1;


                if (onDataSourceItemRemovedCallback != null)
                    dsItemRemovedCallbackId = GetCallbackId(onDataSourceItemRemovedCallback);


                CacheImpl.Delete(key, flagMap, dsItemRemovedCallbackId, lockId, version, accessType, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        /// <summary>
        /// Removes an item from cache if it is not already locked or if the correct lock-id is specified.
        /// </summary>
        /// <param name="key">key of item to be removed</param>
        /// <param name="lockHandle">If the item is locked then, it can be removed only if the correct lockHandle is specified.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <example>The following example demonstrates how to remove a locked item from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// </code>
        /// Add an item int the cache.
        /// <code>
        /// theCache.Add("cachedItemKey", "cachedItemValue");
        /// </code>
        /// Create the lock-handle.
        /// <code>
        /// LockHandle lockHandle = new LockHandle();
        /// </code>    
        /// Get the added item from cache and acquire a lock.
        /// <code>
        /// object cachedItem = theCache.Get("cachedItemKey", ref lockHandle, true);
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///         //Now remove the cached item using lockHandle acquired earlier.
        ///         object removedItem = theCache.Remove("cachedItemKey", lockHandle);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(4)]
        public virtual object Remove(string key, LockHandle lockHandle)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 4;
                return Remove(key, DSWriteOption.None, null, lockHandle, null, LockAccessType.DEFAULT, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes an item from cache if it is not already locked or if the correct lock-id is specified.
        /// </summary>
        /// <param name="key">key of item to be removed</param>
        /// <param name="lockHandle">If the item is locked then, it can be removed only if the correct lockHandle is specified.</param>
        /// <example>The following example demonstrates how to remove a locked item from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// </code>
        /// Add an item int the cache.
        /// <code>
        /// theCache.Add("cachedItemKey", "cachedItemValue");
        /// </code>
        /// Create the lock-handle.
        /// <code>
        /// LockHandle lockHandle = new LockHandle();
        /// </code>    
        /// Get the added item from cache and acquire a lock.
        /// <code>
        /// object cachedItem = theCache.Get("cachedItemKey", ref lockHandle, true);
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///         //Now remove the cached item using lockHandle acquired earlier.
        ///         object removedItem = theCache.Remove("cachedItemKey", lockHandle);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>
        /// 
        [TargetMethod(4)]
        public virtual void Delete(string key, LockHandle lockHandle)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 4;
                Delete(key, DSWriteOption.None, null, lockHandle, null, LockAccessType.DEFAULT, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes an item from cache if the specified version is still the most recent version in the cache.
        /// </summary>
        /// <param name="key">key of item to be removed</param>
        /// <param name="version">The version of the item to be removed. The item is removed from the cache
        /// only if this is still the most recent version in the cache.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <example>The following example demonstrates how to remove a locked item from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// </code>
        /// Add an item in the cache.
        /// <code>
        /// theCache.Add("cachedItemKey", new "cachedItemValue");
        /// </code>
        /// Create the CacheItemVersion.
        /// <code>
        /// CacheItemVersion version = new CacheItemVersion();
        /// </code>    
        /// Get the added item from cache and get the item version.
        /// <code>
        /// object cachedItem = theCache.Get("cachedItemKey", DSReadOption.None, ref version);
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///         //Now remove the cached item using version acquired earlier.
        ///         object removedItem = theCache.Remove("cachedItemKey", version);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(5)]
        public virtual object Remove(string key, CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 5;
                return Remove(key, DSWriteOption.None, null, null, version, LockAccessType.COMPARE_VERSION, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Removes an item from cache if the specified version is still the most recent version in the cache.
        /// </summary>
        /// <param name="key">key of item to be removed</param>
        /// <param name="version">The version of the item to be removed. The item is removed from the cache
        /// only if this is still the most recent version in the cache.</param>
        /// <example>The following example demonstrates how to remove a locked item from the cache.
        /// 
        /// First create a CacheItem.
        /// <code>
        /// Cache theCache = NCache.InitializeCache("myreplicatedcache");
        /// </code>
        /// Add an item in the cache.
        /// <code>
        /// theCache.Add("cachedItemKey", new "cachedItemValue");
        /// </code>
        /// Create the CacheItemVersion.
        /// <code>
        /// CacheItemVersion version = new CacheItemVersion();
        /// </code>    
        /// Get the added item from cache and get the item version.
        /// <code>
        /// object cachedItem = theCache.Get("cachedItemKey", DSReadOption.None, ref version);
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///         //Now remove the cached item using version acquired earlier.
        ///         object removedItem = theCache.Remove("cachedItemKey", version);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(5)]
        public virtual void Delete(string key, CacheItemVersion version)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 5;
                Delete(key, DSWriteOption.None, null, null, version, LockAccessType.COMPARE_VERSION, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }


        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed.</param>
        /// <param name="subGroup">SubGroup of the item to be removed.</param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.Remove(keys, DSWriteOption.WriteBehind, new DataSourceItemsRemovedCallback(OnDataSourceItemsRemoved), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual IDictionary RemoveBulk(string[] keys, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                return RemoveBulk(keys, dsWriteOption, null, onDataSourceItemsRemovedCallback);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.RemoveBulk(keys);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(2)]
        public virtual IDictionary RemoveBulk(string[] keys)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
                return RemoveBulk(keys, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed.</param>
        /// <param name="subGroup">SubGroup of the item to be removed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.Remove(keys, DSWriteOption.WriteBehind, new DataSourceItemsRemovedCallback(OnDataSourceItemsRemoved), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual void DeleteBulk(string[] keys, DSWriteOption dsWriteOption,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                DeleteBulk(keys, dsWriteOption, null, onDataSourceItemsRemovedCallback);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.Remove(keys);
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(2)]
        public virtual void DeleteBulk(string[] keys)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
                DeleteBulk(keys, DSWriteOption.None, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsRemovedCallback">A delegate that, if provided, is called when
        /// <param name="providerName">A unique identifier for the data source</param>
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed.</param>
        /// <param name="subGroup">SubGroup of the item to be removed.</param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.Remove(keys, DSWriteOption.WriteBehind, new DataSourceItemsRemovedCallback(OnDataSourceItemsRemoved), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>    
        /// 
        [TargetMethod(3)]
        public virtual IDictionary RemoveBulk(string[] keys, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            FindNull(keys);
            int noOfObjects = 0;
            long sumObjectSize = 0;
            long sumEncryptedObjectSize = 0;
            long sumCompressedObjecSize = 0;
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();

                BitSet flagMap = new BitSet();
                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                short dsItemsRemovedCallbackId = -1;
                if (onDataSourceItemsRemovedCallback != null)
                    dsItemsRemovedCallbackId = GetCallbackId(onDataSourceItemsRemovedCallback, keys.Length);

                if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                    providerName == null)
                {
                    providerName = _defaultWriteThruProvider;
                }


                IDictionary table = CacheImpl.Remove(keys, flagMap, providerName, dsItemsRemovedCallbackId);

                if (table != null)
                {
                    object[] keyArr = new object[table.Count];

                    table.Keys.CopyTo(keyArr, 0);

                    IEnumerator ie = keyArr.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        CompressedValueEntry result = table[ie.Current] as CompressedValueEntry;
                        if (result != null)
                        {
                            if (result.Value is CallbackEntry)
                            {
                                CallbackEntry e = result.Value as CallbackEntry;
                                result.Value = e.Value;
                            }


                            if (_perfStatsCollector != null && result.Value != null && result.Value is byte[])
                                _perfStatsCollector.IncrementAvgItemSize(((byte[]) result.Value).Length);


                            if (DebugAPIConfiguraions.LoggingEnabled)
                                sumObjectSize += result.Value != null && result.Value is byte[]
                                    ? ((byte[]) result.Value).Length
                                    : 0;

                            table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                        }
                    }
                }

                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize, noOfObjects);


                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerDelBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByDelPerSecStats(keys.Length);
                }

                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }

        /// <summary>
        /// Removes the objects from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="onDataSourceItemsRemovedCallback">A delegate that, if provided, is called when
        /// <param name="providerName">A unique identifier for the data source</param>
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed.</param>
        /// <param name="subGroup">SubGroup of the item to be removed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// 
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.Remove(keys, DSWriteOption.WriteBehind, new DataSourceItemsRemovedCallback(OnDataSourceItemsRemoved), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(3)]
        public virtual void DeleteBulk(string[] keys, DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemsRemovedCallback)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            FindNull(keys);

            try
            {
                BitSet flagMap = new BitSet();
                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                UsageStats stats = new UsageStats();
                stats.BeginSample();

                short dsItemsRemovedCallbackId = -1;
                if (onDataSourceItemsRemovedCallback != null)
                    dsItemsRemovedCallbackId = GetCallbackId(onDataSourceItemsRemovedCallback, keys.Length);

                if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                    providerName == null)
                {
                    providerName = _defaultWriteThruProvider;
                }


                CacheImpl.Delete(keys, flagMap, providerName, dsItemsRemovedCallbackId);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerDelBulkSample(stats.Current);
                    _perfStatsCollector.IncrementByDelPerSecStats(keys.Length);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

#region /                 --- Delete/Remove Query ---            /

        /// <summary>
        /// Executes delete statements on <see cref="Cache"/>.
        /// </summary>
        /// <param name="query">simple SQL like query syntax</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <returns> returns the number of rows affected.</returns>
        /// <example>
        /// 
        /// Only Delete Query is supported yet.
        /// 
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// Cache _cache = NCache.InitializeCache("myCache");
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Name", "Paul Jones");
        /// string query="delete Test.Application.Employee where this.Name = ?";
        /// ICacheReader reader=_cache.ExecuteNonQuery(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Salary", 2000);
        /// query="delete Test.Application.Employee where this.Salary > ?";
        /// reader=_cache.ExecuteNonQuery(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Name", "Paul jones");
        /// values.Add("Salary", 2000);
        /// query="delete Test.Application.Employee where this.Name = ? and this.Salary > ?";
        /// reader=_cache.ExecuteNonQuery(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Name", "Paul Jones");
        /// values.Add("Salary", 2000);
        /// query="delete Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)";
        /// reader=_cache.ExecuteNonQuery(query,values);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual int ExecuteNonQuery(string query, IDictionary values)
        {
            int effectedKeys = 0;
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");

            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                effectedKeys = CacheImpl.ExecuteNonQuery(query, values);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return effectedKeys;
        }

#endregion

        [TargetMethod(1)]
        public virtual void RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback,
            DSWriteOption dsWriteOption, DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                RemoveAsync(key, onAsyncItemRemoveCallback, dsWriteOption, null, onDataSourceItemRemovedCallback);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Removes the object from the <see cref="Cache"/> asynchronously.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="dsWriteOption">Options regarding updating data source</param>
        /// <param name="providerName">WriteThru provider name</param>
        /// <param name="onAsyncItemRemoveCallback">The delegate that can be used by the client application to get the
        /// result of the Asynchronous Remove operation.</param>
        /// <param name="onDataSourceItemRemovedCallback">A delegate that, if provided, is called when
        /// item is removed from data source.</param>
        /// <param name="group">The group of the item to be removed.</param>
        /// <param name="subGroup">SubGroup of the item to be removed.</param>
        /// <returns>The item removed from the Cache. If the value in the key parameter 
        /// is not found, returns a null reference (Nothing in Visual Basic).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// This is similar to <see cref="Remove(string)"/> except that the operation is performed asynchronously.
        /// A <see cref="ItemRemoved"/> event is fired upon successful completion of this method.It is not 
        /// possible to determine if the actual operation has failed, therefore 
        /// use this operation for the cases when it does not matter much.
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
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
        /// OnDataSourceItemsRemoved(IDictionary result)
        /// {
        /// ...
        /// }
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.RemoveAsync("timestamp", DSWriteOption.WriteBehind, new AsyncItemRemovedCallback(OnAsyncItemRemoved), new DataSourceItemsRemovedCallback(OnDataSourceItemsRemoved), "group-name", "subGroup-name");
        /// 
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual void RemoveAsync(string key, AsyncItemRemovedCallback onAsyncItemRemoveCallback,
            DSWriteOption dsWriteOption, string providerName,
            DataSourceItemsRemovedCallback onDataSourceItemRemovedCallback)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            short asyncOperationCallbackID = -1;
            short dsItemsRemovedCallbackId = -1;

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
            if (onAsyncItemRemoveCallback != null)
            {
                asyncOperationCallbackID = GetCallbackId(onAsyncItemRemoveCallback);
            }

            if (onDataSourceItemRemovedCallback != null)
            {
                dsItemsRemovedCallbackId = GetCallbackId(onDataSourceItemRemovedCallback);
            }

            if ((dsWriteOption == DSWriteOption.WriteThru || dsWriteOption == DSWriteOption.WriteBehind) &&
                providerName == null)
            {
                providerName = _defaultWriteThruProvider;
            }

            try
            {
                BitSet flagMap = new BitSet();
                flagMap = this.SetDSUpdateOptBit(flagMap, dsWriteOption);

                CacheImpl.RemoveAsync(key, flagMap, asyncOperationCallbackID, dsItemsRemovedCallbackId, providerName);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Remove the group from cache.
        /// </summary>
        /// <param name="group">group to be removed.</param>
        /// <param name="subGroup">subGroup to be removed.</param>
        [TargetMethod(1)]
        public virtual void RemoveGroupData(string group, string subGroup)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (group == null) throw new ArgumentNullException("group");
            if (group == null && subGroup != null) throw new ArgumentException("group must be specified for sub group");
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            try
            {
                CacheImpl.Remove(group, subGroup);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return;
        }

#endregion

#region /                 --- Search ---            /

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <returns>Returns a list of cache keys</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
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
        [TargetMethod(1)]
        [Obsolete("use ExecuteReader instead")]
        public virtual ICollection Search(string query, IDictionary values)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");

            QueryResultSet resultSet = null;
            IList collection = new ArrayList();
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            try
            {
                resultSet = CacheImpl.Search(query, values);

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common
                                    .Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG",
                                    ((Alachisoft.NCache.Common.Queries.AverageResult) resultSet.AggregateFunctionResult
                                        .Value).Average);
                            }

                            collection.Add(resultSet.AggregateFunctionResult.Value);
                        }

                        break;

                    case QueryType.SearchKeys:
                        collection = resultSet.SearchKeysResult;
                        break;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return collection;
        }

        [TargetMethod(1)]
        [Obsolete("Use ExecuteReader instead")]
        public virtual IDictionary SearchEntries(string query, IDictionary values)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");
            if (query.ToLower().Contains("group by"))
                throw new OperationFailedException(
                    "Queries with GROUP BY clause can not be executed using SearchEntries API.");

            QueryResultSet resultSet = null;
            Hashtable resultValues = new Hashtable();
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            try
            {
                resultSet = CacheImpl.SearchEntries(query, values);

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common
                                    .Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG",
                                    ((Alachisoft.NCache.Common.Queries.AverageResult) resultSet.AggregateFunctionResult
                                        .Value).Average);
                            }

                            resultValues.Add(resultSet.AggregateFunctionResult.Key.ToString(),
                                resultSet.AggregateFunctionResult.Value);
                        }

                        break;

                    case QueryType.SearchEntries:
                        long sumCompressedObjectSize = 0;
                        long sumEncryptedObjectSize = 0;
                        long sumObjectSize = 0;
                        int noOfObjects = 0;

                        IDictionaryEnumerator ide = resultSet.SearchEntriesResult.GetEnumerator();

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

                                if (cmpEntry.Value is CallbackEntry)
                                {
                                    CallbackEntry e = cmpEntry.Value as CallbackEntry;
                                    cmpEntry.Value = e.Value;
                                }


                                if (_perfStatsCollector != null && cmpEntry.Value != null && cmpEntry.Value is byte[])
                                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) cmpEntry.Value).Length);


                                if (DebugAPIConfiguraions.LoggingEnabled)
                                {
                                    noOfObjects++;
                                    sumObjectSize += cmpEntry.Value is byte[] ? ((byte[]) cmpEntry.Value).Length : 0;
                                }

                                resultValues[ide.Key] =
                                    SafeDeserialize(cmpEntry.Value, _serializationContext, cmpEntry.Flag);
                            }
                        }

                        if (DebugAPIConfiguraions.LoggingEnabled)
                            LogSizeInfo(true, sumObjectSize, noOfObjects);

                        break;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return resultValues;
        }

        /// <summary>
        ///  Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <param name="getData">Flag to recieve the values/data alongwith keys</param>
        /// <param name="chunkSize">Size of data/keys packets recieved after search, default value is  512 * 1024 kb's</param>
        /// <returns>Returns a cache data reader <see cref="CacheReader"/> </returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("ProductName", "Chai");
        /// values.Add("UnitsInStock", 250);
        /// 
        /// // Instead of Product, specify fully qualified name of your custom class.
        /// string query = "SELECT Product where this.ProductName = ?";
        /// 
        /// try
        ///  {
        ///     ICacheReader reader = cache.ExecuteReader(query, values, true, 50);
        ///     if (reader.FieldCount > 0)
        ///       {
        ///           while (reader.Read())
        ///           {
        ///              object category = reader.GetValue(0);
        ///               //perform operations
        ///           }
        ///       }
        ///     else
        ///       {
        ///          //perform operations
        ///       }
        ///      reader.Close();
        ///  }
        ///  
        /// catch
        ///  {
        ///  //handle exception
        ///  }
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual ICacheReader ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");
            ICacheReader reader = null;
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            try
            {
                bool isClientReader = false;
                string value = System.Configuration.ConfigurationManager.AppSettings["UseClientSideReader"];
                if (!String.IsNullOrEmpty(value))
                {
                    isClientReader = Convert.ToBoolean(value);
                }

                if (isClientReader)
                {
                    isClientReader = !(query.ToLower().Contains("group by") || query.ToLower().Contains("order by"));
                }

                if (isClientReader)
                {
                    int prevOverload = 0;
                    TargetMethodAttribute.MethodOverload = TargetMethodAttribute.MethodOverload;
                    IClientRecordSetEnumerator clientRecordSet = null;
                    TargetMethodAttribute.MethodOverload = 0;
                    QueryResultSet resultSet = CacheImpl.Search(query, values);
                    TargetMethodAttribute.MethodOverload = prevOverload;


                    switch (resultSet.Type)
                    {
                        case QueryType.AggregateFunction:
                            IList resultAggregate = new ArrayList();
                            if (resultSet.AggregateFunctionResult.Value != null)
                            {
                                if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache
                                        .Common.Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                                {
                                    resultSet.AggregateFunctionResult = new DictionaryEntry("AVG",
                                        ((Alachisoft.NCache.Common.Queries.AverageResult) resultSet
                                            .AggregateFunctionResult.Value).Average);
                                }

                                resultAggregate.Add(resultSet.AggregateFunctionResult.Value);
                            }

                            clientRecordSet = new ClientRecordSetEnumerator(resultAggregate, this, false);
                            break;
                        case QueryType.SearchKeys:
                            clientRecordSet = new ClientRecordSetEnumerator(resultSet.SearchKeysResult, this, getData);
                            break;
                    }

                    reader = new ClientDataReader(clientRecordSet);
                }
                else
                {
                    reader = new CacheDataReader(CacheImpl.ExecuteReader(query, values, getData, chunkSize));
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return reader;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <returns>Returns a cache data reader <see cref="CacheReader"/></returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// 
        /// Cache _cache = NCache.InitializeCache("myCache");
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Name", "Paul Jones");
        /// string query="select Test.Application.Employee where this.Name = ?";
        /// ICacheReader reader=_cache.ExecuteReader(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Salary", 2000);
        /// query="select Test.Application.Employee where this.Salary > ?";
        /// result=_cache.ExecuteReader(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Name", "Paul jones");
        /// values.Add("Salary", 2000);
        /// query="select Test.Application.Employee where this.Name = ? and this.Salary > ?";
        /// result=_cache.ExecuteReader(query,values);
        /// 
        /// values.Clear();
        /// values.Add("Name", "Paul Jones");
        /// values.Add("Salary", 2000);
        /// query="select Test.Application.Employee where Not(this.Name = 'Paul Jones' and this.Salary > 2000)";
        /// reader=_cache.ExecuteReader(query,values);
        /// if (reader.FieldCount > 0)
        ///     {
        ///      while (reader.Read())
        ///         {
        ///           object category = reader.GetValue(0);
        ///           //perform operations
        ///         }
        ///     }
        /// else
        ///    {
        ///       //perform operations
        ///    }
        ///reader.Close();
        ///  
        ///  
        /// </code>
        /// </example>
        ///
        [TargetMethod(2)]
        public virtual ICacheReader ExecuteReader(string query, IDictionary values)
        {
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
            try
            {
                return ExecuteReader(query, values, true, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified.
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <param name="values">The IDictionary of atribute names and values.</param>
        /// <param name="getData">Flag to recieve the values/data alongwith keys</param>
        /// <returns>Returns a cache data reader <see cref="CacheReader"/>
        /// </returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// string query="SELECT Product  WHERE this.ProductName = ? and this.UnitsInStock > ?";
        /// Hashtable values = new Hashtable();
        /// values.Add("ProductName", "Chai");
        /// values.Add("UnitsInStock", 250);
        /// try
        ///  {
        ///     ICacheReader reader = cache.ExecuteReader(query, values, true);
        ///     if (reader.FieldCount > 0)
        ///       {
        ///           while (reader.Read())
        ///           {
        ///              object category = reader.GetValue(0);
        ///               //perform operations
        ///           }
        ///       }
        ///     else
        ///       {
        ///          //perform operations
        ///       }
        ///      reader.Close();
        ///  }
        /// catch
        ///  {
        ///  //handle exception
        ///  }
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual ICacheReader ExecuteReader(string query, IDictionary values, bool getData)
        {
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
            try
            {
                return ExecuteReader(query, values, getData, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        ///  Opens <see cref="ExecuteReader"/> on server nodes to execute query, also registers continous query call backs for data, that falls on query's criteria; if any changes occur in that data.
        /// </summary>
        /// <param name="cquery">simple SQL like query syntax to query objects from cache</param>
        /// <returns>Returns a cache reader of first chunk of data on execution of data reader</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// <code>
        /// Cache _cache = NCache.InitializeCache("mycache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// string qry = "SELECT Product WHERE this.ProductQuantity = ?;"
        /// Hashtable indexTable = new Hashtable();
        /// indexTable.Add("ProductQuantity", 9);
        /// ContinuousQuery cQuery = new ContinuousQuery(qry, indexTable);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// cQuery.RegisterNotification(new QueryDataNotificationCallback(QueryItemCallBack), EventType.ItemUpdated | EventType.ItemRemoved, EventDataFilter.None); 
        /// 
        /// ICacheReader reader = _cache.ExecuteReaderCQ(cQuery);
        ///  if (reader.FieldCount > 0)
        ///       {
        ///           while (reader.Read())
        ///           {
        ///              object category = reader.GetValue(0);
        ///               //perform operations
        ///           }
        ///       }
        ///  else
        ///       {
        ///          //perform operations
        ///       }
        ///  reader.Close();
        /// 
        /// public void QueryItemCallBack(string key, CQEventArg arg)
        /// {
        ///     switch (arg.EventType)
        ///      {
        ///         case EventType.ItemRemoved:
        ///           //do something
        ///            break;
        ///         case EventType.ItemUpdated:
        ///          //do something
        ///            break;
        ///         case EventType.ItemAdded:
        ///           //do something
        ///            break;
        ///       }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual ICacheReader ExecuteReaderCQ(ContinuousQuery cquery)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                return ExecuteReaderCQ(cquery, true, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        ///  Opens <see cref="ExecuteReader"/> on server nodes to execute query, also registers continous query call backs for data, that falls on query's criteria; if any changes occur in that data.
        /// </summary>
        /// <param name="cquery">simple SQL like query syntax to query objects from cache</param>
        /// <param name="getData">Flag to recieve the values/data alongwith keys </param>
        /// <returns>Returns a cache reader of first chunk of data on execution of data reader</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// <code>
        /// Cache _cache = NCache.InitializeCache("mycache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// string qry = "SELECT Product WHERE this.ProductID = ?;"
        /// Hashtable indexTable = new Hashtable();
        /// indexTable.Add("ProductID", 4);
        /// ContinuousQuery cQuery = new ContinuousQuery(qry, indexTable);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// cQuery.RegisterNotification(new QueryDataNotificationCallback(QueryItemCallBack), EventType.ItemUpdated | EventType.ItemRemoved, EventDataFilter.None); 
        /// 
        /// ICacheReader reader = _cache.ExecuteReaderCQ(cQuery,true);
        /// 
        /// if (reader.FieldCount > 0)
        ///     {
        ///      while (reader.Read())
        ///       {
        ///         object category = reader.GetValue(0);
        ///         //perform operations
        ///       }
        ///     }
        /// else
        ///     {
        ///         //perform operations
        ///     }
        /// reader.Close();
        /// 
        /// public void QueryItemCallBack(string key, CQEventArg arg)
        /// {
        ///     switch (arg.EventType)
        ///      {
        ///         case EventType.ItemRemoved:
        ///           //do something
        ///            break;
        ///         case EventType.ItemUpdated:
        ///          //do something
        ///            break;
        ///         case EventType.ItemAdded:
        ///           //do something
        ///            break;
        ///       }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual ICacheReader ExecuteReaderCQ(ContinuousQuery cquery, bool getData)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
                return ExecuteReaderCQ(cquery, getData, -1);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        ///  Opens <see cref="ExecuteReader"/> on server nodes to execute query, also registers <see cref="ContinousQueries"/> call backs for data, that falls on query's criteria; if any changes occur in that data.  
        /// </summary>
        /// <param name="query">simple SQL like query syntax to query objects from cache</param>
        /// <param name="getData">Flag to recieve the values/data alongwith keys</param>
        /// <param name="chunkSize">Size of data/keys packets recieved after search, default value is  512 * 1024 kb's</param>
        /// <returns>Returns a cache reader of first chunk of data on execution of data reader</returns>
        /// <example>
        /// These operators are supported by NCache Queries.
        /// 1. Comparison Operators = , == , != , &lt;> , &lt; , > , &lt;=, >=, IN 
        /// 2. Logical Operators AND , OR , NOT 
        /// 3. Miscellaneous () , DateTime.Now , DateTime("any date time compatible string")
        /// <code>
        /// Cache _cache = NCache.InitializeCache("mycache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// string qry = "SELECT Product where this.ProductName = ?";
        /// Hashtable indexTable = new Hashtable();
        /// indexTable.Add("ProductName", "Tunnbr?d");
        /// ContinuousQuery cQuery = new ContinuousQuery(qry, indexTable);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// cQuery.RegisterNotification(new QueryDataNotificationCallback(QueryItemCallBack), EventType.ItemUpdated | EventType.ItemRemoved, EventDataFilter.None); 
        /// 
        /// ICacheReader reader = _cache.ExecuteReaderCQ(cQuery,true,50);
        /// if (reader.FieldCount > 0)
        ///     {
        ///         while (reader.Read())
        ///         {
        ///             object category = reader.GetValue(0);
        ///             //perform operations
        ///          }
        ///     }
        /// else
        ///     {
        ///       //perform operations
        ///     }
        /// reader.Close();
        /// 
        /// public void QueryItemCallBack(string key, CQEventArg arg)
        /// {
        ///     switch (arg.EventType)
        ///      {
        ///         case EventType.ItemRemoved:
        ///           //do something
        ///            break;
        ///         case EventType.ItemUpdated:
        ///          //do something
        ///            break;
        ///         case EventType.ItemAdded:
        ///           //do something
        ///            break;
        ///       }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual ICacheReader ExecuteReaderCQ(ContinuousQuery query, bool getData, int chunkSize)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
            if (String.IsNullOrEmpty(query.Query)) throw new ArgumentNullException("query");
            if (query.Values == null) throw new ArgumentNullException("values");
            if (!(query.NotifyAdd || query.NotifyUpdate || query.NotifyRemove))
                throw new ArgumentException(
                    "There is no event registered for this query. Please register at least one event (add/update/remove) before activating continous query.");
            try
            {
                ContinuousQueryManager.Register(query);

                return new CacheDataReader(CacheImpl.ExecuteReaderCQ(query, getData, chunkSize, query.ClientUniqueId,
                    query.NotifyAdd, query.NotifyUpdate, query.NotifyRemove));
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }
        
        public virtual void Log(string module, string message)
        {
        }

#endregion

        internal virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag,
            ref long size)
        {
            if (!InternalSerializationEnabled) return serializableObject;

            object serializedObject = null;

            if (CacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            if (serializableObject != null)
            {
                UsageStats statsSerialization = new UsageStats();
                statsSerialization.BeginSample();
                serializedObject = CacheImpl.SafeSerialize(serializableObject, serializationContext, ref flag,
                    CacheImpl, ref size);
                statsSerialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerSerialization(statsSerialization.Current);
            }

            return serializedObject;
        }

        internal virtual object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag)
        {
            if (!_serializationEnabled)
                return serializedObject;

            object deSerializedObject = null;

            if (CacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            if (serializedObject != null)
            {
                UsageStats statsDeserialization = new UsageStats();
                statsDeserialization.BeginSample();
                deSerializedObject = CacheImpl.SafeDeserialize(serializedObject, serializationContext, flag, CacheImpl);
                statsDeserialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerDeserialization(statsDeserialization.Current);
            }

            return deSerializedObject;
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception in case of failure.</para>
        /// </remarks>
        /// <returns>An enumerator to iterate through the <see cref="Cache"/> object.</returns>
        [TargetMethod(1)]
        public virtual IEnumerator GetEnumerator()
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                WebCacheEnumerator enumerator = new WebCacheEnumerator(_serializationContext, null, null, this);
                return enumerator;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        internal virtual IEnumerator GetEnumerator(string group, string subGroup)
        {
            WebCacheEnumerator enumerator = new WebCacheEnumerator(_serializationContext, group, subGroup, this);
            return enumerator;
        }

        internal virtual List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointer)
        {
            if (CacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            List<EnumerationDataChunk> chunks = null;

            try
            {
                chunks = CacheImpl.GetNextChunk(pointer);
            }
            catch (Exception ex)
            {
                //this is a empty call just to dispose the enumeration pointers for this particular enumerator
                //on all the nodes.
                for (int i = 0; i < pointer.Count; i++)
                {
                    pointer[i].isDisposable = true;
                }

                try
                {
                    CacheImpl.GetNextChunk(pointer);
                }
                catch (Exception)
                {
                }

                if (ExceptionsEnabled)
                    throw ex;
            }

            return chunks;
        }

#endregion

#region /   --- ToString() ---   /

        /// <summary>
        /// The string representation of the cache object. 
        /// </summary>
        public override string ToString()
        {
            return this._cacheId;
        }

#endregion


#region /           --- Stream Operations ---                      /

        internal virtual string OpenStream(string key, StreamModes mode, string group, string subGroup,
            DateTime absExpiration, TimeSpan slidingExpiration, CacheDependency dependency, CacheItemPriority priority)
        {
            return CacheImpl.OpenStream(key, mode, group, subGroup, absExpiration, slidingExpiration, dependency,
                priority);
        }

        internal virtual void CloseStream(string key, string lockHandle)
        {
            CacheImpl.CloseStream(key, lockHandle);
        }

        internal virtual int ReadFromStream(ref byte[] buffer, string key, string lockHandle, int offset,
            int streamOffset, int length)
        {
            return CacheImpl.ReadFromStream(ref buffer, key, lockHandle, offset, streamOffset, length);
        }

        internal virtual void WriteToStream(string key, string lockHandle, byte[] buffer, int srcOffset, int dstOffset,
            int length)
        {
            CacheImpl.WriteToStream(key, lockHandle, buffer, srcOffset, dstOffset, length);
        }

        internal virtual long GetStreamLength(string key, string lockHandle)
        {
            return CacheImpl.GetStreamLength(key, lockHandle);
        }

#endregion


#region Map Reduce API

        /// <summary>
        /// Executes the MapReduce task on the cache.
        /// </summary>
        /// <param name="task">Instance of a MapReduceTask.</param>
        /// <returns>Returns an instance to track the submitted task for result and status</returns>
        /// <example>
        /// <b> Note: </b> Implemeting IReducerFactory,ICombinerFactory and ICmobiner is optional. 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// mapReduceTask.Mapper = new WordCountMapper();
        /// mapReduceTask.Combiner = new WordCountCombinerFactory();
        /// mapReduceTask.Reducer = new WordCountReducerFactory();
        /// 
        /// ITrackableTask trackableInstance = _cache.ExecuteTask(mapReduceTask);
        /// 
        /// //Mapper implementation
        /// public class WordCountMapper : IMapper
        /// {
        ///     string[] parsedline;
        ///     string line;
        ///     public void Map(Object key, Object value, IOutputMap context)
        ///     {
        ///         line = value.ToString();
        ///         parsedline = line.Split(' ');
        ///         for (int i = parsedline.Length; i>=0; i++)
        ///         {
        ///             context.Emit(parsedline[i], 1);
        ///         }
        ///     }
        ///     public void Dispose()
        ///    {
        ///        // Release Resources
        ///    }
        /// }
        /// 
        /// //CombinerFactory Implementation
        /// public class WordCountCombinerFactory : ICombinerFactory
        /// {
        ///     public ICombiner Create(object key)
        ///     {
        ///         WordCountCombiner wcCombiner = new WordCountCombiner();
        ///         return wcCombiner;
        ///     }
        /// }
        /// 
        /// //ICombiner Implementation
        /// public class WordCountCombiner : ICombiner
        /// {
        ///     int count = 0;
        ///     public void BeginCombine()
        ///     {
        ///         //any initialization
        ///     }
        ///      public void Combine(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public object FinishChunk()
        ///     {
        ///         return count;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// //IReducerFactory Implementation
        /// public class WordCountReducerFactory : IReducerFactory
        /// {
        ///     public IReducer Create(object key)
        ///     {
        ///         WordCountReducer wcReducer = new WordCountReducer(key);
        ///         return wcReducer;
        ///     }
        /// }
        ///
        /// //IReducer Implementation
        /// 
        /// public class WordCountReducer : IReducer
        /// {
        ///     int count = 0;
        ///     object key;
        ///     public WordCountReducer(object value)
        ///     {
        ///         key = value;
        ///     }
        ///     public void BeginReduce()
        ///     {
        ///         //perform operations
        ///     }
        ///     public void Reduce(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public KeyValuePair FinishReduce()
        ///     {
        ///         KeyValuePair kvp = null;
        ///         kvp.Key = key;
        ///         kvp.Value = count;
        ///         return kvp;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(1)]
        public virtual ITrackableTask ExecuteTask(MapReduceTask task)
        {
            try
            {
                if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
                return ExecuteTaskOperation(task, null, null, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Executes the MapReduce task on the cache using specified KeyFilter implementation.
        /// </summary>
        /// <param name="task">Instance of a MapReduceTask.</param>
        /// <param name="keyFilter">Instance of IKeyFilter implementation.</param>
        /// <returns>Returns an instance to track the submitted task for result and status.</returns>
        /// <example>
        /// <b> Note: </b> Implemeting IReducerFactory,ICombinerFactory and ICmobiner is optional. 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// MapReduceKeyFilter keyFilter= new  MapReduceKeyFilter();
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// mapReduceTask.Mapper = new WordCountMapper();
        /// mapReduceTask.Combiner = new WordCountCombinerFactory();
        /// mapReduceTask.Reducer = new WordCountReducerFactory();
        /// 
        /// ITrackableTask trackableInstance = _cache.ExecuteTask(mapReduceTask,keyFilter);
        /// 
        /// 
        /// //implementation of IKeyFilter
        /// public class MapReduceKeyFilter : IKeyFilter
        /// {
        ///  public bool FilterKey(object key)
        ///   {
        ///    try
        ///    {
        ///        if (key.ToString().Contains("hungry"))
        ///        {
        ///            return true;
        ///        }
        ///
        ///    }
        ///    catch (Exception exp)
        ///    {
        ///        //handle exception
        ///    }
        ///    return false;
        ///   }
        /// }
        ///
        ///
        /// //Mapper implementation
        /// public class WordCountMapper : IMapper
        /// {
        ///     string[] parsedline;
        ///     string line;
        ///     public void Map(Object key, Object value, IOutputMap context)
        ///     {
        ///         line = value.ToString();
        ///         parsedline = line.Split(' ');
        ///         for (int i = parsedline.Length; i>=0; i++)
        ///         {
        ///             context.Emit(parsedline[i], 1);
        ///         }
        ///     }
        ///     public void Dispose()
        ///    {
        ///        // Release Resources
        ///    }
        /// }
        /// 
        /// //CombinerFactory Implementation
        /// public class WordCountCombinerFactory : ICombinerFactory
        /// {
        ///     public ICombiner Create(object key)
        ///     {
        ///         WordCountCombiner wcCombiner = new WordCountCombiner();
        ///         return wcCombiner;
        ///     }
        /// }
        /// 
        /// //ICombiner Implementation
        /// public class WordCountCombiner : ICombiner
        /// {
        ///     int count = 0;
        ///     public void BeginCombine()
        ///     {
        ///         //any initialization
        ///     }
        ///      public void Combine(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public object FinishChunk()
        ///     {
        ///         return count;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// //IReducerFactory Implementation
        /// public class WordCountReducerFactory : IReducerFactory
        /// {
        ///     public IReducer Create(object key)
        ///     {
        ///         WordCountReducer wcReducer = new WordCountReducer(key);
        ///         return wcReducer;
        ///     }
        /// }
        ///
        /// //IReducer Implementation
        /// 
        /// public class WordCountReducer : IReducer
        /// {
        ///     int count = 0;
        ///     object key;
        ///     public WordCountReducer(object value)
        ///     {
        ///         key = value;
        ///     }
        ///     public void BeginReduce()
        ///     {
        ///         //perform operations
        ///     }
        ///     public void Reduce(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public KeyValuePair FinishReduce()
        ///     {
        ///         KeyValuePair kvp = null;
        ///         kvp.Key = key;
        ///         kvp.Value = count;
        ///         return kvp;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// </code>
        /// </example>
        /// 
        [TargetMethod(2)]
        public virtual ITrackableTask ExecuteTask(MapReduceTask task, IKeyFilter keyFilter)
        {
            if (keyFilter == null)
                throw new ArgumentNullException("keyFilter");
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 2;
            try
            {
                return ExecuteTaskOperation(task, keyFilter, null, null);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Executes the MapReduce task on the resultant items with specified query.
        /// </summary>
        /// <param name="task">Instance of a MapReduceTask.</param>
        /// <param name="query">Simple sql like NCache query string</param>
        /// <param name="parameters">NCache query string searchable parameters.</param>
        /// <returns>Returns an instance to track the submitted task for result and status.</returns>
        /// <example>
        /// <b> Note: </b> Implemeting IReducerFactory,ICombinerFactory and ICmobiner is optional. 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Clothes", "", 4, 2);
        /// product2 = new Product(4, ""Shoes", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// mapReduceTask.Mapper = new WordCountMapper();
        /// mapReduceTask.Combiner = new WordCountCombinerFactory();
        /// mapReduceTask.Reducer = new WordCountReducerFactory();
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Category", "Clothes");
        /// 
        /// string query = "SELECT Product WHERE this.Category IN (?)";
        /// ITrackableTask trackableInstance = _cache.ExecuteTask(mapReduceTask,query,values);  
        /// 
        /// 
        /// //Mapper implementation
        /// public class WordCountMapper : IMapper
        /// {
        ///     string[] parsedline;
        ///     string line;
        ///     public void Map(Object key, Object value, IOutputMap context)
        ///     {
        ///         line = value.ToString();
        ///         parsedline = line.Split(' ');
        ///         for (int i = parsedline.Length; i>=0; i++)
        ///         {
        ///             context.Emit(parsedline[i], 1);
        ///         }
        ///     }
        ///     public void Dispose()
        ///    {
        ///        // Release Resources
        ///    }
        /// }
        /// 
        /// //CombinerFactory Implementation
        /// public class WordCountCombinerFactory : ICombinerFactory
        /// {
        ///     public ICombiner Create(object key)
        ///     {
        ///         WordCountCombiner wcCombiner = new WordCountCombiner();
        ///         return wcCombiner;
        ///     }
        /// }
        /// 
        /// //ICombiner Implementation
        /// public class WordCountCombiner : ICombiner
        /// {
        ///     int count = 0;
        ///     public void BeginCombine()
        ///     {
        ///         //any initialization
        ///     }
        ///      public void Combine(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public object FinishChunk()
        ///     {
        ///         return count;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// //IReducerFactory Implementation
        /// public class WordCountReducerFactory : IReducerFactory
        /// {
        ///     public IReducer Create(object key)
        ///     {
        ///         WordCountReducer wcReducer = new WordCountReducer(key);
        ///         return wcReducer;
        ///     }
        /// }
        ///
        /// //IReducer Implementation
        /// 
        /// public class WordCountReducer : IReducer
        /// {
        ///     int count = 0;
        ///     object key;
        ///     public WordCountReducer(object value)
        ///     {
        ///         key = value;
        ///     }
        ///     public void BeginReduce()
        ///     {
        ///         //perform operations
        ///     }
        ///     public void Reduce(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public KeyValuePair FinishReduce()
        ///     {
        ///         KeyValuePair kvp = null;
        ///         kvp.Key = key;
        ///         kvp.Value = count;
        ///         return kvp;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// </code>
        /// </example>
        ///
        [TargetMethod(3)]
        public virtual ITrackableTask ExecuteTask(MapReduceTask task, string query, Hashtable parameters)
        {
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 3;
            try
            {
                return ExecuteTaskOperation(task, null, query, parameters);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Executes the MapReduce task on the resultant items with specified query using specified KeyFilter implementation.
        /// </summary>
        /// <param name="task">Instance of a MapReduceTask.</param>
        /// <param name="keyFilter">Instance of <see cref="IKeyFilter"/> implementation.</param>
        /// <param name="query">Simple sql like NCache query string</param>
        /// <param name="parameters">NCache query string searchable parameters.</param>
        /// <returns>Returns an instance to track the submitted task for result and status.</returns>
        /// <example>
        /// <b> Note: </b> Implemeting IReducerFactory,ICombinerFactory and ICmobiner is optional. 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Clothes", "", 4, 2);
        /// product2 = new Product(4, ""Shoes", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// MapReduceKeyFilter keyFilter= new  MapReduceKeyFilter();
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// mapReduceTask.Mapper = new WordCountMapper();
        /// mapReduceTask.Combiner = new WordCountCombinerFactory();
        /// mapReduceTask.Reducer = new WordCountReducerFactory();
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Category", "Clothes");
        /// string query = "SELECT Product WHERE this.Category IN (?)";
        /// 
        /// ITrackableTask trackableInstance = _cache.ExecuteTaskOperation(mapReduceTask,keyFilter,query,values);  
        /// 
        /// // Implementation of filters and mapper
        /// 
        /// //Implementation of IKeyFilter
        /// public class MapReduceKeyFilter : IKeyFilter
        /// {
        ///  public bool FilterKey(object key)
        ///   {
        ///    try
        ///    {
        ///        if (key.ToString().Contains("hungry"))
        ///        {
        ///            return true;
        ///        }
        ///
        ///    }
        ///    catch (Exception exp)
        ///    {
        ///        //handle exception
        ///    }
        ///    return false;
        ///   }
        /// }
        /// 
        /// //Mapper implementation
        /// public class WordCountMapper : IMapper
        /// {
        ///     string[] parsedline;
        ///     string line;
        ///     public void Map(Object key, Object value, IOutputMap context)
        ///     {
        ///         line = value.ToString();
        ///         parsedline = line.Split(' ');
        ///         for (int i = parsedline.Length; i>=0; i++)
        ///         {
        ///             context.Emit(parsedline[i], 1);
        ///         }
        ///     }
        ///     public void Dispose()
        ///    {
        ///        // Release Resources
        ///    }
        /// }
        /// 
        /// //CombinerFactory Implementation
        /// public class WordCountCombinerFactory : ICombinerFactory
        /// {
        ///     public ICombiner Create(object key)
        ///     {
        ///         WordCountCombiner wcCombiner = new WordCountCombiner();
        ///         return wcCombiner;
        ///     }
        /// }
        /// 
        /// //ICombiner Implementation
        /// public class WordCountCombiner : ICombiner
        /// {
        ///     int count = 0;
        ///     public void BeginCombine()
        ///     {
        ///         //any initialization
        ///     }
        ///      public void Combine(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public object FinishChunk()
        ///     {
        ///         return count;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// //IReducerFactory Implementation
        /// public class WordCountReducerFactory : IReducerFactory
        /// {
        ///     public IReducer Create(object key)
        ///     {
        ///         WordCountReducer wcReducer = new WordCountReducer(key);
        ///         return wcReducer;
        ///     }
        /// }
        ///
        /// //IReducer Implementation
        /// 
        /// public class WordCountReducer : IReducer
        /// {
        ///     int count = 0;
        ///     object key;
        ///     public WordCountReducer(object value)
        ///     {
        ///         key = value;
        ///     }
        ///     public void BeginReduce()
        ///     {
        ///         //perform operations
        ///     }
        ///     public void Reduce(object value)
        ///     {
        ///         count += int.Parse(value.ToString());
        ///     }
        ///     public KeyValuePair FinishReduce()
        ///     {
        ///         KeyValuePair kvp = null;
        ///         kvp.Key = key;
        ///         kvp.Value = count;
        ///         return kvp;
        ///     }
        ///     public void Dispose()
        ///     {
        ///         // Release Resources
        ///     }
        /// }
        /// 
        /// </code>
        /// </example>
        ///
        private ITrackableTask ExecuteTaskOperation(MapReduceTask task, IKeyFilter keyFilter, string query,
            Hashtable parameters)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (task.Mapper == null)
                throw new ArgumentNullException("Mapper cannot be null.");
            if (_cacheId == null || CacheImpl == null)
                throw new OperationFailedException("Cache is not initialized.");

            Guid taskId = Guid.NewGuid();
            MapReduceTaskResult result = new MapReduceTaskResult(this.CacheImpl, taskId.ToString());
            TaskListener listener = new TaskListener(result.OnTaskResult);
            try
            {
                short callbackId = _eventManager.RegisterMapReduceEvent(listener);
                result.UniqueId = callbackId;
                if (this.CacheImpl != null)
                {
                    this.CacheImpl.ExecuteMapReduceTask
                        (task, taskId.ToString(), MROutputOption.IN_MEMORY, callbackId, keyFilter, query, parameters);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        /// <summary> Get a Trackable instance of the task with specified taskId.
        /// </summary>
        /// <param name="taskId">Task Id of the Map reduce task</param>
        /// <returns>Returns an instance to track the task for result and status.</returns>
        /// <example>
        /// <code>
        /// Cache _cache = NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// // Implement Map Reduce Interferaces
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// 
        /// ITrackableTask trackableTask = _cache.ExecuteTask(mapReduceTask);
        /// string taskID1=trackableTask.TaskId;
        /// Random randomNumber = new Random();
        /// string taskId2 = randomNumber.Next(2).ToString();
        /// 
        /// ITrackableTask testResult = _cache.GetTaskResult(taskId1); //returns the trackable instance
        /// 
        /// testResult = _cache.GetTaskResult(taskId12); //returns nothing as no task with such id is being running
        /// </code>
        /// </example>
        ///        
        [TargetMethod(1)]
        public virtual ITrackableTask GetTaskResult(string taskId)
        {
            if (CacheImpl == null)
            {
                throw new OperationFailedException("Cache is not initialized");
            }

            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            MapReduceTaskResult result = new MapReduceTaskResult(this.CacheImpl, taskId);
            TaskListener listener = new TaskListener(result.OnTaskResult);

            // register on client side and get id
            short callbackId = _eventManager.RegisterMapReduceEvent(listener);

            //send to server side
            try
            {
                if (CacheImpl != null)
                {
                    CacheImpl.RegisterMapReduceCallbackListener(callbackId, taskId);
                }
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled)
                {
                    throw ex;
                }
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return result;
        }

        /// <summary> Get All the running tasks.
        /// </summary>
        /// <returns>List of taskIds of running Map Reduce tasks.</returns>
        /// <example>
        /// <code>
        /// Cache _cache = NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// // Implement Map Reduce Interferaces
        /// 
        /// MapReduceTask mapReduceTask = new MapReduceTask();
        /// IList runningTasks = _cache.GetRunningTasks();
        /// </code>
        /// </example>
        ///     
        [TargetMethod(1)]
        public virtual ArrayList GetRunningTasks()
        {
            if (TargetMethodAttribute.MethodOverload == 0) TargetMethodAttribute.MethodOverload = 1;
            if (CacheImpl == null || _cacheId == null)
            {
                throw new OperationFailedException("Cache is not initialized");
            }

            try
            {
                if (CacheImpl != null)
                {
                    return CacheImpl.GetRunningTasks();
                }
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled)
                {
                    throw ex;
                }
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return null;
        }

#endregion


#region Enty Processor API

        /// <summary>
        /// Execution of entry processer regardless of caching topology used, allows to execute code against a cache entry on server side without fetching any data on client side. 
        /// </summary>
        /// <param name="key">Key of Cache entry on which the EntryProcessor is executed.</param>
        /// <param name="entryProcessor">IEntryProcessor instance</param>
        /// <param name="dsReadOption">Readthru option, none if not provided explicity</param>
        /// <param name="readProviderName">Readthru provider name, default if not provided explicity </param>
        /// <param name="dsWriteOption">Writethru option, none if  not provided explicity</param>
        /// <param name="writeProviderName">Writethru provider name, default if not provided explicity</param>
        /// <param name="arguments">Arguments list for the process.</param>
        /// <returns>Returns an instance of IEntryProcessorResult.</returns>
        /// <example>
        /// Argumetns is optional. 
        /// Also exception can be thrown explicitily by specifying such cases while implementing IEntryProcessor interface. 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// //Get a new instance of sample Class implementing EntryProcessor interface.
        /// CustomEntryProcessor myProcessor = new CustomEntryProcessor();
        /// 
        /// _cache.Insert("1", "Value1");
        /// _cache.Insert("2", "Value2");
        /// _cache.Insert("15", "Value3");
        /// //Invoking the Entry processor on a single item.
        /// 
        /// Object invokerVal = _cache.Invoke("1", myProcessor); 
        /// //Invoking the Entry processor against a single item.
        /// invokerVal = _cache.Invoke("15", myProcessor);
        /// 
        /// //Implementation of IEntryProcesser Interface
        /// public class CustomEntryProcessor : IEntryProcessor
        /// {
        ///     public bool IgnoreLock()
        ///     {
        ///         return true;
        ///     }
        ///     public object ProcessEntry(IMutableEntry entry, params object[] arguments)
        ///     {
        ///      if (entry.Key.Equals("1"))
        ///        {
        ///         if (entry.Exists())
        ///          {
        ///            entry.Remove();
        ///            return 0;
        ///          }
        ///         else
        ///         {
        ///            entry.Remove();
        ///            return -1;
        ///         }
        ///       }
        ///      else if (entry.Equals("15"))
        ///     {
        ///        object value = "Greater Than 10";
        ///        entry.Value = value;
        ///        return value;
        ///      }
        ///
        ///    return 1;
        ///    }
        ///}
        /// </code>
        /// </example>
#if !NET40
        public object Invoke(string key, IEntryProcessor entryProcessor, params object[] arguments)
        {
            return Invoke(key, entryProcessor, DSReadOption.None, _defaultReadThruProvider, DSWriteOption.None, _defaultWriteThruProvider, arguments);
        }
        public object Invoke(string key, IEntryProcessor entryProcessor, DSReadOption dsReadOption, string readProviderName, DSWriteOption dsWriteOption, string writeProviderName, params object[] arguments)
#else
        [TargetMethod(1)]
        public object Invoke(string key, IEntryProcessor entryProcessor, DSReadOption dsReadOption = DSReadOption.None,
            string readProviderName = "default", DSWriteOption dsWriteOption = DSWriteOption.None,
            string writeProviderName = "default", params object[] arguments)
#endif
        {
            IEntryProcessorResult result = null;
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");
            if (entryProcessor == null)
                throw new ArgumentNullException("entryProcessor");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                ICollection map = (ICollection) Invoke(new string[] {key}, entryProcessor, dsReadOption,
                    readProviderName, dsWriteOption, writeProviderName, arguments);

                if (map == null)
                    return null;
                else
                {
                    IEnumerator itResult = map.GetEnumerator();
                    if (itResult.MoveNext())
                        result = (IEntryProcessorResult) itResult.Current;
                }

                if (!result.IsSuccessful)
                    throw result.Exception;

                return result.Value;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        ///  Execution of entry processer regardless of caching topology used, allows to execute code against a set of cache entries on server side without fetching any data on client side. 
        /// </summary>
        /// <param name="keys">Set of keys of Cache entries on which EntryProcessor will be executed.</param>
        /// <param name="entryProcessor">>IEntryProcessor instance</param>
        /// <param name="dsReadOption">Readthru option, none if not provided explicity</param>
        /// <param name="readProviderName">Readthru provider name, default if not provided explicity</param>
        /// <param name="dsWriteOption">Writethru option, none if  not provided explicity </param>
        /// <param name="writeProviderName">Writethru provider name, default if not provided explicity </param>
        /// <param name="arguments">Arguments list for the process. </param>
        /// <returns>Returns a collection of instances of IEntryProcessorResult. </returns>
        /// <example>
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// //Get a new instance of sample Class implementing EntryProcessor interface.
        /// CustomEntryProcessor myProcessor = new CustomEntryProcessor();
        /// string[] keys = new string[] { "1", "5", "12", "15" };
        /// 
        /// _cache.Insert(keys[0], "Value1");
        /// _cache.Insert(keys[1], "Value1");
        /// _cache.Insert(keys[2], "Value1");
        /// _cache.Insert(keys[3], "Value1");
        /// //Invoking the Entry processor against a set of items.
        /// 
        /// ICollection retEntries = _cache.Invoke(keys, myProcessor);
        /// 
        /// //Implementation of IEntryProcesser Interface
        /// public class CustomEntryProcessor : IEntryProcessor
        /// {
        ///     public bool IgnoreLock()
        ///     {
        ///         return true;
        ///     }
        ///     public object ProcessEntry(IMutableEntry entry, params object[] arguments)
        ///     {
        ///      if (entry.Key.Equals("1"))
        ///        {
        ///         if (entry.Exists())
        ///          {
        ///            entry.Remove();
        ///            return 0;
        ///          }
        ///         else
        ///         {
        ///            entry.Remove();
        ///            return -1;
        ///         }
        ///       }
        ///      else if (entry.Equals("15"))
        ///     {
        ///        object value = "Greater Than 10";
        ///        entry.Value = value;
        ///        return value;
        ///      }
        ///
        ///    return 1;
        ///    }
        ///}
        /// </code>
        /// </example>
#if !NET40
        public ICollection Invoke(string[] keys, IEntryProcessor entryProcessor, params object[] arguments)
        {
            return Invoke(keys, entryProcessor, DSReadOption.None, _defaultReadThruProvider, DSWriteOption.None, _defaultWriteThruProvider, arguments);
        }

        public ICollection Invoke(string[] keys, IEntryProcessor entryProcessor, DSReadOption dsReadOption, string readProviderName, DSWriteOption dsWriteOption, string writeProviderName, params object[] arguments)
#else
        [TargetMethod(2)]
        public virtual ICollection Invoke(string[] keys, IEntryProcessor entryProcessor,
            DSReadOption dsReadOption = DSReadOption.None, string readProviderName = "default",
            DSWriteOption dsWriteOption = DSWriteOption.None, string writeProviderName = "default",
            params object[] arguments)
#endif
        {
            if (string.IsNullOrEmpty(readProviderName))
                readProviderName = "default";
            if (string.IsNullOrEmpty(writeProviderName))
                writeProviderName = "default";

            if (readProviderName.Equals("default"))
                readProviderName = _defaultReadThruProvider;
            if (writeProviderName.Equals("default"))
                writeProviderName = _defaultWriteThruProvider;
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            if (FindDupliate(keys)) throw new ArgumentException("Duplicate keys found in provided keys array.");
            if (entryProcessor == null) throw new ArgumentNullException("entryProcessor");
            try
            {
                return InvokeEntryProcessorInternal(keys, entryProcessor, readProviderName, dsReadOption,
                    writeProviderName, dsWriteOption, arguments).Values;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        internal virtual Hashtable InvokeEntryProcessorInternal(string[] keys, IEntryProcessor entryProcessor,
            string readThruProviderName, DSReadOption dsReadOption, string writeThruProviderName,
            DSWriteOption dsWriteOption, params object[] arguments)
        {
            BitSet readOptionFlag = new BitSet();
            readOptionFlag = this.SetDSReadOptBit(readOptionFlag, dsReadOption);
            BitSet writeOptionFlag = new BitSet();
            writeOptionFlag = this.SetDSUpdateOptBit(writeOptionFlag, dsWriteOption);
            return CacheImpl.InvokeEntryProcessor(keys, entryProcessor, readThruProviderName, readOptionFlag,
                writeThruProviderName, writeOptionFlag, arguments);
        }

#endregion


#region Aggragator API

        /// <summary> 
        /// Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">For actual grouping and analytical operations on given data using Map Reduce Combiner and Reducer</param>
        /// <returns>Returns a single result depending upon given extractor.</returns>
        /// <example>
        /// IAggregator can perform following operations
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// int value = (int)cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.IntegerSum());
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                return Aggregate(extractor, aggregator, null, null, null, int.MaxValue);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">Returns a single result depending upon given extractor.</param>
        /// <param name="timeout">Time in millisecond in which if result is not returned, thread will be terminated and exception or null will be given.</param>
        /// <returns>Returns a single result depending upon given extractor.</returns>
        /// <example>
        /// IAggregator can perform following operations
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// object result =_cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.Count(), 60000);
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(2)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator, int timeout)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 2;
            try
            {
                return Aggregate(extractor, aggregator, null, null, null, timeout);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">Returns a single result depending upon given extractor.</param>
        /// <param name="keyFilter">Instance of IKeyFilter implementation.</param>
        /// <returns>Returns a single result depending upon given extractor.</returns>
        /// <example>
        /// IAggregator can perform following operations
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// AggregatorKeyFilter keyFilter= new AggregatorKeyFilter();
        /// 
        /// object result =_cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.Count(),keyFilter);
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        /// 
        /// 
        /// //implementation of IKeyFilter
        /// public class AggregatorKeyFilter : IKeyFilter
        /// {
        ///  public bool FilterKey(object key)
        ///   {
        ///    try
        ///    {
        ///        if (key.ToString().Contains("hungry"))
        ///        {
        ///            return true;
        ///        }
        ///
        ///    }
        ///    catch (Exception exp)
        ///    {
        ///        //handle exception
        ///    }
        ///    return false;
        ///   }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(3)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator, IKeyFilter keyFilter)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 3;
            try
            {
                return Aggregate(extractor, aggregator, null, null, keyFilter, int.MaxValue);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        ///  Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">Returns a single result depending upon given extractor.</param>
        /// <param name="query">Simple sql like NCache query string</param>
        /// <param name="parameters">NCache query string searchable parameters.</param>
        /// <returns>Returns a single result depending upon given extractor</returns>
        /// <example>
        /// IAggregator can perform following operations
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("ProductID", 3);
        /// String query = "SELECT Product WHERE this.ProductID=?;
        /// 
        /// object result =_cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.Count(),query,values);
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        [TargetMethod(4)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator, string query,
            Hashtable parameters)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 4;
            try
            {
                return Aggregate(extractor, aggregator, query, parameters, null, int.MaxValue);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">Returns a single result depending upon given extractor.</param>
        /// <param name="query">Simple sql like NCache query string</param>
        /// <param name="parameters">NCache query string searchable parameters.</param>
        /// <param name="timeout">Time in millisecond in which if result is not returned, thread will be terminated and exception or null will be returned.</param>
        /// <returns>Returns a single result depending upon given extractor.</returns>
        /// <example>
        /// IAggregator can perform following operations
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Clothes", "", 4, 2);
        /// product2 = new Product(4, "Shoes", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Category", "Clothes");
        /// String query = "SELECT Product WHERE this.Category=?;
        /// 
        /// object result =_cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.Count(),query,values, 50000);
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(5)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator, string query,
            Hashtable parameters, int timeout)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 5;
            try
            {
                return Aggregate(extractor, aggregator, query, parameters, null, timeout);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        /// <summary>
        /// Built upon MapReduce framework, processes distributed data records to return compiled and statistical results for analytical purposes.
        /// </summary>
        /// <param name="extractor">Implementation of IValueExtractor to extract the meaningful attributes from given objects</param>
        /// <param name="aggregator">Returns a single result depending upon given extractor.</param>
        /// <param name="keyFilter">Instance of IKeyFilter implementation.</param>
        /// <param name="timeout">Time in millisecond in which if result is not returned, thread will be terminated and exception or null will be given.</param>
        /// <returns>Returns a single result depending upon given extractor</returns>
        /// <example>
        /// IAggregator can perform following operations:
        /// Average, Sum, Min, Max, Count, Distinct.
        /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
        /// User can also implement custom aggregator, as well as aggregator for custom data types and also for custom functions like Mean, Median, Mode.
        /// 
        /// <code>
        /// Cache _cache= NCache.InitializeCache("myCache");
        /// 
        /// product1 = new Product(3, "Tunnbr?d", "", 4, 2);
        /// product2 = new Product(4, "Tunnbr?d", "", 5, 9);
        /// 
        /// _cache.Add("2202", product1);
        /// _cache.Add("2203", product2);
        /// 
        /// AggregatorKeyFilter keyFilter= new AggregatorKeyFilter();
        /// 
        /// object result =_cache.Aggregate(new BasicTypeExtractor(), BuiltInAggregator.IntegerMax(),keyFilter,40000);
        /// 
        /// // IValueExtractor Implementaion
        /// public class BasicTypeExtractor : IValueExtractor
        /// {
        ///     public object Extract(object value)
        ///     {
        ///         try
        ///         {
        ///             if (value.GetType() == typeof(int))
        ///             {
        ///                 return 0;
        ///             }
        ///             if (value.GetType() == typeof(float))
        ///             {
        ///                 return 0.0;
        ///             }
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             //handle exception
        ///         }
        ///         return value;
        ///     }
        /// }
        /// 
        /// 
        /// //implementation of IKeyFilter
        /// public class AggregatorKeyFilter : IKeyFilter
        /// {
        ///  public bool FilterKey(object key)
        ///   {
        ///    try
        ///    {
        ///        if (key.ToString().Contains("hungry"))
        ///        {
        ///            return true;
        ///        }
        ///
        ///    }
        ///    catch (Exception exp)
        ///    {
        ///        //handle exception
        ///    }
        ///    return false;
        ///   }
        /// }
        /// </code>
        /// </example>
        [TargetMethod(6)]
        public virtual object Aggregate(IValueExtractor extractor, IAggregator aggregator, IKeyFilter keyFilter,
            int timeout)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 6;
            try
            {
                return Aggregate(extractor, aggregator, null, null, keyFilter, timeout);
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        private object Aggregate(IValueExtractor extractor, IAggregator aggregator, string query, Hashtable parameters,
            IKeyFilter keyFilter, int timeout)
        {
            AggregatorTask aggregatorTask = new AggregatorTask(extractor, aggregator);
            Object value = null;
            bool isResultfatch = false;
            ITrackableTask reduceTaskResult = null;
            IDictionaryEnumerator enumeration = null;
            MapReduceTask mapReduceTask = aggregatorTask.CreateMapReduceTask();
            if (query != null && parameters != null)
            {
                reduceTaskResult = ExecuteTask(mapReduceTask, query, parameters);
            }
            else if (keyFilter != null)
            {
                reduceTaskResult = ExecuteTask(mapReduceTask, keyFilter);
            }
            else
            {
                reduceTaskResult = ExecuteTask(mapReduceTask);
            }

            if (reduceTaskResult != null)
            {
                try
                {
                    if (timeout != null)
                    {
                        enumeration = reduceTaskResult.GetResult((int) timeout).GetEnumerator();
                    }
                    else
                    {
                        enumeration = reduceTaskResult.GetResult().GetEnumerator();
                    }
                }
                catch (OperationFailedException op)
                {
                    throw op;
                }

                if (enumeration != null)
                {
                    if (enumeration.MoveNext())
                    {
                        DictionaryEntry entry = enumeration.Entry;
                        value = entry.Value;
                        isResultfatch = true;
                    }
                }
            }

            if (!isResultfatch)
            {
                value = GetDefaultValue(aggregatorTask.BuiltInAggregatorType);
            }

            return value;
        }
        
        private object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }

#endregion

        /// <summary>
        /// Unregisters notifications for the specified <see cref="ContinuousQuery"/>.
        /// </summary>
        /// <param name="query"><see cref="ContinuousQuery"/> to unregister notifications for.</param>
        /// <example>
        /// <code>
        /// 
        /// string queryString = "SELECT MyApplication.Employee WHERE this.Salary > ?";
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Salary", 50000);
        /// 
        /// ContinuousQuery query = new ContinuousQuery(queryString, values);
        /// 
        /// query.RegisterAddNotification(new ContinuousQueryItemAddedCallback(query_ItemAdded));
        /// query.RegisterUpdateNotification(new ContinuousQueryItemUpdatedCallback(query_ItemUpdated));
        /// query.RegisterRemoveNotification(new ContinuousQueryItemRemovedCallback(query_ItemRemoved));
        /// 
        /// cache.RegisterCQ(query);
        /// 
        /// cache.UnRegisterCQ(query);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual void UnRegisterCQ(ContinuousQuery query)

        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query.Query)) throw new ArgumentNullException("query");
            if (query.Values == null) throw new ArgumentNullException("values");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                if (!String.IsNullOrEmpty(query.ServerUniqueID) && !String.IsNullOrEmpty(query.ClientUniqueId))
                {
                    CacheImpl.UnRegisterCQ(query.ServerUniqueID, query.ClientUniqueId);
                }
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled) throw ex;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            if (_cqCacheClearCallback != null)
            {
                CacheCleared -= _cqCacheClearCallback;
                query.IsClearRegistered = false;
                _cqCacheClearCallback = null;
            }


            ContinuousQueryManager.UnRegister(query);
        }

        /// <summary>
        /// Registers the notifications based on the specified <see cref="ContinuousQuery"/> .
        /// </summary>
        /// <param name="query"><see cref="ContinuousQuery"/> to register notifications for.</param>
        /// <example>
        /// <code>
        /// 
        /// string queryString = "SELECT MyApplication.Employee WHERE this.Salary > ?";
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Salary", 50000);
        /// 
        /// ContinuousQuery query = new ContinuousQuery(queryString, values);
        /// 
        /// query.RegisterAddNotification(new ContinuousQueryItemAddedCallback(query_ItemAdded));
        /// query.RegisterUpdateNotification(new ContinuousQueryItemUpdatedCallback(query_ItemUpdated));
        /// query.RegisterRemoveNotification(new ContinuousQueryItemRemovedCallback(query_ItemRemoved));
        /// 
        /// cache.RegisterCQ(query);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        public virtual void RegisterCQ(ContinuousQuery query)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query.Query)) throw new ArgumentNullException("query");
            if (query.Values == null) throw new ArgumentNullException("values");
            if (!(query.NotifyAdd || query.NotifyUpdate || query.NotifyRemove))
                throw new ArgumentException(
                    "There is no event registered for this query. Please register at least one event (add/update/remove) before activating continous query.");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            try
            {
                query.ServerUniqueID = CacheImpl.RegisterCQ(query, query.ClientUniqueId, query.NotifyAdd,
                    query.NotifyUpdate, query.NotifyRemove);
            }
            catch (Exception ex)
            {
                if (ExceptionsEnabled) throw ex;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            if (query.CacheCleared != null && !query.IsClearRegistered)
            {
                _cqCacheClearCallback = new CacheClearedCallback(query.OnCacheCleared);
                CacheCleared += _cqCacheClearCallback;
                query.IsClearRegistered = true;
            }

            ContinuousQueryManager.Register(query);
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the specified <see cref="ContinuousQuery"/> and registers the notifications.
        /// </summary>
        /// <param name="query"><see cref="ContinuousQuery"/> to perform the search and register notifications for.</param>
        /// <example>
        /// <code>
        /// 
        /// string queryString = "SELECT MyApplication.Employee WHERE this.Salary > ?";
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Salary", 50000);
        /// 
        /// ContinuousQuery query = new ContinuousQuery(queryString, values);
        /// 
        /// query.RegisterAddNotification(new ContinuousQueryItemAddedCallback(query_ItemAdded));
        /// query.RegisterUpdateNotification(new ContinuousQueryItemUpdatedCallback(query_ItemUpdated));
        /// query.RegisterRemoveNotification(new ContinuousQueryItemRemovedCallback(query_ItemRemoved));
        /// 
        /// ICollection results = cache.SearchCQ(query);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        [Obsolete("use ExecuteReaderCQ instead")]
        public virtual ICollection SearchCQ(ContinuousQuery query)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query.Query)) throw new ArgumentNullException("query");
            if (query.Values == null) throw new ArgumentNullException("values");
            if (!(query.NotifyAdd || query.NotifyUpdate || query.NotifyRemove))
                throw new ArgumentException(
                    "There is no event registered for this query. Please register at least one event (add/update/remove) before activating continous query.");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            QueryResultSet resultSet = null;
            IList collection = new ArrayList();

            try
            {
                resultSet = CacheImpl.SearchCQ(query, query.ClientUniqueId, query.NotifyAdd, query.NotifyUpdate,
                    query.NotifyRemove);
                query.ServerUniqueID = resultSet.CQUniqueId;

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common
                                    .Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG",
                                    ((Alachisoft.NCache.Common.Queries.AverageResult) resultSet.AggregateFunctionResult
                                        .Value).Average);
                            }

                            collection.Add(resultSet.AggregateFunctionResult.Value);
                        }

                        break;

                    case QueryType.SearchKeys:
                        collection = resultSet.SearchKeysResult;
                        break;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            if (query.CacheCleared != null && !query.IsClearRegistered)
            {
                _cqCacheClearCallback = new CacheClearedCallback(query.OnCacheCleared);
                CacheCleared += _cqCacheClearCallback;
                query.IsClearRegistered = true;
            }

            ContinuousQueryManager.Register(query);

            return collection;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the specified <see cref="ContinuousQuery"/> and registers the notifications.
        /// </summary>
        /// <param name="query"><see cref="ContinuousQuery"/> to perform the search and register notifications for.</param>
        /// <example>
        /// <code>
        /// 
        /// string queryString = "SELECT MyApplication.Employee WHERE this.Salary > ?";
        /// 
        /// Hashtable values = new Hashtable();
        /// values.Add("Salary", 50000);
        /// 
        /// ContinuousQuery query = new ContinuousQuery(queryString, values);
        /// 
        /// query.RegisterAddNotification(new ContinuousQueryItemAddedCallback(query_ItemAdded));
        /// query.RegisterUpdateNotification(new ContinuousQueryItemUpdatedCallback(query_ItemUpdated));
        /// query.RegisterRemoveNotification(new ContinuousQueryItemRemovedCallback(query_ItemRemoved));
        /// 
        /// IDictionary results = cache.SearchEntriesCQ(query);
        /// 
        /// </code>
        /// </example>
        [TargetMethod(1)]
        [Obsolete("Use ExecuteReaderCQ instead")]
        public virtual IDictionary SearchEntriesCQ(ContinuousQuery query)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query.Query)) throw new ArgumentNullException("query");
            if (query.Values == null) throw new ArgumentNullException("values");
            if (!(query.NotifyAdd || query.NotifyUpdate || query.NotifyRemove))
                throw new ArgumentException(
                    "There is no event registered for this query. Please register at least one event (add/update/remove) before activating continous query.");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;
            QueryResultSet resultSet = null;
            Hashtable resultValues = new Hashtable();

            try
            {
                resultSet = CacheImpl.SearchEntriesCQ(query, query.ClientUniqueId, query.NotifyAdd, query.NotifyUpdate,
                    query.NotifyRemove);
                query.ServerUniqueID = resultSet.CQUniqueId;

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common
                                    .Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG",
                                    ((Alachisoft.NCache.Common.Queries.AverageResult) resultSet.AggregateFunctionResult
                                        .Value).Average);
                            }

                            resultValues.Add(resultSet.AggregateFunctionResult.Key.ToString(),
                                resultSet.AggregateFunctionResult.Value);
                        }

                        break;

                    case QueryType.SearchEntries:
                        long sumCompressedObjectSize = 0;
                        long sumEncryptedObjectSize = 0;
                        long sumObjectSize = 0;
                        int noOfObjects = 0;

                        IDictionaryEnumerator ide = resultSet.SearchEntriesResult.GetEnumerator();

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

                                if (cmpEntry.Value is CallbackEntry)
                                {
                                    CallbackEntry e = cmpEntry.Value as CallbackEntry;
                                    cmpEntry.Value = e.Value;
                                }


                                if (_perfStatsCollector != null && cmpEntry.Value != null && cmpEntry.Value is byte[])
                                    _perfStatsCollector.IncrementAvgItemSize(((byte[]) cmpEntry.Value).Length);

                                if (DebugAPIConfiguraions.LoggingEnabled)
                                {
                                    noOfObjects++;
                                    sumObjectSize += ((byte[]) cmpEntry.Value).Length;
                                }

                                resultValues[ide.Key] =
                                    SafeDeserialize(cmpEntry.Value, _serializationContext, cmpEntry.Flag);
                            }
                        }

                        if (DebugAPIConfiguraions.LoggingEnabled)
                            LogSizeInfo(true, sumObjectSize);

                        break;
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            if (query.CacheCleared != null && !query.IsClearRegistered)
            {
                _cqCacheClearCallback = new CacheClearedCallback(query.OnCacheCleared);
                CacheCleared += _cqCacheClearCallback;
                query.IsClearRegistered = true;
            }

            ContinuousQueryManager.Register(query);

            return resultValues;
        }

        private void RemoveDuplicateKeys(ref string[] keys)
        {
            Hashtable keysAndItems = new Hashtable(keys.Length);
            for (int item = 0; item < keys.Length; item++)
            {
                if (keys[item] != null)
                    keysAndItems[keys[item]] = null;
            }

            keys = new string[keysAndItems.Count];
            keysAndItems.Keys.CopyTo(keys, 0);
        }

        private void RemoveDuplicateTags(ref Tag[] tags)
        {
            Hashtable keysAndItems = new Hashtable(tags.Length);
            for (int index = 0; index < tags.Length; index++)
            {
                if (tags[index] != null && tags[index].TagName != null)
                {
                    keysAndItems[tags[index]] = null;
                }
                else
                {
                    throw new ArgumentException("Tag cannot be null.");
                }
            }

            tags = new Tag[keysAndItems.Count];
            keysAndItems.Keys.CopyTo(tags, 0);
        }

        /// <summary>
        /// Finds duplicate in the 'key' array.
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>        
        /// <returns>True if duplicate exists else false.</returns>       
        /// <exception cref="ArgumentException"><paramref name="keys"/> Thrown if duplicate is found.</exception>        
        private Boolean FindDupliate(string[] keys)
        {
            Hashtable hashtable = new Hashtable(keys.Length);
            bool duplicateFound = false;
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] == null)
                        throw new ArgumentNullException("key");

                    //If Count is less than the capacity of the Hashtable, this method is an O(1) operation. 
                    //If the capacity needs to be increased to accommodate the new element, this method becomes an O(n) operation, 
                    //where n is Count.
                    hashtable.Add(keys[i], null);
                }
            }
            catch (ArgumentNullException e)
            {
                throw new OperationFailedException(e.Message, e);
            }
            catch (ArgumentException e)
            {
                duplicateFound = true;
            }

            hashtable.Clear();
            return duplicateFound;
        }

        private void FindNull(string[] keys)
        {
            Hashtable hashtable = new Hashtable(keys.Length);
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] == null)
                        throw new ArgumentNullException("key");
                    //If Count is less than the capacity of the Hashtable, this method is an O(1) operation. 
                    //If the capacity needs to be increased to accommodate the new element, this method becomes an O(n) operation, 
                    //where n is Count.
                    hashtable.Add(keys[i], null);
                }
            }
            catch (ArgumentNullException e)
            {
                throw new OperationFailedException(e.Message, e);
            }
            catch (ArgumentException e)
            {
            }

            hashtable.Clear();
        }

        /// <summary>
        /// Adds Size info to hashtable for logging
        /// </summary>
        /// <param name="encryptionEnabled"></param>
        /// <param name="compressionEnabled"></param>
        /// <param name="isBulk"></param>
        /// <param name="objectSize">Size of Object after serialization. In case of Bulk, it will be the sum of size of objects.</param>
        /// <param name="encryptedObjectSize">Size of Object after encryption. In case of Bulk, it will be the sum of size of objects after encryption.</param>
        /// <param name="compressedObjectSize">Size of Object after compression. In case of Bulk, it will be the sum of size of objects after compression.</param>
        /// <param name="noOfObjects"></param>
        private void LogSizeInfo(bool isBulk, long objectSize, int noOfObjects)
        {
            APILogging.RuntimeAPILogItem rtAPILogItem = new APILogging.RuntimeAPILogItem();
            rtAPILogItem.IsBulk = isBulk;
            rtAPILogItem.NoOfObjects = noOfObjects;
            if (noOfObjects != 0)
            {
                rtAPILogItem.SizeOfObject = objectSize / noOfObjects;
            }

            APILogHashTable[System.Threading.Thread.CurrentThread.ManagedThreadId] = rtAPILogItem;
        }

        private void LogSizeInfo(bool isBulk, long objectSize)
        {
            LogSizeInfo(isBulk, objectSize, 1);
        }

        [TargetMethod(1)]
        internal PollingResult Poll(bool isNotifiedPoll)
        {
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;



            try
            {
                PollingResult result = CacheImpl.Poll();
                if (_perfStatsCollector != null && result != null)
                {
                    if (isNotifiedPoll)
                    {
                        if (result.RemovedKeys.Count == 0 && result.UpdatedKeys.Count == 0)
                            return result;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }

            return new PollingResult();
        }


        /// <summary>
        /// Returns the list of connected cache clients
        /// </summary>
        /// <returns>A list of ClientInfo objects of all the connected clients</returns>
        public virtual IList<ClientInfo> GetConnectedClientList()
        {
            return CacheImpl.GetConnectedClientList();
        }

        /// <summary>
        /// Displays the information related to this client
        /// </summary>
        public virtual ClientInfo ClientInfo
        {
            get { return CacheImpl.LocalClientInfo; }
        }


#region	/                 --- Touch ---           /

        [TargetMethod(1)]
        internal virtual void Touch(List<string> keys)
        {
            if (CacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (keys == null) throw new ArgumentNullException("keys");
            if (TargetMethodAttribute.MethodOverload == 0)
                TargetMethodAttribute.MethodOverload = 1;

            Hashtable newKeys = new Hashtable();
            for (int i = 0; i < keys.Count; i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                    newKeys[keys[i]] = null;
            }

            try
            {
                string[] keysArray = new string[newKeys.Count];
                newKeys.Keys.CopyTo(keysArray, 0);
                CacheImpl.Touch(new List<string>(keysArray));
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            finally
            {
                TargetMethodAttribute.MethodOverload = 0;
            }
        }

        #endregion
    }
}