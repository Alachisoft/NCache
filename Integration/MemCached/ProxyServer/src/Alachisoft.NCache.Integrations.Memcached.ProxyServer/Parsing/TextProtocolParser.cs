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
using System.IO;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands;
using System.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using System.Collections;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Parsing
{
    class TextProtocolParser : ProtocolParser
    {
        public TextProtocolParser(DataStream inputStream, MemTcpClient parent, LogManager logManager)
            : base(inputStream, parent, logManager)
        {
        }


        /// <summary>
        /// Parses an <see cref="Alachisoft.NCache.Integrations.Memcached.ProxyServer.Commands.AbstractCommand"/> from string
        /// </summary>
        /// <param name="command">string command to pe parsed</param>
        public void Parse(string command)
        {
            string[] commandParts = command.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts == null || commandParts.Length == 0)
            {
                _command = new InvalidCommand();
                this.State = ParserState.ReadyToDispatch;
                return;
            }

            string arguments = null;
            if (commandParts.Length > 1)
                arguments = commandParts[1];

            switch (commandParts[0])
            {
                case "get":
                    CreateRetrievalCommand(arguments, Opcode.Get);
                    break;
                case "gets":
                    CreateRetrievalCommand(arguments, Opcode.Gets);
                    break;

                case "set":
                    CreateStorageCommand(arguments, Opcode.Set);
                    break;
                case "add":
                    CreateStorageCommand(arguments, Opcode.Add);
                    break;
                case "replace":
                    CreateStorageCommand(arguments, Opcode.Replace);
                    break;
                case "append":
                    CreateStorageCommand(arguments, Opcode.Append);
                    break;
                case "prepend":
                    CreateStorageCommand(arguments, Opcode.Prepend);
                    break;
                case "cas":
                    CreateStorageCommand(arguments, Opcode.CAS);
                    break;

                case "delete":
                    CreateDeleteCommand(arguments);
                    break;

                case "incr":
                    CreateCounterCommand(arguments, Opcode.Increment);
                    break;
                case "decr":
                    CreateCounterCommand(arguments, Opcode.Decrement);
                    break;

                case "touch":
                    CreateTouchCommand(arguments);
                    break;

                case "flush_all":
                    CreateFlushCommand(arguments);
                    break;
                case "stats":
                    CreateStatsCommand(arguments);
                    break;
                case "slabs":
                    break;
                case "version":
                    CreateVersionCommand(arguments);
                    break;
                case "verbosity":
                    CreateVerbosityCommand(arguments);
                    break;
                case "quit":
                    CreateQuitCommand();
                    break;
                default:
                    CreateInvalidCommand();
                    break;
            }
        }

        private void CreateQuitCommand()
        {
            _command = new QuitCommand(Opcode.Quit);
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateVerbosityCommand(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                CreateInvalidCommand();
                return;
            }
            string[] argumentsArray;
            argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length > 2 || argumentsArray.Length < 1)
            {
                CreateInvalidCommand();
                return;
            }

            int level;
            bool noreply = false;
            try
            {
                level = int.Parse(argumentsArray[0]);
                noreply = (argumentsArray.Length > 1 && argumentsArray[1] == "noreply") ? true : false;
            }
            catch (Exception)
            {
                CreateInvalidCommand();
                return;
            }

            _command = new VerbosityCommand();
            _command.NoReply = noreply;
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateVersionCommand(string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                CreateInvalidCommand();
                return;
            }

            _command = new VersionCommand();
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateStatsCommand(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                _command = new StatsCommand();
                this.State = ParserState.ReadyToDispatch;
                return;
            }

            string[] argumentsArray;
            argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            switch (argumentsArray[0])
            {
                case "settings":
                case "items":
                case "sizes":
                case "slabs":
                    _command = new StatsCommand(argumentsArray[0]);
                    this.State = ParserState.ReadyToDispatch;
                    break;
                default:
                    CreateInvalidCommand();
                    break;
            }
        }


        private void CreateSlabsCommand(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                CreateInvalidCommand();
                return;
            }
            string[] argumentsArray;
            argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length > 3 || argumentsArray.Length < 2)
            {
                CreateInvalidCommand();
                return;
            }
            try
            {
                bool noreply = false;
                switch (argumentsArray[0])
                {
                    case "reasign":
                        int sourceClass = int.Parse(argumentsArray[1]);
                        int destClass = int.Parse(argumentsArray[2]);
                        noreply = argumentsArray.Length > 2 && argumentsArray[3] == "noreply";
                        _command = new SlabsReassignCommand(sourceClass, destClass);
                        _command.NoReply = noreply;
                        break;
                    case "automove":
                        int option = int.Parse(argumentsArray[1]);
                        noreply = argumentsArray.Length > 2 && argumentsArray[2] == "noreply";
                        _command = new SlabsAuomoveCommand(option);
                        _command.NoReply = noreply;
                        break;
                    default:
                        _command = new InvalidCommand();
                        break;
                }
                this.State = ParserState.ReadyToDispatch;
            }
            catch (Exception)
            {
                CreateInvalidCommand();
            }
        }

        private void CreateTouchCommand(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                CreateInvalidCommand();
                return;
            }

            string[] argumentsArray;
            argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length > 3 || argumentsArray.Length < 2)
            {
                CreateInvalidCommand();
                return;
            }

            string key;
            long expTime;
            bool noreply = false;

            try
            {
                key = argumentsArray[0];
                expTime = long.Parse(argumentsArray[1]);
                if (argumentsArray.Length > 2 && argumentsArray[2] == "noreply")
                    noreply = true;
            }
            catch (Exception)
            {
                CreateInvalidCommand("CLIENT_ERROR bad command line format");
                return;
            }

            _command = new TouchCommand(key, expTime);
            _command.NoReply = noreply;
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateFlushCommand(string arguments)
        {
            int delay = 0;
            bool noreply = false;

            string[] argumentsArray;
            if (string.IsNullOrEmpty(arguments))
            {
                delay = 0;
            }
            else
            {
                argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (argumentsArray.Length > 2)
                {
                    CreateInvalidCommand();
                    return;
                }

                try
                {
                    delay = int.Parse(argumentsArray[0]);
                    if (argumentsArray.Length > 1 && argumentsArray[1] == "noreply")
                        noreply = true;
                }
                catch (Exception e)
                {
                    CreateInvalidCommand("CLIENT_ERROR bad command line format");
                    return;
                }
            }

            _command = new FlushCommand(Opcode.Flush,delay);
            _command.NoReply = noreply;
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateCounterCommand(string arguments, Opcode cmdType)
        {
            string[] argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (argumentsArray.Length > 3 || argumentsArray.Length < 2)
            {
                CreateInvalidCommand();
                return;
            }

            CounterCommand command = new CounterCommand(cmdType);

            try
            {
                command.Key = argumentsArray[0];
                command.Delta = ulong.Parse(argumentsArray[1]);
                if (argumentsArray.Length > 2 && argumentsArray[2] == "noreply")
                    command.NoReply = true;
            }
            catch (Exception)
            {
                command.ErrorMessage = "CLIENT_ERROR bad command line format";
            }

            _command = command;
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateDeleteCommand(string arguments)
        {
            if (String.IsNullOrEmpty(arguments))
            {
                CreateInvalidCommand();
                return;
            }

            string[] argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length > 2 || argumentsArray.Length < 1)
            {
                CreateInvalidCommand();
                return;
            }

            DeleteCommand command = new DeleteCommand(Opcode.Delete);
            switch (argumentsArray.Length)
            {
                case 1:
                    command.Key = argumentsArray[0];
                    break;
                case 2:
                    if (argumentsArray[1] != "noreply")
                        command.ErrorMessage = "CLIENT_ERROR bad command line format. Usage: delete <key> [noreply]";
                    else
                    {
                        command.Key = argumentsArray[0];
                        command.NoReply = true;
                    }
                    break;
                default:
                    command.ErrorMessage = "CLIENT_ERROR bad command line format. Usage: delete <key> [noreply]";
                    break;
            }
            _command = command;
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateRetrievalCommand(string arguments, Opcode cmdType)
        {
            if (arguments == null)
            {
                CreateInvalidCommand();
                return;
            }

            string[] argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length == 0)
            {
                _command = new InvalidCommand();
            }
            else
            {
                GetCommand getCommand = new GetCommand(cmdType);
                getCommand.Keys = argumentsArray;
                _command = getCommand;
            }
            this.State = ParserState.ReadyToDispatch;
        }

        private void CreateStorageCommand(string arguments, Opcode cmdType)
        {
            if (arguments == null)
            {
                CreateInvalidCommand();
                return;
            }

            string[] argumentsArray = arguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (argumentsArray.Length < 4 || argumentsArray.Length > 6)
            {
                CreateInvalidCommand("CLIENT_ERROR bad command line format");
                return;
            }
            else
            {
                try
                {
                    string key = argumentsArray[0];
                    uint flags = UInt32.Parse(argumentsArray[1]);
                    long expTime = long.Parse(argumentsArray[2]);
                    int bytes = int.Parse(argumentsArray[3]);

                    if (cmdType == Opcode.CAS)
                    {
                        ulong casUnique = ulong.Parse(argumentsArray[4]);
                        _command = new StorageCommand(cmdType, key, flags, expTime, casUnique, bytes);
                        if (argumentsArray.Length > 5 && argumentsArray[5] == "noreply")
                            _command.NoReply = true;
                    }
                    else
                    {
                        _command = new StorageCommand(cmdType, key, flags, expTime, bytes);
                        if (argumentsArray.Length > 4 && argumentsArray[4] == "noreply")
                            _command.NoReply = true;
                    }

                    this.State = ParserState.WaitingForData;
                }
                catch (Exception e)
                {
                    CreateInvalidCommand("CLIENT_ERROR bad command line format");
                    return;
                }
            }
        }

        private void CreateInvalidCommand(string error = null)
        {
            _command = new InvalidCommand(error);
            this.State = ParserState.ReadyToDispatch;
            return;
        }

        public void Build(byte[] data)
        {
            StorageCommand command = (StorageCommand)_command;
            if ((char)data[command.DataSize] != '\r' || (char)data[command.DataSize + 1] != '\n')
            {
                command.ErrorMessage = "CLIENT_ERROR bad data chunk";
            }
            else
            {
                byte[] dataToStore = new byte[command.DataSize];
                Buffer.BlockCopy(data, 0, dataToStore, 0, command.DataSize);
                command.DataBlock = (object)dataToStore;
            }
            this.State = ParserState.ReadyToDispatch;
        }

        public int CommandDataSize
        {
            get {
                StorageCommand cmd=_command as StorageCommand;
                return cmd!=null?cmd.DataSize:0; 
            }
        }

        public override void StartParser()
        {
            try
            {
                string command = null;
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
                            noOfBytes = _inputDataStream.Read(_rawData, _rawDataOffset, 1);
                            _rawDataOffset += noOfBytes;

                            if (_rawDataOffset > 1 && (char)_rawData[_rawDataOffset - 2] == '\r' && (char)_rawData[_rawDataOffset - 1] == '\n')
                            {
                                command = MemcachedEncoding.BinaryConverter.GetString(_rawData, 0, (_rawDataOffset - 2));
                                this.Parse(command);
                                _rawDataOffset = 0;
                            }
                            if (_rawDataOffset == _rawData.Length)
                            {
                                byte[] newBuffer = new byte[_rawData.Length * 2];
                                Buffer.BlockCopy(_rawData, 0, newBuffer, 0, _rawData.Length);
                                _rawData = newBuffer;
                            }
                            if (_rawDataOffset > MemConfiguration.MaximumCommandLength)
                            {
                                TcpNetworkGateway.DisposeClient(_memTcpClient);
                                return;
                            }
                            break;

                        case ParserState.WaitingForData:
                            noOfBytes = _inputDataStream.Read(_rawData, _rawDataOffset, this.CommandDataSize + 2 - _rawDataOffset);
                            _rawDataOffset += noOfBytes;

                            if (_rawDataOffset == this.CommandDataSize + 2)
                            {
                                _rawDataOffset = 0;
                                this.Build(_rawData);
                            }
                            break;
                        case ParserState.ReadyToDispatch:
                            _logManager.Debug("TextProtocolParser", _command.Opcode + " command recieved.");
                            ConsumerStatus executionMgrStatus = _commandConsumer.RegisterCommand(_command);
                            this.State = ParserState.Ready;
                            go = executionMgrStatus == ConsumerStatus.Running;
                            break;
                    }
                } while (go);
            }
            catch (Exception e)
            {
                _logManager.Fatal("TextProtocolParser", "Exception occured while parsing text command. " + e.Message);
                TcpNetworkGateway.DisposeClient(_memTcpClient);
                return;
            }
            this.Dispatch();
        }
    }
}
