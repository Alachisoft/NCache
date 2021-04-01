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
using System.Threading;

using Alachisoft.NCache.Storage.Util;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Collections;

namespace Alachisoft.NCache.Storage
{
	/// <summary>
	/// Implements the File based cache storage option. Also implements ICacheStorage interface. 
	/// </summary>
	class FileSystemStorageProvider : StorageProviderBase, IPersistentCacheStorage
	{
		// The default extension to use with object data files
		private const string	STATE_FILENAME = "__ncfs__.state";
		// The default extension to use with object data files
		private const int		STATE_SAVE_INTERVAL = 60 * 1000;
		
		/// <summary> Storage Map </summary>
		protected Hashtable			_itemDict;
		/// <summary> </summary>
		private FileSystemStorage	_internalStore;
		/// <summary> </summary>
		private Timer				_persistenceTimer;
		/// <summary> </summary>
		private int					_stateChangeId;

		/// <summary>
		/// Default constructor.
		/// </summary>
		protected FileSystemStorageProvider()
		{
			_itemDict = (new Hashtable(DEFAULT_CAPACITY));
		}

		/// <summary>
		/// Overloaded constructor. Takes the properties as a map.
		/// </summary>
		/// <param name="properties">properties collection</param>
		public FileSystemStorageProvider(IDictionary properties,bool evictionEnabled):base(properties,evictionEnabled)
		{
			_itemDict = (new Hashtable(DEFAULT_CAPACITY));
			Initialize(properties);
		}

		#region /                  Initialize/Dispose Members                  /

