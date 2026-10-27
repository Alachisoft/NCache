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
    /// The cluster commands - 4 of them, on 4 members: all 1 declared abstract; see RespCompatibilityBase for which this build serves.
    /// <para>
    /// CLUSTER is answered only when keys are distributed by CRC16; otherwise the listener says
    /// so, which is a configuration state rather than a missing capability.
    /// </para>
    /// </summary>
    internal interface IRespClusterCommands
    {
        /// <summary>CLUSTER, a container command.</summary>
        [RespCommand("CLUSTER")]
        RespReply Cluster(RespArgs args);

        /// <summary>ASKING.</summary>
        [RespCommand("ASKING")]
        RespReply Asking(RespArgs args);

        /// <summary>READONLY.</summary>
        [RespCommand("READONLY")]
        RespReply ReadOnly(RespArgs args);

        /// <summary>READWRITE.</summary>
        [RespCommand("READWRITE")]
        RespReply ReadWrite(RespArgs args);
    }
}
