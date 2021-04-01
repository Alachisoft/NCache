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
    /// LockingException is thrown on Insert/Remove operation if the item is locked and incorrect Lockid 
    /// is specified.
    /// </summary>
    [Serializable]
    public class LockingException : OperationFailedException
    {
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public LockingException() { }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public LockingException(string reason)
            : base(reason, false)
        {
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public LockingException(string reason, Exception inner)
            : base(reason, inner, false)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        public LockingException(int errorCode):base(errorCode)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        public LockingException(int errorCode,string reason)
           : base(errorCode,reason, false)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned Errorcode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="stackTrace">stack trace</param>
        public LockingException(int errorCode, string reason,string stackTrace)
          : base(errorCode, reason, false,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned errorcode</param>
        /// <param name="reason">Exception Message</param>
        /// <param name="inner">nested exception</param>
        public LockingException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner, false)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        public LockingException(SerializationInfo info, StreamingContext context)
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