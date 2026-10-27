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
    /// The hashes commands - 28 of them, on 20 members: all 19 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// HGETALL, HKEYS and HVALS are one read. They differ only in which half of each pair the
    /// caller renders, and splitting them into three members would invite three implementations of
    /// the same traversal.
    /// </para>
    /// </summary>
    internal interface IRespHashCommands
    {
        /// <summary>HSET and HMSET. Returns how many fields were new.</summary>
        [RespCommand("HSET", "HMSET")]
        long HashSet(string key, IList<RespField> fields);

        /// <summary>HGET.</summary>
        [RespCommand("HGET")]
        byte[] HashGet(string key, byte[] field);

        /// <summary>HMGET.</summary>
        [RespCommand("HMGET")]
        IList<byte[]> HashGetMany(string key, IList<byte[]> fields);

        /// <summary>HDEL.</summary>
        [RespCommand("HDEL")]
        long HashDelete(string key, IList<byte[]> fields);

        /// <summary>HEXISTS.</summary>
        [RespCommand("HEXISTS")]
        bool HashExists(string key, byte[] field);

        /// <summary>HSETNX.</summary>
        [RespCommand("HSETNX")]
        bool HashSetIfAbsent(string key, byte[] field, byte[] value);

        /// <summary>HLEN.</summary>
        [RespCommand("HLEN")]
        long HashLength(string key);

        /// <summary>HSTRLEN.</summary>
        [RespCommand("HSTRLEN")]
        long HashFieldLength(string key, byte[] field);

        /// <summary>HGETALL, HKEYS and HVALS - one traversal, three renderings.</summary>
        [RespCommand("HGETALL", "HKEYS", "HVALS")]
        IList<RespField> HashEntries(string key);

        /// <summary>HINCRBY.</summary>
        [RespCommand("HINCRBY")]
        long HashIncrementBy(string key, byte[] field, long delta);

        /// <summary>HINCRBYFLOAT.</summary>
        [RespCommand("HINCRBYFLOAT")]
        double HashIncrementByFloat(string key, byte[] field, double delta);

        /// <summary>HRANDFIELD.</summary>
        [RespCommand("HRANDFIELD")]
        RespReply HashRandomField(string key, RespArgs args);

        /// <summary>HSCAN.</summary>
        [RespCommand("HSCAN")]
        RespReply HashScan(string key, RespArgs args);

        /// <summary>HEXPIRE and its three other spellings - one operation over an absolute deadline.</summary>
        [RespCommand("HEXPIRE", "HPEXPIRE", "HEXPIREAT", "HPEXPIREAT")]
        IList<long> HashExpire(string key, RespExpiry expiry, RespExpiryCondition condition, IList<byte[]> fields);

        /// <summary>HTTL and HPTTL, in milliseconds.</summary>
        [RespCommand("HTTL", "HPTTL")]
        IList<long> HashTimeToLive(string key, IList<byte[]> fields);

        /// <summary>HEXPIRETIME and HPEXPIRETIME, as Unix milliseconds.</summary>
        [RespCommand("HEXPIRETIME", "HPEXPIRETIME")]
        IList<long> HashExpireTime(string key, IList<byte[]> fields);

        /// <summary>HPERSIST.</summary>
        [RespCommand("HPERSIST")]
        IList<long> HashPersist(string key, IList<byte[]> fields);

        /// <summary>HGETEX - read the fields, and change the expiry under the HEXPIRE rule.</summary>
        [RespCommand("HGETEX")]
        IList<byte[]> HashGetEx(string key, IList<byte[]> fields, RespExpiry expiry, bool persist);

        /// <summary>HSETEX - write the fields, and set the expiry under the HEXPIRE rule.</summary>
        [RespCommand("HSETEX")]
        bool HashSetEx(string key, IList<RespField> fields, RespExpiry expiry, RespWriteCondition condition);

        /// <summary>HGETDEL.</summary>
        [RespCommand("HGETDEL")]
        RespReply HashGetDelete(RespArgs args);
    }
}
