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
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching.Memcached.Protocol.Text
{
	public class StoreOperationBase : SingleItemOperation
	{
		private static readonly ArraySegment<byte> DataTerminator = new ArraySegment<byte>(new byte[2] { (byte)'\r', (byte)'\n' });
		private StoreCommand command;
		private CacheItem value;
		private uint expires;
		private ulong cas;

		internal StoreOperationBase(StoreCommand mode, string key, CacheItem value, uint expires, ulong cas)
			: base(key)
		{
			this.command = mode;
			this.value = value;
			this.expires = expires;
			this.cas = cas;
		}

		protected internal override System.Collections.Generic.IList<ArraySegment<byte>> GetBuffer()
		{
			// todo adjust the size to fit a request using a fnv hashed key
			var sb = new StringBuilder(128);
			var buffers = new List<ArraySegment<byte>>(3);

			switch (this.command)
			{
				case StoreCommand.Add: sb.Append("add "); break;
				case StoreCommand.Replace: sb.Append("replace "); break;
				case StoreCommand.Set: sb.Append("set "); break;
				case StoreCommand.Append: sb.Append("append "); break;
				case StoreCommand.Prepend: sb.Append("prepend "); break;
				case StoreCommand.CheckAndSet: sb.Append("cas "); break;
				default: throw new MemcachedClientException(command + " is not supported.");
			}

			sb.Append(this.Key);
			sb.Append(" ");
			sb.Append(this.value.Flags.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");
			sb.Append(this.expires.ToString(CultureInfo.InvariantCulture));
			sb.Append(" ");

			var data = this.value.Data;
			sb.Append(Convert.ToString(data.Count, CultureInfo.InvariantCulture));

			if (command == StoreCommand.CheckAndSet)
			{
				sb.Append(" ");
				sb.Append(Convert.ToString(this.cas, CultureInfo.InvariantCulture));
			}

			sb.Append(TextSocketHelper.CommandTerminator);

			TextSocketHelper.GetCommandBuffer(sb.ToString(), buffers);
			buffers.Add(data);
			buffers.Add(StoreOperationBase.DataTerminator);

			return buffers;
		}

		protected internal override IOperationResult ReadResponse(PooledSocket socket)
		{
			return new TextOperationResult
			{
				Success = String.Compare(TextSocketHelper.ReadResponse(socket), "STORED", StringComparison.Ordinal) == 0
			};
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
