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
    /// The HyperLogLog commands - 5 of them, on 5 members: all 5 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespHyperLogLogCommands
    {
        /// <summary>PFADD.</summary>
        [RespCommand("PFADD")]
        bool HyperLogLogAdd(string key, IList<byte[]> elements);

        /// <summary>PFCOUNT.</summary>
        [RespCommand("PFCOUNT")]
        long HyperLogLogCount(IList<string> keys);

        /// <summary>PFMERGE.</summary>
        [RespCommand("PFMERGE")]
        void HyperLogLogMerge(string destination, IList<string> sources);

        /// <summary>PFDEBUG.</summary>
        [RespCommand("PFDEBUG")]
        RespReply HyperLogLogDebug(RespArgs args);

        /// <summary>PFSELFTEST.</summary>
        [RespCommand("PFSELFTEST")]
        RespReply HyperLogLogSelfTest();
    }
}
