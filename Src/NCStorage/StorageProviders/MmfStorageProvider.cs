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
using System.IO;
using Alachisoft.NCache.Storage.Util;
using Alachisoft.NCache.Storage.Mmf;
using Alachisoft.NCache.Serialization;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Collections;

namespace Alachisoft.NCache.Storage
{
    /// <summary>
	/// Implements the memmory mapped file based cache storage option. 
	/// Also implements ICacheStorage interface. 
	/// </summary>
	class MmfStorageProvider : StorageProviderBase, IPersistentCacheStorage
	{
		/// <summary> Storage Map </summary>
		protected Hashtable 		_itemDict;
		private MmfStorage			_internalStore;
		private string				_fileName;
		private uint				_viewCount = 8;
		private uint				_viewSize = 4 * StorageProviderBase.MB;
		private uint				_initialSizeMB = 32;

		static MmfStorageProvider()
		{
			CompactFormatterServices.RegisterCompactType(typeof(StoreItem),80);
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		protected MmfStorageProvider()
		{
			_itemDict = (new Hashtable(DEFAULT_CAPACITY));
			_internalStore = new MmfStorage();
		}

		/// <summary>
		/// Overloaded constructor. Takes the properties as a map.
		/// </summary>
		/// <param name="properties">properties collection</param>
		public MmfStorageProvider(IDictionary properties,bool evictinEnabled)
		{
			_itemDict = (new Hashtable(DEFAULT_CAPACITY));
			_internalStore = new MmfStorage();
			Initialize(properties,evictinEnabled);
		}

		#region /                  Initialize/Dispose Members                  /

		/// <summary>
		/// Initializes the view manager.
		/// </summary>
		/// <param name="properties">Properties to be set</param>
		public new void Initialize(IDictionary properties,bool evictinEnabled)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			try
			{
				if (properties.Contains("file-name"))
					_fileName = Convert.ToString(properties["file-name"]);

				if (properties.Contains("num-views"))
					_viewCount = Convert.ToUInt32(properties["num-views"]);

				if (properties.Contains("view-size"))
					_viewSize = Convert.ToUInt32(properties["view-size"]);

				if (properties.Contains("initial-size-mb"))
					_initialSizeMB = Convert.ToUInt32(properties["initial-size-mb"]);

				_internalStore.OpenMemoryMappedStore(_fileName, _viewCount, _viewSize, _initialSizeMB);
				
                if (!_internalStore.IsPageFileStore)
				{
					((IPersistentCacheStorage)this).LoadStorageState();
				}

				base.Initialize(properties,evictinEnabled);
			}
			catch (Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			if (!_internalStore.IsPageFileStore)
			{
				((IPersistentCacheStorage)this).SaveStorageState();
			}
			_internalStore.Dispose();
			base.Dispose();			
		}

		#endregion

		#region	/                 --- ICacheStorage ---           /

		/// <summary>
		/// returns the number of objects contained in the cache.
		/// </summary>
		public override long Count { get { return _itemDict.Count; }}

		/// <summary>
		/// Removes all entries from the store.
		/// </summary>
		public override void Clear()
		{
			lock (_itemDict.SyncRoot)
			{
				_itemDict.Clear();
				_internalStore.Clear();
                base.Cleared();
			}
		}

		/// <summary>
		/// Determines whether the store contains a specific key.
		/// </summary>
		/// <param name="key">The key to locate in the store.</param>
		/// <returns>true if the store contains an element 
		/// with the specified key; otherwise, false.</returns>
		public override bool Contains(object key)
		{
			return _itemDict.ContainsKey(key);
		}

		/// <summary>
		/// Provides implementation of Get method of the ICacheStorage interface.
		/// Get an object from the store, specified by the passed in key. 
		/// </summary>
		/// <param name="key">key</param>
		/// <returns>object</returns>
		public override object Get(object key)
		{
			try
			{
				MmfObjectPtr info = (MmfObjectPtr)_itemDict[key];
				if (info != null)
				{
					byte[] data = _internalStore.Get(info);
					StoreItem item = StoreItem.FromBinary(data,CacheContext);
					return item.Value;
				}
			}
			catch(Exception e)
			{
				Trace.error("MmfStorageProvider.Get()", e.ToString());
			}
			return null;
		}

        /// <summary>
        /// Get the size of item stored in cache, specified by the passed in key
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>item size</returns>
        public override int GetItemSize(object key)
        {
            try
            {
                MmfObjectPtr info = (MmfObjectPtr)_itemDict[key];
                if (info != null)
                {
                    byte[] data = _internalStore.Get(info);
                    StoreItem item = StoreItem.FromBinary(data, CacheContext);
                    return ((ISizable)item.Value).InMemorySize;
                }
            }
            catch (Exception e)
            {
                Trace.error("MmfStorageProvider.GetItemSize()", e.ToString());
            }
            return 0;
        }

		/// <summary>
		/// Provides implementation of Add method of the ICacheStorage interface. Add the key 
		/// value pair to the store. 
		/// </summary>
		/// <param name="key">key</param>
		/// <param name="item">object</param>
		/// <returns>returns the result of operation.</returns>
		public override StoreAddResult Add(object key, IStorageEntry item, Boolean allowExtendedSize)
		{
			try
			{
				if(_itemDict.ContainsKey(key))
				{
					return StoreAddResult.KeyExists;
				}

                StoreStatus status = HasSpace((ISizable)item, Common.MemoryUtil.GetStringSize(key), allowExtendedSize);

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreAddResult.NotEnoughSpace;
                }

				byte[] buffer = StoreItem.ToBinary(key, item,CacheContext);

				lock (_itemDict.SyncRoot)				
                {                  
                    MmfObjectPtr info = _internalStore.Add(buffer);                    
                    if (info == null)
						return StoreAddResult.NotEnoughSpace;
                    info.View.ParentStorageProvider = this;
                    _itemDict.Add(key, info);

                    base.Added(item, Common.MemoryUtil.GetStringSize(key));
				}

                if (status == StoreStatus.NearEviction)
                {
                    return StoreAddResult.SuccessNearEviction;
                }
			}			
			catch(OutOfMemoryException e)
			{
				Trace.error("OutofMemoryException::MmfStorageProvider.Add()", e.ToString());
				return StoreAddResult.NotEnoughSpace;
			}
			catch(Exception e)
			{
				Trace.error("General Exception::MmfStorageProvider.Add()", e.ToString());
				return StoreAddResult.Failure;
			}
			return StoreAddResult.Success;	
		}

