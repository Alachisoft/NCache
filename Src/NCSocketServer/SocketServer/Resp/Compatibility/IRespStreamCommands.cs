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
    /// The streams commands - 15 of them, on 14 members: all 14 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespStreamCommands
    {
        /// <summary>XADD. A null id is the <c>*</c> form, which asks the server to assign one.</summary>
        [RespCommand("XADD")]
        RespStreamId StreamAdd(string key, RespStreamId? id, IList<RespField> fields, RespArgs options);

        /// <summary>XLEN.</summary>
        [RespCommand("XLEN")]
        long StreamLength(string key);

        /// <summary>XRANGE and XREVRANGE.</summary>
        [RespCommand("XRANGE", "XREVRANGE")]
        IList<RespStreamEntryView> StreamRange(string key, RespArgs args, bool reverse);

        /// <summary>XDEL.</summary>
        [RespCommand("XDEL")]
        long StreamDelete(string key, IList<RespStreamId> ids);

        /// <summary>XTRIM.</summary>
        [RespCommand("XTRIM")]
        long StreamTrim(string key, RespArgs args);

        /// <summary>XREAD.</summary>
        [RespCommand("XREAD")]
        IList<RespStreamSlice> StreamRead(RespArgs args);

        /// <summary>XGROUP, a container command.</summary>
        [RespCommand("XGROUP")]
        RespReply StreamGroup(RespArgs args);

        /// <summary>XREADGROUP.</summary>
        [RespCommand("XREADGROUP")]
        IList<RespStreamSlice> StreamReadGroup(RespArgs args);

        /// <summary>XACK.</summary>
        [RespCommand("XACK")]
        long StreamAcknowledge(string key, string group, IList<RespStreamId> ids);

        /// <summary>XINFO, a container command.</summary>
        [RespCommand("XINFO")]
        RespReply StreamInfo(RespArgs args);

        /// <summary>XPENDING.</summary>
        [RespCommand("XPENDING")]
        RespReply StreamPending(RespArgs args);

        /// <summary>XCLAIM.</summary>
        [RespCommand("XCLAIM")]
        RespReply StreamClaim(RespArgs args);

        /// <summary>XAUTOCLAIM.</summary>
        [RespCommand("XAUTOCLAIM")]
        RespReply StreamAutoClaim(RespArgs args);

        /// <summary>XSETID.</summary>
        [RespCommand("XSETID")]
        void StreamSetId(string key, RespStreamId id, RespArgs args);
    }
}
