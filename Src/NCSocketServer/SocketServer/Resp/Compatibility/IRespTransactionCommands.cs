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
    /// The transactions commands - 5 of them, on 5 members: all 5 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// All refused, and the reason is specific: a Redis transaction is a queued multi-key batch
    /// with rollback, and NCache locking is single-key. MULTI that queued commands and then could
    /// not honour EXEC atomically would be worse than no MULTI, because a client cannot tell the
    /// difference until it has already relied on it.
    /// </para>
    /// </summary>
    internal interface IRespTransactionCommands
    {
        /// <summary>MULTI.</summary>
        [RespCommand("MULTI")]
        void Multi();

        /// <summary>EXEC.</summary>
        [RespCommand("EXEC")]
        RespReply Exec();

        /// <summary>DISCARD.</summary>
        [RespCommand("DISCARD")]
        void Discard();

        /// <summary>WATCH.</summary>
        [RespCommand("WATCH")]
        void Watch(IList<string> keys);

        /// <summary>UNWATCH.</summary>
        [RespCommand("UNWATCH")]
        void Unwatch();
    }
}
