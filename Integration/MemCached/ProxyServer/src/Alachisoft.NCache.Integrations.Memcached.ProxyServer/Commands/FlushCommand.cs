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
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands
{
    class FlushCommand : AbstractCommand
    {
        private int _delay = 0;
        public int Delay
        {
            get { return _delay; }
            set { _delay = value; }
        }

        public FlushCommand(Opcode type, int delay)
            : base(type)
        {
            _delay = delay;
        }

        public FlushCommand(Opcode type)
            : base(type)
        {
            _delay = 0;
        }

        public override void Execute(IMemcachedProvider cacheProvider)
        {
            _result = cacheProvider.Flush_All(_delay);
        }
    }
}
