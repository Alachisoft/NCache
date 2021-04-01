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
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Caching.Exceptions
{
    /// <summary>
    /// Thrown when an operation is performed over an object that has not been initialized as yet
    /// or is in an invalid state.
    /// </summary>
    [Serializable]
    public class PersistenceException: CacheException, ISerializable
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        internal PersistenceException()
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        internal PersistenceException(string reason):base(reason) 
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        internal PersistenceException(string reason, Exception inner):base(reason, inner) 
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned error code</param>
        /// <param name="reason">exception message</param>
        internal PersistenceException(int errorCode,string reason) : base(errorCode,reason)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned error code</param>
        /// <param name="reason">exception message</param>
        /// <param name="stacktrace">stacktrace of exception</param>
        internal PersistenceException(int errorCode, string reason,string stacktrace) : base(errorCode, reason,stacktrace)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned error code</param>
        /// <param name="reason">excetion message</param>
        /// <param name="inner">nested exception</param>
        internal PersistenceException(int errorCode,string reason, Exception inner) : base(errorCode,reason, inner)
        {
        }
        #region /                 --- ISerializable ---           / 

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected PersistenceException(SerializationInfo info, StreamingContext context):base(info, context) 
        {
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}