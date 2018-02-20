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

using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Queries;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
	/// <summary>
	/// Combines two cache stores together and provides abstraction of a single store. Also 
	/// implements ICacheStore. 
	/// </summary>

	internal class OverflowCache : LocalCacheBase
	{
		#region	/                 --- Cache Listeners ---           /
		/// <summary>
		/// Listener for the primary cache.
		/// </summary>
		class PrimaryCacheListener: ICacheEventsListener
		{
			/// <summary> parent composite cache object. </summary>
			private OverflowCache		_parent = null;

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="parent">parent composite cache object</param>
			public PrimaryCacheListener(OverflowCache parent)
			{
				_parent = parent;
			}

			#region	/                 --- ICacheEventsListener ---           /

			/// <summary> 
			/// Fired when an item is added to the cache. 
			/// </summary>
            void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext) { }

			/// <summary> 
			/// handler for item updated event.
			/// </summary>
            void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext) { }

			/// <summary> 
			/// Fire when the cache is cleared. 
			/// </summary>
            void ICacheEventsListener.OnCacheCleared(OperationContext operationContext, EventContext eventContext) { }

			/// <summary> 
			/// Fired when an item is removed from the cache.
			/// </summary>
            void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
			{
                if (reason == ItemRemoveReason.Underused)
                {
                    //if(nnTrace.isInfoEnabled) nTrace.info("PrimaryCacheListener.OnItemRemoved()", "trying to add to secondary cache : " + key);
                    _parent.Secondary.Add(key, (CacheEntry)val, false, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                }
                else
                {
                    ((IDisposable)val).Dispose();
                }
			}
			/// <summary> 
			/// Fired when multiple items are removed from the cache. 
			/// </summary>
            public void OnItemsRemoved(object[] key, object[] val, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContext)
			{
                if (reason == ItemRemoveReason.Underused)
                {
                    //if(nTrace.isInfoEnabled) nTrace.info("PrimaryCacheListener.OnItemsRemoved()", "trying to add to secondary cache : " + key);
                    for (int i = 0; i < key.Length; i++)
                    {
                        _parent.Secondary.Add(key[i], (CacheEntry)val[i], false, operationContext);
                    }
                }
                else 
                {  
                    for (int i = 0; i < key.Length; i++)
                    {
                        ((IDisposable)val[i]).Dispose();
                    }
                }
			}

            /// <summary>
            /// 
            /// </summary>
            /// <param name="notifId"></param>
            /// <param name="data"></param>
            void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext)
            {
            }

#if !CLIENT
            /// <summary>
            /// Fire when hasmap changes when 
            /// - new node joins
            /// - node leaves
            /// - manual/automatic load balance
            /// </summary>
            /// <param name="newHashmap">new hashmap</param>
            void ICacheEventsListener.OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap)
            {
            }
#endif
            /// <summary>
            /// 
            /// </summary>
            /// <param name="operationCode"></param>
            /// <param name="result"></param>
            /// <param name="cbEntry"></param>
            void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, CallbackEntry cbEntry)
            {
            }

            public void OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext) { }

            public void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext) { }

            public void OnActiveQueryChanged(object key, QueryChangeType changeType, System.Collections.Generic.List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
            {
            }

            public void OnPollNotify(string client, short callbackId, Runtime.Events.EventType eventType)
            {
                throw new NotImplementedException();
            }

            public void OnTaskCallback(string taskId, object value, OperationContext operationContext, EventContext eventContext)
            {
            }

            #endregion

        }

		
		/// <summary>
		/// Listener for the secondary cache.
		/// </summary>
		class SecondaryCacheListener: ICacheEventsListener
		{
			/// <summary> parent composite cache object. </summary>
			private OverflowCache		_parent = null;

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="parent">parent composite cache object</param>
			public SecondaryCacheListener(OverflowCache parent)
			{
				_parent = parent;
			}

			#region	/                 --- ICacheEventsListener ---           /

			/// <summary> 
			/// Fired when an item is added to the cache. 
			/// </summary>
            void ICacheEventsListener.OnItemAdded(object key, OperationContext operationContext, EventContext eventContext) { }
			/// <summary> 
			/// handler for item updated event.
			/// </summary>
            void ICacheEventsListener.OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext) { }
			/// <summary> 
			/// Fire when the cache is cleared. 
			/// </summary>
            void ICacheEventsListener.OnCacheCleared(OperationContext operationContext, EventContext eventContext) { }

			/// <summary>
			/// Fired when an item is removed from the cache.
			/// </summary>
            void ICacheEventsListener.OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
			{
				if((reason == ItemRemoveReason.Underused) && (_parent.Listener != null))
				{
					//if(nTrace.isInfoEnabled) nTrace.info("SecondaryCacheListener.OnItemRemoved()", "discarding from secondary cache : " + key);
                    //if (_parent.IsSelfInternal) _parent._context.PerfStatsColl.IncrementEvictPerSecStats();
					_parent.Listener.OnItemRemoved(key, val, reason,operationContext,eventContext);
				}
                ((IDisposable)val).Dispose();
			}
			/// <summary>
			/// Fired when multiple items are removed from the cache.
			/// </summary>
            public void OnItemsRemoved(object[] key, object[] val, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContext)
			{ 
				if(reason != ItemRemoveReason.Underused || (_parent.Listener == null))
					return;

				//if(nTrace.isInfoEnabled) nTrace.info("SecondaryCacheListener.OnItemsRemoved()", "discarding from secondary cache : " + key);
				for(int i=0; i<key.Length; i++)
				{
                    //if (_parent.IsSelfInternal) _parent._context.PerfStatsColl.IncrementEvictPerSecStats();
					_parent.Listener.OnItemRemoved(key[i], val[i], reason,operationContext,eventContext[i]);
                    ((IDisposable)val[i]).Dispose();
				}
			}

            /// <summary>
            /// 
            /// </summary>
            /// <param name="notifId"></param>
            /// <param name="data"></param>
            void ICacheEventsListener.OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext)
            {
            }

