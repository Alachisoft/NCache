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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Parsing
{
    abstract class ProtocolParser : IThreadPoolTask
    {

        private ILogger _logger;
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        /// <summary>
        /// Current state of parser
        /// </summary>
        protected ParserState _state = ParserState.Ready;

        /// <summary>
        /// Data Stream shared b/w ClientHandler and parser
        /// </summary>
        protected DataStream _inputDataStream;

        /// <summary>
        ///Raw data buffer used by parser to store raw data until complete command recieved
        /// </summary>
        protected byte[] _rawData = new byte[1048575];
        protected int _rawDataOffset = 0;

        /// <summary>
        /// Currently under-process command
        /// </summary>
        protected AbstractCommand _command;

        protected LogManager _logManager;

        public ProtocolParser(DataStream inputStream, MemTcpClient parent, LogManager logManager)
        {
            _inputDataStream = inputStream;
            _memTcpClient = parent;
            _logManager = logManager;
        }

        /// <summary>
        /// Gets or sets state of parsers activity
        /// </summary>
        public bool Alive
        {
            get;
            set;
        }

        /// <summary>
        /// Current state of parser
        /// </summary>
        public ParserState State
        {
            get{return _state;}
            set { _state=value;} 
        }

        protected bool _disposed;
        public bool Disposed
        {
            get { return _disposed; }
            internal set { _disposed = value; }
        }

        protected MemTcpClient _memTcpClient;
        /// <summary>
        /// Starts parsing command from inputStream.
        /// </summary>
        public abstract void StartParser();


        protected ICommandConsumer _commandConsumer;
        public ICommandConsumer CommandConsumer
        {
            get { return _commandConsumer; }
            set { _commandConsumer = value; }
        }

        /// <summary>
        /// Dispatch created command for execution.
        /// </summary>
        public void Dispatch()
        {
            try
            {
                lock (this)
                {
                    this.State = ParserState.Ready;
                    if (_inputDataStream.Lenght > 0)
                    {
                        ThreadPool.ExecuteTask(this);
                    }
                    else
                        this.Alive = false;
                }
            }
            catch (Exception e)
            {
                _logManager.Error("ProtocolParser.Dispatch()"," Failed to dispatch parsed command. Exception: "+e.Message);
                return;
            }

            if (_commandConsumer != null)
                _commandConsumer.Start();
        }

        private void StartParser(object obj)
        {
            this.StartParser();
        }

        #region IThreadPoolTask implemented members
        public void Run()
        {
            this.StartParser();
        }

        public void Run(object obj)
        {
            this.Run();
        }

        #endregion
    }
}
