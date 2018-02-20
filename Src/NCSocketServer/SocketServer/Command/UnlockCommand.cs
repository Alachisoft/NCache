// Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class UnlockCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public bool isPreemptive;
            public object lockId;
        }
        
        private OperationResult _unlockResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _unlockResult;
            }
        }


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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("UnlockCmd.Exec", "cmd parsed");

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("UnlockCommand", "command: " + command + " Error" + arEx);
                _unlockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID,command.commandID));
                }
                return;
            }
            catch (Exception exc)
            {
                _unlockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;

                nCache.Cache.Unlock(cmdInfo.Key, cmdInfo.lockId, cmdInfo.isPreemptive, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                stopWatch.Stop();
                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.UnlockResponse unlockResponse = new Alachisoft.NCache.Common.Protobuf.UnlockResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.UNLOCK;
                response.unlockResponse = unlockResponse;

				_serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _unlockResult = OperationResult.Failure;
                _serializedResponsePackets.Add(clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc)));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Unlock.ToLower());
                        log.GeneratUnlockAPILogItem(cmdInfo.Key, cmdInfo.lockId.ToString(), overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("UnlockCmd.Exec", "cmd executed on cache");
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.UnlockCommand unlockCommand = command.unlockCommand;

            cmdInfo.isPreemptive = unlockCommand.preemptive;
            if (!unlockCommand.preemptive)
            {
                cmdInfo.lockId = unlockCommand.lockId;
            }
            cmdInfo.Key = unlockCommand.key;
            cmdInfo.RequestId = unlockCommand.requestId.ToString();
           
            return cmdInfo;
        }
    }
}
