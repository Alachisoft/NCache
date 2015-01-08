// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Runtime;
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
		private object	_v;

	    /// <summary> Absolute expiration for the object. </summary>
		private DateTime _abs;
		/// <summary> Sliding expiration for the object. </summary>
	    private TimeSpan _sld;
		/// <summary> Priority for the object. </summary>
		private CacheItemPriority _p;
        
        /// <summary> Time at which this item was created. </summary>
        internal  DateTime _creationTime = new DateTime();

        /// <summary> Time at which this item was Last modified. </summary>
        internal DateTime _lastModifiedTime = new DateTime();


        private BitSet _flagMap;
        internal Hashtable _queryInfo;


        //new callbacks
        private CacheDataNotificationCallback _cacheItemUpdateCallback;
        private CacheDataNotificationCallback _cacheItemRemovedCallback;
        private EventDataFilter _itemRemovedDataFilter = EventDataFilter.None;
        private EventDataFilter _itemUpdatedDataFilter = EventDataFilter.None;

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
        /// To register different <see cref="EventDataFilter"/> for different <see cref="EventType"/>, you can call this function mutiple times 
        /// with a different <see cref="EventType"/>.
        /// Callback are overriden for the same <see cref="EventType"/> if called again.
        /// <see cref="EventType.ItemAdded"/> is not supported yet.
        /// </summary>
        /// <param name="callback">Callback to be raised when an item is updated or removed</param>
        /// <param name="eventType"><see cref="EventType"/> the callback is registered against</param>
        /// <param name="datafilter"><see cref="EventDataFilter"/> for every <see cref="EventType"/> registered against</param>

        public void SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)
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
            newItem.FlagMap = this.FlagMap;
            newItem._p = this._p;
            newItem._queryInfo = this._queryInfo;
            newItem._v = this._v;
            newItem._cacheItemUpdateCallback = _cacheItemUpdateCallback;
            newItem._cacheItemRemovedCallback = this._cacheItemRemovedCallback;
            newItem._itemUpdatedDataFilter = this._itemUpdatedDataFilter;
            newItem._itemRemovedDataFilter = this._itemRemovedDataFilter;
            return newItem;
        }

        #endregion
    }
}
