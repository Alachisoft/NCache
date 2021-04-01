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
//using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// InvalidTaskEnumeratorException is thrown when MapReduce Task Enumerator is invalid
    /// </summary>
    public class InvalidTaskEnumeratorException : CacheException
    {
        /// <summary>
        /// Default constructor for InvalidTaskEnumeratorException
        /// </summary>
        public InvalidTaskEnumeratorException()
        { }
        /// <summary>
        /// Overloaded constructor for InvalidTaskEnumeratorException that takes reason as an argument
        /// </summary>
        /// <param name="reason">reason for the exception</param>
        public InvalidTaskEnumeratorException(string reason) : base(reason)
        { }
        /// <summary>
        /// Overloaded constructor that takes two arguments 1-reason of Exception 2-inner exception
        /// </summary>
        /// <param name="reason">reason of the exception</param>
        /// <param name="inner">inner exception because of which this exception occurs</param>
        public InvalidTaskEnumeratorException(string reason, Exception inner) : base(reason, inner)
        { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorcode">Assigned Errorcode</param>
        public InvalidTaskEnumeratorException(int errorcode):base(errorcode)
        { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        public InvalidTaskEnumeratorException(int errorCode,string reason) : base(errorCode,reason)
        { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned Errorcode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public InvalidTaskEnumeratorException(int errorCode, string reason,string stackTrace) : base(errorCode, reason,stackTrace)
        { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception Message</param>
        /// <param name="inner">Nested Exception</param>
        public InvalidTaskEnumeratorException(int errorCode,string reason, Exception inner) : base(errorCode,reason, inner)
        { }
    }
}
