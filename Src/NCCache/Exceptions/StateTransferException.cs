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
    [Serializable]
    public class StateTransferException : CacheException, ISerializable
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        internal StateTransferException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        internal StateTransferException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        internal StateTransferException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        internal StateTransferException(int errorCode) :base (errorCode)
        { }
        internal StateTransferException(int errorCode,string reason)
           : base(errorCode,reason)
        {
        }

        internal StateTransferException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner)
        {
        }
    
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected StateTransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}