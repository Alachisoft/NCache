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
using System.Text;
using Enyim.Caching.Memcached.Results;

namespace Enyim.Caching.Memcached.Protocol.Binary
{
	/// <summary>
	/// Implements append/prepend.
	/// </summary>
	public class ConcatOperation : BinarySingleItemOperation, IConcatOperation
	{
		private ArraySegment<byte> data;
		private ConcatenationMode mode;

		public ConcatOperation(ConcatenationMode mode, string key, ArraySegment<byte> data)
			: base(key)
		{
			this.data = data;
			this.mode = mode;
		}

		protected override BinaryRequest Build()
		{
			var request = new BinaryRequest((OpCode)this.mode)
			{
				Key = this.Key,
				Cas = this.Cas,
				Data = this.data
			};

			return request;
		}

		protected override IOperationResult ProcessResponse(BinaryResponse response)
		{
			return new BinaryOperationResult() { Success = true };
		}

		ConcatenationMode IConcatOperation.Mode
		{
			get { return this.mode; }
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
