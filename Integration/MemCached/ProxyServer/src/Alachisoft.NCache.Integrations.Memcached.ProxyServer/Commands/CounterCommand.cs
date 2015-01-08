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
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands
{
    class CounterCommand : AbstractCommand
    {
        private string _key;
        public String Key
        {
            get { return _key; }
            set { _key = value; }
        }

        private ulong _delta;
        public ulong Delta
        {
            get { return _delta; }
            set { _delta = value; }
        }

        private ulong _initialValue;
        public ulong InitialValue
        {
            get { return _initialValue; }
            set { _initialValue = value; }
        }

        private long _expirationTimeInSeconds = uint.MaxValue;
        public long ExpirationTimeInSeconds
        {
            get { return _expirationTimeInSeconds; }
            set { _expirationTimeInSeconds = value; }
        }

        private ulong _cas = 0;
        public ulong CAS
        {
            get { return _cas; }
            set { _cas = value; }
        }

        public CounterCommand(Opcode cmdType)
            : base(cmdType)
        {
        }

        public override void Execute(IMemcachedProvider cacheProvider)
        {
                switch (_opcode)
                {
                    case Opcode.Increment:
                    case Opcode.IncrementQ:
                        _result = cacheProvider.Increment(_key, _delta, _initialValue, _expirationTimeInSeconds, _cas);
                        break;

                    case Opcode.Decrement:
                    case Opcode.DecrementQ:
                        _result = cacheProvider.Decrement(_key, Delta, _initialValue, _expirationTimeInSeconds, _cas);
                        break;
                }
        }
    }
}
