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
using System.Linq;
using System.Text;

namespace Enyim.Caching.Memcached.Results
{

	public interface IOperationResult
	{

		/// <summary>
		/// A value indicating whether an operation was successful
		/// </summary>
		bool Success { get; set; }

		/// <summary>
		/// A message indicating success, warning or failure reason for an operation
		/// </summary>
		string Message { get; set; }

		/// <summary>
		/// An exception that caused a failure
		/// </summary>
		Exception Exception { get; set; }

		/// <summary>
		/// The StatusCode returned from the server
		/// </summary>
		int? StatusCode { get; set; }

		/// <summary>
		/// A result that influenced the current result
		/// </summary>
		IOperationResult InnerResult { get; set; }

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
