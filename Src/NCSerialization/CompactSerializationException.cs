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
using System.Runtime.Serialization;

namespace Alachisoft.NCache.Serialization
{
	/// <summary>
	/// Summary description for SerializationException.
	/// </summary>
	[Serializable]
	public class CompactSerializationException: Exception
	{
		public CompactSerializationException()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		/// <summary>
		/// Special constructor used by Run serialization to contstruct the object.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public CompactSerializationException(SerializationInfo info,StreamingContext context): base(info,context)
		{
			//
			// TODO: Add constructor logic here
			//
		}
		public CompactSerializationException(string message):base(message)
		{
			//
			// TODO: Add constructor logic here
			//
		}

        public CompactSerializationException(string message,Exception innerException):base(message,innerException)
        {
            //
            // TODO: Add constructor logic here
            //
        }
	}
}
