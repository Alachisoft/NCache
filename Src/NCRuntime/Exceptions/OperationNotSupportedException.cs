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
    /// This exception is thrown when an operation is not supported in the current edition of NCache.
    /// </summary>
    [Serializable]
    public class OperationNotSupportedException : CacheException
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public OperationNotSupportedException() { }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationNotSupportedException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public OperationNotSupportedException(string reason, Exception inner)
            : base(reason, inner)
        {
        }
        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationNotSupportedException(int errorCode,string reason)
            : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public OperationNotSupportedException(int errorCode, string reason,string stackTrace)
           : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public OperationNotSupportedException(int errorCode,string reason, Exception inner)
            : base(errorCode,reason, inner)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected OperationNotSupportedException(SerializationInfo info, StreamingContext context)
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