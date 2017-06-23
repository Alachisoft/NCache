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

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class TextOperationFactory : IOperationFactory
	{
		IGetOperation IOperationFactory.Get(string key)
		{
			return new GetOperation(key);
		}

		IMultiGetOperation IOperationFactory.MultiGet(IList<string> keys)
		{
			return new MultiGetOperation(keys);
		}

		IStoreOperation IOperationFactory.Store(StoreMode mode, string key, CacheItem value, uint expires, ulong cas)
		{
			if (cas == 0)
				return new StoreOperation(mode, key, value, expires);

			return new CasOperation(key, value, expires, (uint)cas);
		}

		IDeleteOperation IOperationFactory.Delete(string key, ulong cas)
		{
			if (cas > 0) throw new NotSupportedException("Text protocol does not support delete with cas.");

			return new DeleteOperation(key);
		}

		IMutatorOperation IOperationFactory.Mutate(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires, ulong cas)
		{
			if (cas > 0) throw new NotSupportedException("Text protocol does not support " + mode + " with cas.");

			return new MutatorOperation(mode, key, delta);
		}

		IConcatOperation IOperationFactory.Concat(ConcatenationMode mode, string key, ulong cas, ArraySegment<byte> data)
		{
			if (cas > 0) throw new NotSupportedException("Text protocol does not support " + mode + " with cas.");

			return new ConcateOperation(mode, key, data);
		}

		IStatsOperation IOperationFactory.Stats(string type)
		{
			return new StatsOperation(type);
		}

		IFlushOperation IOperationFactory.Flush()
		{
			return new FlushOperation();
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
