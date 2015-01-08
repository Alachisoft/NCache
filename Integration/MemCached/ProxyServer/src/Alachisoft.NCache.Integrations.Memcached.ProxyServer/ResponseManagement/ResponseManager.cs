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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using System.Net.Sockets;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.ResponseManagement
{
    abstract class ResponseManager : ICommandConsumer
    {
        protected Queue<AbstractCommand> _responseQueue = new Queue<AbstractCommand>();

        protected LogManager _logManager;

        protected NetworkStream _stream;
        protected bool _alive=false;

        public ResponseManager(NetworkStream stream, LogManager logManager)
        {
            _stream = stream;
            _logManager = logManager;
        }

        public ConsumerStatus RegisterCommand(Commands.AbstractCommand command)
        {
            lock (this)
            {
                _responseQueue.Enqueue(command);
                if (_alive)
                    return ConsumerStatus.Running;
                return ConsumerStatus.Idle;
            }
        }
        public abstract void Start();

        protected MemTcpClient _memTcpClient;
        public MemTcpClient MemTcpClient
        {
            set { _memTcpClient = value; }
        }

        protected bool _disposed = false;
        public bool Disposed
        {
            get { return _disposed; }
            internal set { _disposed = value; }
        }
    }
}
