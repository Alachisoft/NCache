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
using System.Collections.Generic;
using System.Text;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;
using Enyim.Caching.Memcached.Results.Helpers;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class GetOperation : BinarySingleItemOperation, IGetOperation
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(GetOperation));
		private CacheItem result;

		public GetOperation(string key) : base(key) { }

		protected override BinaryRequest Build()
		{
			var request = new BinaryRequest(OpCode.Get)
			{
				Key = this.Key,
				Cas = this.Cas
			};

			return request;
		}

		protected override IOperationResult ProcessResponse(BinaryResponse response)
		{
			var status = response.StatusCode;
			var result = new BinaryOperationResult();

			this.StatusCode = status;

			if (status == 0)
			{
				int flags = BinaryConverter.DecodeInt32(response.Extra, 0);
				this.result = new CacheItem((ushort)flags, response.Data);
				this.Cas = response.CAS;

#if EVEN_MORE_LOGGING
				if (log.IsDebugEnabled)
					log.DebugFormat("Get succeeded for key '{0}'.", this.Key);
#endif	

				return result.Pass();
			}

			this.Cas = 0;

#if EVEN_MORE_LOGGING
			if (log.IsDebugEnabled)
				log.DebugFormat("Get failed for key '{0}'. Reason: {1}", this.Key, Encoding.ASCII.GetString(response.Data.Array, response.Data.Offset, response.Data.Count));
#endif

			var message = ResultHelper.ProcessResponseData(response.Data);
			return result.Fail(message);
		}

		CacheItem IGetOperation.Result
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
