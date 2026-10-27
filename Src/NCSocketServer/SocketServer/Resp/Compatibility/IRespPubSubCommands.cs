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

namespace Alachisoft.NCache.SocketServer.Resp.Compatibility
{
    /// <summary>
    /// The publish and subscribe commands - 9 of them, on 4 members: all 4 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// Pattern and shard delivery are flags rather than separate members, because the three
    /// spellings differ only in how a channel name is matched and which nodes see it.
    /// </para>
    /// </summary>
    internal interface IRespPubSubCommands
    {
        /// <summary>PUBLISH and SPUBLISH.</summary>
        [RespCommand("PUBLISH", "SPUBLISH")]
        long Publish(string channel, byte[] message, bool shard);

        /// <summary>SUBSCRIBE, PSUBSCRIBE and SSUBSCRIBE.</summary>
        [RespCommand("SUBSCRIBE", "PSUBSCRIBE", "SSUBSCRIBE")]
        RespReply Subscribe(IList<byte[]> channels, bool pattern, bool shard);

        /// <summary>UNSUBSCRIBE, PUNSUBSCRIBE and SUNSUBSCRIBE. No channels means all of them.</summary>
        [RespCommand("UNSUBSCRIBE", "PUNSUBSCRIBE", "SUNSUBSCRIBE")]
        RespReply Unsubscribe(IList<byte[]> channels, bool pattern, bool shard);

        /// <summary>PUBSUB, a container command.</summary>
        [RespCommand("PUBSUB")]
        RespReply PubSubIntrospection(RespArgs args);
    }
}
