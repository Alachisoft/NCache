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

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown whenever an operation is performed on Cache and state transfer is in progress
    /// </summary>
    [Serializable]
    public class StateTransferInProgressException : Exception, ISerializable
    {
        /// <summary>
        /// Overloaded constructor that takes error as an argument
        /// </summary>
        /// <param name="error">error message of the Exception</param>
        public StateTransferInProgressException(String error)
            : base(error)
        {
        }
        /// <summary>
        /// Overloaded constructor that takes error message and exception as an arguments
        /// </summary>
        /// <param name="error">error message for the exception</param>
        /// <param name="exception">inner exception object</param>
        public StateTransferInProgressException(String error, Exception exception)
            : base(error, exception)
        {
        }
        /// <summary>
        /// overloaded constructor that takes info and context as an arguments
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">context</param>
        public StateTransferInProgressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}