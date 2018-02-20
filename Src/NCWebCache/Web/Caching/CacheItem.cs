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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Class that represents a cached item including its dependencies, 
    /// expiration and  eviction information
    /// </summary>
    /// <example>
    /// You can create an instance of CacheItem class and <see cref="Cache.Add"/> it to the <see cref="Cache"/>
    /// <code>
    /// object someData = new object();
    /// CacheItem item = new CacheItem(someData);
    /// item.SlidingExpiration = new TimeSpan(0,5,0);
    /// item.Priority = CacheItemPriority.High;
    /// item.ItemRemoveCallback = onRemove;
    /// Cache cache = NCache.InitializeCache("myCache");
    /// cache.Add("someData", item);
    /// </code>
    /// </example>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>
    public class CacheItem : ICloneable
    {
        /// <summary> The actual object provided by the client application </summary>
        private object _v;

        /// <summary> Dependency for the object.</summary>
        private CacheDependency _d;

        private CacheSyncDependency _syncDep;

        /// <summary> Absolute expiration for the object. </summary>
        private DateTime _abs;

        /// <summary> Sliding expiration for the object. </summary>
        private TimeSpan _sld;

        /// <summary> Priority for the object. </summary>
        private CacheItemPriority _p;

        /// <summary> ItemRemoveCallback function pointer supplied by client application to be fired on item removal.</summary>
        private CacheItemRemovedCallback _rc;

        /// <summary> ItemRemoveCallback function pointer supplied by client application to be fired on item update.</summary>
        private CacheItemUpdatedCallback _uc;

        /// <summary> Time at which this item was created. </summary>
        internal DateTime _creationTime = new DateTime();

        /// <summary> Time at which this item was Last modified. </summary>
        internal DateTime _lastModifiedTime = new DateTime();


        private AsyncItemAddedCallback _aiacb;
        private AsyncItemUpdatedCallback _aiucb;

        /// <summary> A value that identifies that "Resync Expired Items" is to be provided for the object.
        /// if true "Resync Expired Items" is enabled otherwise not. </summary>
        private bool _r = false;

        private string _resyncProviderName;

        ///// <summary> Holds the group and subgroup information </summary>
        private string _group, _subgroup;
        private int _size;
        private BitSet _flagMap;
        internal Hashtable _queryInfo;

        private CacheItemVersion _version;
        private Tag[] _tags;

        private NamedTagsDictionary _namedTags;

        //new callbacks
        private CacheDataNotificationCallback _cacheItemUpdateCallback;
        private CacheDataNotificationCallback _cacheItemRemovedCallback;
        private EventDataFilter _itemRemovedDataFilter = EventDataFilter.None;
        private EventDataFilter _itemUpdatedDataFilter = EventDataFilter.None;

        internal TimeSpan Expiration { get; set; }


        internal CacheItem()
        {
            _abs = Cache.NoAbsoluteExpiration;
            _sld = Cache.NoSlidingExpiration;
            _p = CacheItemPriority.Default;
        }

        /// <summary>Costructor</summary>
        /// <param name="value">Actual object to be stored in cache</param>
        public CacheItem(object value)
        {
            _v = value;
            _abs = Cache.NoAbsoluteExpiration;
            _sld = Cache.NoSlidingExpiration;
            _p = CacheItemPriority.Default;
        }

        /// <summary>The actual object provided by the client application.</summary>
        /// <remarks>This value must be serializable, otherwise <see cref="ArgumentException"/> 
        /// is thrown when you will try to add or insert the CacheItem.</remarks>
        public virtual object Value
        {
            get { return _v; }
            set { _v = value; }
        }

        /// <summary>The file or cache key dependencies for the item. 
        /// When any dependency changes, the object becomes invalid and is removed from 
        /// the cache. If there are no dependencies, this property contains a null 
        /// reference (Nothing in Visual Basic).</summary>
        /// <remarks></remarks>
        public CacheDependency Dependency
        {
            get { return _d; }
            set { _d = value; }
        }

        /// <summary>
        /// CacheSyncDependency for this item.
        /// </summary>

        public CacheSyncDependency SyncDependency
        {
            get { return _syncDep; }
            set { _syncDep = value; }
        }

        /// <summary>The time at which the added object expires 
        /// and is removed from the cache.
        /// </summary>
        /// <value>The default value is Cache.NoAbsoluteExpiration</value>
        /// <remarks>
        /// You cannot set both sliding and absolute expirations on the same cached item. 
        /// If you do so, an <see cref="ArgumentException"/> is thrown when you will try to
        /// add or insert the CacheItem.
        /// </remarks>
        public DateTime AbsoluteExpiration
        {
            get { return _abs; }
            set { _abs = value; }
        }


        /// <summary>The interval between the time the added object was last 
        /// accessed and when that object expires. If this value is the equivalent of 
        /// 20 minutes, the object expires and is removed from the cache 20 minutes 
        /// after it is last accessed. </summary>
        /// <value>The default value is Cache.NoSlidingExpiration.</value>
        /// <remarks> If the slidingExpiration property is set to NoSlidingExpiration, 
        /// sliding expiration is disabled. If you set the <paramref name="slidingExpiration"/> 
        /// parameter to less than <see cref="TimeSpan.Zero"/>, or the equivalent of 
        /// more than one year, an <see cref="ArgumentOutOfRangeException"/> is thrown when you will try to
        /// add or insert the CacheItem.
        /// You cannot set both sliding and absolute expirations on the 
        /// same cached item. If you do so, an <see cref="ArgumentException"/> is thrown when you will try to
        /// add or insert the CacheItem.</remarks>
        public TimeSpan SlidingExpiration
        {
            get { return _sld; }
            set { _sld = value; }
        }


        /// <summary>The relative cost of the object, as expressed by the enumeration.
        /// The cache uses this value when it evicts objects; objects with a lower cost
        /// are removed from the cache before objects with a higher cost.</summary>
        /// <value>The default value is CacheItemPriority.Default.</value>
        /// <remarks>This property will be used only when the eviction policy is set to 
        /// priority in the configuration.</remarks>
        public CacheItemPriority Priority
        {
            get { return _p; }
            set { _p = value; }
        }

        /// <summary>A delegate that, if provided, is called when an object is removed
        /// from the cache. You can use this to notify applications when their
        /// objects are deleted from the cache. </summary>
        [Obsolete(
            "This delegate is deprecated. 'Please use SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        public CacheItemRemovedCallback ItemRemoveCallback
        {
            get { return _rc; }
            set
            {
                _rc = value;
                //Old implementation requires Data
                _itemRemovedDataFilter = EventDataFilter.DataWithMetadata;
            }
        }

        /// <summary>A delegate that, if provided, is called when an object is updated
        /// in the cache. You can use this to notify applications when their
        /// objects are updated in the cache. </summary>
        [Obsolete(
            "This delegate is deprecated. 'Please use SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)'",
            false)]
        public CacheItemUpdatedCallback ItemUpdateCallback
        {
            get { return _uc; }
            set
            {
                _uc = value;
            }
        }


        /// <summary>A delegate that, if provided, is called when an object is removed
        /// from the cache. You can use this to notify applications when their
        /// objects are deleted from the cache. </summary>
        internal CacheDataNotificationCallback CacheItemRemovedCallback
        {
            get { return _cacheItemRemovedCallback; }
        }

        /// <summary>A delegate that, if provided, is called when an object is updated
        /// in the cache. You can use this to notify applications when their
        /// objects are updated in the cache. </summary>
        internal CacheDataNotificationCallback CacheItemUpdatedCallback
        {
            get { return _cacheItemUpdateCallback; }
        }

        /// <summary>
        /// You can use this to notify applications when their objects are updated or removed in the cache.
        /// Callbacks can be registered against <see cref=" EventType"/> for the key the items is inserted to.
        /// Callback are overriden for the same <see cref="EventType"/> if called again.
        /// <see cref="EventType.ItemAdded"/> is not supported yet.
        /// </summary>
        /// <param name="callback">Callback to be raised when an item is updated or removed</param>
        /// <param name="eventType"><see cref="EventType"/> the callback is registered against</param>
        public void SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType)
        {
            SetCacheDataNotification(callback, eventType, EventDataFilter.None);
        }

        /// <summary>
        /// You can use this to notify applications when their objects are updated or removed in the cache.
        /// Callbacks can be registered against <see cref=" EventType"/> for the key the items is inserted to.
        /// To register different <see cref="EventDataFilter"/> for different <see cref="EventType"/>, you can call this function mutiple times 
        /// with a different <see cref="EventType"/>.
        /// Callback are overriden for the same <see cref="EventType"/> if called again.
        /// <see cref="EventType.ItemAdded"/> is not supported yet.
        /// </summary>
        /// <param name="callback">Callback to be raised when an item is updated or removed</param>
        /// <param name="eventType"><see cref="EventType"/> the callback is registered against</param>
        /// <param name="datafilter"><see cref="EventDataFilter"/> for every <see cref="EventType"/> registered against</param>
        public void SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType,
            EventDataFilter datafilter)
        {
            if (callback == null) return;

            if ((eventType & EventType.ItemRemoved) != 0)
            {
                _cacheItemRemovedCallback = callback;
                _itemRemovedDataFilter = datafilter;
            }

            if ((eventType & EventType.ItemUpdated) != 0)
            {
                _cacheItemUpdateCallback = callback;
                _itemUpdatedDataFilter = datafilter;
            }
        }

        internal EventDataFilter ItemUpdatedCallabackDataFilter
        {
            get { return _itemUpdatedDataFilter; }
        }

        internal EventDataFilter ItemRemovedCallabackDataFilter
        {
            get { return _itemRemovedDataFilter; }
        }

        /// <summary>A delegate that, if provided, is called when an Asynchronous request
        /// for Add Operation completes. You can use this to obtain the results of an 'AddAsync' operation. </summary>

        public AsyncItemAddedCallback AsyncItemAddCallback
        {
            get { return _aiacb; }
            set { _aiacb = value; }
        }


        /// <summary>A delegate that, if provided, is called when an Asynchronous request
        /// for Update Operation completes. You can use this to obtain the results of an 'InsertAsync' operation. </summary>

        public AsyncItemUpdatedCallback AsyncItemUpdateCallback
        {
            get { return _aiucb; }
            set { _aiucb = value; }
        }

        /// <summary>
        /// Gets/Sets a value indicating whether the object when expired will cause a refetch of
        /// the object from the master datasource. (Resync Expired Items)
        /// </summary>
        /// <value><b>true</b> if "Resync Expired Items" on this object has been set; otherwise, 
        /// <b>false</b>. The default is <b>false</b>.</value>
        /// <remarks> 
        /// Whenever an expired item is fectched from the cache it is read thru the datasource. If the
        /// item is not available in the datasource the item is removed from the cache and 
        /// null value is returned.
        /// <para><b>Note:</b> For "Resync Expired Items" to work you must a specify read through provider for the cache.
        /// For more information on read through providers see online documentation.</para>
        /// </remarks>
        public Boolean IsResyncExpiredItems
        {
            get { return _r; }
            set { _r = value; }
        }

        /// <summary> Gets/Sets Provider name for re-synchronization of cache</summary>
        public string ResyncProviderName
        {
            get { return _resyncProviderName; }
            set { _resyncProviderName = value; }
        }

        internal Hashtable QueryInfo
        {
            get { return _queryInfo; }
            set { _queryInfo = value; }
        }

        internal BitSet FlagMap
        {
            get { return _flagMap; }
            set { _flagMap = value; }
        }

        /// <summary>
        /// The name of the group to associate with the cache item. All cache items with the same 
        /// group name are logically grouped together.
        /// </summary>
        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        /// <summary>
        /// The name of the sub-group within a group. This hierarchical grouping gives more control 
        /// over the cache items.
        /// </summary>
        public string SubGroup
        {
            get { return _subgroup; }
            set { _subgroup = value; }
        }

        /// <summary>
        /// Get or Set the cache item version
        /// </summary>
        public CacheItemVersion Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Get the cache item creation time
        /// </summary>
        public DateTime CreationTime
        {
            get { return _creationTime; }
        }

        /// <summary>
        /// Get the cache item last modified time
        /// </summary>
        public DateTime LastModifiedTime
        {
            get { return _lastModifiedTime; }
        }


        /// <summary>
        /// Get or set the tags
        /// </summary>
        public Tag[] Tags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        /// <summary>
        /// Get or set the named tags
        /// </summary>
        public NamedTagsDictionary NamedTags
        {
            get { return _namedTags; }
            set { _namedTags = value; }
        }

        internal int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        #region ICloneable Members

        /// <summary>
        /// Creates a shallow copy of CacheItem
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            CacheItem newItem = new CacheItem();

            newItem._abs = this._abs;
            newItem._sld = this._sld;
            newItem._aiacb = this._aiacb;
            newItem._aiucb = this._aiucb;
            newItem._d = this._d;
            newItem._syncDep = this._syncDep;
            newItem.FlagMap = this.FlagMap;
            newItem._p = this._p;
            newItem._queryInfo = this._queryInfo;
            newItem._r = this._r;
            newItem._uc = this._uc;
            newItem._rc = this._rc;
            newItem._v = this._v;
            newItem._version = this._version;
            newItem._group = this._group;
            newItem._subgroup = this._subgroup;
            newItem.Tags = this._tags;
            newItem.ResyncProviderName = this._resyncProviderName;
            newItem.NamedTags = this._namedTags;
            newItem._cacheItemUpdateCallback = _cacheItemUpdateCallback;
            newItem._cacheItemRemovedCallback = this._cacheItemRemovedCallback;
            newItem._itemUpdatedDataFilter = this._itemUpdatedDataFilter;
            newItem._itemRemovedDataFilter = this._itemRemovedDataFilter;
            newItem._size = this._size;
            return newItem;
        }

        #endregion
    }
}