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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Enyim
{
	/// <summary>
	/// Murmur hash. Uses the same seed values as libhashkit.
	/// </summary>
	/// <remarks>Does not support block based hashing.</remarks>
	internal class HashkitMurmur : HashAlgorithm, IUIntHashAlgorithm
	{
		public HashkitMurmur()
		{
			this.HashSizeValue = 32;
		}

		public override bool CanTransformMultipleBlocks
		{
			get { return false; }
		}

		public override void Initialize() { }

		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			if (array == null) throw new ArgumentNullException("array");
			if (ibStart < 0 || ibStart > array.Length) throw new ArgumentOutOfRangeException("ibStart");
			if (ibStart + cbSize > array.Length) throw new ArgumentOutOfRangeException("cbSize");

			this.CurrentHash = HashkitMurmur.UnsafeHashCore(array, ibStart, cbSize);
		}

		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.CurrentHash);
		}

		public uint CurrentHash { get; private set; }

		#region [ UnsafeHashCore               ]

		// this could be rewritten to support streams tho
		// cache the tail of the buffer if its length is not mod4 then merge with the next buffer (this is a perf hit since we cannot do our pointer magics)
		// then the swicth and the last XORs could be moved into TransformFinal
		// -- or --
		// just cache tail and if we have a cache dvalue and the next block is not mod4 long then throw an exception (thus only allow random length blocks for the last one)
		static unsafe uint UnsafeHashCore(byte[] data, int offset, int length)
		{
			const uint M = 0x5bd1e995;
			const int R = 24;

			uint seed = (uint)(0xdeadbeef * length);
			uint hash = (uint)(seed ^ length);

			int count = length >> 2;

			fixed (byte* start = &(data[offset]))
			{
				uint* ptrUInt = (uint*)start;

				while (count > 0)
				{
					uint current = *ptrUInt;

					current = (uint)(current * M);
					current ^= current >> R;
					current = (uint)(current * M);
					hash = (uint)(hash * M);
					hash ^= current;

					count--;
					ptrUInt++;
				}

				switch (length & 3)
				{
					case 3:
						// reverse the last 3 bytes and convert it to an uint
						// so cast the last to into an UInt16 and get the 3rd as a byte
						// ABC --> CBA; (UInt16)(AB) --> BA
						//h ^= (uint)(*ptrByte);
						//h ^= (uint)(ptrByte[1] << 8);
						hash ^= (*(UInt16*)ptrUInt);
						hash ^= (uint)(((byte*)ptrUInt)[2] << 16);
						hash *= M;
						break;

					case 2:
						hash ^= (*(UInt16*)ptrUInt);
						hash *= M;
						break;

					case 1:
						hash ^= (*((byte*)ptrUInt));
						hash *= M;
						break;
				}
			}

			hash ^= hash >> 13;
			hash *= M;
			hash ^= hash >> 15;

			return hash;
		}
		#endregion
		#region [ IUIntHash                    ]

		uint IUIntHashAlgorithm.ComputeHash(byte[] data)
		{
			this.Initialize();

			this.HashCore(data, 0, data.Length);

			return this.CurrentHash;
		}

		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
