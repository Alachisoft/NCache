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

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represent a stat item returned by Memcached.
	/// </summary>
	public enum StatItem : int
	{
		/// <summary>
		/// The number of seconds the server has been running.
		/// </summary>
		Uptime = 0,
		/// <summary>
		/// Current time according to the server.
		/// </summary>
		ServerTime,
		/// <summary>
		/// The version of the server.
		/// </summary>
		Version,
		/// <summary>
		/// The number of items stored by the server.
		/// </summary>
		ItemCount,
		/// <summary>
		/// The total number of items stored by the server including the ones whihc have been already evicted.
		/// </summary>
		TotalItems,
		/// <summary>
		/// Number of active connections to the server.
		/// </summary>
		ConnectionCount,
		/// <summary>
		/// The total number of connections ever made to the server.
		/// </summary>
		TotalConnections,
		/// <summary>
		/// ?
		/// </summary>
		ConnectionStructures,

		/// <summary>
		/// Number of get operations performed by the server.
		/// </summary>
		GetCount,
		/// <summary>
		/// Number of set operations performed by the server.
		/// </summary>
		SetCount,
		/// <summary>
		/// Cache hit.
		/// </summary>
		GetHits,
		/// <summary>
		/// Cache miss.
		/// </summary>
		GetMisses,

		/// <summary>
		/// ?
		/// </summary>
		UsedBytes,
		/// <summary>
		/// Number of bytes read from the server.
		/// </summary>
		BytesRead,
		/// <summary>
		/// Number of bytes written to the server.
		/// </summary>
		BytesWritten,
		/// <summary>
		/// ?
		/// </summary>
		MaxBytes
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
