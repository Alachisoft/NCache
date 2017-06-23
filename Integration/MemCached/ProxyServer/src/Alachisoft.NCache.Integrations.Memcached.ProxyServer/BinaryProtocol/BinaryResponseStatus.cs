// Copyright (c) 2017 Alachisoft
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

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol
{
    enum BinaryResponseStatus : ushort
    {
        no_error = 0x0000,

        key_not_found = 0x0001,

        key_exists = 0x0002,

        value_too_large = 0x0003,

        invalid_arguments = 0x0004,

        item_not_stored = 0x0005,

        incr_decr_on_nonnumeric_value = 0x0006,

        unknown_commnad = 0x0081,

        out_of_memory = 0x0082
    }
}
