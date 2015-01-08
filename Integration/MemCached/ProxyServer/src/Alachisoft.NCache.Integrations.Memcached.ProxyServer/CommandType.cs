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

namespace MemcachedProxyForNCache
{
    enum Opcode : byte
    {
        get = 0x00,

        set = 0x01,

        add = 0x02,

        replace = 0x03,

        delete = 0x04,

        incr = 0x05,

        decr = 0x06,

        quit = 0x07,

        flush_all = 0x08,

        //getq

        no_op = 0x0A,

        version = 0x0B,

        getk = 0x0C,

        //getkq

        append = 0x0E,

        prepend = 0x0F,

        stats = 0x10,

        //quiet commands

        verbosity = 0x1B,

        touch = 0x1C,

        cas,

        gets,

        slabs_automove,

        slabs_reassign,

        invalid_command
    }
}
