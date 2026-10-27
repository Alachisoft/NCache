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
    /// The bitmaps commands - 7 of them, on 6 members: all 6 declared abstract; see RespCompatibilityBase for which this build serves.
    /// </summary>
    internal interface IRespBitmapCommands
    {
        /// <summary>SETBIT.</summary>
        [RespCommand("SETBIT")]
        long SetBit(string key, long offset, bool value);

        /// <summary>GETBIT.</summary>
        [RespCommand("GETBIT")]
        bool GetBit(string key, long offset);

        /// <summary>BITCOUNT.</summary>
        [RespCommand("BITCOUNT")]
        long BitCount(string key, RespArgs range);

        /// <summary>BITPOS.</summary>
        [RespCommand("BITPOS")]
        long BitPosition(string key, bool value, RespArgs range);

        /// <summary>BITOP.</summary>
        [RespCommand("BITOP")]
        long BitOperation(string operation, string destination, IList<string> sources);

        /// <summary>BITFIELD and BITFIELD_RO.</summary>
        [RespCommand("BITFIELD", "BITFIELD_RO")]
        RespReply BitField(string key, RespArgs operations, bool readOnly);
    }
}
