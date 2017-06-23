// Copyright (c) 2017 Alachisoft
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
using System.Linq;
using System.Configuration;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Factories;
using Enyim.Caching.Memcached.Results.Extensions;
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions;
using System.Collections;
namespace Enyim.Caching
{
    /// <summary>
    /// Memcached client.
    /// </summary>
	public partial class MemcachedClient : IMemcachedClient, IMemcachedResultsClient
    {
        #region  [ Private Global Variables  ]
        internal static readonly MemcachedClientSection DefaultSettings = ConfigurationManager.GetSection("enyim.com/memcached") as MemcachedClientSection;
        private IMemcachedProvider memcachedProvider;
        /// <summary>
        /// Represents a value which indicates that an item should never expire.
        /// </summary>
		public static readonly TimeSpan Infinite = TimeSpan.Zero;
        public event Action<IMemcachedNode> NodeFailed;
        private ITranscoder transcoder;
        private MemcachedProtocol protocol=MemcachedProtocol.Binary;
        private IMemcachedKeyTransformer keyTransformer;
        #endregion

        #region  [ Public Properties         ]

        public IStoreOperationResultFactory StoreOperationResultFactory { get; set; }
		public IGetOperationResultFactory GetOperationResultFactory { get; set; }
		public IMutateOperationResultFactory MutateOperationResultFactory { get; set; }
		public IConcatOperationResultFactory ConcatOperationResultFactory { get; set; }
		public IRemoveOperationResultFactory RemoveOperationResultFactory { get; set; }

        #endregion

        #region  [ Constructors              ]
        /// <summary>
        /// Initializes a new MemcachedClient instance using the default configuration section (enyim/memcached).
        /// </summary>
        public MemcachedClient() : this(DefaultSettings)
        {
        }
        /// <summary>
        /// Initializes a new MemcachedClient instance using the specified configuration section. 
        /// This overload allows to create multiple MemcachedClients with different pool configurations.
        /// </summary>
        /// <param name="sectionName">The name of the configuration section to be used for configuring the behavior of the client.</param>
		public MemcachedClient(string sectionName) :this(GetSection(sectionName))
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MemcachedClient"/> using the specified configuration instance.
        /// </summary>
        /// <param name="configuration">The client configuration.</param>
        public MemcachedClient(IMemcachedClientConfiguration configuration)
        {
            InitCache();
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            this.transcoder = configuration.CreateTranscoder() ?? new DefaultTranscoder();
            this.protocol = configuration.CreateProtocol();
            this.keyTransformer = configuration.CreateKeyTransformer() ?? new DefaultKeyTransformer();

            StoreOperationResultFactory = new DefaultStoreOperationResultFactory();
            GetOperationResultFactory = new DefaultGetOperationResultFactory();
            MutateOperationResultFactory = new DefaultMutateOperationResultFactory();
            ConcatOperationResultFactory = new DefaultConcatOperationResultFactory();
            RemoveOperationResultFactory = new DefaultRemoveOperationResultFactory();

        }

        public MemcachedClient(IServerPool pool, IMemcachedKeyTransformer keyTransformer, ITranscoder transcoder)
            : this(pool, keyTransformer, transcoder, null)
        { }
        
		public MemcachedClient(IServerPool pool, IMemcachedKeyTransformer keyTransformer, ITranscoder transcoder, IPerformanceMonitor performanceMonitor)
		{
            InitCache();
            if (pool == null) throw new ArgumentNullException("pool");
            if (keyTransformer == null) throw new ArgumentNullException("keyTransformer");
            if (transcoder == null) throw new ArgumentNullException("transcoder");
            this.keyTransformer = keyTransformer;
            this.transcoder = transcoder;
		}

        #endregion

        #region  [ Public APIs               ]

        #region [ Get            ]
        /// <summary>
        /// Retrieves the specified item from the cache.
        /// </summary>
        /// <param name="key">The identifier for the item to retrieve.</param>
        /// <returns>The retrieved item, or <value>null</value> if the key was not found.</returns>
        public object Get(string key)
        {
            object tmp;
            return this.TryGet(key, out tmp) ? tmp : null;
        }

