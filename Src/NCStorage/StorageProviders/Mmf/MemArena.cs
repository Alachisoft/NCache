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
using System.Collections;
using System.Runtime.InteropServices;
using Alachisoft.NCache.Storage;
using Alachisoft.NCache.Storage.Util;


namespace Alachisoft.NCache.Storage.Mmf
{
	internal class MemArena
	{
		/// <summary>
		/// A BlockHeader represents cotains the information about the block to
		/// be stored in the memory mapped file.
		/// </summary>
		public struct Header 
		{
			public const byte FREE = 0;
			public const byte USED = 1;

			public byte Status;
			public uint Capacity;
			public uint OffsetNext;
			public uint OffsetPrev;

			/// <summary> Gets/Sets the length of the data stored in the block. </summary>
			public bool IsFree { get { return Status == Header.FREE; } }
			/// <summary> Gets/Sets the length of the data stored in the block. </summary>
			public bool HasNext { get { return OffsetNext > 0; } }
			/// <summary> Gets/Sets the length of the data stored in the block. </summary>
			public bool HasPrevious { get { return OffsetPrev > 0; } }

			public static int Size { get { return Marshal.SizeOf(typeof(Header)); } }

			#region /               -- IRawObject Members --              /

			public void RawRead(MmfFileView raw, int offset)
			{                
                unsafe
				{
					Status = raw.ReadByte(offset); offset += sizeof(byte);
					Capacity = raw.ReadUInt32(offset); offset += sizeof(uint);
					OffsetNext = raw.ReadUInt32(offset); offset += sizeof(uint);
					OffsetPrev = raw.ReadUInt32(offset);
                }
			}

			public void RawWrite(MmfFileView raw, int offset)
			{
				unsafe
				{
					raw.WriteByte(offset, Status); offset += sizeof(byte);
					raw.WriteUInt32(offset, Capacity); offset += sizeof(uint);
					raw.WriteUInt32(offset, OffsetNext); offset += sizeof(uint);
					raw.WriteUInt32(offset, OffsetPrev);
				}
			}

			#endregion

		}

		public struct Content 
		{
			public byte[] Data;

			public static uint RequiredSpace(uint dataLen)
			{
				unsafe
				{
					return dataLen + sizeof(uint);
				}
			}

			#region /               -- IRawObject Members --              /

			public void RawRead(MmfFileView raw, int offset)
			{
				unsafe
				{
					int length = raw.ReadInt32(offset);
					Data = new byte[length];
					raw.Read(Data, offset + sizeof(uint), length);
				}
			}

			public void RawWrite(MmfFileView raw, int offset)
			{
				unsafe
				{
					raw.WriteInt32(offset, Data.Length);
					raw.Write(Data, offset + sizeof(uint), Data.Length);
				}
			}

			#endregion
		}

        public static readonly uint SPLIT_THRESHOLD = (uint)Header.Size + 8;

		private View			_view;
		private Header			_hdr;
		private uint			_offset;

		public MemArena(View memBase) : this(memBase, 0)
		{
		}

