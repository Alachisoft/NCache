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

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Raised when a node is asked for a key that belongs to another node, by a caller that has
    /// declared it routes by slot itself.
    /// <para>
    /// NCache normally hides key location: a node asked for a key it does not own fetches it from
    /// the owner over the cluster channel and answers as if it held it. That is right for a client
    /// that has no notion of ownership, and wrong for one whose whole protocol is built on it - a
    /// Redis client expects to be told <c>MOVED</c> so it can learn the topology and go straight to
    /// the owner next time. Silently proxying denies it that, and costs an extra hop on every
    /// request for as long as the client keeps guessing wrong.
    /// </para>
    /// <para>
    /// So a caller may opt in, per operation, with
    /// <c>OperationContextFieldName.CallerRoutesBySlot</c>. Only then does a node decline, and it
    /// names the owner in <see cref="Owner"/> so the caller can redirect rather than report a miss.
    /// A caller that does not opt in sees the historic behaviour exactly.
    /// </para>
    /// </summary>
    [Serializable]
    public class SlotNotOwnedException : OperationFailedException
    {
        /// <summary>The node that does own the key, as an address the caller can redirect to.</summary>
        public string Owner { get; private set; }

        public SlotNotOwnedException(string owner)
            : base("this node does not own the key; it belongs to " + (owner ?? "another node"))
        {
            Owner = owner;
        }
    }
}