        public T Get<T>(string key)
        {
            object tmp;
            return TryGet(key, out tmp) ? (T)tmp : default(T);
        }
        /// <summary>
        /// Tries to get an item from the cache.
        /// </summary>
        /// <param name="key">The identifier for the item to retrieve.</param>
        /// <param name="value">The retrieved item or null if not found.</param>
        /// <returns>The <value>true</value> if the item was successfully retrieved.</returns>
        public bool TryGet(string key, out object value)
        {
            ulong cas = 0;
            return this.PerformTryGet(key, out cas, out value).Success;
        }

        public CasResult<object> GetWithCas(string key)
        {
            return this.GetWithCas<object>(key);
        }

        public CasResult<T> GetWithCas<T>(string key)
        {
            CasResult<object> tmp;
            return this.TryGetWithCas(key, out tmp)
                    ? new CasResult<T> { Cas = tmp.Cas, Result = (T)tmp.Result }
                    : new CasResult<T> { Cas = tmp.Cas, Result = default(T) };
        }

        public bool TryGetWithCas(string key, out CasResult<object> value)
        {
            object tmp;
            ulong cas;
            var retval = this.PerformTryGet(key, out cas, out tmp);
            value = new CasResult<object> { Cas = cas, Result = tmp };
            return retval.Success;
        }
        /// <summary>
        /// Retrieves multiple items from the cache.
        /// </summary>
        /// <param name="keys">The list of identifiers for the items to retrieve.</param>
        /// <returns>a Dictionary holding all items indexed by their key.</returns>
        public IDictionary<string, object> Get(IEnumerable<string> keys)
        {
            var hashed = new Dictionary<string, string>();
            foreach (var key in keys) hashed[this.keyTransformer.Transform(key)] = key;

            String[] keysArray = hashed.Keys.ToArray<string>();
            var getResult = new Dictionary<string, object>(hashed.Count);

            try
            {
            List<GetOpResult> result = memcachedProvider.Get(keysArray);

            foreach (GetOpResult resultEntry in result)
            {
                CacheItem item = new CacheItem(resultEntry.Flag, new ArraySegment<byte>((byte[])resultEntry.Value));
                object returnedObject = this.transcoder.Deserialize(item);
                getResult.Add(resultEntry.Key, returnedObject);
            }
            }
            catch (Exception e)
            { }
            return getResult;
        }

        public IDictionary<string, CasResult<object>> GetWithCas(IEnumerable<string> keys)
        {
            IDictionary<string, CasResult<object>> getResult = new Dictionary<string, CasResult<object>>();
            string[] keysArray = keys.ToArray<string>();
            List<GetOpResult> result = memcachedProvider.Get(keysArray);

            foreach (GetOpResult resultEntry in result)
            {
                getResult.Add(resultEntry.Key, new CasResult<object> { Cas = resultEntry.Version, StatusCode = 0, Result = resultEntry.Value });
            }
            return getResult;
        }

        #endregion

