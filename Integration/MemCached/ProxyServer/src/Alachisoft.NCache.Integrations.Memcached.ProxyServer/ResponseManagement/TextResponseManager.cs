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
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.ResponseManagement
{
    class TextResponseManager : ResponseManager
    {


        public TextResponseManager(NetworkStream stream, LogManager logManager)
            : base(stream, logManager)
        {

        }


        public override void Start()
        {

            lock (this)
            {
                if (_alive || _responseQueue.Count==0)
                    return;
                _alive = true;
            }

            bool go=false;

            do
            {
                AbstractCommand command = _responseQueue.Dequeue();
                byte[] outpuBuffer = BuildTextResponse(command);

                try
                {
                    if (base._disposed)
                        return;
                     _stream.Write(outpuBuffer, 0, outpuBuffer.Length);
                }
                catch (Exception e)
                {
                    _logManager.Error("TextResponseManager.Start()", "\tFailed to send response. " + e.Message);
                    return;
                }

                lock (this)
                {
                    _alive = go = _responseQueue.Count > 0;
                }
            }
            while (go);
        }

        private byte[] BuildTextResponse(AbstractCommand command)
        {
            _logManager.Debug("TextResponseManager.BuildTextResponse", "Building response for command : " + command.Opcode);

            if (command.NoReply)
                return new byte[]{};

            if(command.ExceptionOccured)
                return MemcachedEncoding.BinaryConverter.GetBytes("SERVER_ERROR " + command.ErrorMessage + "\r\n");
            if (command.ErrorMessage != null)
                return MemcachedEncoding.BinaryConverter.GetBytes(command.ErrorMessage + "\r\n");

            DataStream resultStream = new DataStream();

            switch (command.Opcode)
            {
                case Opcode.Set:
                case Opcode.Add:
                case Opcode.Replace:
                case Opcode.Append:
                case Opcode.Prepend:
                    if (command.OperationResult.ReturnResult == Result.SUCCESS)
                    {
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("STORED\r\n"));
                    }
                    else
                    {
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("NOT_STORED\r\n"));
                    }
                    break;
                case Opcode.CAS:
                    switch (command.OperationResult.ReturnResult)
                    {
                        case Result.SUCCESS:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("STORED\r\n"));
                            break;
                        case Result.ITEM_MODIFIED:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("EXISTS\r\n"));
                            break;
                        case Result.ITEM_NOT_FOUND:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("NOT_FOUND\r\n"));
                            break;
                        default:
                            break;
                    }
                    break;
                case Opcode.Get:
                case Opcode.Gets:
                    List<GetOpResult> results = (command as GetCommand).Results;
                    foreach (GetOpResult result in results)
                    {
                        if (result == null)
                            continue;
                        byte[] value=result.Value as byte[];
                        string valueString = null;
                        if(command.Opcode==Opcode.Get)
                            valueString = string.Format("VALUE {0} {1} {2}\r\n", result.Key, result.Flag, value.Length);
                        else
                            valueString = string.Format("VALUE {0} {1} {2} {3}\r\n", result.Key, result.Flag, value.Length, result.Version);
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes(valueString));
                        resultStream.Write(value);
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("\r\n"));
                    }
                    resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes( "END\r\n"));
                    break;
                case Opcode.Increment:
                case Opcode.Decrement:
                    switch (command.OperationResult.ReturnResult)
                    {
                        case Result.SUCCESS:
                            long value = (long) (command.OperationResult as MutateOpResult).MutateResult;
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes(value.ToString() + "\r\n"));
                            break;
                        case Result.ITEM_TYPE_MISMATCHED:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("CLIENT_ERROR cannot increment or decrement non-numeric value\r\n"));
                            break;
                        case Result.ITEM_NOT_FOUND:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("NOT_FOUND\r\n"));
                            break;
                        default:
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("ERROR\r\n"));
                            break;
                    }
                    break;
                case Opcode.Delete:
                    if (command.OperationResult.ReturnResult == Result.SUCCESS)
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("DELETED\r\n"));
                    else
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("NOT_FOUND\r\n"));
                    break;
                case Opcode.Touch:
                    if (command.OperationResult.ReturnResult == Result.SUCCESS)
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("TOUCHED\r\n"));
                    else
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("NOT_FOUND\r\n"));
                    break;
                case Opcode.Flush:
                    resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("OK\r\n"));
                        break;
                case Opcode.Version:
                        string version = command.OperationResult.Value as string;
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes(version + "\r\n"));
                        break;
                case Opcode.Verbosity:
                case Opcode.Slabs_Reassign:
                case Opcode.Slabs_Automove:
                        if (command.OperationResult.ReturnResult == Result.SUCCESS)
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("OK\r\n"));
                    else
                            resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("ERROR\r\n"));
                    break;
                case Opcode.Stat:
                    Hashtable stats = command.OperationResult.Value as Hashtable;
                    if (stats == null)
                    {
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("END\r\n"));
                        break;
                    }
                    IDictionaryEnumerator ie = stats.GetEnumerator();
                    string statString = null;
                    while (ie.MoveNext())
                    {
                        statString = string.Format("STAT {0} {1}\r\n",ie.Key,ie.Value);
                        resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes(statString));
                    }
                    resultStream.Write(MemcachedEncoding.BinaryConverter.GetBytes("END\r\n"));
                    break;
                case Opcode.Quit:
                    TcpNetworkGateway.DisposeClient(_memTcpClient);
                    break;
            }

            return resultStream.ReadAll();
        }
    }
}
