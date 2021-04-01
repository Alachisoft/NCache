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
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Caching;


namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// NCache uses a "key" and "value" structure for storing objects in cache.
    /// When an object is added in cache it is stored as value and metadata against the specified key.
    /// This combination of value and metadata is defined as CacheItem in NCache.
    /// The value of the objects stored in the cache can range from being simple string types to complex objects.
    /// <para>
    /// CacheItem class in NCache has properties that enable you to set metadata for the item to be added in cache 
    /// in an organized way. In scenarios where multiple attributes have to be set while adding an item in cache 
    /// using CacheItem is preferred.Using CacheItem class object removes the problem of using multiple API overloads 
    /// on adding/updating data in cache.You can easily use the basic API overload and add/update data easily using CacheItem.
    /// </para>
    /// </summary>
    /// <example>
    /// You can create an instance of CacheItem class and <see cref="o:ICache.Add"/> it to the <see cref="ICache"/>
    /// <code>
    /// ICache cache = CacheManager.GetCache("myCache");
    /// 
    /// object someData = new object();
    /// CacheItem item = new CacheItem(someData);
    /// item.Expiration = new Expiration(ExpirationType.Sliding,TimeSpan.FromMinutes(5));
    /// item.Priority = CacheItemPriority.High;
    /// 
    /// cache.Add("someData", item);
    /// </code>
    /// </example>
    public class CacheItem : ICloneable
    {
        #region Fields 
        private object _value;
        private object _currentValue;
        private CacheItemRemovedCallback _itemRemovedCallBack;
        private DateTime _absoluteTime = DateTime.MaxValue.ToUniversalTime();
        private Expiration _expiration = new Expiration();
        #endregion

        #region Constructor

        internal CacheItem()
        {
            Priority = CacheItemPriority.Default;
        }

        /// <summary>
        /// Initialize new instance of cache item.
        /// </summary>
        /// <param name="value">Actual object to be stored in cache.</param>
        public CacheItem(object value)
        {
            _value = value;
            Priority = CacheItemPriority.Default;
        }

        #endregion

        #region Public Properties 

        /// <summary>
        /// This property sets <see cref="Alachisoft.NCache.Runtime.Caching.Expiration"/> for the cache itme. After the specified timespan,
        /// the item expires from cache.
        /// <remarks>If expiration is not set then it is disabled.
        /// </remarks>
        /// </summary>
        /// <example>
        /// Example sets sliding expiration of a cache item as 5 minutes.
        /// <code>
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// CacheItem item = new CacheItem(product);
        /// 
        /// item.Expiration = new Expiration(ExpirationType.Sliding, TimeSpan.FromMinutes(5));
        /// </code>
        /// </example>
        public Expiration Expiration { get

            { return _expiration; } set { _expiration = value; } }

        /// <summary>When the application's cache is full or runs low on memory, the Cache selectively purges
        /// items to free system memory. When an item is added to the Cache, you can assign it a relative priority
        /// compared to the other items stored in the Cache using this property. 
        /// <para>
        /// This eliminates the problem of using API overloads for setting the priority. Items you assign higher
        /// priority values to are less likely to be deleted from the Cache when the server is processing a large
        /// number of requests, while items you assign lower priority values are more likely to be deleted.
        /// </para>
        /// </summary>
        /// <value>The default value is CacheItemPriority.Default.</value>
        /// <remarks>This property will be used only when the eviction policy is set to 
        /// priority in the configuration.</remarks>
        /// <example>
        /// Example sets <see cref="CacheItemPriority"/> of a cache item.
        /// <code>
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// CacheItem item = new CacheItem(product);
        /// item.Priority = CacheItemPriority.Normal;
        /// </code>
        /// </example>
        public CacheItemPriority Priority { get; set; }

        /// <summary>
        /// Specifies when the item was added in cache for the first time.
        /// </summary>
        /// <example>
        /// Example prints creation time of a cache item.
        /// <code>
        /// CacheItem item = cache.GetCacheItem("Product0");
        /// Console.WriteLine("Creation Time: {0}", item.CreationTime.ToString());
        /// </code>
        /// </example>
        public DateTime CreationTime { get; internal set; }

        /// <summary>
        /// This property of CacheItem stores the last modified time of the cache item. If an item is
        /// updated in cache its last modified time is updated as well. Last modified time is checked 
        /// when Least Recently Used based eviction is triggered. 
        /// </summary>
        /// <example>
        /// Example prints creation time of a cache item
        /// <code>
        /// CacheItem item = cache.GetCacheItem("Product0");
        /// Console.WriteLine("Last Modified Time: {0}", item.LastModifiedTime.ToString());
        /// </code>
        /// </example>
        public DateTime LastModifiedTime { get; internal set; }


        #endregion

        #region Internal Properties 

        internal int Size { get; set; }

        internal BitSet FlagMap { get; set; }

        internal string SubGroup { get; set; }

        internal Hashtable QueryInfo { get; set; }

        internal CacheDataNotificationCallback CacheItemRemovedCallback { get; set; }

        internal CacheDataNotificationCallback CacheItemUpdatedCallback { get; set; }

        internal EventDataFilter ItemUpdatedDataFilter { get; set; }

        internal EventDataFilter ItemRemovedDataFilter { get; set; }

        internal string TypeName { get; set; }

        [Obsolete("This delegate is deprecated. 'Please use SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal CacheItemRemovedCallback ItemRemoveCallback
        {
            get { return _itemRemovedCallBack; }
            set
            {
                _itemRemovedCallBack = value;
                ItemRemovedDataFilter = EventDataFilter.None;
            }
        }

        [Obsolete("This delegate is deprecated. 'Please use SetCacheDataNotification(CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter)'", false)]
        internal CacheItemUpdatedCallback ItemUpdateCallback { get; set; }

        internal TimeSpan SlidingExpiration { get { return _expiration != null ? _expiration.Sliding : TimeSpan.Zero; } }

        internal DateTime AbsoluteExpiration { get { return _expiration != null ? _expiration.Absolute : DateTime.MaxValue.ToUniversalTime(); } }

        

        internal Cache CacheInstance { get; set; }

        internal EntryType EntryType { get; set; } = EntryType.CacheItem;


   

        #endregion

        #region Public Methods 


        /// <summary>
        /// Returns the value stored in the cache item.
        /// </summary>
        /// <remarks>
        /// This value must be serializable, otherwise <see cref="ArgumentException"/> 
        /// is thrown when you will try to add or insert the CacheItem.
        /// </remarks>
        /// <typeparam name="T">Specifies the type of value obtained from the cache item.</typeparam>
        /// <returns>Value of the cache item with the type defined.</returns>
        /// <example>
        /// Example gets value of a cache item
        /// <code>
        /// CacheItem item = cache.GetCacheItem("Product0");
        /// 
        /// Product product = item.GetValue&lt;Product&gt;();
        /// </code>
        /// </example>
        public T GetValue<T>()
        {
            if (_value is ValueEmissary)
            {
                if (_currentValue != default(object) && _currentValue is T)
                    return (T)_currentValue;

                var emissary = _value as ValueEmissary;

                if (CacheInstance == default(Cache))
                    return (T)emissary.Data;
                   _currentValue = CacheInstance.SafeDeserialize<T>(emissary.Data, CacheInstance.SerializationContext, FlagMap, UserObjectType.CacheItem);
                    
                return (T)_currentValue;
            }
            return (T)_value;
        }

        /// <summary>
        /// Sets the value of the cache item.
        /// </summary>
        /// <param name="value">object to be stored in cache item.</param>
        /// <example>
        /// Example sets value of a cache item.
        /// <code>
        /// CacheItem item = cache.GetCacheItem("Product0");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// item.SetValue(product);
        /// 
        /// cache.Insert("Product0", product);
        /// </code>
        /// </example> 
        public void SetValue(object value)
        {
            _value = value;
        }

        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a shallow copy of CacheItem
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            CacheItem newItem = new CacheItem();
            newItem.CacheItemRemovedCallback = CacheItemRemovedCallback;
            newItem.CacheItemUpdatedCallback = CacheItemUpdatedCallback;
            newItem.CreationTime = CreationTime;
            newItem.Expiration = Expiration;
            newItem.FlagMap = FlagMap;
            newItem.ItemRemoveCallback = ItemRemoveCallback;
            newItem.ItemRemovedDataFilter = ItemRemovedDataFilter;
            newItem.ItemUpdateCallback = ItemUpdateCallback;
            newItem.ItemUpdatedDataFilter = ItemUpdatedDataFilter;
            newItem.TypeName = TypeName;
            newItem.LastModifiedTime = LastModifiedTime;
            newItem.Priority = Priority;
            newItem.QueryInfo = QueryInfo;
            newItem.Size = Size;
            newItem._value = _value;
            newItem.CacheInstance = CacheInstance;
       

            return newItem;
        }

        #endregion
    }
}