        #region [ Store          ]
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
        /// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
        public bool Store(StoreMode mode, string key, object value)
        {
            return this.PerformStore(mode, key, value, 0).Success;	
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
        public bool Store(StoreMode mode, string key, object value, TimeSpan validFor)
        {
            return this.PerformStore(mode, key, value, MemcachedClient.GetExpiration(validFor, null)).Success;	
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
        public bool Store(StoreMode mode, string key, object value, DateTime expiresAt)
        {
            return this.PerformStore(mode, key, value, MemcachedClient.GetExpiration(null,expiresAt)).Success;	
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location and returns its version.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
        /// <returns>A CasResult object containing the version of the item and the result of the operation (true if the item was successfully stored in the cache; false otherwise).</returns>
        public CasResult<bool> Cas(StoreMode mode, string key, object value, ulong cas)
        {
           return CasStore(mode, key,value, 0,cas);
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location and returns its version.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>A CasResult object containing the version of the item and the result of the operation (true if the item was successfully stored in the cache; false otherwise).</returns>
        public CasResult<bool> Cas(StoreMode mode, string key, object value, TimeSpan validFor, ulong cas)
        {
            return CasStore(mode, key, value, MemcachedClient.GetExpiration(validFor, null), cas);
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location and returns its version.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>A CasResult object containing the version of the item and the result of the operation (true if the item was successfully stored in the cache; false otherwise).</returns>
        public CasResult<bool> Cas(StoreMode mode, string key, object value, DateTime expiresAt, ulong cas)
        {
            return CasStore(mode, key, value, MemcachedClient.GetExpiration(null,expiresAt), cas);
        }
        /// <summary>
        /// Inserts an item into the cache with a cache key to reference its location and returns its version.
        /// </summary>
        /// <param name="mode">Defines how the item is stored in the cache.</param>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <remarks>The item does not expire unless it is removed due memory pressure. The text protocol does not support this operation, you need to Store then GetWithCas.</remarks>
        /// <returns>A CasResult object containing the version of the item and the result of the operation (true if the item was successfully stored in the cache; false otherwise).</returns>
        public CasResult<bool> Cas(StoreMode mode, string key, object value)
        {
            var opResult =this.PerformStore(mode, key, value, 0);
            return CreateCasResultObject<bool>(opResult.Cas, (int)opResult.StatusCode, opResult.Success);
        }

        #endregion

        #region [ Concat         ]
        /// <summary>
        /// Appends the data to the end of the specified item's data on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="data">The data to be appended to the item.</param>
        /// <returns>true if the data was successfully stored; false otherwise.</returns>
        public bool Append(string key, ArraySegment<byte> data)
        {
            ulong cas = 0;
            return PerformConcatenate(ConcatenationMode.Append, key, ref cas, data).Success;
        }
        /// <summary>
        /// Inserts the data before the specified item's data on the server.
        /// </summary>
        /// <returns>true if the data was successfully stored; false otherwise.</returns>
        public bool Prepend(string key, ArraySegment<byte> data)
        {
            ulong cas = 0;
            return PerformConcatenate(ConcatenationMode.Prepend, key, ref cas, data).Success;
        }
        /// <summary>
        /// Appends the data to the end of the specified item's data on the server, but only if the item's version matches the CAS value provided.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <param name="data">The data to be prepended to the item.</param>
        /// <returns>true if the data was successfully stored; false otherwise.</returns>
        public CasResult<bool> Append(string key, ulong cas, ArraySegment<byte> data)
        {
            return CasConcat(ConcatenationMode.Append, key,cas, data);
        }
        /// <summary>
        /// Inserts the data before the specified item's data on the server, but only if the item's version matches the CAS value provided.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <param name="data">The data to be prepended to the item.</param>
        /// <returns>true if the data was successfully stored; false otherwise.</returns>
        public CasResult<bool> Prepend(string key, ulong cas, ArraySegment<byte> data)
        {
            return CasConcat(ConcatenationMode.Prepend, key, cas, data);
        }

        #endregion

        #region [ Remove         ]
        /// <summary>
        /// Removes the specified item from the cache.
        /// </summary>
        /// <param name="key">The identifier for the item to delete.</param>
        /// <returns>true if the item was successfully removed from the cache; false otherwise.</returns>
        public bool Remove(string key)
        {
            return PerformRemove(key).Success;
        }

        #endregion

        #region [ Flush          ]
        /// <summary>
        /// Removes all data from the cache. Note: this will invalidate all data on all servers in the pool.
        /// </summary>
        public void FlushAll()
        {
            try
            {
                memcachedProvider.Flush_All(0);
            }
            catch (Exception e)
            { }
        }
        #endregion

        #region [ Mutate         ]

        #region [ Increment                    ]
        /// <summary>
        /// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Increment(string key, ulong defaultValue, ulong delta)
        {
            return PerformMutate(MutationMode.Increment,key,defaultValue, delta,0).Value;
        }
        /// <summary>
        /// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
        {
            return PerformMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor, null)).Value;
        }
        /// <summary>
        /// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
        {
            return PerformMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(null, expiresAt)).Value;
        }
        /// <summary>
        /// Increments the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, ulong cas)
        {
            return CasMutate(MutationMode.Increment, key, defaultValue, delta,0, cas);
        }
        /// <summary>
        /// Increments the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas)
        {
            return CasMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor,null), cas);
       
        }
        /// <summary>
        /// Increments the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to increase the item.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas)
        {
            return CasMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(null, expiresAt), cas);
       
        }

        #endregion

