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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class MultiGetOperation : MultiItemOperation, IMultiGetOperation
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MultiGetOperation));

		private Dictionary<string, CacheItem> result;

		public MultiGetOperation(IList<string> keys) : base(keys) { }

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			// gets key1 key2 key3 ... keyN\r\n

			var command = "gets " + String.Join(" ", Keys.ToArray()) + TextSocketHelper.CommandTerminator;

			return TextSocketHelper.GetCommandBuffer(command);
		}

		protected internal override IOperationResult ReadResponse(PooledSocket socket)
		{
			var retval = new Dictionary<string, CacheItem>();
			var cas = new Dictionary<string, ulong>();

			try
			{
				GetResponse r;

				while ((r = GetHelper.ReadItem(socket)) != null)
				{
					var key = r.Key;

					retval[key] = r.Item;
					cas[key] = r.CasValue;
				}
			}
			catch (NotSupportedException)
			{
				throw;
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			this.result = retval;
			this.Cas = cas;

			return new TextOperationResult().Pass();
		}

		Dictionary<string, CacheItem> IMultiGetOperation.Result
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
