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
    /// BucketTransferredException is thrown on Add/Insert operation in POR cache when during the 
    /// node up/down process, a bucket is moved to another node and this node has received 
    /// the item according to an older map.
    /// </summary>
    [Serializable]
    public class BucketTransferredException : OperationFailedException
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public BucketTransferredException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public BucketTransferredException(string reason)
            : base(reason, false)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public BucketTransferredException(string reason, Exception inner)
            : base(reason, inner, false)
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">error code associated with exception</param>
        public BucketTransferredException(int errorCode):base(errorCode) { }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">error code associated with exception</param>
        /// <param name="reason">reason for exception</param>
        public BucketTransferredException(int errorCode,string reason)
            : base(errorCode,reason, false)
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">error code associated with exception</param>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public BucketTransferredException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner, false)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        public BucketTransferredException(SerializationInfo info, StreamingContext context)
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