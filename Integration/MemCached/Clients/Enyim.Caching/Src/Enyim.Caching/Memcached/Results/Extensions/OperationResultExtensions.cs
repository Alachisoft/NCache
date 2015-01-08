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

namespace Enyim.Caching.Memcached.Results.Extensions
{
	public static class OperationResultExtensions
	{
		/// <summary>
		/// Set the result Success to false
		/// </summary>
		/// <param name="source">Result to update</param>
		/// <param name="message">Message indicating source of failure</param>
		/// <param name="ex">Exception causing failure</param>
		/// <returns>Updated source</returns>
		public static IOperationResult Fail(this IOperationResult source, string message, Exception ex = null)
		{
			source.Success = false;
			source.Message = message;
			source.Exception = ex;
			return source;
		}

		/// <summary>
		/// Set the result Success to true
		/// </summary>
		/// <param name="source">Result to update</param>
		/// <param name="message">Message indicating a possible warning</param>
		/// <returns>Updated source</returns>
		public static IOperationResult Pass(this IOperationResult source, string message = null)
		{
			source.Success = true;
			source.Message = message;
			return source;
		}

		/// <summary>
		/// Copy properties from one IOperationResult to another.  Does not use reflection.
		/// Ony LCD properties are copied
		/// </summary>
		/// <param name="target"></param>
		public static void Copy(this IOperationResult source, IOperationResult target)
		{
			target.Message = source.Message;
			target.Success = source.Success;
			target.Exception = source.Exception;
			target.StatusCode = source.StatusCode;
		}

		/// <summary>
		/// Copy properties from one IOperationResult to another.  Does not use reflection.
		/// Ony LCD properties are copied
		/// </summary>
		/// <param name="target"></param>
		public static IOperationResult PassOrFail(this IOperationResult source, bool success, string message = "", Exception ex = null)
		{
			return success ? Pass(source) : Fail(source, message, ex);
		}

		/// <summary>
		/// Combine will attempt to minimize the depth of InnerResults and maintain status codes
		/// </summary>
		/// <param name="target"></param>
		public static void Combine(this IOperationResult source, IOperationResult target)
		{
			target.Message = source.Message;
			target.Success = source.Success;
			target.Exception = source.Exception;
			target.StatusCode = source.StatusCode ?? target.StatusCode;
			target.InnerResult = source.InnerResult ?? source;
		}
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
