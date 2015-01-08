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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol;
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.ResponseManagement
{
    class BinaryResponseManager: ResponseManager
    {
        private DataStream _responseStream = new DataStream(10240);

        public BinaryResponseManager(NetworkStream stream, LogManager logManager)
            : base(stream, logManager)
        {
        }



        public override void Start()
        {
            try
            {
                lock (this)
                {
                    if (_alive || _responseQueue.Count == 0)
                        return;
                    _alive = true;
                }
                StartSendingResponse();
            }
            catch (Exception e)
            {
                _logManager.Error("BinaryResponseManager", "\tFailed to process response. " + e.Message);
                return;
            }
        }

        private void StartSendingResponse()
        {
            bool go = false;

            do
            {
                if (_disposed)
                    return;
                AbstractCommand command = _responseQueue.Dequeue();
                byte[] outpuBuffer = BuildBinaryResponse(command);
                try
                {
                    if (this._disposed)
                        return;
                    if(outpuBuffer!=null)
                        _stream.Write(outpuBuffer, 0, outpuBuffer.Length);
                    if (command.Opcode == Opcode.Quit || command.Opcode == Opcode.QuitQ)
                    {
                        TcpNetworkGateway.DisposeClient(_memTcpClient);
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logManager.Error("BinaryResponseManager.StartSendingResponse()", "\tFailed to send response. " + e.Message);
                    return;
                }

                lock (this)
                {
                    _alive = go = _responseQueue.Count > 0;
                }
            } while (go);
        }


        private byte[] BuildBinaryResponse(AbstractCommand command)
        {
            _logManager.Debug("BinaryResponseManager.BuildBinaryResponse()", "Building response for command : " + command.Opcode);

            switch (command.Opcode)
            {
                case Opcode.Set:
                case Opcode.Add:
                case Opcode.Replace:
                case Opcode.Append:
                case Opcode.Prepend:
                    _responseStream.Write(BinaryResponseBuilder.BuildStorageResponse(command as StorageCommand));
                    break;
                case Opcode.SetQ:
                case Opcode.AddQ:
                case Opcode.ReplaceQ:
                case Opcode.AppendQ:
                case Opcode.PrependQ:
                    _responseStream.Write(BinaryResponseBuilder.BuildStorageResponse(command as StorageCommand));
                    if(!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.Get:
                case Opcode.GetK:
                    _responseStream.Write(BinaryResponseBuilder.BuildGetResponse(command as GetCommand));
                    break;

                case Opcode.GetQ:
                case Opcode.GetKQ:
                    _responseStream.Write(BinaryResponseBuilder.BuildGetResponse(command as GetCommand));
                    if (!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.No_op:
                    _responseStream.Write(BinaryResponseBuilder.BuildNoOpResponse(command as NoOperationCommand));
                    break;

                case Opcode.Delete:
                    _responseStream.Write(BinaryResponseBuilder.BuildDeleteResponse(command as DeleteCommand));
                    break;

                case Opcode.DeleteQ: 
                    _responseStream.Write(BinaryResponseBuilder.BuildDeleteResponse(command as DeleteCommand));
                    if (!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.Flush:
                    _responseStream.Write(BinaryResponseBuilder.BuildFlushResponse(command as FlushCommand));
                    break;
                case Opcode.FlushQ:
                    _responseStream.Write(BinaryResponseBuilder.BuildFlushResponse(command as FlushCommand));
                    if (!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.Increment:
                case Opcode.Decrement:
                    _responseStream.Write(BinaryResponseBuilder.BuildCounterResponse(command as CounterCommand));
                    break;
                case Opcode.IncrementQ:
                case Opcode.DecrementQ:
                    _responseStream.Write(BinaryResponseBuilder.BuildCounterResponse(command as CounterCommand));
                    if (!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.Stat:
                    _responseStream.Write(BinaryResponseBuilder.BuildStatsResponse(command as StatsCommand));
                    break;

                case Opcode.Quit:
                    _responseStream.Write(BinaryResponseBuilder.BuildQuitResponse(command as QuitCommand));
                    break;
                case Opcode.QuitQ:
                    _responseStream.Write(BinaryResponseBuilder.BuildQuitResponse(command as QuitCommand));
                    if (!command.ExceptionOccured)
                        return null;
                    break;

                case Opcode.Version:
                    _responseStream.Write(BinaryResponseBuilder.BuildVersionResponse(command as VersionCommand));
                    break;
                default:
                    _responseStream.Write(BinaryResponseBuilder.BuildInvalidResponse(command as InvalidCommand));
                    break;
            }
            return _responseStream.ReadAll();
        }
    }
}
