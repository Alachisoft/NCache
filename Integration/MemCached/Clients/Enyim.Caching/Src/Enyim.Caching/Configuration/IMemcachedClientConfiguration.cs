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
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface IMemcachedClientConfiguration
	{
		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		IList<IPEndPoint> Servers { get; }

		/// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		ISocketPoolConfiguration SocketPool { get; }

		/// <summary>
		/// Gets the authentication settings.
		/// </summary>
		IAuthenticationConfiguration Authentication { get; }

		/// <summary>
		/// Creates an <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> instance which will be used to convert item keys for Memcached.
		/// </summary>
		IMemcachedKeyTransformer CreateKeyTransformer();

		/// <summary>
		/// Creates an <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> instance which will be used to assign items to Memcached nodes.
		/// </summary>
		IMemcachedNodeLocator CreateNodeLocator();

		/// <summary>
		/// Creates an <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> instance which will be used to serialize or deserialize items.
		/// </summary>
		ITranscoder CreateTranscoder();

        MemcachedProtocol CreateProtocol();

		IPerformanceMonitor CreatePerformanceMonitor();


        //string CacheName { get; }
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
