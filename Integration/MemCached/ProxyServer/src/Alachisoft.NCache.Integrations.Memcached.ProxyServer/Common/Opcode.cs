// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common
{
    public enum Opcode : byte
    {
        Get = 0x00,

        Set = 0x01,

        Add = 0x02,

        Replace = 0x03,

        Delete = 0x04,

        Increment = 0x05,

        Decrement = 0x06,

        Quit = 0x07,

        Flush = 0x08,

        GetQ = 0x09,

        No_op = 0x0A,

        Version = 0x0B,

        GetK = 0x0C,

        GetKQ = 0x0D,

        Append = 0x0E,

        Prepend = 0x0F,

        Stat = 0x10,

        SetQ = 0x11,

        AddQ = 0x12,

        ReplaceQ = 0x13,

        DeleteQ = 0x14,

        IncrementQ = 0x15,

        DecrementQ = 0x16,

        QuitQ = 0x17,

        FlushQ = 0x18,

        AppendQ = 0x19,

        PrependQ = 0x1A,

        //for text protocol
        Gets,

        CAS,

        Touch,

        Slabs_Reassign,

        Slabs_Automove,

        Verbosity,

        Invalid_Command,

        //binary unknown command
        unknown_command
    }
}
