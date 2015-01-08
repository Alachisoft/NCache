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

namespace Enyim
{
	/// <summary>
	/// Combines multiple hash codes into one.
	/// </summary>
	public class HashCodeCombiner
	{
		private int currentHash;

		public HashCodeCombiner() : this(0x1505) { }

		public HashCodeCombiner(int initialValue)
		{
			this.currentHash = initialValue;
		}

		public static int Combine(int code1, int code2)
		{
			return ((code1 << 5) + code1) ^ code2;
		}

		public void Add(int value)
		{
			this.currentHash = HashCodeCombiner.Combine(this.currentHash, value);
		}

		public int CurrentHash
		{
			get { return this.currentHash; }
		}

		public static int Combine(int code1, int code2, int code3)
		{
			return HashCodeCombiner.Combine(HashCodeCombiner.Combine(code1, code2), code3);
		}

		public static int Combine(int code1, int code2, int code3, int code4)
		{
			return HashCodeCombiner.Combine(HashCodeCombiner.Combine(code1, code2), HashCodeCombiner.Combine(code3, code4));
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
