//  Copyright (c) 2018 Alachisoft
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
using System.Collections;
using System.Security.Permissions;
using System.Runtime.Serialization;


#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
#endif 


namespace Alachisoft.NCache.Caching.Exceptions
{
	/// <summary>
	/// Thrown when an exception occurs during configuration. Likely causes are badly specified
	/// configuration strings.
	/// </summary>
	[Serializable]
	public class LicensingException: CacheException, ISerializable
	{
		/// <summary> 
		/// default constructor. 
		/// </summary>
		public LicensingException() {}

		/// <summary> 
		/// overloaded constructor, takes the reason as parameter. 
		/// </summary>
		public LicensingException(string reason):base(reason) 
		{
		}

		/// <summary>
		/// overloaded constructor. 
		/// </summary>
		/// <param name="reason">reason for exception</param>
		/// <param name="inner">nested exception</param>
		internal LicensingException(string reason, Exception inner):base(reason, inner) 
		{
		}

        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        internal LicensingException(int errorCode, string reason) : base(errorCode, reason)
        {

        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        internal LicensingException(int errorCode, string reason, string stackTrace) : base(errorCode, reason, stackTrace)
        {

        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        internal LicensingException(int errorCode, string reason, Exception inner):base(errorCode,reason,inner)
        {

        }
		#region /                 --- ISerializable ---           / 

		/// <summary> 
		/// overloaded constructor, manual serialization. 
		/// </summary>
		protected LicensingException(SerializationInfo info, StreamingContext context):base(info, context) 
		{
		}

		/// <summary>
		/// manual serialization
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}

		#endregion
	}
}
