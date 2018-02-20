// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Exceptions
#else
namespace Alachisoft.NCache.Runtime.Exceptions
#endif
{
    /// <summary>
    /// StreamAlreadLockedException is thrown if stream is already locked.
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
        
        #region ISerializable Members

        /// <summary> 
        /// overloaded constructor, manual serialization. 
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
