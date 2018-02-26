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
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// StreamException is thrown if any error occurs during operation on CacheStream.
    /// </summary>
    [Serializable]
    public class StreamException :CacheException, ISerializable
    {
        /// <summary>
        /// Default constructor. Initializes the instance of StreamException.
        /// </summary>
        public StreamException() : base() { }

        /// <summary>
        /// Initializes the instance of StreamException with give message.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public StreamException(string message) : base(message) { }

        /// <summary>
        /// Initializes the instance of StreamException with give message.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public StreamException(string message,Exception innerException):base(message,innerException){}

        #region ISerializable Members

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected StreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}
