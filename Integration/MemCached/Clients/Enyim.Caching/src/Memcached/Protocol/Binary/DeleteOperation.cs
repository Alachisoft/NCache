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
using System.Collections.Generic;
using System.Text;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;
using Enyim.Caching.Memcached.Results.Helpers;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	public class DeleteOperation : BinarySingleItemOperation, IDeleteOperation
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(DeleteOperation));
		public DeleteOperation(string key) : base(key) { }

		protected override BinaryRequest Build()
		{
			var request = new BinaryRequest(OpCode.Delete)
			{
				Key = this.Key,
				Cas = this.Cas
			};

			return request;
		}

		protected override IOperationResult ProcessResponse(BinaryResponse response)
		{
			var result = new BinaryOperationResult();
#if EVEN_MORE_LOGGING
			if (log.IsDebugEnabled)
				if (response.StatusCode == 0)
					log.DebugFormat("Delete succeeded for key '{0}'.", this.Key);
				else
					log.DebugFormat("Delete failed for key '{0}'. Reason: {1}", this.Key, Encoding.ASCII.GetString(response.Data.Array, response.Data.Offset, response.Data.Count));
#endif
			if (response.StatusCode == 0)
			{
				return result.Pass();
			}
			else
			{
				var message = ResultHelper.ProcessResponseData(response.Data);
				return result.Fail(message);
			}
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
