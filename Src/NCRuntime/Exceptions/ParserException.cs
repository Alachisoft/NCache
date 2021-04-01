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
    /// This exception is thrown when an exception occurs during configuration. Likely causes are badly specified
    /// configuration strings.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(ConfigurationException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class ParserException : CacheException
    {
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public ParserException() { }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public ParserException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public ParserException(string reason, Exception inner)
            : base(reason, inner)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned errorcode</param>
        public ParserException(int errorCode):base(errorCode) { }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        public ParserException(int errorCode,string reason)
           : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public ParserException(int errorCode, string reason,string stackTrace)
          : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        public ParserException(int errorCode,string reason, Exception inner)
          : base(errorCode,reason, inner)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected ParserException(SerializationInfo info, StreamingContext context)
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