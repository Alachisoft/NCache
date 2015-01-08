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
using System.Collections;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkAddCommand : BulkAddAndInsertCommandBase
    {
        private OperationResult _addBulkResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _addBulkResult;
            }
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            ClientId = clientManager.ClientID;

            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                serailizationContext = nCache.CacheId;
                cmdInfo = base.ParseCommand(command, clientManager, serailizationContext);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkAddCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {
                _addBulkResult = OperationResult.Failure;
                
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));

                return;
            }

            byte[] dataPackage = null;
            
            try
            {
                //PROTOBUF:RESPONSE
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);
                
                Hashtable addResult = (Hashtable)nCache.Cache.Add(cmdInfo.Keys, cmdInfo.Values, cmdInfo.CallbackEnteries, cmdInfo.ExpirationHint, cmdInfo.EvictionHint, cmdInfo.QueryInfo, cmdInfo.Flags, operationContext);
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.BulkAddResponse bulkAddResponse = new Alachisoft.NCache.Common.Protobuf.BulkAddResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.intendedRecipient = cmdInfo.IntendedRecipient;
                
                bulkAddResponse.keyExceptionPackage = new Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse();

                Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysExceptions(addResult, bulkAddResponse.keyExceptionPackage);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_BULK;
                response.bulkAdd = bulkAddResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

               
            }
            catch (Exception exc)
            {
                _addBulkResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkAddCmd.Exec", "cmd executed on cache");

        }
    }
}