		public MemArena(View memBase, uint offSet)
		{            
            _offset = offSet;
			_view = memBase;            
            ReadHeader();
		}

        
        /// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal Header ArenaHeader { get { return _hdr; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal View View { get { return _view; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal MmfFileView RawView { get { return _view.MmfView; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal uint Offset { get { return _offset; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal uint DataOffset { get { return (uint)(_offset + Header.Size); } }

		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		public bool HasNext { get { return _hdr.HasNext; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		public bool HasPrevious { get { return _hdr.HasPrevious; } }
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		public uint TotalLength { get { return (uint)(Capacity + Header.Size); } }

		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		public bool IsFree
		{
			get { return _hdr.IsFree; }
			set
			{
				_hdr.Status = value ? Header.FREE : Header.USED;
				RawView.WriteByte((int)_offset, _hdr.Status);
			}
		}
		
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		public unsafe uint Capacity
		{
			get { return _hdr.Capacity; }
			set
			{
				_hdr.Capacity = value;
				RawView.WriteUInt32((int)_offset + sizeof(byte), _hdr.Capacity);
			}
		}
	
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal unsafe uint OffsetNext
		{
			get { return _hdr.OffsetNext; }
			set
			{
				_hdr.OffsetNext = value;
				RawView.WriteUInt32((int)_offset + (sizeof(byte) + sizeof(uint)), _hdr.OffsetNext);
			}
		}
		/// <summary> Gets/Sets the length of the data stored in the block. </summary>
		internal unsafe uint OffsetPrev
		{
			get { return _hdr.OffsetPrev; }
			set
			{        
                _hdr.OffsetPrev = value;
				RawView.WriteUInt32((int)_offset + (sizeof(byte) + 2 * sizeof(uint)), _hdr.OffsetPrev);
			}
		}	

		public bool HasDataSpace(uint len)
		{
			return Capacity >= MemArena.Content.RequiredSpace(len);
		}

		public MemArena NextArena()
		{
			if (HasNext)
				return new MemArena(_view, OffsetNext);
			return null;
		}

		public MemArena PreviousArena()
		{
			if (HasPrevious)
				return new MemArena(_view, OffsetPrev);
			return null;
		}

		public byte[] GetMemContents()
		{
			Content item = new Content();
			item.RawRead(RawView, (int)DataOffset);
			return item.Data;
		}

		public bool SetMemContents(byte[] value)
		{
			// capacity minus data-length minus data            
            int diff = (int)(Capacity - Content.RequiredSpace((uint)value.Length));
			if (diff >= 0)
			{
				Content item = new Content();
				item.Data = value;
				item.RawWrite(RawView, (int)DataOffset);

				if (diff > SPLIT_THRESHOLD)
				{                    
                    MemArena newArena = SplitArena(this, Content.RequiredSpace((uint)value.Length));
                    if (newArena != this) 
                        newArena.IsFree = true;
				}

				return true;
			}
			return false;
		}


		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append(" - Arena, ").Append(IsFree ? "-" : "X")
				.Append(", Cap.=").Append(Capacity)
				.Append(", Off.=").Append(Offset)
				.Append(", N=").Append(OffsetNext)
				.Append(", P=").Append(OffsetPrev);
			return b.ToString();
		}

		/// <summary>
		/// Returns true if the specified arenas are adjacent in the view.
		/// </summary>
		private static bool AreAdjacent(MemArena arena1, MemArena arena2)
		{
			return arena1.OffsetNext == arena2.Offset || 
				arena2.OffsetNext == arena1.Offset;
		}

		/// <summary>
		/// Splits an arena into two adjacent arenas. Size of the first arena is equal
		/// to the size parameter. The second arenas occupies rest of the size of the 
		/// parent arena.
		/// </summary>
		internal static MemArena SplitArena(MemArena arena, uint size)
		{
            uint sizeWithHeader = (uint) (size + Header.Size);
            if (arena.OffsetNext != 0) //Quick and dirty...
                arena.Capacity = (uint)(arena.OffsetNext - (arena.Offset + Header.Size));
            
            if (sizeWithHeader > arena.Capacity) //size is replaced by sizeWithHeader...
				return null;      
            
            uint remainingCapacity = (uint)(arena.Capacity - size - Header.Size); //if value is negative .uint gived garbage. Reason for above change.
            
            
            // Check if the remaining space will be useful at all!
			// if not then there is no need to split and use the whole
			// arena instead.
			if (remainingCapacity < MemArena.SPLIT_THRESHOLD)
			{
                arena.View.MyFreeSpace -= (uint)(arena.Capacity + Header.Size);
                return arena;
			}

			// Reduce parent arena's capacity.
			arena.Capacity = size;
            
            MemArena newArena = arena.View.ArenaAtOffset(arena.Offset + arena.TotalLength);
			newArena.Capacity = remainingCapacity;
            arena.View.MyFreeSpace -= (sizeWithHeader);
            
            // Fix up links!

            MemArena tempArena = arena.NextArena();
            SetNextArena(newArena, GetActualArena(tempArena));
            SetNextArena(arena, newArena);			
            return newArena;
		}

		/// <summary>
		/// Coalesces/Merges all the adjacent FREE arenas into one arena. 
		/// </summary>
		internal static MemArena CoalesceAdjacent(MemArena arena)
		{
			// Only free arenas can be coalesced.
			if (!arena.IsFree) return arena;

			// Check if there is an arena next to this one.
            MemArena curr = arena; 
            MemArena next = arena.NextArena();
            int nHeaderCount = 0;//need to know how many headers are being freed while merging free arenas.
            if (next != null)
			{
				uint freedSpace = 0;
				// Find the first USED arena below this arena in the view. 
				// Skip/Merge all the free arenas along the way.
				while (next!= null && next.IsFree)
                {
                        nHeaderCount++;
                        freedSpace += next.TotalLength;
                        curr = next;
                        next = next.NextArena();
				}

				// Fix up links!
				if (freedSpace > 0)
				{
					arena.Capacity += freedSpace;                    
                    uint nCapacity = arena.Capacity;
                    arena = SetNextArena(arena, GetActualArena(next)); //update only the actual arena 
                    arena.Capacity = nCapacity;
				}
			}

			if (arena.HasPrevious)
			{
				uint freedSpace = 0;
				// Find the first USED arena above this arena in the view.
				// Skip/Merge all the free arenas along the way.
                MemArena cur = arena;
                MemArena prev = null;
                do
				{
					prev = cur.PreviousArena();
					if (prev == null || !prev.IsFree)
						break;

					freedSpace += prev.TotalLength;
					cur = prev;
                    nHeaderCount++;
				} 
				while (true);

				// Fix up links! For even if we find previous arena free.. The last from previous is now our Pivot.Its call to adjust its Next then.
				if (freedSpace > 0)
				{
					arena.Capacity += freedSpace;                    
                    uint nCapacity = arena.Capacity;
                    MemArena tempNext = arena.NextArena();
                    arena = SetNextArena(cur, GetActualArena(next)); //update only the actual arena
                    arena.Capacity = nCapacity;
				}
			}
            if (nHeaderCount > 0) //update memory freed, while merging arenas.
                arena.View.MyFreeSpace += (uint)(nHeaderCount * Header.Size);
            
            return arena;
		}

		/// <summary>
		/// Swaps two arenas that are adjacent in view.
		/// </summary>
		internal static void SwapAdjacent(MemArena arena1, MemArena arena2)
		{
            arena1 = GetActualArena(arena1);
            arena2 = GetActualArena(arena2);            
            
            // Only support swaping of adjacent aenas.            
            if (!AreAdjacent(arena1, arena2))
				return;

			// Determine arena order and cache settings!
			MemArena arenaLo = arena1, arenaHi = arena2;
			if (arena1.Offset > arena2.Offset)
			{
				arenaLo = arena2;
				arenaHi = arena1;
			}

			Header hdrLo = arenaLo.ArenaHeader;
			Header hdrHi = arenaHi.ArenaHeader;

			MemArena arenaHiNext = arenaHi.NextArena();
			MemArena arenaLoPrev = arenaLo.PreviousArena();
            
            arenaHiNext = GetActualArena(arenaHiNext);            
            arenaLoPrev = GetActualArena(arenaLoPrev);            
            
            // Swap memory contents of the arenas including headers!
			int loMemNeeded = arenaLo.IsFree ? Header.Size : (int)arenaLo.TotalLength;
			int hiMemNeeded = arenaHi.IsFree ? Header.Size : (int)arenaHi.TotalLength;

			byte[] arenaLoMem = arenaLo.RawView.Read((int)arenaLo.Offset, (int)loMemNeeded);
			byte[] arenaHiMem = arenaHi.RawView.Read((int)arenaHi.Offset, (int)hiMemNeeded);

			arenaHi._offset = arenaLo.Offset + arenaHi.TotalLength;

			arenaLo.RawView.Write(arenaHiMem, (int)arenaLo.Offset, (int)arenaHiMem.Length);
			arenaHi.RawView.Write(arenaLoMem, (int)arenaHi.Offset, (int)arenaLoMem.Length);

			// Fix up links and restore new settings!
			arenaLo.IsFree = hdrHi.IsFree;
			arenaLo.Capacity = hdrHi.Capacity;
			arenaLo.OffsetNext = arenaHi.Offset;
			arenaLo.OffsetPrev = hdrLo.OffsetPrev;
			SetPreviousArena(arenaLo, arenaLoPrev);

			// Fix up links and restore new settings!
			arenaHi.IsFree = hdrLo.IsFree;
			arenaHi.Capacity = hdrLo.Capacity;
			arenaHi.OffsetNext = hdrHi.OffsetNext;
			arenaHi.OffsetPrev = arenaLo.Offset;
			SetNextArena(arenaHi, arenaHiNext);
		}

		private static MemArena SetNextArena(MemArena arena, MemArena arenaNext)
		{

            if (arenaNext != null) //avoid scenario when two identical arenas are there for addition. 
            {
                if (arenaNext == arena)
                    return arena;                
            }
            
            arena.OffsetNext = arenaNext == null ? 0:arenaNext.Offset;
			if(arenaNext!= null) 
				arenaNext.OffsetPrev = arena.Offset;

            return arena;
		}

		private static MemArena SetPreviousArena(MemArena arena, MemArena arenaPrev)
		{
            if (arenaPrev != null) //avoid scanario when two identical arenas are there for addition. 
            {
                if (arenaPrev == arena)
                    return arena;            
            }
            
            arena.OffsetPrev = arenaPrev == null ? 0 : arenaPrev.Offset;
			if (arenaPrev != null)
				arenaPrev.OffsetNext = arena.Offset;
            
            return arena;
		}

        /// <summary>
        /// Gets the primary allocated arena for the arena provided. Checks key and then  gets the original one.
        /// </summary>
        // To keep the HashTable synch with the MemArena changes, we need to apply changes  to only those objects that
        // are allocated primarily to be added into hash-table. This function returns the original allocated MemArena against a key
        // so that at time of MemArena updates _itemDict-->MmfObjectPtr-->MemArena is the one to be updated
        internal static MemArena GetActualArena(MemArena arenaCopy)
        {
            if (arenaCopy != null)
            {
                if (!arenaCopy.IsFree)
                {
                    byte[] data = arenaCopy.GetMemContents();
                    StoreItem item = StoreItem.FromBinary(data, arenaCopy.View.ParentStorageProvider.CacheContext);                    
                    MemArena arenAct = arenaCopy.View.ParentStorageProvider.GetMemArena(item.Key);                    
                    return (arenAct);
                }
                else
                    return arenaCopy;
            }
            return null;
        }


		#region /               -- IRawObject Members --              /

		private void ReadHeader()
		{            
            _hdr.RawRead(RawView, (int)_offset);
		}

		#endregion
	}
}

