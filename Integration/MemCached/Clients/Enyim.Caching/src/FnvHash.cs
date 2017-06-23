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
using System.Security.Cryptography;

namespace Enyim
{
	/// <summary>
	/// Implements a 64 bit long FNV1 hash.
	/// </summary>
	/// <remarks>
	/// Calculation found at http://lists.danga.com/pipermail/memcached/2007-April/003846.html, but 
	/// it is pretty much available everywhere
	/// </remarks>
	public class FNV64 : System.Security.Cryptography.HashAlgorithm, IUIntHashAlgorithm
	{
		protected const ulong Init = 0xcbf29ce484222325L;
		protected const ulong Prime = 0x100000001b3L;

		protected ulong CurrentHashValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FNV64"/> class.
		/// </summary>
		public FNV64()
		{
			base.HashSizeValue = 64;
		}

		/// <summary>
		/// Initializes an instance of <see cref="T:FNV64"/>.
		/// </summary>
		public override void Initialize()
		{
			this.CurrentHashValue = Init;
		}

		/// <summary>Routes data written to the object into the <see cref="T:FNV64" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.CurrentHashValue *= Prime;
				this.CurrentHashValue ^= array[i];
			}
		}

		/// <summary>
		/// Returns the computed <see cref="T:FNV64" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.CurrentHashValue);
		}

		#region [ IUIntHashAlgorithm           ]

		uint IUIntHashAlgorithm.ComputeHash(byte[] data)
		{
			this.Initialize();
			this.HashCore(data, 0, data.Length);

			return (uint)this.CurrentHashValue;
		}

		#endregion
	}

	/// <summary>
	/// Implements a 64 bit long FVNV1a hash.
	/// </summary>
	public sealed class FNV64a : FNV64
	{
		/// <summary>Routes data written to the object into the <see cref="T:FNV64" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.CurrentHashValue ^= array[i];
				this.CurrentHashValue *= Prime;
			}
		}
	}

	/// <summary>
	/// Implements an FNV1 hash algorithm.
	/// </summary>
	public class FNV1 : HashAlgorithm, IUIntHashAlgorithm
	{
		protected const uint Prime = 16777619;
		protected const uint Init = 2166136261;

		/// <summary>
		/// The current hash value.
		/// </summary>
		protected uint CurrentHashValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FNV1a"/> class.
		/// </summary>
		public FNV1()
		{
			this.HashSizeValue = 32;
		}

		/// <summary>
		/// Initializes an instance of <see cref="T:FNV1a"/>.
		/// </summary>
		public override void Initialize()
		{
			this.CurrentHashValue = Init;
		}

		/// <summary>Routes data written to the object into the <see cref="T:FNV1a" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.CurrentHashValue *= FNV1.Prime;
				this.CurrentHashValue ^= array[i];
			}
		}

		/// <summary>
		/// Returns the computed <see cref="T:FNV1a" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			return BitConverter.GetBytes(this.CurrentHashValue);
		}

		#region [ IUIntHashAlgorithm           ]

		uint IUIntHashAlgorithm.ComputeHash(byte[] data)
		{
			this.Initialize();
			this.HashCore(data, 0, data.Length);

			return this.CurrentHashValue;
		}

		#endregion
	}

	/// <summary>
	/// Implements an FNV1a hash algorithm.
	/// </summary>
	public class FNV1a : FNV1
	{
		/// <summary>Routes data written to the object into the <see cref="T:FNV1a" /> hash algorithm for computing the hash.</summary>
		/// <param name="array">The input data. </param>
		/// <param name="ibStart">The offset into the byte array from which to begin using data. </param>
		/// <param name="cbSize">The number of bytes in the array to use as data. </param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			int end = ibStart + cbSize;

			for (int i = ibStart; i < end; i++)
			{
				this.CurrentHashValue ^= array[i];
				this.CurrentHashValue *= FNV1.Prime;
			}
		}
	}

	/// <summary>
	/// Implements a modified FNV hash. Provides better distribution than FNV1 but it's only 32 bit long.
	/// </summary>
	/// <remarks>Algorithm found at http://bretm.home.comcast.net/hash/6.html</remarks>
	public class ModifiedFNV : FNV1a
	{
		/// <summary>
		/// Returns the computed <see cref="T:ModifiedFNV" /> hash value after all data has been written to the object.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		protected override byte[] HashFinal()
		{
			this.CurrentHashValue += this.CurrentHashValue << 13;
			this.CurrentHashValue ^= this.CurrentHashValue >> 7;
			this.CurrentHashValue += this.CurrentHashValue << 3;
			this.CurrentHashValue ^= this.CurrentHashValue >> 17;
			this.CurrentHashValue += this.CurrentHashValue << 5;

			return base.HashFinal();
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
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
