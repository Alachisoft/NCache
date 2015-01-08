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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.ExecutionManagement
{
    public abstract class ExecutionManager : IThreadPoolTask , ICommandConsumer
    {

        protected IMemcachedProvider _cacheProvider;
        protected LogManager _logManager;

        public ExecutionManager(LogManager logManager)
        {
            _cacheProvider = CacheFactory.CreateCacheProvider(MemConfiguration.CacheName);
            _logManager = logManager;
        }

        protected ICommandConsumer _commandConsumer;
        public ICommandConsumer CommandConsumer
        {
            get { return _commandConsumer; }
            set { _commandConsumer = value; }
        }

        protected bool _disposed;
        public bool Disposed
        {
            get { return _disposed; }
            internal set { _disposed = value; }
        }

        public abstract void Start();
        public abstract ConsumerStatus RegisterCommand(AbstractCommand command);

        public abstract void Run();
        public void Run(object obj)
        {
            this.Run();
        }
    }
}
