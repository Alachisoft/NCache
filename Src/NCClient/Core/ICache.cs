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


using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alachisoft.NCache.Client.Services;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// This interface contians the services and methods that are used to perform operations on the cache.
    /// </summary>
    public interface ICache :IDisposable, IEnumerable
    {
        #region Properties 

        /// <summary>
        /// Gets the number of items stored in the cache.
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Displays the information related to this client.
        /// </summary>
        ClientInfo ClientInfo { get; }

        /// <summary>
        /// Gets the information of all connected clients to the cache.
        /// </summary>
        IList<ClientInfo> ConnectedClientList { get; }

        /// <summary>
        /// Gets an instance of <see cref="IMessagingService"/>.
        /// </summary>
        IMessagingService MessagingService { get; }


        /// <summary>
        /// Gets an instance of <see cref="IExecutionService"/>.
        /// </summary>
        //IExecutionService ExecutionService { get; }

        /// <summary>
        /// Gets an instance of <see cref="IDataTypeManager"/>.
        /// </summary>
        //IDataTypeManager DataTypeManager { get; }

        #endregion

        #region Add Operations

        /// <summary>
        /// Adds an item into the Cache object with a cache key to reference its location.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="value">The item (object) to be stored in the cache.</param>
        /// <example>Example demonstrates how to add a value to cache.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        /// 
        ///	cache.Add(key, product);
        /// </code>        
        /// </example>    
        void Add(string key, object value);

        /// <summary>
        /// Adds a <see cref="CacheItem"/> to the cache.
        /// Using CacheItem, you can also specify properties for the cache items, for e.g., expiration and priority.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="item"><see cref="CacheItem"/> that is to be stored in the cache.</param>
        /// <remarks>If CacheItem contains invalid values, the related exception is thrown. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions.</remarks>
        /// <example>Example demonstrates how to add an item to the cache with a sliding expiration of 5 minutes, a priority of 
        /// high.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// CacheItem item = new CacheItem(product);
        /// item.Expiration = new Expiration(ExpirationType.Sliding,new TimeSpan(0, 5, 0));
        /// item.Priority = CacheItemPriority.High;
        /// 
        /// string key = "Product0";
        /// 
        /// 
        /// cache.Add(key, item);
        /// </code>
        /// </example>
        void Add(string key, CacheItem item);

        /// <summary>
        /// Adds a dictionary of cache keys with <see cref="CacheItem"/> to the cache. 
        /// The CacheItem contains properties to associate with the item, like expiration, dependencies and eviction information. 
        /// </summary>
        /// <param name="items">Dictionary of keys and <see cref="CacheItem"/>. Keys must be unique.</param>>
        /// <returns>Dictionary of Keys along with Exception that were unable to store in cache.</returns>
        /// <example>The following example demonstrates how to add items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        ///
        /// CacheItem[] cacheItems = new CacheItem[2];
        /// 
        /// Product product_1 = new Product();
        /// product_1.Id = 1;
        /// product_1.Name = "Chai";
        /// 
        /// Product product_2 = new Product();
        /// product_2.Id = 2;
        /// product_2.Name = "Chang";
        /// 
        /// Product product_3 = new Product();
        /// product_3.Id = 2;
        /// product_3.Name = "Aniseed Syrup";
        /// 
        /// cacheItems[0] = new CacheItem(product_1);
        /// cacheItems[0].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[0].Priority = CacheItemPriority.High;
        /// 
        /// cacheItems[1] = new CacheItem(product_2);
        /// cacheItems[1].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[1].Priority = CacheItemPriority.Normal;
        /// 
        /// cacheItems[2] = new CacheItem(product_3);
        /// cacheItems[2].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[2].Priority = CacheItemPriority.Low;
        /// 
        /// IDictionary&lt;string, CacheItem&gt; items = new Dictionary&lt;string, CacheItem&gt;()
        /// {
        ///     { "Product0",cacheItems[0]},
        ///     { "Product1",cacheItems[1]},
        ///     { "Product2",cacheItems[2]}
        /// }
        /// 
        /// 
        /// cache.AddBulk(items);
        /// </code>        
        /// </example>

        IDictionary<string, Exception> AddBulk(IDictionary<string, CacheItem> items);

       

        #endregion

        #region Insert Operations

        /// <summary>
        /// Inserts an item (object) into the cache.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="value">The item (object) that is to be inserted into the cache.</param>
        /// <remarks> 
        /// If the key already exists, this overload overwrites the values of the existing <seealso cref="ICache"/> item. If the key does not exist, it adds the item to the cache.
        /// If CacheItem contains invalid values, the related exception is thrown. 		
        /// </remarks> 
        /// <example>The following example demonstrates how to insert an item (object) into the cache.
        /// <code>
        /// Cache cache = NCache.InitializeCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        ///  
        /// cache.Insert(key,product);
        /// </code>
        /// </example>
        void Insert(string key, object value);

        // functionality of lockhandle with readthru is not supported.
        /// <summary>
        /// Inserts a <see cref="CacheItem"/> into the cache, allowing to specify the Write-Through option.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="item">The CacheItem that is to be inserted into the cache.</param>
        /// <param name="lockHandle">An instance of <seealso cref="LockHandle"/> that holds the lock information. If the item is locked, then it can only be updated if the correct lockHandle is specified.</param>
        /// <param name="releaseLock">A flag to determine whether or not the lock should be released after operation is performed.</param>
        /// <remarks> If the key already exists, this overload overwrites the values of the existing <seealso cref="ICache"/> item. If the key does not exist, it adds the item to the cache.
        /// If CacheItem contains invalid values the related exception is thrown. Functionality of lockhandle with Readthru is not supported. 
        /// See <see cref="CacheItem"/> for invalid property values and related exceptions.</remarks>			
        /// <example>Example demonstrates how to insert an item to the cache with a sliding expiration of 5 minutes, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        ///
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        ///
        /// CacheItem item = new CacheItem(product);
        /// item.Priority = CacheItemPriority.Low;
        ///
        /// string key = "Product0";
        ///
        /// cache.Add(key, item);
        ///
        /// LockHandle lockHandle = new LockHandle();
        ///
        /// CacheItem cachedItem = cache.Get&lt;CacheItem&gt;("cachedItemKey", true, new TimeSpan(0, 5, 0), ref lockHandle);
        ///
        /// if (cachedItem != null)
        /// {
        ///     try
        ///     {
        ///        cachedItem.Priority = CacheItemPriority.High;
        ///        cachedItem.Expiration = new Expiration(ExpirationType.Sliding, new TimeSpan(0, 2, 0));
        ///
        ///
        ///        cache.Insert(key, cachedItem, lockHandle, true);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///     ...
        ///     }
        /// }
        /// </code>
        /// </example>
        void Insert(string key, CacheItem item, LockHandle lockHandle = null, bool releaseLock = false);

        /// <summary>
        /// Inserts a dictionary of cache keys with <see cref="CacheItem"/> to the cache with the WriteThruOptions. 
        /// The CacheItem contains properties to associate with the item, like expiration, dependencies and eviction information. 
        /// </summary>
        /// <param name="items">Dictionary of keys and <see cref="CacheItem"/>. Keys must be unique.</param>>
        /// <returns>Dictionary of Keys along with Exception that were unable to store in cache.</returns>
        /// <remarks> If the key or multilple keys already exist, this overload overwrites the values of the existing <seealso cref="ICache"/> items.
        /// If the key does not exist, it adds the item to the cache.</remarks>
        /// <example>The following example demonstrates how to insert items to the cache with an absolute 
        /// expiration 2 minutes from now, a priority of 
        /// high, and that notifies the application when the item is removed from the cache.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        ///
        /// CacheItem[] cacheItems = new CacheItem[2];
        /// 
        /// Product product_1 = new Product();
        /// product_1.Id = 1;
        /// product_1.Name = "Chai";
        /// 
        /// Product product_2 = new Product();
        /// product_2.Id = 2;
        /// product_2.Name = "Chang";
        /// 
        /// Product product_3 = new Product();
        /// product_3.Id = 2;
        /// product_3.Name = "Aniseed Syrup";
        /// 
        /// cacheItems[0] = new CacheItem(product_1);
        /// cacheItems[0].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[0].Priority = CacheItemPriority.High;
        /// 
        /// cacheItems[1] = new CacheItem(product_2);
        /// cacheItems[1].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[1].Priority = CacheItemPriority.Normal;
        /// 
        /// cacheItems[2] = new CacheItem(product_3);
        /// cacheItems[2].Expiration = new Expiration(ExpirationType.Absolute, new TimeSpan(0, 2, 0));
        /// cacheItems[2].Priority = CacheItemPriority.Low;
        /// 
        /// IDictionary&lt;string, CacheItem&gt; items = new Dictionary&lt;string, CacheItem&gt;()
        /// {
        ///     { "Product0",cacheItems[0]},
        ///     { "Product1",cacheItems[1]},
        ///     { "Product2",cacheItems[2]}
        /// }
        /// 
        /// 
        /// cache.InsertBulk(items);
        /// </code>        
        /// </example>
        IDictionary<string , Exception> InsertBulk(IDictionary<string, CacheItem> items);


        
        /// <summary>
        /// Update <see cref="CacheItemAttributes"/> of an existing item in cache.
        /// </summary>
        /// <param name="key">Unique key to identify the cache item.</param>
        /// <param name="attributes">An instance of<see cref="CacheItemAttributes"/> to update item in the cache.</param>
        /// <returns>Flag that determines status of the Update operation. <b>True</b> if attributes of 
        /// the item in cache was updated successfully and <b>False</b> if operation failed.
        /// </returns>
        /// <example>Example demonstrates how to update Absolute Expiration of 5 minutes on an existing item in cache. 
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        /// 
        /// cache.Insert(key, product);
        /// 
        /// CacheItemAttributes attributes = new CacheItemAttributes();
        /// 
        /// if(cache.UpdateAttributes(key, attributes))
        /// {
        /// ...
        /// }
        /// </code>
        /// </example>
        //bool UpdateAttributes(string key, CacheItemAttributes attributes);

        #endregion

        #region Get Operations 

        /// <summary>
        /// Retrieves the specified item from the Cache object, with read-through caching option available. If the option of read-through has been set, the object will be fetched from the data source if it does not exist in cache.
        /// </summary>
        /// <typeparam name="T">Specifies the type of value obtained from the cache.</typeparam>
        /// <param name="key">The unique identifier for the cache item to be retrieved.</param>
        /// <returns>The retrieved cache item, or a null reference if the key is not found.</returns>
        /// <remarks>
        /// If the key does not exists in the cache then null is returned.
        /// </remarks>
        /// <example>Example demonstrates how to retrieve the value from cached 
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// string key = "Product0";
        /// 
        /// 
        ///	Product product = cache.Get&lt;Product&gt;(key,readThruOptions);
        /// </code>
        /// </example>
        T Get<T>(string key);

        // functionality of lockhandle with readthru is not supported. 
        /// <summary>
        /// Retrieves the specified item from the Cache if it is not already locked. Otherwise it returns null.
        /// This is different from the basic Get operation where an item is returned ignoring the lock
        /// altogether.
        /// </summary>
        /// <typeparam name="T">Specifies the type of value obtained from the cache.</typeparam>
        /// <param name="key">Unique identifier for the cache item to be retrieved.</param>
        /// <param name="acquireLock">A flag to determine whether to acquire a lock or not.</param>
        /// <param name="lockTimeout">The TimeSpan after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <seealso cref="LockHandle"/> to hold the lock information.</param>
        /// <returns>The retrieved cache item, or a null reference if the key is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
        /// <example>Example demonstrates how to retrieve the cached value and acquire a lock at the same time for minutes.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        /// 
        /// cache.Add(key, product);
        /// 
        /// LockHandle lockHandle = new LockHandle();
        ///  
        /// object cachedItem = cache.Get&lt;Product&gt;(key, true, new TimeSpan(0, 2, 0), ref lockHandle);
        /// </code>
        /// </example>   
        T Get<T>(string key, bool acquireLock, TimeSpan lockTimeout, ref LockHandle lockHandle);


   

        /// <summary>
        /// Retrieves the objects from cache for the given keys as key-value pairs.
        /// </summary>
        /// <typeparam name="T">Specifies the type of value obtained from the cache.</typeparam>
        /// <param name="keys">The keys against which items are to be fetched from cache.</param>
        /// <returns>The retrieved cache items as key-value pairs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> cannot be serialized.</exception>
        /// <example>Example demonstrates how to retrieve the value cached against multiple keys with Read through Options.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        ///     "Product0",
        ///     "Product1",
        ///     "Product2"
        /// };
        /// 
        /// 
        /// IDictionary&lt;string, Product&gt; items = cache.GetBulk&lt;Product&gt;(keys);
        /// </code>
        /// </example>
        IDictionary<string, T> GetBulk<T>(IEnumerable<string> keys);

        /// <summary>
        /// Retrieves the specified CacheItem from the Cache object. This overload also allows specifying the read-through option. If read-through is set and the object does not exist in the cache,
        /// the object will be fetched from the data source and added to the cache.
        /// </summary>
        /// <param name="key">Unique identifier for the cache item to be retrieved.</param>  
        /// <returns>The specified CacheItem. If the key does not exist, it returns a null reference.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference.</exception>
        /// <example>Example demonstrates how to retrieve the cache item 
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// string key = "Product0";
        ///	CacheItem cacheItem = cache.GetCacheItem(key);
        /// </code>
        /// </example>
        CacheItem GetCacheItem(string key);

        /// <summary>
        /// Get the cache item stored in cache. Loack handle can be given with this and a flag can be set if you want to acquire lock.
        /// </summary>
        /// <param name="key">Key used to reference the desired object.</param>
        /// <param name="acquireLock">A flag to determine whether to acquire a lock or not.</param>
        /// <param name="lockTimeout">The TimeSpan after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> to hold the lock information.</param>
        /// <returns>The retrieved cache item. If key is not found, a null reference.</returns>
        /// /// <example>Example demonstrates how to retrieve cache item with lock handle, timeout and flag
        /// box server control.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// string key = "Product0";
        /// LockHandle lockHandle = new LockHandle();
        ///	CacheItem item = cache.GetCacheItem(key, true, TimeSpan.FromSeconds(30), ref lockHandle);
        /// </code>
        /// </example>
        CacheItem GetCacheItem(string key, bool acquireLock, TimeSpan lockTimeout, ref LockHandle lockHandle);

       

        /// <summary>
        /// Retrieves the specified CacheItems from the Cache object.
        /// </summary>
        /// <param name="keys">IEnumerable list of unique identifier for the cache items to be retrieved.</param>
        /// <returns>The retrieved cache items as key-value pairs.</returns>
        /// /// <example>Example demonstrates how to retrieve the cache items with read thru option.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        ///     "Product0",
        ///     "Product1",
        ///     "Product2"
        /// };
        /// 
        ///	IDictionary&lt;string, CacheItem&gt; items = cache.GetCacheItemBulk(keys);
        /// </code>
        /// </example>
        IDictionary<string, CacheItem> GetCacheItemBulk(IEnumerable<string> keys);


        #endregion

        #region Remove Operations 

        // add an out parameter for object be removed same for other overloads.
        /// <summary>
        /// Removes the specified item from the <see cref="ICache"/>. 
        /// </summary>
        /// <param name="key">Unique key of the item to be removed.</param>
        /// <param name="lockHandle">If the item is locked, it can be removed only if the correct lockHandle is specified. lockHandle should be the same which was used initially to lock the item, otherwise you will get the 'OperationFailedException'.</param>
        /// <example>Example demonstrates how to remove a locked item in the cache with write through options.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        /// 
        /// 
        /// LockHandle lockHandle = new LockHandle();
        /// 
        /// object item = cache.Get&lt;Product&gt;(key, true, TimeSpan.Zero, ref lockHandle);
        /// 
        /// if (item != null)
        /// {
        ///     try
        ///     {
        ///        
        ///         cache.Remove(key, lockHandle);
        ///     }
        ///     catch (OperationFailedException ex)
        ///     {
        ///     ...
        ///     }
        /// }
        /// </code>
        /// </example>
        void Remove(string key, LockHandle lockHandle = null);

        /// <summary>
        /// Removes the specified item from the <see cref="ICache"/> and returns it to the application as an out parameter. You can also specify the write option such that the item may be removed from both cache and data source.
        /// </summary>
        /// <typeparam name="T">Specifies the type of value obtained from the cache.</typeparam>
        /// <param name="key">Unique key of the item to be removed.</param>
        /// <param name="removedItem">out Parameter through which the removed item from cache is returned</param>
        /// <param name="lockHandle">If the item is locked, it can be removed only if the correct lockHandle is specified. lockHandle should be the same which was used initially to lock the item, otherwise you will get the 'OperationFailedException'.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>Example demonstrates how you can remove an item from cache
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// Product product = new Product();
        /// product.Id = 1;
        /// product.Name = "Chai";
        /// 
        /// string key = "Product0";
        /// 
        /// 
        /// LockHandle lockHandle = new LockHandle();
        /// object item = cache.Get&lt;Product&gt;(key, true, TimeSpan.Zero, ref lockHandle);
        /// 
        /// 
        /// try
        /// {
        ///     Product removedProduct = null;
        ///     if (cache.Remove&lt;Product&gt;(key, out removedProduct, lockHandle))
        ///     {
        ///         //Removed successfully
        ///     }
        ///     else
        ///     {
        ///         //Error Occured
        ///     }
        /// }
        /// catch(Exception ex)
        /// {
        /// ...
        /// }
        /// </code>
        /// </example>
        bool Remove<T>(string key, out T removedItem, LockHandle lockHandle = null);

        /// <summary>
        /// Removes the specified items from the <see cref="ICache"/>.
        /// </summary>
        /// <param name="keys">List of unique keys to reference the items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <example>The following example demonstrates how you can remove an item from your application's 
        /// <see cref="ICache"/> object.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        ///     "Product0",
        ///     "Product1",
        ///     "Product2"
        /// };
        /// 
        /// 
        /// cache.RemoveBulk(keys);
        /// </code>
        /// </example>
        void RemoveBulk(IEnumerable<string> keys );

        /// <summary>
        /// Removes the specified items from the <see cref="Cache"/> and returns them to the application in the form of a dictionary as an out Parameter.
        /// </summary>
        /// <typeparam name="T">Specifies the type of value obtained from the cache.</typeparam>
        /// <param name="keys">List of unique keys to reference the items.</param>
        /// <param name="removedItems">out Parameter through which the removed items from cache are returned</param>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <remarks>
        /// <para>RemovedItems dictionary contains the key and value of the items that were successfully removed from the cache.</para>
        /// </remarks>
        /// <example>The following example demonstrates how you can remove multiple of items from your application's 
        /// <see cref="ICache"/> object.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        ///     "Product0",
        ///     "Product1",
        ///     "Product2"
        /// };
        /// 
        /// 
        /// IDictionary&lt;string, Product&gt; products = null;
        /// cache.RemoveBulk&lt;Product&gt;(keys,out products);
        /// </code>
        /// </example>
        void RemoveBulk<T>(IEnumerable<string> keys, out IDictionary<string, T> removedItems);


        #endregion

        #region Conatins Operations

        /// <summary>
        /// Determines whether the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ICache"/>.</param>
        /// <returns><b>true</b> if the <see cref="ICache"/> contains an element 
        /// with the specified key; otherwise, <b>false</b>.</returns>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="ICache"/>
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        ///	if(cache.Contains("Product0"))
        ///	{
        ///		Response.Write("Item found!");
        ///	}
        /// </code>        
        /// </example>
        bool Contains(string key);

        /// <summary>
        /// Determines whether the cache contains specifiec keys.
        /// </summary>
        /// <param name="keys">IEnumerable collection of keys.</param>
        /// <returns>
        /// Dictionary of Keys with Flag to dertermine presence of each key in cache.
        /// <b>true</b> if the <see cref="ICache"/> contains an element 
        /// with the specified key; otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        /// <para><b>Note:</b> In a partitioned cluster this operation is an expensive one as it might 
        /// result in network calls. It is therefore advised to use this property only when required.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> contains a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> is not serializable.</exception>
        /// <example>The following example demonstrates how to check for containment of an item in the <see cref="ICache"/>.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        /// List&lt;string&gt; keys = new List&lt;string&gt;()
        /// {
        ///     "Product0",
        ///     "Product1"
        /// };
        /// 
        /// IDictionary&lt;string, bool&gt; result = cache.ContainsBulk(list);
        /// </code>        
        /// </example>
        IDictionary<string, bool> ContainsBulk(IEnumerable<string> keys);

        #endregion

        #region Clear Operations

        /// <summary>
        /// Removes all elements from the <see cref="ICache"/>.
        /// </summary>
        /// <remarks>In most of the cases this method's implementation is close to O(1).
        /// </remarks>
        /// <example>Example demonstrates how to check for containment of an item in the <see cref="ICache"/>.
        /// <code>
        /// ICache cache = CacheManager.GetCache("myCache");
        /// 
        ///	cache.Clear();
        /// </code>   
        /// </example>
        void Clear();


        #endregion

        #region Lock Operations 

        /// <summary>
        /// Unlocks a locked cached item if the correct LockHandle is specified.
        /// If LockHandle is null Forcefully unlocks a locked cached item.
        /// </summary>
        /// <param name="key">Key of the cached item to be unlocked.</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> that is generated when the lock is acquired.</param>
        /// <example>
        /// Following example demonstrates how to unlock a cached item.
        /// <code>
        /// ...
        /// string key = "Product0";
        /// ...
        /// theCache.Unlock(key, lockHandle);
        /// ...
        /// </code>
        /// </example>
        void Unlock(string key, LockHandle lockHandle = null);

        /// <summary>
        /// Acquires a lock on an item in the cache.
        /// </summary>
        /// <param name="key">key of cached item to be locked.</param>
        /// <param name="lockTimeout">An instance of <see cref="TimeSpan"/> after which the lock is automatically released.</param>
        /// <param name="lockHandle">An instance of <see cref="LockHandle"/> that will be filled in with the lock information if lock is acquired successfully.</param>
        /// <returns>Whether or not lock was acquired successfully.</returns>
        /// <example>
        /// Example demonstrates how to lock a cached item.
        /// <code>
        /// ...
        /// string key = "Product0";
        /// LockHandle lockHandle = new LockHandle();
        /// bool locked = theCache.lock(key, new TimeSpan(0,0,10), out lockHandle);
        /// ...
        /// </code>
        /// </example>
        bool Lock(string key, TimeSpan lockTimeout, out LockHandle lockHandle);

        #endregion

    

        #region Enumerators
        /// <summary>
        /// Retrieves a dictionary enumerator used to iterate
        /// through the key settings and their values as JSON objects
        /// contained in the cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// To use GetJsonEnumerator method, cache serilization must be set to JSON instead of Binary.
        /// </para>
        /// If items are added or removed from the cache while enumerating through the items
        /// the behavior is not predictable. It is therefore advised not to update the cache keys
        /// while enumerating.
        /// <para><b>Note:</b> Just like <see cref="Cache.Count"/> in a cluster especially partitioned 
        /// this operation is an expensive one and may require network calls. It is therefore advised to use 
        /// this method only when required.
        /// </para>
        /// </remarks>
        /// <returns>An enumerator to iterate through the <see cref="Cache"/> as JSON objects.</returns>
        //IEnumerator GetJsonEnumerator();

        #endregion
    }
}