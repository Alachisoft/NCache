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
    /// The sets commands - 17 of them, on 13 members: all 13 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespSetCommands
    {
        /// <summary>SADD.</summary>
        [RespCommand("SADD")]
        long SetAdd(string key, IList<byte[]> members);

        /// <summary>SREM.</summary>
        [RespCommand("SREM")]
        long SetRemove(string key, IList<byte[]> members);

        /// <summary>SISMEMBER.</summary>
        [RespCommand("SISMEMBER")]
        bool SetContains(string key, byte[] member);

        /// <summary>SCARD.</summary>
        [RespCommand("SCARD")]
        long SetCardinality(string key);

        /// <summary>SMEMBERS.</summary>
        [RespCommand("SMEMBERS")]
        IList<byte[]> SetMembers(string key);

        /// <summary>SPOP.</summary>
        [RespCommand("SPOP")]
        IList<byte[]> SetPop(string key, int count);

        /// <summary>SRANDMEMBER. A negative count allows repeats, as Redis does.</summary>
        [RespCommand("SRANDMEMBER")]
        IList<byte[]> SetRandomMembers(string key, int count);

        /// <summary>SUNION, SINTER and SDIFF.</summary>
        [RespCommand("SUNION", "SINTER", "SDIFF")]
        IList<byte[]> SetCombine(string operation, IList<string> keys);

        /// <summary>SUNIONSTORE, SINTERSTORE and SDIFFSTORE.</summary>
        [RespCommand("SUNIONSTORE", "SINTERSTORE", "SDIFFSTORE")]
        long SetCombineAndStore(string operation, string destination, IList<string> keys);

        /// <summary>SINTERCARD.</summary>
        [RespCommand("SINTERCARD")]
        long SetIntersectionCardinality(IList<string> keys, long limit);

        /// <summary>SMISMEMBER.</summary>
        [RespCommand("SMISMEMBER")]
        IList<bool> SetContainsMany(string key, IList<byte[]> members);

        /// <summary>SMOVE.</summary>
        [RespCommand("SMOVE")]
        bool SetMove(string source, string destination, byte[] member);

        /// <summary>SSCAN.</summary>
        [RespCommand("SSCAN")]
        RespReply SetScan(string key, RespArgs args);
    }
}
