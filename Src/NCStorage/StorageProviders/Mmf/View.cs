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
using System.IO;
using System.Runtime.InteropServices; 

namespace Alachisoft.NCache.Storage.Mmf
{
	internal class View 
	{
		struct ViewHeader
		{
			public const uint SIGNATURE = 0xface0ff;
			public uint Signature;
			public uint FreeSpace;
			public uint MaxFreeSpace;
            public uint MyFreeSpace;

			public bool IsValid { get { return Signature == SIGNATURE; } }

			public static int Size { get { return Marshal.SizeOf(typeof(ViewHeader)); } }

			#region /               -- IRawObject Members --              /

			public unsafe void RawRead(MmfFileView view, int offset)
			{
				Signature = view.ReadUInt32(offset); offset += sizeof(uint);
				FreeSpace = view.ReadUInt32(offset); offset += sizeof(uint);
				MaxFreeSpace = view.ReadUInt32(offset);
			}

			public unsafe void RawWrite(MmfFileView view, int offset)
			{
				view.WriteUInt32(offset, Signature); offset += sizeof(uint);
				view.WriteUInt32(offset, FreeSpace); offset += sizeof(uint);
				view.WriteUInt32(offset, MaxFreeSpace);
			}

			#endregion
		}

		private MmfFile _mmf;
		private MmfFileView _view;
		private ViewHeader _hdr;
		private uint _vid;
		private uint _size;
		private int _usage;
        private MmfStorageProvider _parentStorageProvider; // we need to get hold of hash-table _itemDict in order to 
                                                           // keep the mem-arena and MmfObjectPtr in synch.
        private MemArena _lastFreeArena; //why loop all the arena for every add.. Lets get to end of arena before actual re-use of previously de-allocated ones.
		
        public View(MmfFile mmf, uint id, uint size)
		{
			_mmf = mmf;
			_vid = id;
			_size = size;
		}

		public uint ID { get { return _vid; } }
		public uint Size { get { return _size; } }
		internal MmfFileView MmfView { get { return _view; } }

		public bool IsOpen { get { return _view != null; } }
		public bool HasValidHeader { get { return _hdr.IsValid; } }
		public int Usage 
		{
			get { return _usage; }
			set { _usage = value; } 
		}

        /// <summary>
        /// Returns the parent MmfStorageProvider reference back
        /// Required to get hold of hashtable _itemDict
        /// </summary>
        internal MmfStorageProvider ParentStorageProvider
        {
            get { return _parentStorageProvider; }
            set { _parentStorageProvider = value; }
        }
        public void Open()
		{
			_view = _mmf.MapView(_vid * _size, _size);
			if (_view != null) 
				ReadHeader();
		}

		public void Close()
		{
			if (IsOpen)
			{
				_mmf.UnMapView(_view);
				_view = null;
				_usage = 0;
			}
		}

		internal uint Signature
		{
			get { return _hdr.Signature; }
			set
			{
				_hdr.Signature = value;
				if (IsOpen) _view.WriteUInt32(0, _hdr.Signature);
			}
		}

		public unsafe uint FreeSpace
		{
			get { return _hdr.FreeSpace; }
			set
			{
				_hdr.FreeSpace = value;
				if (IsOpen)
					_view.WriteUInt32(1 * sizeof(int), _hdr.FreeSpace);
			}
		}

        public MemArena LastFreeArena
        {
            get { return _lastFreeArena; }
            set { value = _lastFreeArena; }
        }
        
        public unsafe uint MyFreeSpace
        {
            get { return _hdr.MyFreeSpace; } 
            set { _hdr.MyFreeSpace = value;}
        }
        
        public unsafe uint MaxFreeSpace
		{
			get { return _hdr.MaxFreeSpace; }
			set
			{
				_hdr.MaxFreeSpace = value;
				if (IsOpen)
					_view.WriteUInt32(2 * sizeof(int), _hdr.MaxFreeSpace);
			}
		}

		public MemArena FirstArena()
		{
			return new MemArena(this, (uint)ViewHeader.Size);
		}

		public MemArena ArenaAtOffset(uint offSet)
		{
			return new MemArena(this, offSet);
		}

		/// <summary>
		/// Formats the view and writes a valid header. Analogous to a 
		/// hard-disk format operation.
		/// </summary>
		public void Format()
		{
			if (!IsOpen) return;

			int headerSize = ViewHeader.Size;
			Signature = ViewHeader.SIGNATURE;
			FreeSpace = MaxFreeSpace = (uint)(_view.Length - headerSize);
            MyFreeSpace = FreeSpace;
            _parentStorageProvider = null;			
            MemArena arena = FirstArena();
			arena.IsFree = true;
			arena.Capacity = (uint)(_view.Length - headerSize - MemArena.Header.Size);
			arena.OffsetNext = 0;
			arena.OffsetPrev = 0;            
		}