        #region [ Decrement                    ]
        /// <summary>
        /// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Decrement(string key, ulong defaultValue, ulong delta)
        {
            return PerformMutate(MutationMode.Decrement, key, defaultValue, delta, 0).Value;
        }
        /// <summary>
        /// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor)
        {
            return PerformMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor, null)).Value;
        }
        /// <summary>
        /// Decrements the value of the specified key by the given amount. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public ulong Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt)
        {
            return PerformMutate(MutationMode.Increment, key, defaultValue, delta, MemcachedClient.GetExpiration(null, expiresAt)).Value;
        }
        /// <summary>
        /// Decrements the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, ulong cas)
        {
            return CasMutate(MutationMode.Decrement, key, defaultValue, delta, 0, cas);
       
        }
        /// <summary>
        /// Decrements the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <param name="validFor">The interval after the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas)
        {
            return CasMutate(MutationMode.Decrement, key, defaultValue, delta, MemcachedClient.GetExpiration(validFor,null), cas);
       
        }
        /// <summary>
        /// Decrements the value of the specified key by the given amount, but only if the item's version matches the CAS value provided. The operation is atomic and happens on the server.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="defaultValue">The value which will be stored by the server if the specified item was not found.</param>
        /// <param name="delta">The amount by which the client wants to decrease the item.</param>
        /// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
        /// <param name="cas">The cas value which must match the item's version.</param>
        /// <returns>The new value of the item or defaultValue if the key was not found.</returns>
        /// <remarks>If the client uses the Text protocol, the item must be inserted into the cache before it can be changed. It must be inserted as a <see cref="T:System.String"/>. Moreover the Text protocol only works with <see cref="System.UInt32"/> values, so return value -1 always indicates that the item was not found.</remarks>
        public CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas)
        {
            return  CasMutate(MutationMode.Decrement, key, defaultValue, delta, MemcachedClient.GetExpiration(null,expiresAt), cas);
        }

        #endregion

        #endregion

        #region [ Stats          ]
        /// <summary>
        /// Returns statistics about the servers.
        /// </summary>
        /// <returns></returns>
        public ServerStats Stats()
        {
            return this.Stats(null);
        }

        public ServerStats Stats(string type)
        {
            var results = new Dictionary<IPEndPoint, Dictionary<string, string>>();
            if (memcachedProvider == null)
            {
                return new ServerStats(results);
            }
            var addresses = Dns.GetHostAddresses("127.0.0.1");
            IPEndPoint ip = new IPEndPoint(addresses[0], 11211);
            try
            {
                OperationResult opResult = memcachedProvider.GetStatistics(type);
                Hashtable ht = (Hashtable)opResult.Value;

                Dictionary<string, string> stats = new Dictionary<string,string>();
                foreach (DictionaryEntry entry in ht)
                    stats.Add(entry.Key.ToString(), entry.Value.ToString());
                results.Add(ip, stats);
            }
            catch (Exception e)
            { }
            return new ServerStats(results);
        }
        #endregion

        #endregion

        #region [ Utility Methods           ]

        private void InitCache()
        {
            string cacheName="";
            try
            {
                cacheName = ConfigurationManager.AppSettings["NCache.CacheName"];
                if (String.IsNullOrEmpty(cacheName))
                {
                    Exception ex = new Exception("Unable to read NCache Name from configuration");
                    throw ex;
                }

            }
            catch(Exception e)
            {
                throw e;
            }
            try
            {
                memcachedProvider = CacheFactory.CreateCacheProvider(cacheName);
            }
            catch (Exception e)
            { 
                //do nothing
            }
        }

        protected virtual IStoreOperationResult PerformStore(StoreMode mode, string key, object value, uint expires)
        {
            var hashedKey = this.keyTransformer.Transform(key);
            var result = StoreOperationResultFactory.Create();
            if (memcachedProvider == null)
            {
                result.Fail("Unable to locate node");
            }
            else
            {

                if (protocol == MemcachedProtocol.Text && expires > int.MaxValue)
                    throw new MemcachedClientException("bad command line format");

                CacheItem item;

                try { item = this.transcoder.Serialize(value); }
                catch (Exception e)
                {
                    result.Fail("PerformStore failed", e);
                    return result;
                }
                switch (mode)
                {
                    case StoreMode.Add:
                        result = PerformAdd(hashedKey, item.Flags, item.Data.Array, expires);
                        break;
                    case StoreMode.Replace:
                        result = PerformReplace(hashedKey, item.Flags, item.Data.Array, expires);
                        break;
                    case StoreMode.Set:
                        result = PerformSet(hashedKey, item.Flags, item.Data.Array, expires);
                        break;
                }
                
            }
            return result;
        }

        private IStoreOperationResult PerformStoreWithCas(StoreMode mode, string key, object value, uint expires, ulong cas)
        {
            var result = StoreOperationResultFactory.Create();
            
            if (memcachedProvider == null)
            {
                result.StatusCode = 0xfffffff;
                result.Fail("Unable to locate node");
            }
            else
            {
                if (cas == 0)
                    return PerformStore(mode, key, value, expires);

                if (protocol == MemcachedProtocol.Text && expires > int.MaxValue)
                    throw new MemcachedClientException("bad command line format");
                var hashedKey = this.keyTransformer.Transform(key);
                var innerResult = StoreOperationResultFactory.Create();
                CacheItem item = new CacheItem();
                item = this.transcoder.Serialize(value);
                try
                {
                    OperationResult opResult = memcachedProvider.CheckAndSet(hashedKey, item.Flags, expires, cas, item.Data.Array);

                    switch (opResult.ReturnResult)
                    {
                        case Result.SUCCESS:
                            if (protocol == MemcachedProtocol.Binary)
                                result.Cas = Convert.ToUInt64(opResult.Value);
                            else
                                result.Cas = 0;
                            result.StatusCode = 0;
                            result.Pass();
                            break;
                        case Result.ITEM_MODIFIED:
                            result.Cas = 0;
                            if (protocol == MemcachedProtocol.Binary)
                            {
                                result.StatusCode = 2;
                                innerResult.Message = result.Message = "Data exists for key.";
                            }
                            else
                                result.StatusCode = 0;
                            result.InnerResult = innerResult;
                            break;
                        case Result.ITEM_NOT_FOUND:
                            result.Cas = 0;
                            if (protocol == MemcachedProtocol.Binary)
                            {
                                result.StatusCode = 1;
                                innerResult.Message = result.Message = "Not found";
                            }
                            else
                                result.StatusCode = 0;
                            result.InnerResult = innerResult;
                            break;
                    }
                }
                catch (InvalidArgumentsException e)
                {
                    throw new MemcachedClientException("bad command line format");
                }
                catch (Exception e)
                {
                    result.Fail("Unable to locate node");
                }
            }

                return result;
        }

        private IStoreOperationResult PerformAdd(string key,uint flags,  object value, long expirationTimeInSeconds)
        {
           
            var result = StoreOperationResultFactory.Create();
            var innerResult = StoreOperationResultFactory.Create();
            try
            {
                OperationResult opResult = memcachedProvider.Add(key,flags, expirationTimeInSeconds, value);
                
                switch (opResult.ReturnResult)

                {
                    case Result.SUCCESS:
                        if (protocol == MemcachedProtocol.Binary)
                            result.Cas = Convert.ToUInt64(opResult.Value);
                        else
                            result.Cas = 0;
                        result.StatusCode=0;
                        result.Pass();
                        break;
                    case Result.ITEM_EXISTS:
                        result.Cas = 0;
                        if (protocol == MemcachedProtocol.Binary)
                        {
                            result.StatusCode = 2;
                            innerResult.Message=result.Message = "Data exists for key";
                        }
                        else
                            result.StatusCode = 0;
                        result.InnerResult = innerResult;
                        break;
                }
                
            }
            catch (InvalidArgumentsException e)
            {
                throw new MemcachedClientException("bad command line format");
            }
            catch(Exception e)
            {
                result.Fail("Unable to locate node");
            }
            
            return result;
        }

        private IStoreOperationResult PerformReplace(string key,uint flags, object value, long expirationTimeInSeconds)
        {
            var result = StoreOperationResultFactory.Create();
            var innerResult = StoreOperationResultFactory.Create();
            try
            {
                OperationResult opResult = memcachedProvider.Replace(key, flags, expirationTimeInSeconds, value);

                switch (opResult.ReturnResult)
                {
                    case Result.SUCCESS:
                        if (protocol == MemcachedProtocol.Binary)
                            result.Cas = Convert.ToUInt64(opResult.Value);
                        else
                            result.Cas = 0;
                        result.StatusCode=0;
                        result.Pass();
                        break;
                    case Result.ITEM_NOT_FOUND:
                        result.Cas = 0;
                        if (protocol == MemcachedProtocol.Binary)
                        {
                            result.StatusCode = 1;
                            innerResult.Message=result.Message = "Not found";

                        }
                        else
                            result.StatusCode = 0;
                        result.InnerResult = innerResult;
                        break;
                }
            }
            catch (InvalidArgumentsException e)
            {
                throw new MemcachedClientException("bad command line format");
            }
            catch (Exception e)
            {
                result.Fail("Unable to locate node");
            }
            return result;
        }

        private IStoreOperationResult PerformSet(string key,uint flags, object value, long expirationTimeInSeconds)
        {
            var result = StoreOperationResultFactory.Create();
            try
            {
                OperationResult opResult = memcachedProvider.Set(key, flags, expirationTimeInSeconds, value);

                switch (opResult.ReturnResult)
                {
                    case Result.SUCCESS:
                        if (protocol == MemcachedProtocol.Binary)
                            result.Cas = Convert.ToUInt64(opResult.Value);
                        else
                            result.Cas = 0;
                        result.StatusCode=0;
                        result.Pass();
                        break;
                }
            }
            catch (InvalidArgumentsException e)
            {
                throw new MemcachedClientException("bad command line format");
            }
            catch (Exception e)
            {
                result.Fail("Unable to locate node");
            }
            return result;
        }

        protected IRemoveOperationResult PerformRemove(string key)
        {
            var hashedKey = this.keyTransformer.Transform(key);
            var result = RemoveOperationResultFactory.Create();
            if (memcachedProvider == null)
            {
                result.Fail("Unable to locate node");
            }
            else
            {
                try
                {
                    OperationResult opResult = memcachedProvider.Delete(hashedKey,0);
                    if (opResult.ReturnResult == Result.SUCCESS)
                        result.Pass();
                    else
                    {
                        var innerResult = RemoveOperationResultFactory.Create();
                        if (protocol == MemcachedProtocol.Binary)
                            innerResult.Message = "Not found";

                        innerResult.InnerResult = innerResult;
                        if (protocol == MemcachedProtocol.Binary)
                            innerResult.StatusCode = 1;

                        result.InnerResult = innerResult;
                        result.Fail("Failed to remove item, see InnerResult or StatusCode for details");

                    }
                }
                catch (InvalidArgumentsException e)
                {
                    throw new MemcachedClientException("bad command line format");
                }
                catch (Exception e)
                {
                    result.Fail("Unable to locate node");
                }
            }
            return result;
        }

        protected virtual IGetOperationResult PerformTryGet(string key, out ulong cas, out object value)
        {
            var hashedKey = this.keyTransformer.Transform(key);
            var result = GetOperationResultFactory.Create();
            cas = 0;
            value = null;
            if (memcachedProvider == null)
            {
                result.Fail("Unable to locate node");
            }
            else
            {
                var innerResult = GetOperationResultFactory.Create();
                
                try
                {
                    List<GetOpResult> opResult = memcachedProvider.Get(new[] { hashedKey });

                    if (opResult.Count == 0)
                    {
                        result.Cas = 0;
                        if (protocol == MemcachedProtocol.Binary)
                        {
                            innerResult.Message = result.Message = "Not found";
                            result.StatusCode = 1;
                        }
                        else
                            innerResult.Message = result.Message = "Failed to read response";

                        result.InnerResult = innerResult;
                    }
                    else
                    {
                        byte[] objectByte = (byte[])opResult[0].Value;
                        ArraySegment<byte> arraySegment = new ArraySegment<byte>(objectByte);
                        CacheItem item = new CacheItem(opResult[0].Flag, arraySegment);
                        value = this.transcoder.Deserialize(item);
                        cas = result.Cas = opResult[0].Version;
                        result.Pass();
                    }
                }
                catch (InvalidArgumentsException e)
                {
                    throw new MemcachedClientException("bad command line format");
                }
                catch (Exception e)
                {
                    result.Fail("Unable to locate node");
                }
            }
            return result;
        }

        protected virtual IDictionary<string, IGetOperationResult> PerformMultiGet(IEnumerable<string> keys)
        {
            var hashed = new Dictionary<string, string>();
            foreach (var key in keys) hashed[this.keyTransformer.Transform(key)] = key;

            String[] keysArray = hashed.Keys.ToArray<string>();
            var retval = new Dictionary<string, IGetOperationResult>(hashed.Count);
            if (memcachedProvider != null)
            {
                try
                {
                    List<GetOpResult> opResult = memcachedProvider.Get(keysArray);
                    foreach (GetOpResult resultEntry in opResult)
                    {
                        var result = GetOperationResultFactory.Create();
                        CacheItem item = new CacheItem(opResult[0].Flag, new ArraySegment<byte>((byte[])resultEntry.Value));
                        result.Value = this.transcoder.Deserialize(item);
                        result.Cas = resultEntry.Version;
                        result.Pass();
                        retval.Add(resultEntry.Key, result);
                    }
                }
                catch (Exception e)
                { }
            }

            return retval;
        }

        private IMutateOperationResult PerformMutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
        {
            ulong tmp = 0;

            return PerformMutate(mode, key, defaultValue, delta, expires, ref tmp);
        }
        
        protected virtual IMutateOperationResult PerformMutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires, ref ulong cas)
        {
            var hashedKey = this.keyTransformer.Transform(key);
            var result = MutateOperationResultFactory.Create();
            if (memcachedProvider == null)
            {
                result.Fail("Unable to locate node");
            }
            else
            {
                if (protocol == MemcachedProtocol.Text)
                    if (cas > 0) throw new NotSupportedException("Text protocol does not support " + mode + " with cas.");

                var innerResult = MutateOperationResultFactory.Create();
                MutateOpResult opResult = new MutateOpResult();
                try
                {
                    if (mode == MutationMode.Increment)
                        if (protocol == MemcachedProtocol.Binary)
                            opResult = memcachedProvider.Increment(hashedKey, delta, defaultValue, expires, cas);
                        else
                            opResult = memcachedProvider.Increment(hashedKey, delta, null, 0, 0);
                    else
                        if (protocol == MemcachedProtocol.Binary)
                            opResult = memcachedProvider.Decrement(hashedKey, delta, defaultValue, expires, cas);
                        else
                            opResult = memcachedProvider.Decrement(hashedKey, delta, null, 0, 0);

                }
                catch (InvalidArgumentsException e)
                {
                    throw new MemcachedClientException("bad command line format");
                }
                catch (Exception e)
                {
                    result.Fail("Mutate operation failed, see InnerResult or StatusCode for more details");

                }
                switch (opResult.ReturnResult)
                {
                    case Result.SUCCESS:
                        result.Value = opResult.MutateResult;
                        result.StatusCode = 0;
                        if (protocol == MemcachedProtocol.Binary)
                            result.Cas = Convert.ToUInt64(opResult.Value);
                        else
                            result.Cas = 0;
                        result.Pass();
                        break;
                    case Result.ITEM_NOT_FOUND:
                        if (protocol == MemcachedProtocol.Text)
                        {
                            innerResult.Message = "Failed to read response.  Item not found";
                            innerResult.InnerResult = innerResult;
                        }
                        result.Value = 0;
                        result.Cas = 0;
                        result.Message = "Unable to locate node.";
                        result.Success = false;
                        result.StatusCode = 0;
                        result.InnerResult = innerResult;
                        break;
                    case Result.ITEM_TYPE_MISMATCHED:
                        if (protocol == MemcachedProtocol.Text)
                            throw new MemcachedClientException("cannot increment or decrement non-numeric value");

                        innerResult.Message = "Non-numeric server-side value for incr or decr";
                        innerResult.InnerResult = result.InnerResult = innerResult;
                        innerResult.StatusCode = result.StatusCode = 6;
                        result.Value = 0;
                        result.Cas = 0;
                        result.Message = "Unable to locate node.";
                        result.Success = false;
                      
                        break;
                    case Result.ITEM_MODIFIED:

                        innerResult.Message = "Data exists for key.";
                        innerResult.InnerResult = result.InnerResult= innerResult;
                        innerResult.StatusCode = result.StatusCode = 2;
                        result.Value = 0;
                        result.Cas = 0;
                        result.Message = "Unable to locate node.";
                        result.Success = false;
                        break;
                }
            }
            return result;

        }

        protected virtual IConcatOperationResult PerformConcatenate(ConcatenationMode mode, string key, ref ulong cas, ArraySegment<byte> arraySegment)
        {
            var hashedKey = this.keyTransformer.Transform(key);

            var result = ConcatOperationResultFactory.Create();

            if (memcachedProvider == null)
            {
                result.Fail("Unable to locate node");
            }
            else
            {
                if (protocol == MemcachedProtocol.Text)
                    if (cas > 0) throw new NotSupportedException("Text protocol does not support " + mode + " with cas.");

                byte[] data = arraySegment.Array;
                var innerResult = ConcatOperationResultFactory.Create();
                try
                {
                    OperationResult opResult = new OperationResult();

                    if (mode == ConcatenationMode.Append)
                        opResult = memcachedProvider.Append(hashedKey, data, cas);
                    else
                        opResult = memcachedProvider.Prepend(hashedKey, data, cas);

                    switch (opResult.ReturnResult)
                    {
                        case Result.SUCCESS:
                            if (protocol == MemcachedProtocol.Binary)
                                result.Cas = Convert.ToUInt64(opResult.Value);
                            else
                                result.Cas = 0;
                            result.Pass();
                            break;
                        case Result.ITEM_NOT_FOUND:
                            if (protocol == MemcachedProtocol.Binary)
                                innerResult.StatusCode = 5;
                            innerResult.Message = null;
                            innerResult.InnerResult = innerResult;

                            result.InnerResult = innerResult;
                            result.Cas = 0;
                            result.Message = "Concat operation failed, see InnerResult or Status Code for details.";
                            break;
                        case Result.ITEM_MODIFIED:
                            if (protocol == MemcachedProtocol.Binary)
                                innerResult.StatusCode = 2;
                            innerResult.Message = null;
                            innerResult.InnerResult = innerResult;

                            result.InnerResult = innerResult;
                            result.Cas = 0;
                            result.Message = "Concat operation failed, see InnerResult or Status Code for details.";
                            break;
                    }
                }
                catch (InvalidArgumentsException e)
                {
                    throw new MemcachedClientException("bad command line format");
                }
                catch (Exception e)
                {
                    result.Fail("Concat operation failed", e);
                }
            }
            return result;
        }
        
        private CasResult<ulong> CasMutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires, ulong cas)
        {
            var opResult = PerformMutate(mode, key, defaultValue, delta, expires, ref  cas);
            return CreateCasResultObject<ulong>(opResult.Cas, (int)opResult.StatusCode, opResult.Value);
        }

        private CasResult<bool> CasConcat(ConcatenationMode mode, string key, ulong cas, ArraySegment<byte> data)
        {
            var opResult = PerformConcatenate(mode, key, ref cas, data);
            return CreateCasResultObject<bool>(opResult.Cas, (int)opResult.StatusCode, opResult.Success);
        }
        
        private CasResult<bool> CasStore(StoreMode mode, string key, object value, uint expires, ulong cas)
        {
            var opResult = PerformStoreWithCas(mode, key, value, expires, cas);
            return CreateCasResultObject<bool>(opResult.Cas, (int)opResult.StatusCode, opResult.Success);
        }

        private CasResult<T> CreateCasResultObject<T>(ulong cas, int status, T result)
        {
            CasResult<T> casResultObject = new CasResult<T> { Cas = cas, StatusCode = status, Result = (T)result };
            return casResultObject;
        }
        private static IMemcachedClientConfiguration GetSection(string sectionName)
        {
            MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);
            if (section == null)
                throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");

            return section;
        }
        #region [ Expiration helper            ]

        protected const int MaxSeconds = 60 * 60 * 24 * 30;
		protected static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

		protected static uint GetExpiration(TimeSpan? validFor, DateTime? expiresAt)
		{
			if (validFor != null && expiresAt != null)
				throw new ArgumentException("You cannot specify both validFor and expiresAt.");

			// convert timespans to absolute dates
			if (validFor != null)
			{
				// infinity
				if (validFor == TimeSpan.Zero || validFor == TimeSpan.MaxValue) return 0;

				expiresAt = DateTime.Now.Add(validFor.Value);
			}

			DateTime dt = expiresAt.Value;

			if (dt < UnixEpoch) throw new ArgumentOutOfRangeException("expiresAt", "expiresAt must be >= 1970/1/1");

			// accept MaxValue as infinite
			if (dt == DateTime.MaxValue) return 0;

			uint retval = (uint)(dt.ToUniversalTime() - UnixEpoch).TotalSeconds;
            
			return retval;
		}

		#endregion

        #endregion

        #region [ IDisposable               ]

        ~MemcachedClient()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		void IDisposable.Dispose()
		{
            
			this.Dispose();
		}
        /// <summary>
        /// Releases all resources allocated by this instance
        /// </summary>
        /// <remarks>You should only call this when you are not using static instances of the client, so it can close all conections and release the sockets.</remarks>
		public void Dispose()
		{
            CacheFactory.DisposeCacheProvider();
		}

		#endregion
	}
}
