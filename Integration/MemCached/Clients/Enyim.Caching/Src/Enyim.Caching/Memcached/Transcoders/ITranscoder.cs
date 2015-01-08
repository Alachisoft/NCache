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
	/// <summary>
	/// Provides an interface for serializing items for Memcached.
	/// </summary>
	public interface ITranscoder
	{
		/// <summary>
		/// Serializes an object for storing in the cache.
		/// </summary>
		/// <param name="value">The object to serialize</param>
		/// <returns>The serialized object</returns>
		CacheItem Serialize(object value);

		/// <summary>
		/// Deserializes the <see cref="T:CacheItem"/> into an object.
		/// </summary>
		/// <param name="item">The stream that contains the data to deserialize.</param>
		/// <returns>The deserialized object</returns>
		object Deserialize(CacheItem item);
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
