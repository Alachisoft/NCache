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
    class GetCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public BitSet FlagMap;
            public LockAccessType LockAccessType;
            public object LockId;
            public TimeSpan LockTimeout;
         }

        
        private OperationResult _getResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _getResult;
            }
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }
        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GetCmd.Exec", "cmd parsed");

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "GetCommand", "command: " + command + " Error" + arEx);
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID));
                return;
            }
            catch (Exception exc)
            {
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                return;
            }

            try
            {
                object lockId = cmdInfo.LockId;
                DateTime lockDate = new DateTime();
                NCache nCache = clientManager.CmdExecuter as NCache;
                CompressedValueEntry flagValueEntry = null;

                flagValueEntry = nCache.Cache.Get(cmdInfo.Key, cmdInfo.FlagMap, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                UserBinaryObject ubObj = (flagValueEntry == null) ? null : (UserBinaryObject)flagValueEntry.Value;

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetResponse getResponse = new Alachisoft.NCache.Common.Protobuf.GetResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET;
                if (lockId != null)
                {
                    getResponse.lockId = lockId.ToString();
                }
                getResponse.lockTime = lockDate.Ticks;

                if (ubObj == null)
                {
                    response.get = getResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                else
                {
                    getResponse.flag = flagValueEntry.Flag.Data;
                    getResponse.data.AddRange(ubObj.DataList);
                    response.get = getResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                _getResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GetCmd.Exec", "cmd executed on cache");

        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCommand getCommand = command.getCommand;

            cmdInfo.FlagMap = new BitSet((byte)getCommand.flag);

            cmdInfo.Key = getCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)getCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCommand.lockInfo.lockTimeout);
            cmdInfo.RequestId = getCommand.requestId.ToString();


            return cmdInfo;
        }
    }
}
