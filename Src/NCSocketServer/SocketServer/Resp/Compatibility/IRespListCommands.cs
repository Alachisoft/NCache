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
    /// The lists commands - 22 of them, on 15 members: all 15 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// Several refusals here are not about lists being unavailable but about element comparison:
    /// the engine compares collection elements through a JSON parse, so remove-by-value and
    /// find-by-value cannot be made binary-safe, and a half-working LREM is worse than a refused
    /// one.
    /// </para>
    /// </summary>
    internal interface IRespListCommands
    {
        /// <summary>LPUSH, RPUSH, LPUSHX and RPUSHX.</summary>
        [RespCommand("LPUSH", "RPUSH", "LPUSHX", "RPUSHX")]
        long ListPush(string key, RespListEnd end, IList<byte[]> values, bool onlyIfExists);

        /// <summary>LPOP and RPOP, with the COUNT form.</summary>
        [RespCommand("LPOP", "RPOP")]
        IList<byte[]> ListPop(string key, RespListEnd end, int count);

        /// <summary>LLEN.</summary>
        [RespCommand("LLEN")]
        long ListLength(string key);

        /// <summary>LINDEX.</summary>
        [RespCommand("LINDEX")]
        byte[] ListIndex(string key, long index);

        /// <summary>LSET.</summary>
        [RespCommand("LSET")]
        void ListSet(string key, long index, byte[] value);

        /// <summary>LRANGE.</summary>
        [RespCommand("LRANGE")]
        IList<byte[]> ListRange(string key, long start, long stop);

        /// <summary>LREM.</summary>
        [RespCommand("LREM")]
        long ListRemove(string key, long count, byte[] value);

        /// <summary>LPOS.</summary>
        [RespCommand("LPOS")]
        RespReply ListPosition(string key, byte[] value, RespArgs args);

        /// <summary>LINSERT.</summary>
        [RespCommand("LINSERT")]
        long ListInsert(string key, bool before, byte[] pivot, byte[] value);

        /// <summary>LTRIM.</summary>
        [RespCommand("LTRIM")]
        void ListTrim(string key, long start, long stop);

        /// <summary>BLPOP and BRPOP.</summary>
        [RespCommand("BLPOP", "BRPOP")]
        RespReply ListBlockingPop(IList<string> keys, RespListEnd end, double timeoutSeconds);

        /// <summary>BLMOVE and BRPOPLPUSH.</summary>
        [RespCommand("BLMOVE", "BRPOPLPUSH")]
        byte[] ListBlockingMove(string source, string destination, RespListEnd from, RespListEnd to, double timeoutSeconds);

        /// <summary>BLMPOP.</summary>
        [RespCommand("BLMPOP")]
        RespReply ListBlockingPopMany(RespArgs args);

        /// <summary>RPOPLPUSH and LMOVE.</summary>
        [RespCommand("RPOPLPUSH", "LMOVE")]
        byte[] ListMove(string source, string destination, RespListEnd from, RespListEnd to);

        /// <summary>LMPOP.</summary>
        [RespCommand("LMPOP")]
        RespReply ListPopMany(RespArgs args);
    }
}
