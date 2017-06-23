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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands
{
    public abstract class AbstractCommand
    {
        protected Opcode _opcode;
        protected string _errorMessage;
        protected bool _noReply;
        protected int _opaque;

        protected OperationResult _result;

        //used in binary commands
        public int Opaque
        {
            get { return _opaque; }
            set { _opaque = value; }
        }

        public Opcode Opcode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        public bool ExceptionOccured
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public bool NoReply
        {
            get { return _noReply; }
            set { _noReply = value; }
        }

        public OperationResult OperationResult
        {
            get { return _result; }
            set { _result = value; }
        }

        public AbstractCommand(Opcode type)
        {
            _opcode = type;
        }
        public abstract void Execute(IMemcachedProvider cacheProvider);
    }
}