		public MemArena Allocate(uint size)
		{            
            // Check if there isnt enough storage left in this view
			if (size > FreeSpace)
			{
				return null;
			}

			MemArena arena = null;

			// Check if there isnt enough CONTIGOUS storage available
			// defragment the memory to create a large chunk of mem at
			// the end of view.
			size = MemArena.Content.RequiredSpace(size);
            arena = FindFirstFreeArena(size);
            if (arena == null)
            {
                CalculateArenaMemoryUsage(); //keep it here ..we need it.
                arena = DeFragment();
                if (arena == null)
                    return arena;            
            }
            /*if (size > MaxFreeSpace) //Now call to CalculateArenaMemoryUsage is avoided so lets call it ..if required.
			{
				arena = DeFragment();
			}
			else
			{
				arena = FindFirstFreeArena(size);
			}
			if (arena == null) return arena;*/

			// allocate space in parent arena.
			MemArena newArena = MemArena.SplitArena(arena, size);
			if (newArena == null) return arena;

            arena.IsFree = false;
            if (arena != newArena) // deal with the case when splitarena returns the same arena back. Saves situation where allocated arena is accidently set to be free.
            {
                newArena.IsFree = true;
                _lastFreeArena = newArena;
            }

            FreeSpace = MyFreeSpace - (uint)MemArena.Header.Size;
            //CalculateArenaMemoryUsage(); removing it ....
			Usage++;

			return arena;
		}

		public MemArena DeAllocate(MemArena arena)
		{
			if (arena.IsFree) return arena;
            
			uint freedMem = arena.Capacity;
            arena.View.MyFreeSpace += arena.Capacity;
            arena.IsFree = true;
			arena = MemArena.CoalesceAdjacent(arena);
            arena.IsFree = true;

            FreeSpace = MyFreeSpace - (uint)MemArena.Header.Size;
            //CalculateArenaMemoryUsage(); removing it..

			Usage++;

			return arena;
		}

		private void CalculateArenaMemoryUsage()
		{
            MemArena arena = FirstArena();
            MemArena firstArena = FirstArena(); //need to keep the first ..for avoiding circular linkages..
            uint maxFree = 0;
			uint totalFree = 0;            
            
            while (arena != null)
			{                
                try
                {                    
                    if (arena.IsFree)
                    {
                        totalFree += arena.Capacity;
                        if (arena.Capacity >= maxFree)
                            maxFree = arena.Capacity;
                    }                    
                    arena = arena.NextArena();
                    if (arena != null)
                    {                        
                        if (arena.OffsetNext == firstArena.Offset)                       
                            arena.OffsetNext = 0;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);   
                }
			}            
            this.FreeSpace = totalFree;
			this.MaxFreeSpace = maxFree;
            this.MyFreeSpace = this.FreeSpace + (uint)MemArena.Header.Size;
                  
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public MemArena DeFragment()
		{            
            MemArena lastArena = FirstArena();
			do
			{
				MemArena current = lastArena.NextArena();
				if (current == null)
					break;

				// If the last arena is used we dont need to swap it down.
				if (!lastArena.IsFree)
				{
					lastArena = current;
					continue;
				}

				// Two consecutive free arenas, rare, but taken care of!
				if (current.IsFree)				
					lastArena = MemArena.CoalesceAdjacent(current);									
				else
				{
					// A free arena followed by a used one so we swap down the free one.
					MemArena.SwapAdjacent(current, lastArena);
					lastArena = current;					
				}
			} 
			while (true);

			this.FreeSpace = lastArena.Capacity;
			this.MaxFreeSpace = lastArena.Capacity;
            CalculateArenaMemoryUsage(); //may be we need it here.. very low frequency of this to be called..
            return lastArena;
		}

		/// <summary>
		/// Finds the first free memarea with enough required contigous space.
		/// </summary>
		/// <param name="memRequirements">space requirements</param>
		/// <returns>null if no such area exists, i.e view is full</returns>
		private MemArena FindFirstFreeArena(uint memRequirements)
		{            
            memRequirements += (uint)MemArena.Header.Size;
			MemArena arena = FirstArena();       
            
            if (arena.OffsetNext != 0) 
                arena.Capacity = (uint)(arena.OffsetNext - (arena.Offset + MemArena.Header.Size));

            if (_lastFreeArena != null) //save time .This improves performance by many times.. I am saving the last free arena, to be used for next instert. This eliminates list traversal for every add operation.
            {
                if (_lastFreeArena.IsFree && _lastFreeArena.Capacity >= memRequirements)
                    return _lastFreeArena;
            }
            
            while (arena != null)
			{                
                if (arena.IsFree && arena.Capacity >= memRequirements)                                
                    return arena;                
				
                arena = arena.NextArena();
			}
			return null;
		}

		private void ReadHeader()
		{
			if (IsOpen) 
				_hdr.RawRead(_view, 0);
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();

			b.Append("View, ID=").Append(ID)
			 .Append(", maxFree=").Append(MaxFreeSpace)
			 .Append(", Free=").Append(FreeSpace)
			 .Append("\r\n");

			if (IsOpen)
			{
				MemArena next = FirstArena();
				while (next != null)
				{
					b.Append(next.ToString()).Append("\r\n");
					next = next.NextArena();
				}
			}
			return b.ToString();
		}
	}
}