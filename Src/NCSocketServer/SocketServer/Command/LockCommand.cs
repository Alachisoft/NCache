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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;

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

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("LockCmd.Exec", "cmd parsed");

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "LockCommand", "command: " + command + " Error" + arEx);
                _lockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID));
                }
                return;
            }
            catch (Exception exc)
            {
                _lockResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                object lockId = null;
                DateTime lockDate = DateTime.Now;

                bool res = nCache.Cache.Lock(cmdInfo.Key, cmdInfo.LockTimeout, out lockId, out lockDate, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                string lockIdString = lockId == null ? "" : lockId.ToString();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.LockResponse lockResponse = new Alachisoft.NCache.Common.Protobuf.LockResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.LOCK;
                response.lockResponse = lockResponse;

                lockResponse.lockId = lockIdString;
                lockResponse.locked = res;
                lockResponse.lockTime = lockDate.Ticks;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));             
            }
            catch (Exception exc)
            {
                _lockResult = OperationResult.Failure;

                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            finally
            {
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


            return cmdInfo;
        }
    }
}
