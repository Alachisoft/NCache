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
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching
{
	/// <summary>
	/// Interface for API methods that return detailed operation results
	/// </summary>
	public interface IMemcachedResultsClient
	{
		IGetOperationResult ExecuteGet(string key);
		IGetOperationResult<T> ExecuteGet<T>(string key);
		IDictionary<string, IGetOperationResult> ExecuteGet(IEnumerable<string> keys);

		IGetOperationResult ExecuteTryGet(string key, out object value);
		
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value);
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value, DateTime expiresAt);
		IStoreOperationResult ExecuteStore(StoreMode mode, string key, object value, TimeSpan validFor);

		IStoreOperationResult ExecuteCas(StoreMode mode, string key, object value);
		IStoreOperationResult ExecuteCas(StoreMode mode, string key, object value, ulong cas);
		IStoreOperationResult ExecuteCas(StoreMode mode, string key, object value, DateTime expiresAt, ulong cas);
		IStoreOperationResult ExecuteCas(StoreMode mode, string key, object value, TimeSpan validFor, ulong cas);

		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta);
		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt);
		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor);

		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta, ulong cas);
		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas);
		IMutateOperationResult ExecuteDecrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas);
		
		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta);
		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt);
		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor);

		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta, ulong cas);
		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas);
		IMutateOperationResult ExecuteIncrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas);

		IConcatOperationResult ExecuteAppend(string key, ArraySegment<byte> data);
		IConcatOperationResult ExecuteAppend(string key, ulong cas, ArraySegment<byte> data);
		
		IConcatOperationResult ExecutePrepend(string key, ArraySegment<byte> data);
		IConcatOperationResult ExecutePrepend(string key, ulong cas, ArraySegment<byte> data);

		IRemoveOperationResult ExecuteRemove(string key);
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
 *    @copyright 2012 Attila Kiskó, enyim.com
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
