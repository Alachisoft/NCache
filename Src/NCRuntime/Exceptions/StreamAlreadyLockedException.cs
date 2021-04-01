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
    /// StreamAlreadyLockedException is thrown if a stream is already locked.
    /// </summary>
    /// <remarks>CacheStream opened for reading or writing mode acquires read or writer lock.
    ///If stream is already opened with reader/writer lock then this exception is thrown.
    /// </remarks>
    [Serializable]
    public class StreamAlreadyLockedException: StreamException, ISerializable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public StreamAlreadyLockedException() : base("Stream is already locked.") { }
        /// <summary>
        /// Default constructor with errorCode
        /// </summary>
        public StreamAlreadyLockedException(int errorCode) : base(errorCode) { }
        /// <summary>
        /// Default constructor with error code and message
        /// </summary>
        public StreamAlreadyLockedException(int errorCode,string reason= "Stream is already locked.") : base(errorCode, reason) { }
        //public StreamAlreadyLockedException(int errorCode, string reason = "Stream is already locked.",string stackTrace) : base(errorCode, reason,stackTrace) { }
        #region ISerializable Members

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected StreamAlreadyLockedException(SerializationInfo info, StreamingContext context)
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
