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

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// Contains properties required for a message to publish on a topic. 
    /// Must be implemented by Message.
    /// </summary>

    public interface IMessage
    {
        /// <summary>
        /// Unique id of message.
        /// </summary>
        string MessageId { get; }
        /// <summary>
        /// Expiration time provided by user, i.e. message will be expired if no subscriber exists.
        /// </summary>
        TimeSpan? ExpirationTime { get; set; }
        /// <summary>
        /// Payload of message.
        /// </summary>
        object Payload { get;  }
        /// <summary>
        /// Creation time of message.
        /// </summary>
        DateTime CreationTime { get; }

    }
}
