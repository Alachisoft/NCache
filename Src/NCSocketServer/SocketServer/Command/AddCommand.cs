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
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddCommand : AddAndInsertCommandBase
    {

        private OperationResult _addResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _addResult;
            }
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                serializationContext = nCache.CacheId;
                cmdInfo = base.ParseCommand(command, clientManager, serializationContext);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {
                _addResult = OperationResult.Failure;
           
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));

                return;
            }

            CallbackEntry callbackEntry = null;

            if (cmdInfo.UpdateCallbackId != -1 || cmdInfo.RemoveCallbackId != -1 || !cmdInfo.RequestId.Equals("-1") )
            {
                callbackEntry = new CallbackEntry(clientManager.ClientID,
                    Convert.ToInt32(cmdInfo.RequestId),
                    cmdInfo.value,
                    cmdInfo.RemoveCallbackId,
                    cmdInfo.UpdateCallbackId,
                    cmdInfo.Flag,
                    (EventDataFilter) cmdInfo.UpdateDataFilter,
                    (EventDataFilter) cmdInfo.RemoveDataFilter);
            }


                try
                {
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    nCache.Cache.Add(cmdInfo.Key,
                    callbackEntry == null ? cmdInfo.value : (object)callbackEntry,
                    cmdInfo.ExpirationHint,
                    cmdInfo.EvictionHint,
                    cmdInfo.queryInfo,
                    cmdInfo.Flag, operationContext);


                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.AddResponse addResponse =
                    new Alachisoft.NCache.Common.Protobuf.AddResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD;
                response.addResponse = addResponse;

                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _addResult = OperationResult.Failure;
                _serializedResponsePackets.Add(
                    Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddCmd.Exec", "cmd executed on cache");

        }
    }
}


