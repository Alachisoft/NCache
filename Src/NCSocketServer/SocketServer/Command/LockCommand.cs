//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class LockCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public BitSet FlagMap;
            public TimeSpan LockTimeout;
            public int ThreadId;
        }

        private OperationResult _lockResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _lockResult;
            }
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LockCmd.Exec", "cmd parsed");

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "LockCommand", "command: " + command + " Error" + arEx);
                _lockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                 //   _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(arEx, base.immatureId), base.ParsingExceptionMessage(arEx));
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(arEx, command.requestID,command.commandID, clientManager.ClientVersion));
                }
                return;
            }
            catch (Exception exc)
            {
                _lockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                }
                return;
            }
            object lockId = null;
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
             
                DateTime lockDate = DateTime.Now;

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                operationContext.Add(OperationContextFieldName.ClientThreadId, cmdInfo.ThreadId);
                operationContext.Add(OperationContextFieldName.IsRetryOperation, command.isRetryCommand);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                bool res = nCache.Cache.Lock(cmdInfo.Key, cmdInfo.LockTimeout, out lockId, out lockDate, operationContext);
                stopWatch.Stop();
                string lockIdString = lockId == null ? "" : lockId.ToString();

                //PROTOBUF:RESPONSE
                //Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.LockResponse lockResponse = new Alachisoft.NCache.Common.Protobuf.LockResponse();

                lockResponse.lockId = lockIdString;
                lockResponse.locked = res;
                lockResponse.lockTime = lockDate.Ticks;
                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(lockResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(lockResponse, Common.Protobuf.Response.Type.LOCK));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.lockResponse = lockResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.LOCK);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (Exception exc)
            {
                _lockResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Lock.ToLower());
                        log.GenerateLockAPILogItem(cmdInfo.Key, cmdInfo.LockTimeout, lockId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());


                    }
                }
                catch
                {
                }
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LockCmd.Exec", "cmd executed on cache");
            }

        }


        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.LockCommand lockCommand = command.lockCommand;
            //HACK : Flag map missing, becuase it never used
            cmdInfo.Key = lockCommand.key;
            cmdInfo.LockTimeout = new TimeSpan(lockCommand.lockTimeout);
            cmdInfo.RequestId = lockCommand.requestId.ToString();
            cmdInfo.ThreadId = lockCommand.threadId;

            return cmdInfo;
        }

        

    }
}
