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

namespace Alachisoft.NCache.Runtime
{
	/// <summary>
	/// Specifies the relative priority of items stored in the <see cref="Cache"/>.
	/// </summary>
	/// <remarks>
	/// When the application's cache is full or runs low on memory, the <see cref="Cache"/> selectively purges 
	/// items to free system memory. When an item is added to the <see cref="Cache"/>, you can assign it a 
	/// relative priority compared to the other items stored in the <see cref="Cache"/>. Items you assign higher 
	/// priority values to are less likely to be deleted from the <see cref="Cache"/> when the server is processing 
	/// a large number of requests, while items you assign lower priority values are more likely to be 
	/// deleted. The default is <see cref="CacheItemPriority.Normal"/>.
	/// </remarks>
	/// <example>The following example demonstrates how to insert an item in a <see cref="Cache"/> object with a 
	/// <see cref="CacheItemPriority"/> of <see cref="CacheItemPriority.High"/>.
	/// <code>
	/// 
	///	NCache.Cache.Insert("DSN", connectionString, null, d, t, CacheItemPriority.High, onRemove);
	///	Cache.Insert("DSN", connectionString, null, d, t, CacheItemPriority.High, onRemove);
	/// 
	/// </code>
	/// </example>
	/// <requirements>
	/// <constraint>This member is not available in SessionState edition.</constraint> 
	/// </requirements>
	[Serializable]
	public enum CacheItemPriority
	{
        /// <summary>
		/// Cache items with this priority level are the most likely to be deleted 
		/// from the cache as the server frees system memory.
		/// </summary>
		Low = -2,

		/// <summary>
		/// Cache items with this priority level are more likely to be deleted from 
		/// the cache as the server frees system memory than items assigned a Normal priority.
		/// </summary>
		BelowNormal = -1,

		/// <summary>
		/// Cache items with this priority level are likely to be deleted from the 
		/// cache as the server frees system memory only after those items with Low 
		/// or BelowNormal priority. This is the default.
		/// </summary>
		Normal = 0,

		/// <summary>
		/// Cache items with this priority level are less likely to be deleted as 
		/// the server frees system memory than those assigned a Normal priority.
		/// </summary>
		AboveNormal = 1,

		/// <summary>
		/// Cache items with this priority level are the least likely to be deleted 
		/// from the cache as the server frees system memory.
		/// </summary>
		High = 2,

		/// <summary>
		/// The cache items with this priority level will not be deleted from the 
		/// cache as the server frees system memory.
		/// </summary>
		NotRemovable = 3,

        /// <summary>
        /// The default value for a cached item's priority is <see cref="Normal"/>.
        /// </summary>
        Default = 4
	}
}
