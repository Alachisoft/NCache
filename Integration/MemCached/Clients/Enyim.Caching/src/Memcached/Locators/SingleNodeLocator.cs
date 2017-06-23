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

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// This is a simple node locator with no computation overhead, always returns the first server from the list. Use only in single server deployments.
	/// </summary>
	public sealed class SingleNodeLocator : IMemcachedNodeLocator
	{
		private IMemcachedNode node;
		private bool isInitialized;
		private object initLock = new Object();

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			if (this.isInitialized)
				throw new InvalidOperationException("Instance is already initialized.");

			// locking on this is rude but easy
			lock (initLock)
			{
				if (this.isInitialized)
					throw new InvalidOperationException("Instance is already initialized.");

				if (nodes.Count > 0)
					node = nodes[0];

				this.isInitialized = true;
			}
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (!this.isInitialized)
				throw new InvalidOperationException("You must call Initialize first");

			return this.node.IsAlive
					? this.node
					: null;
		}

		IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
		{
			return this.node.IsAlive
					? new IMemcachedNode[] { this.node }
					: Enumerable.Empty<IMemcachedNode>();
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
