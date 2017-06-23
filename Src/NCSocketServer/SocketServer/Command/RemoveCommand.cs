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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RemoveCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public BitSet FlagMap;
            public object LockId;
            public LockAccessType LockAccessType;
        }

        private OperationResult _removeResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _removeResult;
            }
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RemCmd.Exec", "cmd parsed");

            }
            catch (Exception exc)
            {
                _removeResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }
            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                CallbackEntry cbEntry = null;
                

                CompressedValueEntry flagValueEntry = null;

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                flagValueEntry = nCache.Cache.Remove(cmdInfo.Key, cmdInfo.FlagMap, cbEntry, cmdInfo.LockId, cmdInfo.LockAccessType, operationContext);

                UserBinaryObject ubObject = (flagValueEntry == null) ? null : (UserBinaryObject) flagValueEntry.Value;

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RemoveResponse removeResponse =
                    new Alachisoft.NCache.Common.Protobuf.RemoveResponse();
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REMOVE;
                response.remove = removeResponse;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                if (ubObject != null)
                {
                    removeResponse.value.AddRange(ubObject.DataList);
                    removeResponse.flag = flagValueEntry.Flag.Data;
                }
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _removeResult = OperationResult.Failure;

                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(
                    Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            finally
            {
                if (ServerMonitor.MonitorActivity)
                    ServerMonitor.LogClientActivity("RemCmd.Exec", "cmd executed on cache");
            }

        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.RemoveCommand removeCommand = command.removeCommand;
            cmdInfo.FlagMap = new BitSet((byte)removeCommand.flag);
            cmdInfo.Key = removeCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)removeCommand.lockAccessType;
            cmdInfo.LockId = removeCommand.lockId;
            cmdInfo.RequestId = removeCommand.requestId.ToString();
            
            return cmdInfo;
        }
    }
}
