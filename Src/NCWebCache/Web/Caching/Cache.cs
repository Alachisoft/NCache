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
using Microsoft.Win32;
using System.Reflection;
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Web.Net;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Web.RemoteClient.Config;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching.APILogging;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Web.Statistics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.IO;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Queries;
using Web = Alachisoft.NCache.Web;
using Alachisoft.NCache.Runtime.Caching;




/// <summary>
/// The <see cref="Alachisoft.NCache.Web.Caching"/> namespace provides classes for caching frequently used data 
/// in a cluster This includes the <see cref="Cache"/> class, a dictionary that allows you to store 
/// arbitrary data objects, such as hash tables and data sets. It also provides expiration functionality 
/// for those objects, and methods that allow you to add and removed the objects. 
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
        private string _serializationContext;

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

        internal event CacheStoppedCallback _cacheStopped;
        private int _refCacheStoppedCount = 0;
        private int _refClearCount = 0;
        private CacheAsyncEventsListener _asyncListener;
        private CacheEventsListener _listener;

        private EventManager _eventManager;

        private PerfStatsCollector2 _perfStatsCollector;


        /// <summary> Cache event listener object. Implements all events.</summary>
        private ClusterEventsListener _clusterListener;
        
        private ResourcePool _asyncCallbackIDsMap = new ResourcePool();
        private ResourcePool _asyncCallbacksMap = new ResourcePool();

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
        
        private ArrayList _secondaryInprocInstances;
        private Hashtable _apiLogHastable = Hashtable.Synchronized(new Hashtable());
        internal Hashtable APILogHashTable
        {
            get { return _apiLogHastable; }
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
        ///	cache.Insert("DSN", connectionString,Cache.NoAbsoluteExpiration, TimeSpan.FromSeconds(10), CacheItemPriority.Normal);      
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
        ///	cache.Insert("DSN", connectionString, DateTime.Now.AddMinutes(2), Cache.NoSlidingExpiration, CacheItemPriority.Normal);
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
        private CacheEventDescriptor _addDiscriptor;
        private CacheEventDescriptor _updateDiscriptor;
        private CacheEventDescriptor _removeDiscriptor;

        internal virtual Cache.ClusterEventsListener ClusterListener
        {
            get { return _clusterListener; }
        }

        internal virtual Cache.CacheAsyncEventsListener AsyncListener
        {
            get { return _asyncListener; }
        }

        internal virtual Cache.CacheEventsListener EventListener
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

        #endregion

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        internal Cache()
        {
        }

    
        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="config"></param>
        internal Cache(CacheImplBase objectCache, CacheConfig config)
        {
            _cacheImpl = objectCache;
            _config = config;
            _cacheId = config.CacheId;

            if (_cacheImpl != null)
            {
                _serializationContext = _cacheImpl.Name; //Sets the serialization context.
                _cacheId = _cacheImpl.Name;
            }
            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListener(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListener(this);
             AddRef();
        }
   

        /// <summary>
        /// Initializes a new instance of the Cache class.
        /// </summary>
        /// <param name="objectCache"></param>
        /// <param name="cacheId"></param>        

        internal Cache(CacheImplBase objectCache, string cacheId, PerfStatsCollector2 perfStatsCollector)
        {
            _cacheImpl = objectCache;
            _cacheId = cacheId;
            if (_cacheImpl != null)
            {
                _serializationContext = _cacheImpl.Name; //Sets the serialization context.
            }

            _eventManager = new EventManager(_cacheId, null, this);
            _listener = new CacheEventsListener(this, _eventManager);
            _asyncListener = new CacheAsyncEventsListener(this);

            _perfStatsCollector = perfStatsCollector;
          
            AddRef();
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
                    else if (_refCount < 0) _refCount = 0;

                    // remove from the cache table.
                    lock (NCache.Caches)
                    {
                        if (_cacheId != null)
                            NCache.Caches.Remove(_cacheId);
                    }

                    if (_cacheImpl != null) _cacheImpl.Dispose(disposing);
                    _cacheImpl = null;
                   
                    if (disposing) GC.SuppressFinalize(this);
                }
            }
            catch (Exception ex) { }
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
        /// </code>
        /// </example>
        public virtual bool ExceptionsEnabled
        {
            get { lock (this) { return s_exceptions; } }
            set { lock (this) { s_exceptions = value; } }
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
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <value>The number of items stored in the cache.</value>
        public virtual long Count
        {
            get
            {
                try
                {
                    if (_cacheImpl != null) return _cacheImpl.Count;
                }
                catch (Exception)
                {
                    if (ExceptionsEnabled) throw;
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to clear <see cref="Cache"/>.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Clear();
        /// 
        /// </code>        
        /// </example>
        public virtual void Clear()
        {
            if (_cacheImpl == null) return;
            try
            {

                _cacheImpl.Clear(new BitSet());
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
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
        /// setting, this property throws exception incase of failure.</para>
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
        public virtual bool Contains(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (!key.GetType().IsSerializable)
                throw new ArgumentException("key is not serializable");

            if (_cacheImpl == null) return false;
            try
            {
                return _cacheImpl.Contains(key);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return false;
        }

        #endregion
       
        private void ValidateKeyValue(object key, object value)
        {
            Type type = typeof(ICompactSerializable);
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (key is string && (string)key == string.Empty) throw new ArgumentException("key cannot be empty string");
            if (!key.GetType().IsSerializable && !type.IsAssignableFrom(key.GetType())) throw new ArgumentException("key is not serializable");
        }


        /// <summary>
        /// Set Attributes for a specific key in cache
        /// </summary>
        /// <param name="key">The key for which Attributes are set in the <see cref="Cache"/>.</param>        
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to set attributes for item in the <see cref="Cache"/>.
        /// <code>        
        /// CacheItemAttributes attrib=new CacheItemAttributes();
        /// attrib.AbsoluteExpiration=DateTime.Now.AddHours(10);
        /// Cache cache = NCache.InitializeCache("myCache");
        /// cache.SetAttributes("MyKey",attrib);
        /// </code>        
        /// </example>
        /// 
        public virtual bool SetAttributes(string key, CacheItemAttributes attributes)
        {
            if (key == null) throw new ArgumentNullException();

            if (attributes == null) throw new ArgumentNullException();

            return _cacheImpl.SetAttributes(key, attributes);
        }

        private Hashtable GetQueryInfo(Object value)
        {
            Hashtable queryInfo = null;

            if (_cacheImpl.TypeMap == null)
                return null;

            try
            {
                string typeName = value.GetType().FullName;
                typeName = typeName.Replace("+", ".");

                int handleId = _cacheImpl.TypeMap.GetHandleId(typeName);
                if (handleId != -1)
                {
                    queryInfo = new Hashtable();
                    Type valType = null; // (Cattering Case-InSensetive string comparisons.
                    ArrayList attribValues = new ArrayList();
                    ArrayList attributes = _cacheImpl.TypeMap.GetAttribList(handleId);
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        PropertyInfo propertyAttrib = value.GetType().GetProperty((string)attributes[i]);
                        if (propertyAttrib != null)
                        {
                            Object attribValue = propertyAttrib.GetValue(value, null);
                            //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                            attribValues.Add(attribValue);
                        }
                        else
                        {
                            FieldInfo fieldAttrib = value.GetType().GetField((string)attributes[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);//value.GetType().GetField((string)attributes[i]);
                            if (fieldAttrib != null)
                            {
                                Object attribValue = fieldAttrib.GetValue(value);
                                //Donot lower strings here because we need to return the string in original form in case of MIN and MAX
                               // if (attribValue!=null)
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
            catch (Exception) { }
            return queryInfo;
        }

        #region	/                 --- Add ---           /
        /// <summary>
        /// Adds an item into the Cache object with a cache key to reference its location 
        /// and using default values provided by the CacheItemPriority enumeration.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="value">The item to be added to the cache.</param>        
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If the <paramref name="key"/> or <paramref name="value"/> parameter is null,
        /// an <see cref="ArgumentNullException"/> is thrown.</para>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("DSN", connectionString);
        /// 
        /// </code>        
        /// </example>
        public virtual void Add(string key, object value)
        {
            long size = 0;
            try
            {
                AddOperation(key, value,Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                            CacheItemPriority.Default, null, null, 
                            EventDataFilter.None, EventDataFilter.None, ref size, true);
              
            }
            catch (Exception) { if (ExceptionsEnabled) throw; }
        }

        /// <summary>
        /// When any dependency changes, the object becomes invalid and is removed from 
        /// the cache. If there are no dependencies, this paramter contains a null 
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to add an item to the cache with no abolute 
        /// expiration, a sliding expiration of 5 minutes and with a priority of 
        /// high.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("timestamp", timeStamp, Cache.NoAbsoluteExpiration, new TimeSpan(0,5,0), CacheItemPriority.High);
        /// 
        /// </code>        
        /// </example>

        public virtual void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            long size = 0;
            try
            {
                AddOperation(key, value, absoluteExpiration, slidingExpiration, priority, null, null, 
                     EventDataFilter.None, EventDataFilter.None, ref size, true);
                
            }
            catch (Exception) { if (ExceptionsEnabled) throw; }
        }


        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>        
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
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add("someData", item);
        ///
        /// </code>        
        /// </example>
        public virtual void Add(string key, CacheItem item)
        {
            try
            {
                long size = 0;
                if (item == null)
                    throw new ArgumentNullException("CacheItem");

                AddOperation(key, item.Value, item.AbsoluteExpiration,
                    item.SlidingExpiration,
                    item.Priority, item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback, item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter
                    , ref size, true);
               
            }
            catch (Exception) { if (ExceptionsEnabled) throw; }
           
        }



       ///  <summary>
        ///  Add array of <see cref="CacheItem"/> to the cache.
        ///  </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>keys that are added or that alredy exists in the cache and their status.</returns>
        ///  <remarks> If CacheItem contains invalid values the related exception is thrown. 
        ///  See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>		
        ///  <example>The following example demonstrates how to add items to the cache with an absolute 
        ///  expiration 2 minutes from now, a priority of 
        ///  high, and that notifies the application when the item is removed from the cache.
        ///  
        ///  First create a CacheItems.
        ///  <code>
        ///  string keys = {"First", "Second"};
        ///  CacheItem items = new CacheItem[2]
        ///  items[0] = new CacheItem(firstTimeStamp);
        ///  items[0].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        ///  items[0].Priority = CacheItemPriority.High;
        ///  items[0].ItemRemoveCallback = onRemove;
        /// 
        ///  items[1] = new CacheItem(secondTimeStamp);
        ///  items[1].AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        ///  items[1].Priority = CacheItemPriority.Low;
        ///  items[1].ItemRemoveCallback = onRemove;
        ///  </code>
        ///  
        ///  Then add CacheItem to the cache
        ///  <code>
        ///  
        ///  Cache cache = NCache.InitializeCache("myCache");
        /// 	cache.Add(keys, items);
        /// 
        ///  </code>        
        ///  </example>
        public virtual IDictionary AddBulk(string[] keys, CacheItem[] items)
        {
            long[] sizes = new long[items.Length];
            return AddBulkOperation(keys, items, ref sizes, true);
        }


        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        internal virtual object AddOperation(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority, 
            
            CacheDataNotificationCallback cacheItemUdpatedCallback, CacheDataNotificationCallback cacheItemRemovedCallaback,
            EventDataFilter itemUpdateDataFilter, EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            ValidateKeyValue(key, value);
            UsageStats stats = new UsageStats();
            stats.BeginSample();

            Hashtable queryInfo = new Hashtable();
            if (allowQueryTags)
            {
                queryInfo["query-info"] = GetQueryInfo(value);
            }
            BitSet flagMap = new BitSet();
            try
            {
                long objectSize = 0;
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[])value).Length : 0;
                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[])value).Length);
                if (DebugAPIConfiguraions.LoggingEnabled)
                {
                    LogSizeInfo(false, objectSize);
                }

                short removeCallbackID = -1;
                short updateCallbackID = -1;
                if (cacheItemRemovedCallaback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback, EventType.ItemRemoved, itemRemovedDataFilter);
                    removeCallbackID = callabackIds[1];
                }
                

                if (cacheItemUdpatedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback, EventType.ItemUpdated, itemUpdateDataFilter);
                    updateCallbackID = callabackIds[0];
                }
               

                absoluteExpiration = ToUTC(absoluteExpiration);
                
                _cacheImpl.Add(key, value, absoluteExpiration, slidingExpiration, priority, removeCallbackID, updateCallbackID, queryInfo, flagMap, itemUpdateDataFilter, itemRemovedDataFilter, size);

                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerAddSample(stats.Current);  
                    _perfStatsCollector.IncrementAddPerSecStats();
                }
                return value;
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

        internal virtual void MakeTargetCacheActivePassive(bool makeActive)
        {
            try
            {
                _cacheImpl.MakeTargetCacheActivePassive(makeActive);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }

        ///  <summary>
        ///  Add array of <see cref="CacheItem"/> to the cache.
        ///  </summary>
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
      
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Add(keys, items, "Customer", "Orders");
        ///
        /// </code>        
        /// </example>
        internal virtual IDictionary AddBulkOperation(string[] keys, CacheItem[] items, ref long[] sizes, bool allowQueryTags)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (items == null) throw new ArgumentNullException("items");
            if (keys.Length != items.Length) throw new ArgumentException("keys count is not equals to items count");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            if (IsNullOrDupliate(keys)) throw new ArgumentException("Duplicate keys found in provided 'key' array.");
           
            CacheItem[] clonedItems = new CacheItem[items.Length];

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
                
                string key = keys[i];
                BitSet flagMap = new BitSet();

                long size = 0;
                if (sizes[i] > 0)
                    size = sizes[i];

                CacheItem cloned = items[i].Clone() as CacheItem;
                if (cloned == null) throw new ArgumentNullException("items[" + i + "]");
                ValidateKeyValue(key, cloned.Value);

                Hashtable queryInfo = new Hashtable();

                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(cloned.Value);
                }
                cloned.QueryInfo = queryInfo;

                cloned.Value = SafeSerialize(cloned.Value, _serializationContext, ref flagMap, ref size);
                sizes[i] = size;
                if (DebugAPIConfiguraions.LoggingEnabled)
                    sumObjectSize += cloned.Value is byte[] ? ((byte[])cloned.Value).Length : 0;
                if (_perfStatsCollector != null)
                {
                    if (cloned.Value != null && cloned.Value is byte[]) _perfStatsCollector.IncrementAvgItemSize(((byte[])cloned.Value).Length);
                }

                cloned.FlagMap = flagMap;

                cloned.AbsoluteExpiration = ToUTC(cloned.AbsoluteExpiration);

                clonedItems[i] = cloned;
            }
            if (DebugAPIConfiguraions.LoggingEnabled)
                LogSizeInfo(true, sumObjectSize, items.Length);

            short dsItemAddedCallbackID = -1;
            try
            {
                return _cacheImpl.Add(keys, clonedItems, sizes);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Get("MyTextBox.Value");       
        /// </code>
        /// </example>
        public virtual object Get(string key)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                
                LockHandle lockHandle = null;
                return GetInternal(key, accessType, Cache.NoLockExpiration, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
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
        /// setting, this property throws exception incase of failure.</para>
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
        public virtual object Get(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)
        {
            try
            {
                LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
                return GetInternal(key, accessType, lockTimeout, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
        }

        ///  <summary>
        ///  Retrieves the object from the cache for the given keys as key value pairs
        ///  </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>
        /// <returns>The retrieved cache items.</returns>
        ///  <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        ///  <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        ///  <remarks>
        ///  <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        ///  setting, this property throws exception incase of failure.</para>
        ///  </remarks>
        ///  <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        ///  box server control.
        ///  <code>
        ///  Cache cache = NCache.InitializeCache("myCache");
        ///  string[] keys = new string[]{"myItem1", "myItem2"};
        /// 	IDictionary items = cache.Get(keys);
        ///  
        ///  </code>
        ///  </example>
        public virtual IDictionary GetBulk(string[] keys)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            RemoveDuplicateKeys(ref keys);
            try
            {

                BitSet flagMap = new BitSet();
                IDictionary table = (IDictionary)_cacheImpl.Get(keys, flagMap);

                long sumObjectSize = 0;
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
                                _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);

                            if (DebugAPIConfiguraions.LoggingEnabled)
                                sumObjectSize += result.Value is byte[] ? ((byte[])result.Value).Length : 0;

                            table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                        }
                    }
                }
                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize, noOfObjects);

                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }

        /// <summary>
        /// Get the cache item stored in cache.
        /// </summary>
        /// <param name="key">Key used to reference the desired object</param>        
        /// <returns>CacheItem</returns>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to retrieve the value cached for an ASP.NET text 
        /// box server control.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	CacheItem item = cache.GetCacheItem(key);
        /// 
        /// </code>
        /// </example>
        public virtual CacheItem GetCacheItem(string key)
        {
            try
            {
                LockAccessType accessType = LockAccessType.IGNORE_LOCK;
                
                LockHandle lockHandle = null;
                return GetCacheItemInternal(key, accessType, Cache.NoLockExpiration, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
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
        /// setting, this property throws exception incase of failure.</para>
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
        public virtual CacheItem GetCacheItem(string key, TimeSpan lockTimeout, ref LockHandle lockHandle, bool acquireLock)
        {
            try
            {
                LockAccessType accessType = acquireLock ? LockAccessType.ACQUIRE : LockAccessType.DONT_ACQUIRE;
                return GetCacheItemInternal(key, accessType, lockTimeout, ref lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
        }

        internal virtual CacheItem GetCacheItemInternal(string key, LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("keys");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            try
            {
                CacheItem item = new CacheItem();
                Alachisoft.NCache.Caching.CacheEntry entry = null;


                BitSet flagMap = new BitSet();
                object value = _cacheImpl.GetCacheItem(key, flagMap, ref lockHandle, lockTimeout, accessType);

                long objectSize = 0;

                if (value == null) return null;
                if (value is CacheItem)
                {
                    item = (CacheItem)value;
                    if (_perfStatsCollector != null && ((CacheItem)value).Value != null && ((CacheItem)value).Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])((CacheItem)value).Value).Length);
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = ((byte[])item.Value).Length;

                    item.Value = SafeDeserialize(item.Value, _serializationContext, item.FlagMap);
                    if (item.AbsoluteExpiration != Cache.NoAbsoluteExpiration)
                    {
                        item.AbsoluteExpiration = item.AbsoluteExpiration.ToLocalTime();
                    }
                    if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(false, objectSize);
                   
                    return item;
                }

                entry = (Alachisoft.NCache.Caching.CacheEntry)value;
                item.FlagMap = entry.Flag;

                if (entry.Value is CallbackEntry)
                {
                    CallbackEntry cb = (CallbackEntry)entry.Value;
                    if (cb.ItemRemoveCallbackListener != null && cb.ItemRemoveCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemRemoveCallbackListener)
                        {
                            if (cbInfo.Client == _cacheImpl.ClientID)
                            {
                                item.SetCacheDataNotification((CacheDataNotificationCallback)_callbackIDsMap.GetResource(cbInfo.Callback), EventType.ItemRemoved);
                               break;
                            }
                        }
                    }
                    if (cb.ItemUpdateCallbackListener != null && cb.ItemUpdateCallbackListener.Count > 0)
                    {
                        foreach (CallbackInfo cbInfo in cb.ItemUpdateCallbackListener)
                        {
                            if (cbInfo.Client == _cacheImpl.ClientID)
                            {

                                item.SetCacheDataNotification((CacheDataNotificationCallback)_callbackIDsMap.GetResource(cbInfo.Callback), EventType.ItemUpdated);
                                break;
                            }
                        }
                    }
                    item.Value = cb.Value;
                    if (_perfStatsCollector != null && item.Value != null && item.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])item.Value).Length);

                    item.Value = SafeDeserialize(cb.Value, _serializationContext, item.FlagMap);
                }
                else
                {
                    item.Value = entry.Value;
                    if (_perfStatsCollector != null && item.Value != null && item.Value is byte[])
                        _perfStatsCollector.IncrementAvgItemSize(((byte[])item.Value).Length);

                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = ((byte[])entry.Value).Length;
                    item.Value = SafeDeserialize(entry.Value, _serializationContext, item.FlagMap);
                }
                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(false, objectSize);
                if (entry != null)
                {
                    item.Priority = (CacheItemPriority)entry.Priority;
                }

                ExpirationHint hint = entry.ExpirationHint;
                
                DateTime absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
                TimeSpan slidingExpiration = TimeSpan.Zero;

                Alachisoft.NCache.Caching.AutoExpiration.DependencyHelper.GetActualCacheDependency(hint, ref absoluteExpiration, ref slidingExpiration);

                if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                {
                    item.AbsoluteExpiration = absoluteExpiration.ToLocalTime();
                }
                item.SlidingExpiration = slidingExpiration;
                item._creationTime = entry.CreationTime;
                item._lastModifiedTime = entry.LastModifiedTime;
                return item;

            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }
        
        internal virtual object GetInternal(string key,  LockAccessType accessType, TimeSpan lockTimeout, ref LockHandle lockHandle)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            long objectSize = 0;

            CompressedValueEntry result = null;
            try
            {
                BitSet flagMap = new BitSet();
                UsageStats stats = new UsageStats();
                stats.BeginSample();
                result = _cacheImpl.Get(key, flagMap, ref lockHandle, lockTimeout, accessType);
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
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = result.Value is byte[] ? ((byte[])result.Value).Length : 0;

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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to insert an item into an application's cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString);
        /// 
        /// </code>
        /// </example>
        public virtual void Insert(string key, object value)
        {
            long size = 0;
            InsertOperation(key, value,
                    Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                    CacheItemPriority.Default, 
                    null, LockAccessType.IGNORE_LOCK, null, null, EventDataFilter.None, EventDataFilter.None, ref size, true);

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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how to assign an item high priority when you insert 
        /// it into your application's <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Insert("DSN", connectionString, DateTime.Now.AddMinutes(2), TimeSpan.Zero, CacheItemPriority.High);
        /// 
        /// </code>
        /// </example>


        public virtual void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority)
        {
            long size = 0;
            InsertOperation(key, value, absoluteExpiration, slidingExpiration, priority, null, LockAccessType.IGNORE_LOCK, null, null, EventDataFilter.None, EventDataFilter.None, ref size, true);
        }

        /// <summary>
        /// Add a <see cref="CacheItem"/> to the cache
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>        
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
        public virtual void Insert(string key, CacheItem item)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");
            long size = 0;
            InsertOperation(key, item.Value,item.AbsoluteExpiration, item.SlidingExpiration,
                    item.Priority,
                    null,
                    LockAccessType.IGNORE_LOCK,
                    item.CacheItemUpdatedCallback, item.CacheItemRemovedCallback, item.ItemUpdatedCallabackDataFilter, item.ItemRemovedCallabackDataFilter, ref size, true);
        }

        /// <summary>
        /// Inserts a <see cref="CacheItem"/> to the cache if not already existing.
        /// Otherwise updates an existing item if it not already locked or if the correct lock-id is specified.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <param name="item">The item that is to be stored</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/>. If the item is locked, then it can be updated only if the correct lockHandle is specified.</param>
        /// <param name="releaseLock">A flag to determine whether or not release lock after operation is performed.</param>        
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
        public virtual void Insert(string key, CacheItem item, LockHandle lockHandle, bool releaseLock)
        {
            if (item == null)
                throw new ArgumentNullException("CacheItem");

            LockAccessType accessType = releaseLock ? LockAccessType.RELEASE : LockAccessType.DONT_RELEASE;
            long size = 0;
            InsertOperation(key, item.Value,
                   item.AbsoluteExpiration, item.SlidingExpiration,

                    item.Priority,
                    lockHandle,
                    accessType,
                    item.CacheItemUpdatedCallback,
                    item.CacheItemRemovedCallback,
                    item.ItemUpdatedCallabackDataFilter,
                    item.ItemRemovedCallabackDataFilter, ref size, true);
        }


        ///  <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        ///  <remarks> If CacheItem contains invalid values the related exception is thrown. 
        ///  See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        ///  <example>The following example demonstrates how to assign an item high priority when you insert 
        ///  it into your application's <see cref="Cache"/> object.
        /// 	<para><b>Note: </b>For more information about how to use this method with the <see cref="CacheItemRemovedCallback"/> 
        /// 	delegate, see <see cref="CacheItemRemovedCallback"/>.
        /// 	</para>
        ///  First create CacheItems.
        ///  <code>
        ///  string[] keys = {"SQLDSN", "ORADSN"};
        ///  CacheItem items[] = new CacheItem[2];
        ///  items[0] = new CacheItem(sqlConnectionString);
        ///  item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        ///  item.Priority = CacheItemPriority.High;
        ///  item.ItemRemoveCallback = onRemove;
        ///  
        ///  items[1] = new CacheItem(oraConnectionString);
        ///  item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        ///  item.Priority = CacheItemPriority.Low;
        ///  item.ItemRemoveCallback = onRemove;
        ///  </code>
        ///  
        ///  Then insert CacheItems to the cache
        ///  <code>
        ///  Cache cache = NCache.InitializeCache("myCache");
        ///  string[] keys = new string[]{"myItem1", "myItem2"};
        ///  CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        ///  
        ///  </code>
        ///  </example>
        public virtual IDictionary InsertBulk(string[] keys, CacheItem[] items)
        {
            long[] sizes = new long[items.Length];
            return InsertBulkOperation(keys, items, ref sizes, true);
        }

        ///  <summary> Insert list of <see cref="CacheItem"/> to the cache </summary>
        /// <param name="keys">The cache keys used to reference the items.</param>
        /// <param name="items">The items that are to be stored</param>
        /// <param name="sizes"></param>
        /// <param name="allowQueryTags"></param>
        /// <returns>returns keys that are added or updated successfully and their status.</returns>
        ///  <remarks> If CacheItem contains invalid values the related exception is thrown. 
        ///  See <see cref="CacheItem"/> for invalid property values and related exceptions</remarks>
        ///  <example>The following example demonstrates how to assign an item high priority when you insert 
        ///  it into your application's <see cref="Cache"/> object.
        /// 	<para><b>Note: </b>For more information about how to use this method with the <see cref="CacheItemRemovedCallback"/> 
        /// 	delegate, see <see cref="CacheItemRemovedCallback"/>.
        /// 	</para>
        ///  First create CacheItems.
        ///  <code>
        ///  string[] keys = {"SQLDSN", "ORADSN"};
        ///  CacheItem items[] = new CacheItem[2];
        ///  items[0] = new CacheItem(sqlConnectionString);
        ///  item.AbsoluteExpiration = DateTime.Now.AddMinutes(2);
        ///  item.Priority = CacheItemPriority.High;
        ///  item.ItemRemoveCallback = onRemove;
        ///  
        ///  items[1] = new CacheItem(oraConnectionString);
        ///  item.AbsoluteExpiration = DateTime.Now.AddMinutes(1);
        ///  item.Priority = CacheItemPriority.Low;
        ///  item.ItemRemoveCallback = onRemove;
        ///  </code>
        ///  
        ///  Then insert CacheItems to the cache
        ///  <code>
        ///  Cache cache = NCache.InitializeCache("myCache");
        ///  string[] keys = new string[] { "myItem1", "myItem2" };
        ///  CacheItem[] items = new CacheItem[]{myItem1, myItem2};
        /// 	cache.Insert(keys, items, "Connection", null);
        ///  
        ///  </code>
        ///  </example>        
        internal virtual IDictionary InsertBulkOperation(string[] keys, CacheItem[] items, ref long[] sizes, bool allowQueryTags)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (items == null) throw new ArgumentNullException("items");
            if (keys.Length != items.Length) throw new ArgumentException("keys count is not equals to items count");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            if (IsNullOrDupliate(keys)) throw new ArgumentException("Duplicate keys found in provided 'key' array.");
            CacheItem[] clonedItems = new CacheItem[items.Length];
            
            int noOfObjects = 0;
            long sumObjectSize = 0;

            if (DebugAPIConfiguraions.LoggingEnabled)
                noOfObjects = items.Length;

            for (int i = 0; i < items.Length; i++)
            {
                string key = keys[i];
                BitSet flagMap = new BitSet();
                if (items[i] == null)
                    throw new Exception("CacheItem cannot be null");
                
                CacheItem cloned = items[i].Clone() as CacheItem;

                long size = 0;
                if (sizes[i] > 0)
                    size = sizes[i];

                ValidateKeyValue(key, cloned.Value);

                Hashtable queryInfo = new Hashtable();
                if (allowQueryTags)
                {
                    queryInfo["query-info"] = GetQueryInfo(cloned.Value);
                }
                cloned.QueryInfo = queryInfo;

                cloned.Value = SafeSerialize(cloned.Value, _serializationContext, ref flagMap, ref size);
                sizes[i] = size;

                if (DebugAPIConfiguraions.LoggingEnabled)
                    sumObjectSize += cloned.Value is byte[] ? ((byte[])cloned.Value).Length : 0;

                if (_perfStatsCollector != null)
                {
                    if (cloned.Value != null) _perfStatsCollector.IncrementAvgItemSize(((byte[])cloned.Value).Length);
                }

                cloned.FlagMap = flagMap;

                cloned.AbsoluteExpiration = ToUTC(cloned.AbsoluteExpiration);

                clonedItems[i] = cloned;
            }

            if (DebugAPIConfiguraions.LoggingEnabled)
                LogSizeInfo(true, sumObjectSize, noOfObjects);

            short dsItemAddedCallbackID = -1;


            try
            {
                return _cacheImpl.Insert(keys, clonedItems, sizes);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
                return null;
            }
        }

        /// <summary>
        /// Function that choose the appropriate function of NCache's Cache, that need to be called
        /// according to the data provided to it.</summary>
        internal virtual void InsertOperation(string key, object value, DateTime absoluteExpiration,
            TimeSpan slidingExpiration, CacheItemPriority priority,
            LockHandle lockHandle, LockAccessType accessType, CacheDataNotificationCallback cacheItemUdpatedCallback,
            CacheDataNotificationCallback cacheItemRemovedCallaback, EventDataFilter itemUpdateDataFilter, EventDataFilter itemRemovedDataFilter, ref long size, bool allowQueryTags)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");


            ValidateKeyValue(key, value);
            UsageStats stats = new UsageStats();
            stats.BeginSample();
            Hashtable queryInfo = new Hashtable();
            object lockId = (lockHandle == null) ? null : lockHandle.LockId;

            if (allowQueryTags)
            {
                queryInfo["query-info"] = GetQueryInfo(value);
            }            
            short removeCallbackId = -1;
            short updateCallbackId = -1;
            short dsItemUpdateCallbackId = -1;

            BitSet flagMap = new BitSet();
            try
            {
                value = SafeSerialize(value, _serializationContext, ref flagMap, ref size);

                long objectSize = 0;
                if (DebugAPIConfiguraions.LoggingEnabled)
                    objectSize = value is byte[] ? ((byte[])value).Length : 0;
                if (_perfStatsCollector != null && value != null && value is byte[])
                    _perfStatsCollector.IncrementAvgItemSize(((byte[])value).Length);
               
                if (cacheItemRemovedCallaback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemRemovedCallaback, EventType.ItemRemoved, itemRemovedDataFilter);
                    removeCallbackId = callabackIds[1];
                }
                

                if (cacheItemUdpatedCallback != null)
                {
                    short[] callabackIds = _eventManager.RegisterSelectiveEvent(cacheItemUdpatedCallback, EventType.ItemUpdated, itemUpdateDataFilter);
                    updateCallbackId = callabackIds[0];
                }
                

                if (lockId != null && ((string)lockId) != string.Empty)
                    flagMap.SetBit(BitSetConstants.LockedItem);
                else
                    flagMap.UnsetBit(BitSetConstants.LockedItem);

                absoluteExpiration = ToUTC(absoluteExpiration);
                
                _cacheImpl.Insert(key, value, absoluteExpiration, slidingExpiration, priority, removeCallbackId, updateCallbackId, queryInfo, flagMap, lockId, accessType, itemUpdateDataFilter, itemRemovedDataFilter, size);
 
                if (_perfStatsCollector != null)
                {
                    stats.EndSample();
                    _perfStatsCollector.IncrementMsecPerUpdSample(stats.Current);
                    _perfStatsCollector.IncrementUpdPerSecStats();
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }
        #endregion

        #region /                --- Key based notifications registration ---       /


        
        /// <summary>
        /// Registers ItemUpdate or ItemRemoved events with cache
        /// </summary>
        /// <remarks>
        /// Client application can show interest in receiving events if an item is update or removed from the cache.
        /// As soon as the item is updated or removed from the cache, the client application is notified
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
        internal virtual CacheEventDescriptor RegisterCacheNotification(CacheDataNotificationCallback cacheDataNotificationCallback, EventType eventType, EventDataFilter datafilter)
        {
            if (cacheDataNotificationCallback == null)
                throw new ArgumentException("cacheDataNotificationCallback");

            return RegisterCacheNotificationInternal(null, cacheDataNotificationCallback, eventType, datafilter, true);
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
        public virtual void RegisterCacheNotification(string key, CacheDataNotificationCallback selectiveCacheDataNotificationCallback, EventType eventType)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentNullException("key");

            if (selectiveCacheDataNotificationCallback == null)
                throw new ArgumentException("selectiveCacheDataNotificationCallback");
            EventDataFilter datafilter = EventDataFilter.None;

            RegisterCacheNotificationInternal(key, selectiveCacheDataNotificationCallback, eventType, datafilter, true);
        }

        internal virtual CacheEventDescriptor RegisterCacheNotificationInternal(string key, CacheDataNotificationCallback callback, EventType eventType, EventDataFilter datafilter, bool notifyOnItemExpiration)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            CacheEventDescriptor discriptor = null;
            try
            {
                if (key != null)
                {
                    short[] callbackRefs = _eventManager.RegisterSelectiveEvent(callback, eventType, datafilter);
                    _cacheImpl.RegisterKeyNotificationCallback(key, callbackRefs[0], callbackRefs[1], datafilter, notifyOnItemExpiration);
                }
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return discriptor;
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
        /// cache.RegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated, EventDataFilter.None);
        /// </code>
        /// Now, Unregister this event by providing the key, callback and eventtype
        /// <code>
        /// cache.UnRegisterCacheNotification(key, new CacheDataNotificationCallback(ItemCallback), EventType.ItemUpdated);
        /// </code>
        /// </example>
        public virtual void UnRegisterCacheNotification(string key, CacheDataNotificationCallback callback, EventType eventType)
        {
            if (_cacheImpl == null)
                if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null)
                throw new ArgumentNullException("key");
            if (callback == null)
                throw new ArgumentNullException("CacheDataNotificationCallback");
            try
            {
                short[] value = this._eventManager.UnregisterSelectiveNotification(callback, eventType);

                short update = value[0];
                short remove = value[1];

                _cacheImpl.UnRegisterKeyNotificationCallback(key, update, remove);
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
        public virtual void Unlock(string key)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");

            try
            {
                _cacheImpl.Unlock(key);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
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
        public virtual void Unlock(string key, LockHandle lockHandle)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");
            object lockId = (lockHandle == null) ? null : lockHandle.LockId;
            try
            {
                _cacheImpl.Unlock(key, lockId);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
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
        public virtual bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");

            lockHandle = null;
            bool lockAcquired = false;
            try
            {
                lockAcquired = _cacheImpl.Lock(key, lockTimeout, out lockHandle);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
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
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");
            if (key == null) throw new ArgumentNullException("key is null.");

            try
            {
                return _cacheImpl.IsLocked(key, ref lockHandle);
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Remove("timestamp");
        /// 
        /// </code>
        /// </example>
        public virtual object Remove(string key)
        {
            return Remove(key, null, LockAccessType.IGNORE_LOCK);
        }

        /// <summary>
        /// Delete the object from the <see cref="Cache"/>.
        /// </summary>
        /// <param name="key">The cache key used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        ///	cache.Delete("timestamp");
        /// 
        /// </code>
        /// </example>
        public virtual void Delete(string key)
        {
            Delete(key, null, LockAccessType.IGNORE_LOCK);
        }


        internal virtual object Remove(string key, LockHandle lockHandle, LockAccessType accessType)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            long objectSize = 0;

            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();
                object lockId = (lockHandle == null) ? null : lockHandle.LockId; 
                BitSet flagMap = new BitSet();
                short dsItemRemovedCallbackId = -1;

                CompressedValueEntry result = _cacheImpl.Remove(key, flagMap, lockId,  accessType);
                if (result != null && result.Value != null)
                {
                    if (result.Value != null && result.Value is CallbackEntry)
                    {
                        result.Value = ((CallbackEntry)result.Value).Value;
                    }
                    if (DebugAPIConfiguraions.LoggingEnabled)
                        objectSize = result.Value is byte[] ? ((byte[])result.Value).Length : 0;
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
        
        internal virtual void Delete(string key, LockHandle lockHandle,  LockAccessType accessType)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (key == null) throw new ArgumentNullException("key");
            if (key == string.Empty) throw new ArgumentException("key cannot be empty string");
            try
            {
                UsageStats stats = new UsageStats();
                stats.BeginSample();
                object lockId = (lockHandle == null) ? null : lockHandle.LockId; 
                BitSet flagMap = new BitSet();
               
                _cacheImpl.Delete(key, flagMap, lockId, accessType);
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

        public virtual object Remove(string key, LockHandle lockHandle)
        {
            return Remove(key, lockHandle, LockAccessType.DEFAULT);
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
        ///         theCache.Delete("cachedItemKey", lockHandle);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///         //Do something
        ///     }
        /// }
        /// </code>
        /// </example>

        public virtual void Delete(string key, LockHandle lockHandle)
        {
            Delete(key, lockHandle, LockAccessType.DEFAULT);
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.RemoveBulk(keys);
        /// 
        /// </code>
        /// </example>
        /// 
        public virtual IDictionary RemoveBulk(string[] keys)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            IsNullOrDupliate(keys);
            int noOfObjects = 0;
            long sumObjectSize = 0;

            try
            {
                BitSet flagMap = new BitSet();
                IDictionary table = _cacheImpl.Remove(keys, flagMap);

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
                                _perfStatsCollector.IncrementAvgItemSize(((byte[])result.Value).Length);


                            if (DebugAPIConfiguraions.LoggingEnabled)
                                sumObjectSize += result.Value != null && result.Value is byte[] ? ((byte[])result.Value).Length : 0;

                            table[ie.Current] = SafeDeserialize(result.Value, _serializationContext, result.Flag);
                        }
                    }
                }
                if (DebugAPIConfiguraions.LoggingEnabled)
                    LogSizeInfo(true, sumObjectSize, noOfObjects);
                return table;
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
            return null;
        }

        ///  <summary>
        ///  Removes the objects from the <see cref="Cache"/>.
        ///  </summary>
        /// <param name="keys">The cache keys used to reference the item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para><b>Note:</b> If exceptions are enabled through the <see cref="ExceptionsEnabled"/> 
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="Cache"/> object.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// string[] keys = new string[]{"myItem1", "myItem2"};
        ///	cache.DeleteBulk(keys);
        /// 
        /// </code>
        /// </example>
        /// 

        public virtual void DeleteBulk(string[] keys)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (keys == null) throw new ArgumentNullException("keys");
            if (keys.Length == 0) throw new ArgumentException("There is no key present in keys array");
            IsNullOrDupliate(keys);
            try
            {
                BitSet flagMap = new BitSet();
                _cacheImpl.Delete(keys, flagMap);
            }
            catch (Exception)
            {
                if (ExceptionsEnabled) throw;
            }
        }


        #endregion

        #region /                 --- Search ---            /

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified, and return keys.
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
        public virtual ICollection Search(string query, IDictionary values)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");

            QueryResultSet resultSet = null;
            ArrayList collection = new ArrayList();

            try
            {
                resultSet = _cacheImpl.Search(query, values);

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common.Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG", ((Alachisoft.NCache.Common.Queries.AverageResult)resultSet.AggregateFunctionResult.Value).Average);
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

            return collection;
        }

        /// <summary>
        /// Performs search on the <see cref="Cache"/> based on the query specified, and return cache items.
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
        public virtual IDictionary SearchEntries(string query, IDictionary values)
        {
            if (_cacheImpl == null) throw new OperationFailedException("Cache is not initialized");

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");
            if (values == null) throw new ArgumentNullException("values");
            if (query.ToLower().Contains("group by")) throw new OperationFailedException("Queries with GROUP BY clause can not be executed using SearchEntries API.");

            QueryResultSet resultSet = null;
            Hashtable resultValues = new Hashtable();

            try
            {
                resultSet = _cacheImpl.SearchEntries(query, values);

                switch (resultSet.Type)
                {
                    case QueryType.AggregateFunction:
                        if (resultSet.AggregateFunctionResult.Value != null)
                        {
                            if (resultSet.AggregateFunctionResult.Key.ToString().ToUpper() == Alachisoft.NCache.Common.Enum.AggregateFunctionType.AVG.ToString().ToUpper())
                            {
                                resultSet.AggregateFunctionResult = new DictionaryEntry("AVG", ((Alachisoft.NCache.Common.Queries.AverageResult)resultSet.AggregateFunctionResult.Value).Average);
                            }
                            resultValues.Add(resultSet.AggregateFunctionResult.Key.ToString(), resultSet.AggregateFunctionResult.Value);
                        }
                        break;

                    case QueryType.SearchEntries:
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
                                    _perfStatsCollector.IncrementAvgItemSize(((byte[])cmpEntry.Value).Length);
                                if (DebugAPIConfiguraions.LoggingEnabled)
                                {
                                    noOfObjects++;
                                    sumObjectSize += cmpEntry.Value is byte[] ? ((byte[])cmpEntry.Value).Length : 0;
                                }

                                resultValues[ide.Key] = SafeDeserialize(cmpEntry.Value, _serializationContext, cmpEntry.Flag);

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

            return resultValues;
        }

        #endregion

        internal virtual object SafeSerialize(object serializableObject, string serializationContext, ref BitSet flag, ref long size)
        {
            object serializedObject = null;

            if (_cacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            if (serializableObject != null)
            {
                UsageStats statsSerialization = new UsageStats();
                statsSerialization.BeginSample();
                serializedObject = _cacheImpl.SafeSerialize(serializableObject, serializationContext, ref flag, _cacheImpl, ref size);
                statsSerialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerSerialization(statsSerialization.Current);
            }

            return serializedObject;
        }

        internal virtual object SafeDeserialize(object serializedObject, string serializationContext, BitSet flag)
        {
            object deSerializedObject = null;

            if (_cacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            if (serializedObject != null)
            {
                UsageStats statsSerialization = new UsageStats();
                statsSerialization.BeginSample();
                deSerializedObject = _cacheImpl.SafeDeserialize(serializedObject, serializationContext, flag, _cacheImpl);
                statsSerialization.EndSample();
                if (_perfStatsCollector != null)
                    _perfStatsCollector.IncrementMsecPerSerialization(statsSerialization.Current);
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
        /// setting, this property throws exception incase of failure.</para>
        /// </remarks>
        /// <returns>An enumerator to iterate through the <see cref="Cache"/> object.</returns>
        public virtual IEnumerator GetEnumerator()
        {
            WebCacheEnumerator enumerator = new WebCacheEnumerator(_serializationContext, this);
            return enumerator;
        }

        internal virtual List<EnumerationDataChunk> GetNextChunk(List<EnumerationPointer> pointer)
        {
            if (_cacheImpl == null)
                throw new OperationFailedException("Cache is not initialized");

            List<EnumerationDataChunk> chunks = null;

            try
            {
                chunks = _cacheImpl.GetNextChunk(pointer);
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
                    _cacheImpl.GetNextChunk(pointer);
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

        #region	/                 --- Nested Classes ---           /

        ///// <summary>
        ///// Providers hanlders for <see cref="Alachisoft.NCache.Caching.Cache"/> events. This is an 
        ///// internal class and must not be used from outside
        ///// </summary>
        internal class CacheAsyncEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache _parent;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal CacheAsyncEventsListener(Cache parent)
            {
                _parent = parent;
            }


            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
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
        internal class ClusterEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>

            private Web.Caching.Cache _parent;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal ClusterEventsListener(Cache parent)
            {
                _parent = parent;

            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
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
        internal class CacheEventsListener : MarshalByRefObject, IDisposable
        {
            /// <summary> Underlying implementation of NCache. </summary>
            private Web.Caching.Cache _parent;
            private EventManager _eventManager;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            internal CacheEventsListener(Cache parent, EventManager eventManager)
            {
                _parent = parent;
                _eventManager = eventManager;

            }

            #region	/                 --- IDisposable ---           /

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or 
            /// resetting unmanaged resources.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public virtual void Dispose()
            {
                try
                {

                }
                catch { }
            }

            #endregion

            public virtual void OnCacheStopped(string cacheId, bool notifyAsync)
            {
                try
                {

                    if (_parent._cacheStopped != null)
                    {
                        Delegate[] list = _parent._cacheStopped.GetInvocationList();
                        CacheStoppedCallback cb;
                        for (int i = 0; i < list.Length; i++)
                        {
                            cb = list[i] as CacheStoppedCallback;

                            if (notifyAsync)
                            {
                                cb.BeginInvoke(cacheId, null, null);
                            }
                            else
                                cb(cacheId);
                        }
                    }

                }
                catch { }
            }

            public virtual void OnCustomRemoveCallback(string key, object value, CacheItemRemovedReason reason, BitSet flag, bool notifyAsync, EventCacheItem item)
            {
                try
                {
                    object[] args = value as object[];
                    if (args != null)
                    {
                        object val = args[0];
                        CallbackInfo cbInfo = args[1] as CallbackInfo;
                        if (cbInfo != null)
                        {
                            if (_parent._perfStatsCollector != null)
                                _parent._perfStatsCollector.IncrementEventsProcessedPerSec();

                            if (item != null) item.Value = GetObject(item.Value, flag);
                            EventHandle handle = new EventHandle((short)cbInfo.Callback);
                            _parent.EventManager.RaiseSelectiveCacheNotification(key, EventType.ItemRemoved, item, null, reason, notifyAsync, handle, cbInfo.DataFilter);
                        }
                    }
                }
                catch { }
            }

            public virtual void OnCustomUpdateCallback(string key, object value, bool notifyAsync, EventCacheItem item,
                EventCacheItem oldItem, BitSet flag)
            {
                try
                {

                    CallbackInfo cbInfo = value as CallbackInfo;
                    if (cbInfo != null)
                    {
                        if (item != null) item.Value = GetObject(item.Value, flag);
                        if (oldItem != null) oldItem.Value = GetObject(oldItem.Value, flag);

                        if (_parent._perfStatsCollector != null)
                            _parent._perfStatsCollector.IncrementEventsProcessedPerSec();

                        EventHandle handle = new EventHandle((short) cbInfo.Callback);
                        this._eventManager.RaiseSelectiveCacheNotification(key, EventType.ItemUpdated, item, oldItem,
                            CacheItemRemovedReason.Underused, notifyAsync, handle, cbInfo.DataFilter);
                    }
                }
                catch
                {
                }
            }


            private object GetObject(object value, BitSet Flag)
            {
                try
                {
                    if (value is CallbackEntry)
                    {
                        value = ((CallbackEntry)value).Value;
                    }

                    if (value is UserBinaryObject)
                        value = ((UserBinaryObject)value).GetFullObject();

                    return _parent.SafeDeserialize(value, _parent._serializationContext, Flag);
                }
                catch (Exception ex)
                {
                    return value;
                }
            }

        }
        #endregion

        #region/          ---Serialization Compact Framework---         /
       
        /// <summary>
        /// Initializes the Compact Serilization Framework.
        /// </summary>
        internal virtual void InitializeCompactFramework()
        {
            if (CacheImpl is RemoteCache)
                Alachisoft.NCache.Util.MiscUtil.RegisterCompactTypes();
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.ProductVersion), 302);
            CompactFormatterServices.RegisterCompactType(typeof(CallbackEntry), 107);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.Net.Address), 110);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.NCacheSessionItem), 129);
           
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
        
        /// <summary>
        /// Finds duplicate in the 'key' array.
        /// </summary>
        /// <param name="keys">The keys against which items are to be fetched.</param>        
        /// <returns>True if duplicate exists else false.</returns>       
        /// <exception cref="ArgumentException"><paramref name="keys"/> Thrown if duplicate is found.</exception>        
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> Thrown if Key is Null.</exception> 
        private Boolean IsNullOrDupliate(string[] keys)
        {
            Hashtable hashtable = new Hashtable(keys.Length);
            bool duplicateFound = false;
            try
            {

                for (int i = 0; i < keys.Length; i++)
                {
                    //If Count is less than the capacity of the Hashtable, this method is an O(1) operation. 
                    //If the capacity needs to be increased to accommodate the new element, this method becomes an O(n) operation, 
                    //where n is Count.
                    hashtable.Add(keys[i], null);
                }
            }
            catch (ArgumentNullException e)
            {
                hashtable.Clear();
                throw e;
            }
            catch (ArgumentException e)
            {
                duplicateFound = true;
                hashtable.Clear();
            }
            return duplicateFound;
        }

        /// <summary>
        /// Adds Size info to hashtable for logging
        /// </summary>
        /// <param name="isBulk"></param>
        /// <param name="objectSize">Size of Object after serialization. Incase of Bulk, it will be the sum of size of objects.</param>
        /// <param name="noOfObjects"></param>
        private void LogSizeInfo(bool isBulk, long objectSize, int noOfObjects)
        {
            Web.Caching.APILogging.RuntimeAPILogItem rtAPILogItem = new Web.Caching.APILogging.RuntimeAPILogItem();
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

        #region /                                   --- internal classes ----                       /

        internal class GeneralDataNotificationWrapper
        {
            private Cache _parentCache;

            public GeneralDataNotificationWrapper(Cache parentCache)
            {
                _parentCache = parentCache;
            }
        }

        #endregion
    }
}

