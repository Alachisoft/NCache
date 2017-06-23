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
using System.Collections.Generic;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Defines a locator class which maps item keys to memcached servers.
	/// </summary>
	public interface IMemcachedNodeLocator
	{
		/// <summary>
		/// Initializes the locator.
		/// </summary>
		/// <param name="nodes">The memcached nodes defined in the configuration.</param>
		/// <remarks>This called first when the server pool is initialized, and subsequently every time 
		/// when a node goes down or comes back. If your locator has its own logic to deal with dead nodes 
		/// then ignore all calls but the first. Otherwise make sure that your implementation can handle 
		/// simultaneous calls to Initialize and Locate in a thread safe manner.</remarks>
		/// <seealso cref="T:DefaultNodeLocator"/>
		/// <seealso cref="T:KetamaNodeLocator"/>
		void Initialize(IList<IMemcachedNode> nodes);

		/// <summary>
		/// Returns the memcached node the specified key belongs to.
		/// </summary>
		/// <param name="key">The key of the item to be located.</param>
		/// <returns>The <see cref="T:MemcachedNode"/> the specifed item belongs to</returns>
		IMemcachedNode Locate(string key);

		/// <summary>
		/// Returns all the working nodes currently available to the locator.
		/// </summary>
		/// <remarks>It should return an instance which is safe to enumerate multiple times and provides the same results every time.</remarks>
		/// <returns></returns>
		IEnumerable<IMemcachedNode> GetWorkingNodes();
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
