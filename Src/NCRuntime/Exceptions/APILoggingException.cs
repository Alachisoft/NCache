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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace Alachisoft.NCache.Runtime.Exceptions
{

    /// <summary>
    /// This Exception is thrown when Logging is stopped because of some connection loss or due to some other problem.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(APILoggingException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    public class APILoggingException :OperationFailedException
    {
        /// <summary>
        /// Default constructor
        /// </summary>
         public APILoggingException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public APILoggingException(string reason)
            : base(reason, false)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public APILoggingException(string reason, Exception inner)
            : base(reason, inner, false)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        public APILoggingException(int errorCode):base(errorCode) { }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        public APILoggingException(int errorCode,string reason)
           : base(errorCode,reason, false)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        public APILoggingException(int errorCode,string reason, Exception inner)
          : base(errorCode,reason, inner, false)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace for exception</param>
        public APILoggingException(int errorCode, string reason,string stackTrace)
          : base(errorCode, reason, false,stackTrace)
        {
        }

        #region /                 --- ISerializable ---           /
        /// <summary>
        /// overloaded constructor, takes serialization info and streaming context.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public APILoggingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// manual serialization
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