#if !CLIENT
            /// <summary>
            /// Fire when hasmap changes when 
            /// - new node joins
            /// - node leaves
            /// - manual/automatic load balance
            /// </summary>
            /// <param name="newHashmap">new hashmap</param>
            void ICacheEventsListener.OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap)
            {
            }
#endif
            /// <summary>
            /// 
            /// </summary>
            /// <param name="operationCode"></param>
            /// <param name="result"></param>
            /// <param name="cbEntry"></param>
            void ICacheEventsListener.OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, CallbackEntry cbEntry)
            {
            }

            /// <summary>
            /// Fired when an item which has CacheItemUpdateCallback is updated.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public void OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext) { }

            /// <summary>
            /// Fired when an item which has CacheItemRemoveCallback is removed.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="reason"></param>
            public void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext) { }

            public void OnActiveQueryChanged(object key, QueryChangeType changeType, System.Collections.Generic.List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext)
            {
            }

            public void OnPollNotify(string client, short callbackId, Runtime.Events.EventType eventType)
            {
                throw new NotImplementedException();
            }

            public void OnTaskCallback(string taskId, object value, OperationContext operationContext, EventContext eventContext)
            {
            }
          
            #endregion

        }
		#endregion

		/// <summary> the front cache store. </summary>
		[CLSCompliant(false)]
		protected LocalCacheBase		_primary = null;

		/// <summary> the backing cache store. </summary>
		[CLSCompliant(false)]
		protected LocalCacheBase		_secondary = null;

		/// <summary>
		/// Overloaded constructor. Takes the properties as a map.
		/// </summary>
		/// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
		/// <param name="properties">properties collection for this cache.</param>
		/// <param name="listener">listener for the cache</param>
		/// <param name="timeSched">scheduler to use for periodic tasks</param>
        public OverflowCache(IDictionary cacheClasses, CacheBase parentCache, IDictionary properties, ICacheEventsListener listener, CacheRuntimeContext context, ActiveQueryAnalyzer activeQueryAnalyzer)
            : base(properties, parentCache, listener, context, activeQueryAnalyzer)
		{
			_stats.ClassName = "overflow-cache";
			Initialize(cacheClasses, properties);

			CacheStatistics pstat = _primary.Statistics, sstat = _secondary.Statistics;
			if(pstat.MaxCount == 0 || sstat.MaxCount == 0)
				_stats.MaxCount = 0;
			else
				_stats.MaxCount = pstat.MaxCount + sstat.MaxCount;
		}


		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			if(_primary != null)
			{
				_primary.Dispose();
				_primary = null;
			}
			if(_secondary != null)
			{
				_secondary.Dispose();
				_secondary = null;
			}
			base.Dispose();
		}

		#endregion

		/// <summary>
		/// front cache store.
		/// </summary>
		public CacheBase Primary
		{
			get { return _primary; }
		}

        /// <summary>
        /// Get the size of data in store, in bytes.
        /// </summary>
        internal override long Size
        {
            get
            {
                if (_primary == null || _secondary == null)
                    throw new InvalidOperationException();

                long size = 0;
                size += _primary.Size;
                size += _secondary.Size;
                return size;
            }
        }
		/// <summary>
		/// backing cache store.
		/// </summary>
		public CacheBase Secondary
		{ 
			get { return _secondary; }
		}

		/// <summary>
		/// returns the number of objects contained in the cache.
		/// </summary>
		public override long Count
		{
			get 
			{ 
				if(_primary == null || _secondary == null)
					throw new InvalidOperationException();

				return _primary.Count + _secondary.Count; 
			}
		}

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Method that allows the object to initialize itself. Passes the property map down 
		/// the object hierarchy so that other objects may configure themselves as well..
		/// </summary>
		/// <param name="cacheSchemes">collection of cache schemes (config properties).</param>
		/// <param name="properties">properties collection for this cache.</param>
		protected override void Initialize(IDictionary cacheClasses, IDictionary properties)
		{
			if(properties == null)
				throw new ArgumentNullException("properties");

			try
			{
				base.Initialize(cacheClasses, properties);
				{
					IDictionary schemeProps = ConfigHelper.GetCacheScheme(cacheClasses, properties, "primary-cache");
					string cacheType = Convert.ToString(schemeProps[ "type" ]).ToLower();
					if(cacheType.CompareTo("local-cache") == 0)
					{
						// very important to note that the perf collector is not passed further down.
						_primary = CreateLocalCache(this,cacheClasses, schemeProps);
                        _primary._allowAsyncEviction = false; //do not evict item asynchronously.
					}
					else if(cacheType.CompareTo("overflow-cache") == 0)
					{
						_primary = CreateOverflowCache(cacheClasses, schemeProps);
					}
					else
					{
                        throw new ConfigurationException("invalid or non-local cache class specified in composite cache");
					}
				}
				{
					IDictionary schemeProps = ConfigHelper.GetCacheScheme(cacheClasses, properties, "secondary-cache");
					string cacheType = Convert.ToString(schemeProps[ "type" ]).ToLower();
					if(cacheType.CompareTo("local-cache") == 0)
					{                       
						_secondary = CreateLocalCache(_parentCache, cacheClasses, schemeProps);
                        _secondary._allowAsyncEviction = true;
					}
					else if(cacheType.CompareTo("overflow-cache") == 0)
					{						
						_secondary = CreateOverflowCache(cacheClasses, schemeProps);
					}
					else
					{
                        throw new ConfigurationException("invalid or non-local cache class specified in composite cache");
					}
				}
				_primary.Listener = new PrimaryCacheListener(this);
				_secondary.Listener = new SecondaryCacheListener(this);
			}
			catch(ConfigurationException e)
			{
				if (_context != null)
                {
                    _context.NCacheLog.Error("OverflowCache.Initialize()",  e.Message); 
                }
				Dispose();
				throw;
			}
			catch(Exception e)
			{
				if (_context != null)
                {
                    _context.NCacheLog.Error("OverflowCache.Initialize()",  e.Message); 
                }
				Dispose();
				throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
			}						
		}


		protected virtual LocalCacheBase CreateLocalCache(CacheBase parentCache,IDictionary cacheClasses, IDictionary schemeProps)
		{
			return new LocalCache( cacheClasses,parentCache, schemeProps, null, _context, _activeQueryAnalyzer);
		}


		protected virtual LocalCacheBase CreateOverflowCache(IDictionary cacheClasses, IDictionary schemeProps)
		{
            return new OverflowCache(cacheClasses, this, schemeProps, null, _context, _activeQueryAnalyzer);
		}

		#endregion

		#region	/                 --- LocalCacheBase ---           /

		/// <summary>
		/// Removes all entries from the store.
		/// </summary>
		internal override void ClearInternal()
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			_secondary.ClearInternal();
			_primary.ClearInternal();
		}

		/// <summary>
		/// Determines whether the cache contains a specific key.
		/// </summary>
		/// <param name="key">The key to locate in the cache.</param>
		/// <returns>true if the cache contains an element 
		/// with the specified key; otherwise, false.</returns>
		internal override bool ContainsInternal(object key)
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			return _primary.ContainsInternal(key) || _secondary.ContainsInternal(key);
		}

		/// <summary>
		/// Provides implementation of Get method of the ICacheStore interface. Get an object from the store, specified by the passed in key. 
		/// </summary>
		/// <param name="key">key</param>
		/// <returns>object</returns>
        internal override CacheEntry GetInternal(object key, bool isUserOperation, OperationContext operationContext)
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			// check the front cache for object
			CacheEntry e = _primary.GetInternal(key, isUserOperation,operationContext);
			if(e == null)
			{
				// check the backing cache for object
				e = _secondary.RemoveInternal(key, ItemRemoveReason.Removed, false,operationContext);
				if(e != null)
				{					
                    _primary.Add(key, e, false,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
				}
			}
			return e;
		}

		/// <summary>
		/// Adds a pair of key and value to the cache. Throws an exception or reports error 
		/// if the specified key already exists in the cache.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="cacheEntry">the cache entry.</param>
		/// <returns>returns the result of operation.</returns>
        internal override CacheAddResult AddInternal(object key, CacheEntry cacheEntry, bool isUserOperation, OperationContext operationContext)
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			// If the secondary has it then we are bound to return error
			if(_secondary.ContainsInternal(key))
				return CacheAddResult.KeyExists;

			// If the call succeeds there might be some eviction, which is handled by
			// the primary listener. Otherwise there is some error so we may try the second
			// instance.
			return _primary.AddInternal(key, cacheEntry, false,operationContext);
		}


        internal override bool AddInternal(object key, ExpirationHint eh, OperationContext operationContext)
        {
            if (_primary == null || _secondary == null)
                throw new InvalidOperationException();

            // If the primary has it then we are bound to update that item
            if (_primary.ContainsInternal(key))
            {
                return _primary.AddInternal(key, eh,operationContext);
            }

            // If the secondary has it then we are bound to update that item
            if (_secondary.Contains(key,operationContext))
            {
                return _secondary.AddInternal(key, eh,operationContext);
            }

            return false;
        }

		/// <summary>
		/// Adds a pair of key and value to the cache. If the specified key already exists 
		/// in the cache; it is updated, otherwise a new item is added to the cache.
		/// </summary>
		/// <param name="key">key of the entry.</param>
		/// <param name="cacheEntry">the cache entry.</param>
		/// <returns>returns the result of operation.</returns>
        internal override CacheInsResult InsertInternal(object key, CacheEntry cacheEntry, bool isUserOperation, CacheEntry oldEntry, OperationContext operationContext, bool updateIndex)
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			// If the primary has it then we are bound to update that item
			if(_primary.ContainsInternal(key))
			{
				return _primary.InsertInternal(key, cacheEntry, false,oldEntry,operationContext, updateIndex);
			}

			// If the secondary has it then we are bound to update that item
			if(_secondary.Contains(key,operationContext))
			{
				return _secondary.InsertInternal(key, cacheEntry, false,oldEntry,operationContext, updateIndex);
			}

			CacheAddResult result = AddInternal(key, cacheEntry, false,operationContext);
			switch(result)
			{
				case CacheAddResult.Success: return CacheInsResult.Success;
				case CacheAddResult.NeedsEviction: return CacheInsResult.NeedsEviction;
			}
			return CacheInsResult.Failure;
		}

        /// <summary>
        /// remove item from the primary cache and move items to the secondary cache if items are
        /// being evicted from the primary cache.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="reason"></param>
        /// <param name="notify"></param>
        /// <returns></returns>
        public override object RemoveSync(object[] keys, ItemRemoveReason reason, bool notify, OperationContext operationContext)
        {
            if (reason == ItemRemoveReason.Expired)
            {
                return _context.CacheImpl.RemoveSync(keys, reason, notify,operationContext);
            }
            if (_primary != null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    CacheEntry entry = _primary.Remove(keys[i], reason, false, null, 0, LockAccessType.IGNORE_LOCK,operationContext);
                    if (entry == null) continue;
                    if (reason == ItemRemoveReason.Underused && entry != null)
                    {
                        _secondary.Add(keys[i],entry, false,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
                    }
                    else
                    {
                        ((IDisposable)entry).Dispose();
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Removes the object and key pair from the cache. The key is specified as parameter.
        /// Moreover it take a removal reason and a boolean specifying if a notification should
        /// be raised.
        /// </summary>
        /// <param name="key">key of the entry.</param>
        /// <param name="removalReason">reason for the removal.</param>
        /// <param name="notify">boolean specifying to raise the event.</param>
        /// <returns>item value</returns>
        internal override CacheEntry RemoveInternal(object key, ItemRemoveReason removalReason, bool isUserOperation, OperationContext operationContext)
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			// check the front cache for object
			CacheEntry e = _primary.RemoveInternal(key, ItemRemoveReason.Removed, false,operationContext);
			if(e == null)
			{
				// check the backing cache for object
                e = _secondary.RemoveInternal(key, ItemRemoveReason.Removed, false,operationContext);
			}
			return e;
		}

		#endregion
		
		#region	/                 --- ICache ---           /

		/// <summary>
		/// Returns a .NET IEnumerator interface so that a client should be able
		/// to iterate over the elements of the cache store.
		/// </summary>
		public override IDictionaryEnumerator GetEnumerator()
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			return new AggregateEnumerator(_primary.GetEnumerator(), _secondary.GetEnumerator());
		}

		#endregion

		/// <summary>
		/// Evicts items from the store.
		/// </summary>
		/// <returns></returns>
		public override void Evict()
		{
			if(_primary == null || _secondary == null)
				throw new InvalidOperationException();

			_primary.Evict();
		}
	}
}
