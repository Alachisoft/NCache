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
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown whenever one of the data partition goes down
    /// </summary>
    [Serializable]
    public class InvalidReaderException : OperationFailedException
    {
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public InvalidReaderException() { }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public InvalidReaderException(string reason)
            : base(reason, false)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public InvalidReaderException(string reason, Exception inner)
            : base(reason, inner, false)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        public InvalidReaderException(int errorCode):base(errorCode)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        public InvalidReaderException(int errorCode,string reason)
         : base(errorCode,reason, false)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        public InvalidReaderException(int errorCode,string reason, Exception inner)
            : base(errorCode,reason, inner, false)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="stackTrace">Stacktrace</param>
        public InvalidReaderException(int errorCode, string reason,string stackTrace)
        : base(errorCode, reason, false,stackTrace)
        {
        }
        #region /                 --- ISerializable ---           /
        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error
        //  message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public InvalidReaderException(SerializationInfo info, StreamingContext context)
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
