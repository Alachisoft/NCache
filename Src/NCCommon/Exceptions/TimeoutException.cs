//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;

namespace Alachisoft.NCache.Common.Exceptions
{
	[Serializable]
	public class TimeoutException : System.Exception
	{
		public System.Collections.IList failed_mbrs = null; // members that failed responding
		
		public TimeoutException():base("TimeoutExeption")
		{
		}
		
		public TimeoutException(string msg):base(msg)
		{
		}
		
		public TimeoutException(System.Collections.IList failed_mbrs):base("TimeoutExeption")
		{
			this.failed_mbrs = failed_mbrs;
		}
		
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			
			sb.Append(base.ToString());
			
			if (failed_mbrs != null && failed_mbrs.Count > 0)
				sb.Append(" (failed members: ").Append(failed_mbrs);
			return sb.ToString();
		}
	}
}