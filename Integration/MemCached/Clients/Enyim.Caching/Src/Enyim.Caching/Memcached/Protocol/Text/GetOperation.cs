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
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class GetOperation : SingleItemOperation, IGetOperation
	{
		private CacheItem result;

		internal GetOperation(string key) : base(key) { }

		protected internal override System.Collections.Generic.IList<System.ArraySegment<byte>> GetBuffer()
		{
			var command = "gets " + this.Key + TextSocketHelper.CommandTerminator;

			return TextSocketHelper.GetCommandBuffer(command);
		}

		protected internal override IOperationResult ReadResponse(PooledSocket socket)
		{
			GetResponse r = GetHelper.ReadItem(socket);
			var result = new TextOperationResult();

			if (r == null) return result.Fail("Failed to read response");

			this.result = r.Item;
			this.Cas = r.CasValue;

			GetHelper.FinishCurrent(socket);

			return result.Pass();
		}

		CacheItem IGetOperation.Result
		{
			get { return this.result; }
		}

		protected internal override bool ReadResponseAsync(PooledSocket socket, System.Action<bool> next)
		{
			throw new System.NotSupportedException();
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
