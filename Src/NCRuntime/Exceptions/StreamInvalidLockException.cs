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
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;


namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// StreamInvalidLockException is thrown if the current lock handle becomes invalid.
    /// </summary>
    /// <remarks>CacheStream opened for reading or writing mode acquires read or writer lock.
    /// Suppose there are two cache clients. First opens stream for either reading/writing.
    /// Before first client closes the stream, it is removed from the cache due to expiration
    /// or eviction. Now at this moment second client opens a fresh stream. If first client
    /// performs any operation on the stream, his lock handle becomes invalid and StreamInvalidLockException
    /// is thrown.
    /// </remarks>
    [Serializable]
    public class StreamInvalidLockException : StreamException, ISerializable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public StreamInvalidLockException() : base("Invalid lock handle") { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        public StreamInvalidLockException(int errorCode) : base(errorCode) { }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">assigned errorCode</param>
        /// <param name="reason">exception message</param>
        public StreamInvalidLockException(int errorCode,string reason= "Invalid lock handle") : base(errorCode,reason) { }

        #region ISerializable Members

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected StreamInvalidLockException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}
