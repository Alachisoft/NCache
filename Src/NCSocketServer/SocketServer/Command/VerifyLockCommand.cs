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
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class VerifyLockCommand : CommandBase
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
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                object lockId = null;
                DateTime lockDate = DateTime.Now;
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                bool res = nCache.Cache.Lock(cmdInfo.Key, cmdInfo.LockTimeout, out lockId, out lockDate, operationContext);

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.VerifyLockResponse verifyLockResponse = new Alachisoft.NCache.Common.Protobuf.VerifyLockResponse();

                verifyLockResponse.lockId = lockId.ToString();
                verifyLockResponse.success = res;
                verifyLockResponse.lockExpiration = lockDate.Ticks;

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(verifyLockResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(verifyLockResponse, Common.Protobuf.Response.Type.LOCK_VERIFY));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.lockVerify = verifyLockResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.LOCK_VERIFY);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (Exception exc)
            {
                _lockResult = OperationResult.Failure;
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

      

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.LockVerifyCommand lockVerifyCommand = command.lockVerifyCommand;

            cmdInfo.Key = lockVerifyCommand.key;
            cmdInfo.RequestId = lockVerifyCommand.requestId.ToString();


            return cmdInfo;
        }

     

    }
}
