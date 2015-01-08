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

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Inidicates the mode how the items are stored in Memcached.
	/// </summary>
	public enum StoreMode
	{
		/// <summary>
		/// Store the data, but only if the server does not already hold data for a given key
		/// </summary>
		Add = 1,
		/// <summary>
		/// Store the data, but only if the server does already hold data for a given key
		/// </summary>
		Replace,
		/// <summary>
		/// Store the data, overwrite if already exist
		/// </summary>
		Set
	};

	internal enum StoreCommand
	{
		/// <summary>
		/// Store the data, but only if the server does not already hold data for a given key
		/// </summary>
		Add = 1,
		/// <summary>
		/// Store the data, but only if the server does already hold data for a given key
		/// </summary>
		Replace,
		/// <summary>
		/// Store the data, overwrite if already exist
		/// </summary>
		Set,
		/// <summary>
		/// Appends the data to an existing key's data
		/// </summary>
		Append,
		/// <summary>
		/// Inserts the data before an existing key's data
		/// </summary>
		Prepend,
		/// <summary>
		/// Stores the data only if it has not been updated by someone else. Uses a "transaction id" to check for modification.
		/// </summary>
		CheckAndSet
	};
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
