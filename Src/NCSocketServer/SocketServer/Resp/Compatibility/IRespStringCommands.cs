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
    /// The strings and counters commands - 21 of them, on 14 members: all 14 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// SET absorbs SETEX, PSETEX and SETNX, because those are SET with an expiry or a guard and
    /// nothing else. The counters absorb each other the same way: INCR, DECR, INCRBY and DECRBY are
    /// one operation over a signed delta.
    /// </para>
    /// </summary>
    internal interface IRespStringCommands
    {
        /// <summary>GET. Null for a key that does not exist.</summary>
        [RespCommand("GET")]
        byte[] Get(string key);

        /// <summary>SET and its narrower spellings SETEX, PSETEX and SETNX.</summary>
        [RespCommand("SET", "SETEX", "PSETEX", "SETNX")]
        RespSetOutcome Set(string key, byte[] value, RespExpiry expiry, RespWriteCondition condition, bool returnPrevious);

        /// <summary>GETSET.</summary>
        [RespCommand("GETSET")]
        byte[] GetSet(string key, byte[] value);

        /// <summary>GETDEL.</summary>
        [RespCommand("GETDEL")]
        byte[] GetDelete(string key);

        /// <summary>GETEX. <paramref name="persist"/> is the PERSIST option, which clears the expiry instead of setting one.</summary>
        [RespCommand("GETEX")]
        byte[] GetWithExpiry(string key, RespExpiry expiry, bool persist);

        /// <summary>MGET. One entry per key, in order, null where the key is absent.</summary>
        [RespCommand("MGET")]
        IList<byte[]> GetMany(IList<string> keys);

        /// <summary>MSET.</summary>
        [RespCommand("MSET")]
        void SetMany(IList<KeyValuePair<string, byte[]>> pairs);

        /// <summary>APPEND. Returns the new length.</summary>
        [RespCommand("APPEND")]
        long Append(string key, byte[] value);

        /// <summary>STRLEN.</summary>
        [RespCommand("STRLEN")]
        long StringLength(string key);

        /// <summary>GETRANGE and its alias SUBSTR. Inclusive, and negative offsets count from the end.</summary>
        [RespCommand("GETRANGE", "SUBSTR")]
        byte[] GetRange(string key, long start, long end);

        /// <summary>SETRANGE. Returns the new length.</summary>
        [RespCommand("SETRANGE")]
        long SetRange(string key, long offset, byte[] value);

        /// <summary>INCR, DECR, INCRBY and DECRBY - one operation over a signed delta.</summary>
        [RespCommand("INCR", "DECR", "INCRBY", "DECRBY")]
        long IncrementBy(string key, long delta);

        /// <summary>MSETNX. Needs all-or-nothing, which an NCache bulk add cannot promise.</summary>
        [RespCommand("MSETNX")]
        bool SetManyIfAllAbsent(IList<KeyValuePair<string, byte[]>> pairs);

        /// <summary>INCRBYFLOAT.</summary>
        [RespCommand("INCRBYFLOAT")]
        double IncrementByFloat(string key, double delta);
    }
}
