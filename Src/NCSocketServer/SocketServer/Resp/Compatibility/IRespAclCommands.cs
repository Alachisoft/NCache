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
    /// The access control commands - 1 of them, on 1 members: all 1 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespAclCommands
    {
        /// <summary>ACL. NCache authorises a user for a cache, not for a key pattern, so there is nothing per-key to describe.</summary>
        [RespCommand("ACL")]
        RespReply Acl(RespArgs args);
    }
}
