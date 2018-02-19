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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;

using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Web.Caching
{
    internal sealed class InprocCache : CacheImplBase, IEnumerable, IRecordSetLoader
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
            get
            {
                return false;
            }
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
                _listener = new CacheEventsListener(_parent.EventListener, _nCache);
                _nCache.OnClientConnected(ClientID, _config.CacheId);
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
                else if (_refCount < 0) _refCount = 0;
                lock (NCache.Caches)
                {
                    if (_config != null)
                        NCache.Caches.Remove(_config.CacheId);
                }
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

        public override bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException("key");

            if (attributes == null) throw new ArgumentNullException("attributes");
            ExpirationHint hint = DependencyHelper.GetExpirationHint(attributes.AbsoluteExpiration, Cache.NoSlidingExpiration);
            return _nCache.AddExpirationHint(key, hint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

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
            if (_nCache != null) _nCache.Unlock(key, lockId, false, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
        public override void Clear(BitSet flagMap)
        {
            if (_nCache == null) return;

            CallbackEntry cbEntry = null;
            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            _nCache.Clear(flagMap, cbEntry, operationContext);
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

            return _nCache.Contains(key, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            return false;
        }

        #endregion

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
        /// <param name="absoluteExpiration"></param>
        /// <param name="slidingExpiration"></param>
        /// <param name="priority"></param>
        /// <param name="onRemoveCallback"></param>
        /// <returns></returns>
        private object MakeCompactEntry(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, Hashtable queryInfo, BitSet Flag, object lockId, LockAccessType accessType, EventDataFilter updateCallbackFilter, EventDataFilter removeCallabackFilter)
        {
            //we create a user binary object.
            if (this.SerializationEnabled)
            {
                value = UserBinaryObject.CreateUserBinaryObject((byte[])value); 
            }

            if ((short)onRemoveCallback != -1 || (short)onUpdateCallback != -1 )
                value = new CallbackEntry(ClientID, -1, value, onRemoveCallback, onUpdateCallback, Flag, updateCallbackFilter, removeCallabackFilter);
            ////we can not specify both tags and groups for the same cache item.
      
            byte expType = WebCacheHelper.EvaluateExpirationParameters(absoluteExpiration, slidingExpiration);
            int options = 0;

            if (expType < 2)
            {
                options = expType;
            }

            int prio = Convert.ToInt32(priority);
            prio += 2;
            prio = (prio << 2);
            options = options | prio;

            long expiration = expType == 1 ? absoluteExpiration.Ticks : slidingExpiration.Ticks;

            ExpirationHint hint =  null;

            object entry = new CompactCacheEntry(key, value, hint, 
                                                 expiration, (byte)options, null, queryInfo, Flag, lockId, accessType);
            return entry;
        }


        #region	/                 --- Add ---           /
        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        public override void Add(string key, object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration,
            CacheItemPriority priority, short onRemoveCallback, short onUpdateCallback, 
            Hashtable queryInfo, BitSet flagMap, EventDataFilter updateCallbackFilter,
            EventDataFilter removeCallabackFilter, long size)
        {

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

            object entry = MakeCompactEntry(key, value, absoluteExpiration, slidingExpiration,
                                             priority, onRemoveCallback, onUpdateCallback, queryInfo, flagMap, null, LockAccessType.IGNORE_LOCK, updateCallbackFilter, removeCallabackFilter);

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);

            _nCache.AddEntry(entry, operationContext);
        }


        /// <summary>
        /// Add array of <see cref="CacheItem"/> to the cache.
        /// </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
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
        ///	NCache.Cache.Add(keys, items);
        ///
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Add(keys, items);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Add(string[] keys, CacheItem[] items, long[] sizes)
        {
            if (_nCache == null)
                return null;

            ExpirationHint hint = null;

            object[] entries = new object[items.Length];

            short itemRemovedCallback = -1;
            short itemUpdatedCallback = -1;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                itemRemovedCallback = -1;
                itemUpdatedCallback = -1;

                if (item.CacheItemRemovedCallback != null)
                {
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventType.ItemRemoved, item.ItemRemovedCallabackDataFilter);
                    itemRemovedCallback = callabackIds[1];
                }
                if (item.CacheItemUpdatedCallback != null)
                {
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventType.ItemUpdated, item.ItemUpdatedCallabackDataFilter);
                    itemUpdatedCallback = callabackIds[0];
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

                entries[i] = MakeCompactEntry(key, item.Value,
                                              item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                                              itemRemovedCallback, itemUpdatedCallback, 
                                              item.QueryInfo, item.FlagMap, null, LockAccessType.IGNORE_LOCK, item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return _nCache.AddEntries(entries, operationContext);
        }

        #endregion

        #region	/                 --- Get ---           /

        /// <summary>
        /// Retrieves the specified item from the Cache object. If the object is read thru the data source,
        ///</summary>
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
        public override CompressedValueEntry Get(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            object lockId = lockHandle == null ? null : lockHandle.LockId;
            DateTime lockDate = new DateTime();

            if (_nCache != null)
            {

                CompressedValueEntry cmpEntry = _nCache.Get(key, flagMap, ref lockId, ref lockDate, lockTimeout, accessType, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                if (cmpEntry != null && cmpEntry.Value != null)
                {
                    //incase of inproc local cache we will get the user object directly...
                    //therefore, we need to confirm if the value is user binary object or not...
                    if (cmpEntry.Value is UserBinaryObject)
                    {
                        UserBinaryObject ubObject = cmpEntry.Value as UserBinaryObject;
                        cmpEntry.Value = ubObject.GetFullObject();
                    }
                }

                if (lockHandle == null) lockHandle = new LockHandle();
                lockHandle.LockId = lockId == null ? null : lockId as string;
                lockHandle.LockDate = lockDate;
                return cmpEntry;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the object from the cache for the given keys as key value pairs
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        ///  box server control.
        ///  <code>
        ///  
        /// 	NCache.Cache.Get(keys);
        ///  
        ///  </code>
        ///  Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        ///  <code>
        ///  
        /// 	Cache.Get(keys);
        ///  
        ///  </code> 
        ///  </example>
        public override IDictionary Get(string[] keys, BitSet flagMap)
        {
            if (_nCache != null)
            {
                IDictionary items = (IDictionary)_nCache.GetBulk(keys, flagMap, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

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
            return null;
        }

        public override object GetCacheItem(string key, BitSet flagMap, ref LockHandle lockHandle, TimeSpan lockTimeout, LockAccessType accessType)
        {
            if (lockHandle == null) lockHandle = new LockHandle();
            object lockId = lockHandle.LockId;
            DateTime lockDate = new DateTime();

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            CacheEntry entry = _nCache.GetCacheEntry(key, ref lockId, ref lockDate, lockTimeout, accessType, operationContext) as CacheEntry;

            if (entry != null)
            {
                if (entry.Value is CallbackEntry)
                {
                    CallbackEntry cbEntry = entry.Value as CallbackEntry;
                    if (cbEntry.Value is UserBinaryObject)
                    {
                        cbEntry.Value = ((UserBinaryObject)cbEntry.Value).GetFullObject();
                    }
                }
                else
                {
                    if (entry.Value is UserBinaryObject)
                    {
                        entry.Value = ((UserBinaryObject)entry.Value).GetFullObject();
                    }
                }

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
        public override void Insert(string key, object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority, short onRemoveCallback,
            short onUpdateCallback, Hashtable queryInfo, BitSet flagMap, 
            object lockId,LockAccessType accessType, EventDataFilter updateCallbackFilter, 
            EventDataFilter removeCallabackFilter, long size)
        {

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

            object entry = MakeCompactEntry(key, value, absoluteExpiration, slidingExpiration,
                priority, onRemoveCallback, onUpdateCallback, queryInfo, flagMap, lockId, accessType, updateCallbackFilter, removeCallabackFilter);

				OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ValueDataSize, size);

            _nCache.InsertEntry(entry, operationContext);
        }


        ///  <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
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
        ///	NCache.Cache.Insert(keys, items);
        /// 
        /// </code>
        /// Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        /// <code>
        /// 
        ///	Cache.Insert(keys, items);
        /// 
        /// </code>
        /// </example>
        public override IDictionary Insert(string[] keys, CacheItem[] items, long[] sizes)
        {

            if (_nCache == null) return null;

            object[] entries = new object[items.Length];

            ExpirationHint hint = null;

            short itemRemovedCallback = -1;
            short itemUpdatedCallback = -1;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                CacheItem item = items[i];

                itemRemovedCallback = -1;
                itemUpdatedCallback = -1;
                if (item.CacheItemRemovedCallback != null)
                {
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemRemovedCallback, EventType.ItemRemoved, item.ItemRemovedCallabackDataFilter);
                    itemRemovedCallback = callabackIds[1];
                }
                if (item.CacheItemUpdatedCallback != null)
                {
                    short[] callabackIds = _parent.EventManager.RegisterSelectiveEvent(item.CacheItemUpdatedCallback, EventType.ItemUpdated, item.ItemUpdatedCallabackDataFilter);
                    itemUpdatedCallback = callabackIds[0];
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

                entries[i] = MakeCompactEntry(key, item.Value,
                    item.AbsoluteExpiration, item.SlidingExpiration, item.Priority,
                    itemRemovedCallback, itemRemovedCallback,  item.QueryInfo, item.FlagMap, null, LockAccessType.IGNORE_LOCK, item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter);
            }

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            operationContext.Add(OperationContextFieldName.ValueDataSize, sizes);

            return _nCache.InsertEntries(entries, operationContext);
        }
        #endregion

        #region	/                 --- Remove ---           /

        ///  <summary>
        ///  Removes the object from the <see cref="Cache"/>.
        ///  </summary>
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
        public override CompressedValueEntry Remove(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            if (_nCache != null)
            {
                
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CompressedValueEntry cmpEntry = _nCache.Remove(key, flagMap, null, lockId, accessType, operationContext);

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


        ///  <summary>
        ///  Removes the object from the <see cref="Cache"/>.
        ///  </summary>
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
        public override void Delete(string key, BitSet flagMap, object lockId, LockAccessType accessType)
        {
            if (_nCache != null)
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                _nCache.Delete(key, flagMap, null, lockId, accessType, operationContext);
            }
        }


        ///  <summary>
        ///  Removes the objects from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <param name="flagMap"></param>
        /// <returns>The items removed from the Cache. If the value in the keys parameter 
        ///  is not found, returns a null reference (Nothing in Visual Basic).</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="NCache.ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how you can remove an item from your application's 
        ///  <see cref="Cache"/> object.
        ///  <code>
        ///  
        /// 	NCache.Cache.Remove(keys);
        ///  
        ///  </code>
        ///  Or simply in a class deriving from <see cref="Alachisoft.NCache.Web.UI.NPage"/> or <see cref="Alachisoft.NCache.Web.UI.NUserControl"/>.
        ///  <code>
        ///  
        /// 	Cache.Remove(keys);
        ///  
        ///  </code>
        ///  </example>
        public override IDictionary Remove(string[] keys, BitSet flagMap)
        {
            CallbackEntry cbEntry = null;
            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            IDictionary items = _nCache.Remove(baseKeys, flagMap, cbEntry, operationContext);

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

        public override void Delete(string[] keys, BitSet flagMap)
        {
            CallbackEntry cbEntry = null;
            object[] baseKeys = new object[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                baseKeys[i] = keys[i];

            OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
            _nCache.Delete(baseKeys, flagMap, cbEntry, operationContext);
        }

       
        #endregion

        #region /                 --- Query ---           /

        #region ------------ Search Queries -----------
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

            Alachisoft.NCache.Caching.Queries.QueryResultSet resultSet = _nCache.Search(query, tempValues, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

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

            Alachisoft.NCache.Caching.Queries.QueryResultSet resultSet = _nCache.SearchEntries(query, tempValues, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

            return resultSet;
        }

        #endregion


        #region ---------------------- Cache Data Reader ----------------------
        /// <summary>
        /// Provide data reader on <see cref="Cache"/> based on the query specified.
        /// </example> 
        public override IRecordSetEnumerator ExecuteReader(string query, IDictionary values, bool getData, int chunkSize)
        {
            if (_nCache == null)
                return null;

            IRecordSetEnumerator result = null;
            Hashtable tempValues = GetValues(values);
            ClusteredList<ReaderResultSet> readerResultSet = _nCache.ExecuteReader(query, tempValues, getData, chunkSize, true, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
            if (readerResultSet != null && readerResultSet.Count > 0)
            {
                IRecordSetEnumerator rse = new RecordSetEnumerator(readerResultSet[0].RecordSet);
                if (readerResultSet[0].RecordSet != null)
                    rse = readerResultSet[0].RecordSet.GetEnumerator();

                result = new PartitionRSEnumerator(rse, readerResultSet[0].ReaderID, readerResultSet[0].NodeAddress, readerResultSet[0].NextIndex, this);
            }
            return result;
        }
        #endregion

        #endregion

        #region /                 --- Serialize ---           /
        public override object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, CacheImplBase cacheImpl, ref long size)
        {
            if (size <= 0)
            {
                System.Type type = serializableObject.GetType();

                if (typeof(byte[]).Equals(type) && flag != null)
                {
                    flag.SetBit(BitSetConstants.BinaryData);
                    size = serializableObject is byte[] ? ((byte[])serializableObject).Length : 0;
                    return serializableObject;
                }
                object seralized = CompactBinaryFormatter.ToByteBuffer(serializableObject, serializationContext);
                size = seralized is byte[] ? ((byte[])seralized).LongLength : 0;
            }

            return serializableObject;
        }

        public override object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag, CacheImplBase cacheImpl)
        {
            return serializedObject;
        }
        #endregion

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
        public override void RegisterKeyNotificationCallback(string key, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration)
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
                _nCache.UnregisterKeyNotificationCallback(key, new CallbackInfo(ClientID, updateCallbackid, EventDataFilter.None), new CallbackInfo(ClientID, removeCallbackid, EventDataFilter.DataWithMetadata), new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
        }

        public override void RegisterKeyNotificationCallback(string[] keys, short update, short remove, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            CallbackInfo cbUpdate = null;
            CallbackInfo cbRemove = null;


            cbUpdate = new CallbackInfo(ClientID, update, datafilter);
            cbRemove = new CallbackInfo(ClientID, remove, datafilter, notifyOnItemExpiration);

            if (_nCache != null)
                _nCache.RegisterKeyNotificationCallback(keys,cbUpdate,cbRemove, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
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
                }
                catch { }
            }

            #endregion
        }

        ///// <summary>
        ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        ///// internal class and must not be used from outside
        ///// </summary>
        private class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache.CacheAsyncEventsListener _parent;

            /// <summary> Underlying implementation of NCache. </summary>
            private Alachisoft.NCache.Caching.Cache _nCache;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="nCache"></param>
            internal CacheAsyncEventsListener(Cache.CacheAsyncEventsListener parent, Alachisoft.NCache.Caching.Cache nCache)
            {
                _parent = parent;
                _nCache = nCache;
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
                }
                catch { }
            }

            #endregion

        }

        /// <summary>
        /// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        /// internal class and must not be used from outside
        /// </summary>
        private class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache.CacheEventsListener _listener;

            /// <summary> Underlying implementation of NCache. </summary>
            private Alachisoft.NCache.Caching.Cache _nCache;
            private CustomRemoveCallback _customRemoveCallback;
            private CustomUpdateCallback _customUpdateCallback;
            private Alachisoft.NCache.Caching.CacheStoppedCallback _cacheStopped;
            private string _serializationContext;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="nCache"></param>
            internal CacheEventsListener(Cache.CacheEventsListener parent, Alachisoft.NCache.Caching.Cache nCache)
            {
                _listener = parent;
                _nCache = nCache;
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
                    _nCache.CustomRemoveCallbackNotif -= _customRemoveCallback;
                    _nCache.CustomUpdateCallbackNotif -= _customUpdateCallback;
                }
                catch { }
            }

            #endregion

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
        /// <summary>
        /// Initializes the Compact Serilization Framework.
        /// </summary>
        internal void InitializeCompactFramework()
        {
            CompactFormatterServices.RegisterCompactType(typeof(CallbackEntry), 107);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address), 110);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.NCacheSessionItem), 129);

        }

        #endregion

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
                        
                        ArrayList list = new ArrayList((ArrayList)dicEnum.Value);
                        //ArrayList list = (ArrayList)dicEnum.Value;
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


        #region ---------------------- IRecordSetLoader ----------------------
        public ReaderResultSet GetRecordSet(string readerID, string nodeIP, int nextIndex)
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

    }
}


