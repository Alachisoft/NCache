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
using System.Text;
using System.Collections;

using Alachisoft.NCache.Storage.Mmf;
using Alachisoft.NCache.Storage.Interop;

namespace Alachisoft.NCache.Storage.Mmf
{
    internal class MmfStorage : IDisposable
    {
		private MmfFile			_mmf;
        private ViewManager		_viewManager;
        private string			_fileName;
		private uint			_viewCount = 8;
		private uint			_viewSize = 4 * StorageProviderBase.MB;
		private uint			_initialSizeMB = 32;

        /// <summary>
        /// Initializes the Binary Mapped storage.
        /// </summary>
        /// <param name="fileName">Name of the file to be mapped</param>
        /// <param name="mapObjName">Name of the mapped object</param>
        public MmfStorage()
        {
        }

        /// <summary>
        /// Gets/Sets the name of the file to be mapped.
        /// </summary>
		public String FileName { get { return _fileName; } }
		public bool IsPageFileStore { get { return _mmf.IsPageFile; } }


		#region /                  Initialize/Dispose Members                  /

		public void Dispose()
		{
			CloseMemoryMappedStore();
		}

		#endregion
		
		/// <summary>
        /// Maps the file to the memory of the process.
        /// </summary>
		public void OpenMemoryMappedStore(string fileName, uint viewCount, uint viewSize, uint initialSizeMB)
        {
            try
            {
				_fileName = fileName;
				_viewCount = viewCount;
				_viewSize = (uint)SysUtil.AllignViewSize(viewSize);
				_initialSizeMB = initialSizeMB;

				_mmf = MmfFile.Create(_fileName, _initialSizeMB * StorageProviderBase.MB, false);
				_viewManager = new ViewManager(_mmf, _viewSize);
                _viewManager.CreateInitialViews(_viewCount);
            }
            catch(Exception e)
            {
                //Console.WriteLine("MmfStorage.OpenMemoryMappedStore" + "Error:" + e);
                throw;
            }
            
        }

		/// <summary>
		/// Maps the file to the memory of the process.
		/// </summary>
		private void GrowMemoryMappedStore(int numViewsToAdd)
		{
			try
			{
				ulong newLength = (ulong) _mmf.MaxLength + (ulong)numViewsToAdd * _viewSize;
				_mmf.SetMaxLength(newLength);
				_viewManager.ExtendViewsBucket(numViewsToAdd);
			}

            catch (OutOfMemoryException ex)
            {
                throw;
            }
            catch (Exception e)
			{
				throw;
			}

		}
		/// <summary>
        /// Unmaps the file to the memory of the process.
        /// </summary>
		private void CloseMemoryMappedStore()
        {
            try
            {
				_viewManager.CloseAllViews();
				_mmf.Close();
            }
            catch(Exception e)
            {
                Trace.error("MmfStorage.CloseMemoryMappedStore" + "Error:", e.ToString());
                throw;
            }
        }

		/// <summary>
		/// Creates and returns a Ptr to object.
		/// </summary>
		public MmfObjectPtr GetPtr(uint vid, uint offset)
		{
			View view = _viewManager.GetViewByID(vid);
			if (view == null) return null;

			try
			{
				MemArena arena = view.ArenaAtOffset(offset);
				return new MmfObjectPtr(view, arena);
			}
			catch (Exception)
			{ 
			}
			return null;
		}

		/// <summary>
        /// Gets the object from the memory mapped file. 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
		public byte[] Get(MmfObjectPtr info)
        {
			if (info == null)
				throw new ArgumentNullException("info");

			if (!info.View.IsOpen)
			{
				_viewManager.OpenView(info.View);
			}
			
			byte[] item = info.Arena.GetMemContents();
			info.View.Usage++;

            return item;
        }

		public MmfObjectPtr Add(byte[] item)
        {
			if (item == null)
				throw new ArgumentNullException("item");
			if(item.Length > _viewSize)
				throw new ArgumentException("item size is larger than view size");

			// Find a view with enough space to contain the item.
			
			View view = _viewManager.GetMatchingView((uint)item.Length);
            try
            {
                if (view == null)
                {
                    GrowMemoryMappedStore(1);
                    view = _viewManager.GetMatchingView((uint)item.Length);
                }

                if (view != null)
                {
                    MemArena arena = view.Allocate((uint)item.Length);
                    if (arena == null) return null;

                    if (!arena.SetMemContents(item)) // It would return false only when size of Arena allocated is less then required even after all efforts to get arena of required size. 
                    {
                        view.DeAllocate(arena);
                        return null;
                    }

                    return new MmfObjectPtr(view, arena);
                }
            }
            catch (OutOfMemoryException ex)
            {
                throw;
            }

            catch (Exception ex)
            {
                throw;
            }
            return null;
        }

		public MmfObjectPtr Insert(MmfObjectPtr info, byte[] item)
        {
			if (info == null)
				throw new ArgumentNullException("info");
			if (info == null)
				throw new ArgumentNullException("item");

			if (!info.View.IsOpen)
			{
				_viewManager.OpenView(info.View);
			}

			// Check if this item can be accomodated in existing space.
			if (info.Arena.HasDataSpace((uint)item.Length))
			{                
                info.Arena.SetMemContents(item);
			}
			else
			{
				// Try to add it elsewhere and then delete the current space.                
                MmfObjectPtr newInfo = Add(item);
				//if (newInfo != null)
				//{                    
                    //Remove(info);  //as we are updating original hash table links, so before call remove we must update the hash-table entry, instead of updating later.
                    //I am moving this remove call to MmfStorageProvide 'Insert' methods ....
				//}
				info = newInfo;
			}
			
			return info;
        }

		public byte[] Remove(MmfObjectPtr info)
        {
			if (info == null)
				throw new ArgumentNullException("info");

			if (!info.View.IsOpen)
			{
				_viewManager.OpenView(info.View);
			}

			byte[] item = info.Arena.GetMemContents();
			// Reclaim space for current item.
            
            info.View.DeAllocate(info.Arena);

			return item;
		}

        public void Clear()
        {
			_viewManager.ClearAllViews();
        }

		public override string ToString()
		{
			StringBuilder b = new StringBuilder(1024);
			b.Append(_viewManager);
			return b.ToString();
		}

		public IEnumerator GetEnumerator()
		{
			return new MmfStorageEnumerator(this);
		}

		/// <summary>
		/// Class that implements enumerator over MmfStorage.
		/// </summary>
		class MmfStorageEnumerator : IEnumerator
		{
			private MmfStorage _storage;
			private View _view;
			private MemArena _arena; 
			
			public MmfStorageEnumerator(MmfStorage storage)
			{
				_storage = storage;
				_view = _storage._viewManager.GetViewByID(0);
				_arena = null;
			}

			#region	/                 --- IEnumerator ---           /

			object IEnumerator.Current
			{
				get 
				{
					_storage._viewManager.OpenView(_view);
					return _arena; 
				}
			}

			bool IEnumerator.MoveNext()
			{
				if (_view == null)
					return false;

				_storage._viewManager.OpenView(_view);
				if (_arena == null)
					_arena = _view.FirstArena();
				else
				{
					_arena = _arena.NextArena();
					if (_arena == null)
					{
						_view = _storage._viewManager.GetViewByID(_view.ID + 1);
						if (_view != null)
						{
							_storage._viewManager.OpenView(_view);
							_arena = _view.FirstArena();
						}
					}
				}

				return _arena != null;
			}

			void IEnumerator.Reset()
			{
				_view = _storage._viewManager.GetViewByID(0);
				_arena = null;
			}

			#endregion
		}
	}
}