		/// <summary>
		/// Provides implementation of Insert method of the ICacheStorage interface. Insert 
		/// the key value pair to the store. 
		/// </summary>
		/// <param name="key">key</param>
		/// <param name="item">object</param>
		/// <returns>returns the result of operation.</returns>
        public override StoreInsResult Insert(object key, IStorageEntry item, Boolean allowExtendedSize)
		{
			try
			{   
                MmfObjectPtr info = (MmfObjectPtr)_itemDict[key];
                IStorageEntry oldItem = null;

                if (info == null)
				{
					StoreAddResult res = Add(key, item,allowExtendedSize);
					switch (res)
					{
						case StoreAddResult.NotEnoughSpace: return StoreInsResult.NotEnoughSpace;
						case StoreAddResult.Failure: return StoreInsResult.Failure;
					}
					return StoreInsResult.Success;
				}

                oldItem = (IStorageEntry)Get(key);
				
                StoreStatus status = HasSpace(oldItem as ISizable, (ISizable)item,Common.MemoryUtil.GetStringSize(key),allowExtendedSize);

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreInsResult.NotEnoughSpace;
                }

				byte[] buffer = StoreItem.ToBinary(key, item,CacheContext);
				lock (_itemDict.SyncRoot)
				{
					MmfObjectPtr newInfo = _internalStore.Insert(info, buffer);
                    if (newInfo == null)
                        return StoreInsResult.NotEnoughSpace;
                    else
                    {
                        if (newInfo.Arena != info.Arena)                        
                        {                            
                            _itemDict[key] = newInfo;
                            _internalStore.Remove(info);
                        }

                        base.Inserted(oldItem , item , Common.MemoryUtil.GetStringSize(key));
                    }
                    if (status == StoreStatus.NearEviction)
                    {
                        return oldItem != null ? StoreInsResult.SuccessOverwriteNearEviction : StoreInsResult.SuccessNearEviction;
                    }
					return newInfo != null ? StoreInsResult.SuccessOverwrite : StoreInsResult.Success;
				}
			}
			catch(OutOfMemoryException e)
			{
				Trace.error("MmfStorageProvider.Insert()", e.ToString());
				return StoreInsResult.NotEnoughSpace;
			}
			catch(Exception e)
			{
				Trace.error("MmfStorageProvider.Insert()", e.ToString());
				return StoreInsResult.Failure;
			}
		}

