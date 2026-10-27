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
    /// The sorted sets commands - 35 of them, on 20 members: all 20 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// Every member here is refused, because the engine has no scored collection. They are
    /// declared with real signatures all the same: when that gap closes, the shape is already
    /// agreed and the compiler names what is missing rather than leaving someone to rediscover
    /// thirty-five commands.
    /// </para>
    /// </summary>
    internal interface IRespSortedSetCommands
    {
        /// <summary>ZADD.</summary>
        [RespCommand("ZADD")]
        long SortedSetAdd(string key, IList<RespScoredMember> members, RespArgs options);

        /// <summary>ZINCRBY.</summary>
        [RespCommand("ZINCRBY")]
        double SortedSetIncrementBy(string key, byte[] member, double delta);

        /// <summary>ZREM.</summary>
        [RespCommand("ZREM")]
        long SortedSetRemove(string key, IList<byte[]> members);

        /// <summary>ZSCORE.</summary>
        [RespCommand("ZSCORE")]
        double? SortedSetScore(string key, byte[] member);

        /// <summary>ZMSCORE.</summary>
        [RespCommand("ZMSCORE")]
        IList<double?> SortedSetScores(string key, IList<byte[]> members);

        /// <summary>ZRANK and ZREVRANK.</summary>
        [RespCommand("ZRANK", "ZREVRANK")]
        long? SortedSetRank(string key, byte[] member, bool reverse);

        /// <summary>ZRANGE and its five by-score and by-lex variants.</summary>
        [RespCommand("ZRANGE", "ZREVRANGE", "ZRANGEBYSCORE", "ZREVRANGEBYSCORE", "ZRANGEBYLEX", "ZREVRANGEBYLEX")]
        RespReply SortedSetRange(string key, RespArgs args);

        /// <summary>ZRANGESTORE.</summary>
        [RespCommand("ZRANGESTORE")]
        long SortedSetRangeAndStore(string destination, string key, RespArgs args);

        /// <summary>ZREMRANGEBYRANK, ZREMRANGEBYSCORE and ZREMRANGEBYLEX.</summary>
        [RespCommand("ZREMRANGEBYRANK", "ZREMRANGEBYSCORE", "ZREMRANGEBYLEX")]
        long SortedSetRemoveRange(string key, RespArgs args);

        /// <summary>ZCARD.</summary>
        [RespCommand("ZCARD")]
        long SortedSetCardinality(string key);

        /// <summary>ZCOUNT and ZLEXCOUNT.</summary>
        [RespCommand("ZCOUNT", "ZLEXCOUNT")]
        long SortedSetCount(string key, RespArgs range);

        /// <summary>ZPOPMIN and ZPOPMAX.</summary>
        [RespCommand("ZPOPMIN", "ZPOPMAX")]
        IList<RespScoredMember> SortedSetPop(string key, bool highest, int count);

        /// <summary>BZPOPMIN and BZPOPMAX.</summary>
        [RespCommand("BZPOPMIN", "BZPOPMAX")]
        RespReply SortedSetBlockingPop(IList<string> keys, bool highest, double timeoutSeconds);

        /// <summary>ZRANDMEMBER.</summary>
        [RespCommand("ZRANDMEMBER")]
        RespReply SortedSetRandomMember(string key, RespArgs args);

        /// <summary>ZSCAN.</summary>
        [RespCommand("ZSCAN")]
        RespReply SortedSetScan(string key, RespArgs args);

        /// <summary>ZUNION, ZINTER and ZDIFF.</summary>
        [RespCommand("ZUNION", "ZINTER", "ZDIFF")]
        RespReply SortedSetCombine(string operation, RespArgs args);

        /// <summary>ZUNIONSTORE, ZINTERSTORE and ZDIFFSTORE.</summary>
        [RespCommand("ZUNIONSTORE", "ZINTERSTORE", "ZDIFFSTORE")]
        long SortedSetCombineAndStore(string operation, string destination, RespArgs args);

        /// <summary>ZINTERCARD.</summary>
        [RespCommand("ZINTERCARD")]
        long SortedSetIntersectionCardinality(RespArgs args);

        /// <summary>ZMPOP.</summary>
        [RespCommand("ZMPOP")]
        RespReply SortedSetPopMany(RespArgs args);

        /// <summary>BZMPOP.</summary>
        [RespCommand("BZMPOP")]
        RespReply SortedSetBlockingPopMany(RespArgs args);
    }
}
