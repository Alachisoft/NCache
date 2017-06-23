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
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public abstract class BinarySingleItemOperation : SingleItemOperation
	{
		protected BinarySingleItemOperation(string key) : base(key) { }

		protected abstract BinaryRequest Build();

		protected internal override IList<ArraySegment<byte>> GetBuffer()
		{
			return this.Build().CreateBuffer();
		}

		protected abstract IOperationResult ProcessResponse(BinaryResponse response);

		protected internal override IOperationResult ReadResponse(PooledSocket socket)
		{
			var response = new BinaryResponse();
			var retval = response.Read(socket);
			
			this.Cas = response.CAS;
			this.StatusCode = response.StatusCode;

			var result = new BinaryOperationResult()
			{
				Success = retval,
				Cas = this.Cas,
				StatusCode = this.StatusCode
			};

			IOperationResult responseResult;
			if (! (responseResult = this.ProcessResponse(response)).Success)
			{
				result.InnerResult = responseResult;
				responseResult.Combine(result);
			}
			
			return result;
		}

		protected internal override bool ReadResponseAsync(PooledSocket socket, Action<bool> next)
		{
			throw new NotImplementedException();
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
