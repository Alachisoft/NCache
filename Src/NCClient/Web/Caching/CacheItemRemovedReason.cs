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

namespace Alachisoft.NCache.Client
{
	/// <summary>
	/// Specifies the reason an item was removed from the <see cref="ICache"/>.
	/// </summary>
	/// <remarks>
	/// This enumeration works in concert with the <see cref="CacheItemRemovedCallback"/> delegate to 
	/// notify your applications when and why an object was removed from the <see cref="Cache"/>.</remarks>
	///<requirements>
	/// <constraint>This member is not available in SessionState edition.</constraint> 
	/// </requirements>
	[Serializable]
    public enum CacheItemRemovedReason
	{
		/// <summary>
		/// The item is removed from the cache because it expired.
		/// </summary>
		Expired,

		/// <summary>
		/// The item is removed from the cache by a <see cref="Cache.Remove"/> method call or by an 
		/// <see cref="o:Cache.Insert"/> method call that specified the same key.
		/// </summary>
		Removed,

		/// <summary>
		/// The item is removed from the cache because the system removed it to free memory.
		/// </summary>
		Underused
	}
}
