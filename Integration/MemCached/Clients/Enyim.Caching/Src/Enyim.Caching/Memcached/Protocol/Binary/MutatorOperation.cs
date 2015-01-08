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
using Enyim.Caching.Memcached.Results.Extensions;
using Enyim.Caching.Memcached.Results.Helpers;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class MutatorOperation : BinarySingleItemOperation, IMutatorOperation
	{
		private ulong defaultValue;
		private ulong delta;
		private uint expires;
		private MutationMode mode;
		private ulong result;

		public MutatorOperation(MutationMode mode, string key, ulong defaultValue, ulong delta, uint expires)
			: base(key)
		{
			if (delta < 0) throw new ArgumentOutOfRangeException("delta", "delta must be >= 0");

			this.defaultValue = defaultValue;
			this.delta = delta;
			this.expires = expires;
			this.mode = mode;
		}

		protected unsafe void UpdateExtra(BinaryRequest request)
		{
			byte[] extra = new byte[20];

			fixed (byte* buffer = extra)
			{
				BinaryConverter.EncodeUInt64(this.delta, buffer, 0);

				BinaryConverter.EncodeUInt64(this.defaultValue, buffer, 8);
				BinaryConverter.EncodeUInt32(this.expires, buffer, 16);
			}

			request.Extra = new ArraySegment<byte>(extra);
		}

		protected override BinaryRequest Build()
		{
			var request = new BinaryRequest((OpCode)this.mode)
			{
				Key = this.Key,
				Cas = this.Cas
			};

			this.UpdateExtra(request);

			return request;
		}

		protected override IOperationResult ProcessResponse(BinaryResponse response)
		{
			var result = new BinaryOperationResult();
			var status = response.StatusCode;
			this.StatusCode = status;

			if (status == 0)
			{
				var data = response.Data;
				if (data.Count != 8)
					return result.Fail("Result must be 8 bytes long, received: " + data.Count, new InvalidOperationException());

				this.result = BinaryConverter.DecodeUInt64(data.Array, data.Offset);

				return result.Pass();
			}

			var message = ResultHelper.ProcessResponseData(response.Data);
			return result.Fail(message);
		}

		MutationMode IMutatorOperation.Mode
		{
			get { return this.mode; }
		}

		ulong IMutatorOperation.Result
		{
			get { return this.result; }
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