		/// <summary>
		/// Provides implementation of Remove method of the ICacheStorage interface.
		/// Removes an object from the store, specified by the passed in key
		/// </summary>
		/// <param name="key">key</param>
		/// <returns>object</returns>
		public override object Remove(object key)
		{
			try
			{                
                lock (_itemDict.SyncRoot)
				{
                    MmfObjectPtr info = (MmfObjectPtr)_itemDict[key];
                   
					if (info != null)
					{
						byte[] data = _internalStore.Remove(info);
						StoreItem item = StoreItem.FromBinary(data,CacheContext);						
                        _itemDict.Remove(key);

                        IStorageEntry strEntry = item.Value as IStorageEntry;
                        base.Removed(strEntry, Common.MemoryUtil.GetStringSize(key),strEntry.Type);
						return item.Value;
					}
				}
			}
			catch (Exception e)
			{
				Trace.error("MmfStorageProvider.Remove()", e.ToString());
			}
			return null;
		}

		/// <summary>
		/// Returns a .NET IEnumerator interface so that a client should be able
		/// to iterate over the elements of the cache store.
		/// </summary>
		public override IDictionaryEnumerator GetEnumerator()
		{
			return new LazyStoreEnumerator(this,_itemDict.GetEnumerator());
		}

		#endregion

		#region /                   -- IPersistentCacheStorage Members --                    /

		/// <summary>
		/// Load store state and data from persistent medium.
		/// </summary>
		void IPersistentCacheStorage.LoadStorageState()
		{
			try
			{
				lock (_itemDict.SyncRoot)
				{
					_itemDict.Clear();
					for (IEnumerator i = _internalStore.GetEnumerator(); i.MoveNext(); )
					{
						MemArena arena = (MemArena)i.Current;
						if (!arena.IsFree)
						{
							byte[] data = arena.GetMemContents();
							StoreItem item = StoreItem.FromBinary(data,CacheContext);

							_itemDict.Add(item.Key, new MmfObjectPtr(arena.View, arena));
						}
					}
				}
			}
			catch (Exception e)
			{
				Trace.error("MmfStorageProvider.IPersistentCacheStorage()", e.ToString());
			}
		}

		/// <summary>
		/// Save store state and data to persistent medium.
		/// </summary>
		void IPersistentCacheStorage.SaveStorageState()
		{
		}

        /// <summary>
        /// Returns the MemArena against specific key.
        /// </summary>
        //I need it to update 'THIS' arena when ever next/previous references change [Asif Imam]
        public MemArena GetMemArena(object key) 
        {
            MmfObjectPtr info = (MmfObjectPtr)_itemDict[key];
            return info.Arena;
        }

        public void SetMemArena(object key,MemArena arenaTmp)
        {
            _itemDict[key] = arenaTmp;            
        }

        #endregion

    }
}