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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// LicensingException is thrown when Either license has expired or some
    /// error occurred during the validation of license.
    /// </summary>
    [Serializable]


    public class LicensingException : CacheException

    {
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public LicensingException() { }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public LicensingException(string reason)
            : base(reason)
        {
        }

      
        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public LicensingException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="errorCode">errorcode which is associated with exception</param>
        public LicensingException(int errorCode) : base(errorCode)
        {
        }
        /// <summary> 
        /// Overloaded constructor, takes the reason and errorCode as parameter. 
        /// </summary>
        public LicensingException(int errorCode,string reason)
            : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">exception Message</param>
        /// <param name="stackTrace">Stacktrace</param>
        public LicensingException(int errorCode, string reason,string stackTrace)
          : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        /// <param name="errorCode">ErrorCode</param>

        public LicensingException(int errorCode,string reason, Exception inner)
            : base(errorCode,reason, inner)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        public LicensingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}