		/// <summary>
		/// Initializes the view manager.
		/// </summary>
		/// <param name="properties">Properties to be set</param>
		public void Initialize(IDictionary properties)
		{
			try
			{
				string rootDir = null;
				if (properties.Contains("root-dir"))
					rootDir = Convert.ToString(properties["root-dir"]);

				// This key is used as the data-folder by the store. If there is no key
				// a random folder is used, which disables persistence.
				string persistenceKey = null;
				if (properties.Contains("persistence-key"))
					persistenceKey = Convert.ToString(properties["persistence-key"]);

				int persistenceInterval = STATE_SAVE_INTERVAL;
				if (properties.Contains("persistence-interval"))
				{
					persistenceInterval = Convert.ToInt32(properties["persistence-interval"]);
					persistenceInterval = Math.Max(1000, persistenceInterval);
				}

				_internalStore = new FileSystemStorage(rootDir, persistenceKey);
				// A random folder is being used, persistence is not possible.
				if (_internalStore.DataFolder != null)
				{
					((IPersistentCacheStorage)this).LoadStorageState();

					// Start a timer for periodic saving of state.
					TimerCallback callback = new TimerCallback(OnPersistStateTimer);
					_persistenceTimer = new Timer(new TimerCallback(callback),
						persistenceInterval, persistenceInterval, persistenceInterval);
				}
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
			if (_persistenceTimer != null)
			{
				_persistenceTimer.Dispose();
				_persistenceTimer = null;
			}
			if (_internalStore.DataFolder != null)
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
		public override long Count { get { return _itemDict.Count; } }

		/// <summary>
		/// Removes all entries from the store.
		/// </summary>
		public override void Clear()
		{
			lock (_itemDict)
			{
				_itemDict.Clear();
				_internalStore.Clear();
                base.Cleared();

				SetStateChanged();
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
				return (object)_internalStore.Get(_itemDict[key],CacheContext);
			}
			catch (Exception e)
			{
                Trace.error("FileSystemStorageProvider.Get()", e.ToString());
				return null;
			}
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
                return ((ISizable)_internalStore.Get(_itemDict[key], CacheContext)).InMemorySize;
            }
            catch (Exception e)
            {
                Trace.error("FileSystemStorageProvider.GetItemSize()", e.ToString());
                return 0;
            }
        }

		/// <summary>
		/// Provides implementation of Add method of the ICacheStorage interface. Add the key 
		/// value pair to the store. 
		/// </summary>
		/// <param name="key">key</param>
		/// <param name="item">object</param>
		/// <returns>returns the result of operation.</returns>
		public override StoreAddResult Add(object key, IStorageEntry item,   Boolean allowExtendedSize)
		{
			try
			{
				if (_itemDict.ContainsKey(key))
				{
					return StoreAddResult.KeyExists;
				}

                StoreStatus status = HasSpace((ISizable)item, Common.MemoryUtil.GetStringSize(key),allowExtendedSize);

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreAddResult.NotEnoughSpace;
                }
               
				lock (_itemDict)
				{
					object value = _internalStore.Add(key, item,CacheContext);
					if (value != null)
					{
						_itemDict.Add(key, value);
						SetStateChanged();

                        base.Added(item, Common.MemoryUtil.GetStringSize(key));
					}
				}
                if (status == StoreStatus.NearEviction)
                {
                    return StoreAddResult.SuccessNearEviction;
                }
			}
			catch (OutOfMemoryException e)
			{

                Trace.error("FileSystemStorageProvider.Add()", e.ToString());
				return StoreAddResult.NotEnoughSpace;
			}
			catch (Exception e)
			{
                Trace.error("FileSystemStorageProvider.Add()", e.ToString());
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
                IStorageEntry oldItem =(IStorageEntry) Get(key);

                StoreStatus status = HasSpace((ISizable)item, Common.MemoryUtil.GetStringSize(key),allowExtendedSize);

                if (status == StoreStatus.HasNotEnoughSpace)
                {
                    return StoreInsResult.NotEnoughSpace;
                }

				lock (_itemDict)
				{
                    object value = _internalStore.Insert(_itemDict[key], item, CacheContext);
					if (value == null) return StoreInsResult.Failure;                

					_itemDict[key] = value;
					SetStateChanged();

                    base.Inserted(oldItem, item, Common.MemoryUtil.GetStringSize(key));
				}
                if (status == StoreStatus.NearEviction)
                {
                    return oldItem != null ? StoreInsResult.SuccessOverwriteNearEviction : StoreInsResult.SuccessNearEviction;
                }
                return oldItem != null ? StoreInsResult.SuccessOverwrite : StoreInsResult.Success;
			}
			catch (OutOfMemoryException e)
			{
                Trace.error("FileSystemStorageProvider.Insert()", e.ToString());
				return StoreInsResult.NotEnoughSpace;
			}
			catch (Exception e)
			{
				Trace.error("FileSystemStorageProvider.Insert()", e.ToString());
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
			lock (_itemDict)
			{
                IStorageEntry e = (IStorageEntry)Get(key);

				if (e != null)
				{
					_internalStore.Remove(_itemDict[key]);
                    base.Removed(e, Common.MemoryUtil.GetStringSize(key),e.Type);

					_itemDict.Remove(key);
					SetStateChanged();
				}
				return e;
			}
		}

		/// <summary>
		/// Returns a .NET IEnumerator interface so that a client should be able
		/// to iterate over the elements of the cache store.
		/// </summary>
		public override IDictionaryEnumerator GetEnumerator()
		{
			return new LazyStoreEnumerator(this, _itemDict.GetEnumerator());
		}

		#endregion

		private void SetStateChanged()
		{
			_stateChangeId++;
		}

		private void ResetStateChanged()
		{
			_stateChangeId = 0;
		}

		
        #region /                   -- Persistent Storage --                    /

		/// <summary>
		/// A TimerCallback executed periodically to save state.
		/// </summary>
		/// <param name="state"></param>
		private void OnPersistStateTimer(object state)
		{
			_persistenceTimer.Change(Timeout.Infinite, 0);
			Trace.info("FileSystemStorageProvider.OnPersistStateTimer()");
			if (_internalStore.DataFolder != null)
			{
				((IPersistentCacheStorage)this).SaveStorageState();

				int nextInterval = Convert.ToInt32(state);
				_persistenceTimer.Change(nextInterval, nextInterval);
			}
		}

		/// <summary>
		/// Load store state and data from persistent medium.
		/// </summary>
		void IPersistentCacheStorage.LoadStorageState()
		{
			try
			{

				string fileName = Path.Combine(_internalStore.RootDir, STATE_FILENAME);
				lock (_itemDict)
				{
					using (Stream state = new FileStream(fileName, FileMode.Open))
					{
						using (BinaryReader reader = new BinaryReader(state))
						{
							// Read count of total items.
							int count = reader.ReadInt32();
							// Clear current state; if any
							_itemDict.Clear();
							if (count < 1) return;

							for (int i = 0; i < count; i++)
							{
								int datalen = reader.ReadInt32();
								// -1 means the item was not written correctly
								if (datalen < 0) continue;

								try
								{
									byte[] buffer = reader.ReadBytes(datalen);
									StoreItem item = StoreItem.FromBinary(buffer,CacheContext);

									// key was successfuly serialized
									if (_internalStore.Contains(item.Value))
									{
										_itemDict.Add(item.Key, item.Value);
									}
								}
								catch (Exception)
								{
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Trace.error("FileSystemStorageProvider.LoadStorageState()", e.ToString());
			}
			finally
			{
				ResetStateChanged();
			}
		}

		/// <summary>
		/// Save store state and data to persistent medium.
		/// </summary>
		void IPersistentCacheStorage.SaveStorageState()
		{
			try
			{
				if (_stateChangeId == 0) return;

				string fileName = Path.Combine(_internalStore.RootDir, STATE_FILENAME);
				lock (_itemDict)
				{
					using (Stream state = new FileStream(fileName, FileMode.Create))
					{
						using (BinaryWriter writer = new BinaryWriter(state))
						{
							// Write count of total items in store.
							writer.Write(_itemDict.Count);

							IDictionaryEnumerator i = _itemDict.GetEnumerator();
							for (; i.MoveNext(); )
							{
								try
								{
									byte[] buffer = StoreItem.ToBinary(i.Key, i.Value,CacheContext);
									writer.Write(buffer.Length);
									writer.Write(buffer);
								}
								catch (Exception)
								{
									// Indicate failure by writing -1.
									writer.Write((int)-1);
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Trace.error("FileSystemStorageProvider.SaveStorageState()", e.ToString());
			}
			finally
			{
				ResetStateChanged();
			}
		}

        #endregion


    }
}
