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
using System;

namespace Enyim.Caching.Memcached
{
	public class DefaultKeyTransformer : KeyTransformerBase
	{
		static readonly char[] ForbiddenChars = { 
			'\u0000', '\u0001', '\u0002', '\u0003',
			'\u0004', '\u0005', '\u0006', '\u0007',
			'\u0008', '\u0009', '\u000a', '\u000b',
			'\u000c', '\u000d', '\u000e', '\u000f',
			'\u0010', '\u0011', '\u0012', '\u0013',
			'\u0014', '\u0015', '\u0016', '\u0017',
			'\u0018', '\u0019', '\u001a', '\u001b',
			'\u001c', '\u001d', '\u001e', '\u001f',
			'\u0020'
		};

		public override string Transform(string key)
		{
			if (key.IndexOfAny(ForbiddenChars) > -1)
				throw new ArgumentException("Keys cannot contain the chars 0x00-0x20 and space.");

			return key;
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
