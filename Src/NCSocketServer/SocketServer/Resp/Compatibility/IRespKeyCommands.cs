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
    /// The keys, expiry and the keyspace commands - 29 of them, on 23 members: all 22 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// Expiry is modelled once. Redis spells the same operation four ways - EXPIRE, PEXPIRE,
    /// EXPIREAT, PEXPIREAT, in seconds or milliseconds, relative or absolute - and all four mean
    /// "give this key this deadline", so the caller converts and this layer takes a
    /// <see cref="RespExpiry"/>. TTL and PTTL collapse the same way.
    /// </para>
    /// </summary>
    internal interface IRespKeyCommands
    {
        /// <summary>DEL. Returns how many existed.</summary>
        [RespCommand("DEL")]
        long Delete(IList<string> keys);

        /// <summary>UNLINK. NCache frees synchronously, so this is DEL; the reply shape is what clients depend on.</summary>
        [RespCommand("UNLINK")]
        long Unlink(IList<string> keys);

        /// <summary>EXISTS. Counts repeats, as Redis does: EXISTS k k is 2 when k exists.</summary>
        [RespCommand("EXISTS")]
        long Exists(IList<string> keys);

        /// <summary>TYPE.</summary>
        [RespCommand("TYPE")]
        RespKeyType KeyType(string key);

        /// <summary>TOUCH.</summary>
        [RespCommand("TOUCH")]
        long Touch(IList<string> keys);

        /// <summary>EXPIRE, PEXPIRE, EXPIREAT and PEXPIREAT - one operation, four spellings.</summary>
        [RespCommand("EXPIRE", "PEXPIRE", "EXPIREAT", "PEXPIREAT")]
        bool Expire(string key, RespExpiry expiry, RespExpiryCondition condition);

        /// <summary>TTL and PTTL. Milliseconds; -1 for a key with no expiry, -2 for one that does not exist.</summary>
        [RespCommand("TTL", "PTTL")]
        long TimeToLiveMilliseconds(string key);

        /// <summary>EXPIRETIME and PEXPIRETIME. The absolute deadline as Unix milliseconds, with the same -1/-2 convention.</summary>
        [RespCommand("EXPIRETIME", "PEXPIRETIME")]
        long ExpireTimeMilliseconds(string key);

        /// <summary>PERSIST.</summary>
        [RespCommand("PERSIST")]
        bool Persist(string key);

        /// <summary>KEYS.</summary>
        [RespCommand("KEYS")]
        IList<string> Keys(byte[] pattern);

        /// <summary>SCAN.</summary>
        [RespCommand("SCAN")]
        RespReply Scan(RespArgs args);

        /// <summary>RANDOMKEY.</summary>
        [RespCommand("RANDOMKEY")]
        string RandomKey();

        /// <summary>RENAME.</summary>
        [RespCommand("RENAME")]
        void Rename(string key, string newKey);

        /// <summary>RENAMENX.</summary>
        [RespCommand("RENAMENX")]
        bool RenameIfAbsent(string key, string newKey);

        /// <summary>COPY.</summary>
        [RespCommand("COPY")]
        bool Copy(string source, string destination, bool replace);

        /// <summary>MOVE.</summary>
        [RespCommand("MOVE")]
        bool MoveToDatabase(string key, int database);

        /// <summary>DUMP.</summary>
        [RespCommand("DUMP")]
        byte[] Dump(string key);

        /// <summary>RESTORE.</summary>
        [RespCommand("RESTORE")]
        void Restore(string key, RespExpiry expiry, byte[] serialized, bool replace);

        /// <summary>OBJECT, a container command.</summary>
        [RespCommand("OBJECT")]
        RespReply ObjectIntrospection(RespArgs args);

        /// <summary>MIGRATE.</summary>
        [RespCommand("MIGRATE")]
        RespReply Migrate(RespArgs args);

        /// <summary>SORT and SORT_RO - one operation, differing only in whether STORE is allowed.</summary>
        [RespCommand("SORT", "SORT_RO")]
        RespReply Sort(string key, RespArgs args, bool readOnly);

        /// <summary>LCS.</summary>
        [RespCommand("LCS")]
        RespReply LongestCommonSubsequence(string first, string second, RespArgs args);

        /// <summary>RESTORE-ASKING.</summary>
        [RespCommand("RESTORE-ASKING")]
        RespReply RestoreAsking(RespArgs args);
    }
}
