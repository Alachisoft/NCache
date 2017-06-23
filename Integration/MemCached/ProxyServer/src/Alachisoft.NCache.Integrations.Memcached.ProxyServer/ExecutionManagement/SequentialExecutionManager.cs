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
using System.Collections.Generic;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using System;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Parsing;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.ExecutionManagement
{
    public class SequentialExecutionManager : ExecutionManager
    {
        public SequentialExecutionManager(LogManager logManager)
            : base(logManager)
        { }


        Queue<AbstractCommand> _commandsQueue = new Queue<AbstractCommand>();
         
        private bool _alive = false;

        public override ConsumerStatus RegisterCommand(AbstractCommand command)
        {
            if (command == null)
                return ConsumerStatus.Running;
            lock (this)
            {
                _commandsQueue.Enqueue(command);
                if (_alive)
                    return ConsumerStatus.Running;
                return ConsumerStatus.Idle;
            }
        }

        public override void Start()
        {
            try
            {
                lock (this)
                {
                    if (_alive || _commandsQueue.Count == 0)
                        return;
                    _alive = true;
                }
                StartSequentialExecution();
            }
            catch (Exception e)
            {
                _logManager.Error("SequentialExecutionManager.Start()", " Failed to start sequential execution manager. Exception: " + e.Message);
                return;
            }
        }

        public override void Run()
        {
            try
            {
                StartSequentialExecution();
            }
            catch (Exception e)
            {
                _logManager.Error("SequentialExecutionManager.Run()", " Failed to start sequential execution manager. Exception: " + e.Message);
                return;
            }
        }

        public void StartSequentialExecution()
        {
            bool go = false;

            do
            {
                AbstractCommand command;
                lock (this)
                {
                    command = _commandsQueue.Dequeue();
                }
                try
                {
                    _logManager.Debug("SequentialExecutionManager", " Executing command : " + command.Opcode);
                    if (command.ErrorMessage == null)
                        command.Execute(_cacheProvider);
                    else
                        _logManager.Debug("SequentialExecutionManager", "\tCannot execute command: " + command.Opcode + "  Error:"+command.ErrorMessage);
                }
                catch (Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions.InvalidArgumentsException e)
                {
                    _logManager.Error("SequentialExecutionManager.StartSequentialExecution()", "\tError while executing command. CommandType = " + command.Opcode.ToString() + "  " + e.Message);
                    command.ExceptionOccured = true;
                    command.ErrorMessage = e.Message;
                }
                catch (Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions.CacheRuntimeException e)
                {
                    _logManager.Error("SequentialExecutionManager.StartSequentialExecution()", "\tError while executing command. CommandType = " + command.Opcode.ToString() + "  " + e.Message);
                    command.ExceptionOccured = true;
                    command.ErrorMessage = e.Message;
                }

                ConsumerStatus responseMgrStatus = _commandConsumer.RegisterCommand(command);

                lock (this)
                {
                    go = (responseMgrStatus==ConsumerStatus.Running) && _commandsQueue.Count > 0;
                }
            } while (go);


            lock (this)
            {
                if (_commandsQueue.Count > 0)
                {
                    ThreadPool.ExecuteTask(this);
                }
                else
                    _alive = false;
            }
            
            if(_commandConsumer!=null)
                _commandConsumer.Start();
        }

        private void StartSequentialExecution(object obj)
        {
            this.StartSequentialExecution();
        }
    }
}
