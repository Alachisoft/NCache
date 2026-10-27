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
    /// The server and administration commands - 29 of them, on 28 members: all 25 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// Where a command would reach into NCache's own management surface it is refused rather
    /// than approximated - emptying a cache through FLUSHDB is fine, because that is Cache.Clear,
    /// but shutting a node down over the Redis port is not.
    /// </para>
    /// </summary>
    internal interface IRespServerCommands
    {
        /// <summary>INFO.</summary>
        [RespCommand("INFO")]
        RespReply Info(RespArgs sections);

        /// <summary>CONFIG, a container command.</summary>
        [RespCommand("CONFIG")]
        RespReply Config(RespArgs args);

        /// <summary>COMMAND and its subcommands, answered from RespCommandTable.</summary>
        [RespCommand("COMMAND")]
        RespReply Command(RespArgs args);

        /// <summary>TIME. Seconds and microseconds, as two bulk strings.</summary>
        [RespCommand("TIME")]
        RespReply Time();

        /// <summary>FLUSHDB. Maps onto Cache.Clear.</summary>
        [RespCommand("FLUSHDB")]
        void FlushDatabase(bool asynchronous);

        /// <summary>FLUSHALL. Identical to FLUSHDB here, because there is only one keyspace.</summary>
        [RespCommand("FLUSHALL")]
        void FlushAll(bool asynchronous);

        /// <summary>DBSIZE.</summary>
        [RespCommand("DBSIZE")]
        long DatabaseSize();

        /// <summary>SHUTDOWN.</summary>
        [RespCommand("SHUTDOWN")]
        void Shutdown(RespArgs args);

        /// <summary>DEBUG.</summary>
        [RespCommand("DEBUG")]
        RespReply Debug(RespArgs args);

        /// <summary>MONITOR.</summary>
        [RespCommand("MONITOR")]
        void Monitor();

        /// <summary>SLOWLOG.</summary>
        [RespCommand("SLOWLOG")]
        RespReply SlowLog(RespArgs args);

        /// <summary>SWAPDB.</summary>
        [RespCommand("SWAPDB")]
        void SwapDatabases(int first, int second);

        /// <summary>LATENCY.</summary>
        [RespCommand("LATENCY")]
        RespReply Latency(RespArgs args);

        /// <summary>MEMORY.</summary>
        [RespCommand("MEMORY")]
        RespReply Memory(RespArgs args);

        /// <summary>LOLWUT.</summary>
        [RespCommand("LOLWUT")]
        RespReply Lolwut(RespArgs args);

        /// <summary>SAVE.</summary>
        [RespCommand("SAVE")]
        void Save();

        /// <summary>BGSAVE.</summary>
        [RespCommand("BGSAVE")]
        void BackgroundSave(bool scheduleOnly);

        /// <summary>BGREWRITEAOF.</summary>
        [RespCommand("BGREWRITEAOF")]
        void BackgroundRewriteAppendOnlyFile();

        /// <summary>LASTSAVE.</summary>
        [RespCommand("LASTSAVE")]
        long LastSave();

        /// <summary>REPLICAOF and its old name SLAVEOF. NCache manages replication through its own topology.</summary>
        [RespCommand("REPLICAOF", "SLAVEOF")]
        void ReplicaOf(string host, int port);

        /// <summary>FAILOVER.</summary>
        [RespCommand("FAILOVER")]
        RespReply Failover(RespArgs args);

        /// <summary>SYNC.</summary>
        [RespCommand("SYNC")]
        RespReply Sync();

        /// <summary>PSYNC.</summary>
        [RespCommand("PSYNC")]
        RespReply PartialSync(RespArgs args);

        /// <summary>WAIT.</summary>
        [RespCommand("WAIT")]
        long WaitForReplicas(int replicas, long timeoutMilliseconds);

        /// <summary>ROLE.</summary>
        [RespCommand("ROLE")]
        RespReply Role();

        /// <summary>WAITAOF.</summary>
        [RespCommand("WAITAOF")]
        RespReply WaitForAppendOnlyFile(RespArgs args);

        /// <summary>REPLCONF.</summary>
        [RespCommand("REPLCONF")]
        RespReply ReplicationConfig(RespArgs args);

        /// <summary>MODULE, a container command.</summary>
        [RespCommand("MODULE")]
        RespReply Module(RespArgs args);
    }
}
