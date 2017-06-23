// Copyright (c) 2017 Alachisoft
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
// limitations under the License.

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
        public StateTransferInProgressException(String error)
            : base(error)
        {
        }

        public StateTransferInProgressException(String error, Exception exception)
            : base(error, exception)
        {
        }

        public StateTransferInProgressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
