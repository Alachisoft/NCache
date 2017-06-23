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
	///	This is Jenkin's "One at A time Hash".
	///	http://en.wikipedia.org/wiki/Jenkins_hash_function
	/// 
	///	Coming from libhashkit.
	/// </summary>
	/// <remarks>Does not support block based hashing.</remarks>
	internal class HashkitOneAtATime : HashAlgorithm, IUIntHashAlgorithm
	{
		public HashkitOneAtATime()
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

			HashkitOneAtATime.UnsafeHashCore(array, ibStart, cbSize);
		}

		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.CurrentHash);
		}

		public uint CurrentHash { get; private set; }

		#region [ UnsafeHashCore               ]

		// see the murmur hash about stream support
		private static unsafe uint UnsafeHashCore(byte[] data, int offset, int count)
		{
			uint hash = 0;

			fixed (byte* start = &(data[offset]))
			{
				var ptr = start;

				while (count > 0)
				{
					hash += *ptr;
					hash += (hash << 10);
					hash ^= (hash >> 6);

					count--;
					ptr++;
				}
			}

			hash += (hash << 3);
			hash ^= (hash >> 11);
			hash += (hash << 15);

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
