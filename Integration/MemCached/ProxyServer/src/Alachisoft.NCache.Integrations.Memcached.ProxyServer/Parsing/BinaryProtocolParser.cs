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
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.BinaryProtocol;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Parsing
{
    class BinaryProtocolParser : ProtocolParser
    {
        private BinaryRequestHeader _requestHeader;

        public BinaryProtocolParser(DataStream inputSream, MemTcpClient parent, LogManager logManager)
            : base(inputSream, parent, logManager)
        {
        }

        private int CommandDataSize
        {
            get { return _requestHeader == null ? 0 : _requestHeader.TotalBodyLenght; }
        }

        public override void StartParser()
        {
            try
            {
                int noOfBytes = 0;
                bool go = true;

                do
                {
                    lock (this)
                    {
                        if (_inputDataStream.Lenght == 0 && this.State != ParserState.ReadyToDispatch)
                        {
                            this.Alive = false;
                            return;
                        }
                    }

                    switch (this.State)
                    {
                        case ParserState.Ready:
                            noOfBytes = _inputDataStream.Read(_rawData, _rawDataOffset, 24 - _rawDataOffset);
                            _rawDataOffset += noOfBytes;
                            if (_rawDataOffset == 24)
                            {
                                _rawDataOffset = 0;
                                this.Parse();
                            }
                            break;
                        case ParserState.WaitingForData:
                            noOfBytes = _inputDataStream.Read(_rawData, _rawDataOffset, this.CommandDataSize - _rawDataOffset);
                            _rawDataOffset += noOfBytes;
                            if (_rawDataOffset == this.CommandDataSize)
                            {
                                _rawDataOffset = 0;
                                this.Build();
                            }
                            break;
                        case ParserState.ReadyToDispatch:

                            _logManager.Debug("BinaryProtocolParser", _command.Opcode + " command recieved.");
                            ConsumerStatus executionMgrStatus = _commandConsumer.RegisterCommand(_command);//this.RegisterForExecution(_command);
                            this.State = ParserState.Ready;
                            go = executionMgrStatus == ConsumerStatus.Running;
                            break;
                    }

                } while (go);
            }
            catch (Exception e)
            {
                _logManager.Fatal("BinaryProtocolParser", "Exception occured while parsing text command. " + e.Message);
                TcpNetworkGateway.DisposeClient(_memTcpClient);
                return;
            }
            this.Dispatch();
        }

        void Parse()
        {
            _requestHeader = new BinaryRequestHeader(_rawData);
            this.State = ParserState.WaitingForData;
            ProcessHeader();
        }

        void ProcessHeader()
        {
            bool valid = true;
            if (_requestHeader.TotalBodyLenght < _requestHeader.KeyLength + _requestHeader.ExtraLength)
                valid = false;
            if (_requestHeader.MagicByte != Magic.Request)
                valid = false;
            if (!valid)
            {
                _requestHeader = new BinaryRequestHeader();
                _requestHeader.Opcode = Opcode.Invalid_Command;
                this.State = ParserState.ReadyToDispatch;
            }

            if (_requestHeader.TotalBodyLenght == 0)
                this.Build();
        }

        void Build()
        {
            switch (_requestHeader.Opcode)
            {
                //Get command
                case Opcode.Get:
                //GetK command
                case Opcode.GetK:
                    CreateGetCommand(_requestHeader.Opcode, false);
                    break;

                //Set command
                case Opcode.Set:
                //Add command
                case Opcode.Add:
                //Replace command
                case Opcode.Replace:
                //Append command
                case Opcode.Append:
                //Prepend command
                case Opcode.Prepend:
                    CreateStorageCommand(_requestHeader.Opcode, false);
                    break;

                //Delete command
                case Opcode.Delete:
                    CreateDeleteCommand(_requestHeader.Opcode, false);
                    break;

                //Increment command
                case Opcode.Increment:
                //Decrement command
                case Opcode.Decrement:
                    CreateCounterCommand(_requestHeader.Opcode, false);
                    break;

                //Quit command
                case Opcode.Quit:
                    _command = new QuitCommand(_requestHeader.Opcode);
                    break;

                //Flush command
                case Opcode.Flush:
                    CreateFlushCommand(_requestHeader.Opcode,false);
                    break;

                //GetQ command
                case Opcode.GetQ:
                //GetKQ command
                case Opcode.GetKQ:
                    CreateGetCommand(_requestHeader.Opcode, true);
                    break;

                //No-op command
                case Opcode.No_op:
                    _command = new NoOperationCommand();
                    break;

                //Version command
                case Opcode.Version:
                    CreateVersionCommand();
                    break;

                //Stat command
                case Opcode.Stat:
                    CreateStatsCommand();
                    break;

                //SetQ command
                case Opcode.SetQ:
                //AddQ command
                case Opcode.AddQ:
                //ReplaceQ command
                case Opcode.ReplaceQ:
                //AppendQ command
                case Opcode.AppendQ:
                //PrependQ command
                case Opcode.PrependQ:
                    CreateStorageCommand(_requestHeader.Opcode, true);
                    break;

                //DeleteQ command
                case Opcode.DeleteQ:
                    CreateDeleteCommand(_requestHeader.Opcode,true);
                    break;

                //IncrementQ command
                case Opcode.IncrementQ:
                //DecrementQ command
                case Opcode.DecrementQ:
                    CreateCounterCommand(_requestHeader.Opcode, true);
                    break;

                //QuitQ command
                case Opcode.QuitQ:
                    _command = new QuitCommand(_requestHeader.Opcode);
                    _command.NoReply = true;
                    break;

                //FlushQ command
                case Opcode.FlushQ:
                    CreateFlushCommand(_requestHeader.Opcode, true);
                    break;

                default:
                    CreateInvalidCommand();
                    _command.Opcode = Opcode.unknown_command;
                    break;
            }

            _command.Opaque = _requestHeader.Opaque;

            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateStorageCommand(Opcode cmdType, bool noreply)
        {
            int offset = 0;
            uint flags = 0;
            long exp = 0;

            switch (cmdType)
            {
                case Opcode.Append:
                case Opcode.Prepend:
                case Opcode.AppendQ:
                case Opcode.PrependQ:
                    break;
                default:
                    flags = MemcachedEncoding.BinaryConverter.ToUInt32(_rawData,offset);
                    exp = (long)MemcachedEncoding.BinaryConverter.ToInt32(_rawData,offset+4);;
                    offset += 8;
                    break;
            }
            string key = MemcachedEncoding.BinaryConverter.GetString(_rawData, offset, _requestHeader.KeyLength);
            byte[] value = new byte[_requestHeader.ValueLength];
            Buffer.BlockCopy(_rawData, _requestHeader.KeyLength + offset, value, 0, _requestHeader.ValueLength);
            StorageCommand cmd = new StorageCommand(cmdType, key, flags, exp, _requestHeader.CAS, _requestHeader.ValueLength);
            cmd.DataBlock = value;
            cmd.NoReply = noreply;
            cmd.Opaque = _requestHeader.Opaque;
            _command = cmd;
        }

        private void CreateGetCommand(Opcode cmdType, bool noreply)
        {
            string key = MemcachedEncoding.BinaryConverter.GetString(_rawData, 0, _requestHeader.KeyLength);
            GetCommand cmd = new GetCommand(cmdType);
            cmd.Keys = new string[] { key };
            cmd.NoReply = noreply;
            _command = cmd;
        }

        private void CreateDeleteCommand(Opcode type, bool noreply)
        {
            string key = MemcachedEncoding.BinaryConverter.GetString(_rawData, 0, _requestHeader.KeyLength);
            DeleteCommand cmd = new DeleteCommand(type);
            cmd.Key = key;
            cmd.NoReply = noreply;
            _command = cmd;
        }

        private void CreateCounterCommand(Opcode cmdType, bool noreply)
        {
            CounterCommand cmd = new CounterCommand(cmdType);
            cmd.Delta = MemcachedEncoding.BinaryConverter.ToUInt64(_rawData, 0);
            cmd.InitialValue = MemcachedEncoding.BinaryConverter.ToUInt64(_rawData, 8);
            cmd.ExpirationTimeInSeconds = (long)MemcachedEncoding.BinaryConverter.ToUInt32(_rawData, 16);
            cmd.Key = MemcachedEncoding.BinaryConverter.GetString(_rawData, 20, _requestHeader.KeyLength);
            cmd.CAS = _requestHeader.CAS;
            cmd.NoReply = noreply;
            _command = cmd;
        }

        public void CreateFlushCommand(Opcode type, bool noreply)
        {
            int delay = 0;
            if (_requestHeader.ExtraLength > 0)
                delay = ((_rawData[0] << 24) | (_rawData[1] << 16) | (_rawData[2] << 8) | _rawData[3]);
            _command = new FlushCommand(type,delay);
            _command.NoReply = noreply;
        }

        public void CreateVersionCommand()
        {
            _command = new VersionCommand();
        }

        public void CreateStatsCommand()
        {
            string argument = null;
            if (_requestHeader.KeyLength > 0)
                argument = MemcachedEncoding.BinaryConverter.GetString(_rawData, 0, _requestHeader.KeyLength);
            _command = new StatsCommand(argument);
        }

        public void CreateVerbosityCommand()
        {
            int level = ((_rawData[0] << 24) | (_rawData[1] << 16) | (_rawData[2] << 8) | _rawData[3]);
            _command = new VerbosityCommand(level);
        }

        public void CreateInvalidCommand()
        {
            _command = new InvalidCommand();
        }
    }
}
