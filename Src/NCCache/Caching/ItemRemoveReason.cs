//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;

namespace Alachisoft.NCache.Caching
{
	/// <summary>
	/// Enumeration that defines the different reasons for item eviction.
	/// </summary>
	[Serializable]
	public enum ItemRemoveReason
	{
		/// <summary>
		/// The item is removed from the cache because a file or key dependency changed.
		/// </summary>
		DependencyChanged,

		/// <summary>
		/// The item is removed from the cache because it expired.
		/// </summary>
		Expired,

		/// <summary>
		/// The item is removed from the cache by a Remove method call or by an Insert method call that specified the same key.
		/// </summary>
		Removed,

		/// <summary>
		/// The item is removed from the cache because the system removed it to free memory.
		/// </summary>
		Underused,

        /// <summary>
        /// The item is removed from the cache because of sql invalid dependency.
        /// </summary>
        DependencyInvalid
	}
}
