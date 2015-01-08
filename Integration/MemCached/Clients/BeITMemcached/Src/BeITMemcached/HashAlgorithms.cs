// Copyright (c) 2015 Alachisoft
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
//Copyright (c) 2007-2008 Henrik Schr�der, Oliver Kofoed Pedersen

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Security.Cryptography;

namespace BeIT.MemCached
{
	/// <summary>
	/// Fowler-Noll-Vo hash, variant 1, 32-bit version.
	/// http://www.isthe.com/chongo/tech/comp/fnv/
	/// </summary>
	public class FNV1_32 : HashAlgorithm
	{
		private static readonly uint FNV_prime = 16777619;
		private static readonly uint offset_basis = 2166136261;

		protected uint hash;

		public FNV1_32() {
			HashSizeValue = 32;
		}

		public override void Initialize() {
			hash = offset_basis;
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			int length = ibStart + cbSize;
			for (int i = ibStart; i < length; i++) {
				hash = (hash * FNV_prime)^array[i];
			}
		}

		protected override byte[] HashFinal() {
			return BitConverter.GetBytes(hash);
		}
	}

	/// <summary>
	/// Fowler-Noll-Vo hash, variant 1a, 32-bit version.
	/// http://www.isthe.com/chongo/tech/comp/fnv/
	/// </summary>
	public class FNV1a_32 : HashAlgorithm
	{
		private static readonly uint FNV_prime = 16777619;
		private static readonly uint offset_basis = 2166136261;

		protected uint hash;

		public FNV1a_32() {
			HashSizeValue = 32;
		}

		public override void Initialize() {
			hash = offset_basis;
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			int length = ibStart + cbSize;
			for (int i = ibStart; i < length; i++) {
				hash = (hash^array[i]) * FNV_prime;
			}
		}

		protected override byte[] HashFinal() {
			return BitConverter.GetBytes(hash);
		}
	}

	/// <summary>
	/// Modified Fowler-Noll-Vo hash, 32-bit version.
	/// http://home.comcast.net/~bretm/hash/6.html
	/// </summary>
	public class ModifiedFNV1_32 : FNV1_32
	{
		protected override byte[] HashFinal() {
			hash += hash << 13;
			hash ^= hash >> 7;
			hash += hash << 3;
			hash ^= hash >> 17;
			hash += hash << 5;
			return BitConverter.GetBytes(hash);
		}
	}
}
