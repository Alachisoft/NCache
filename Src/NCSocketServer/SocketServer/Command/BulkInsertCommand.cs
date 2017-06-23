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
using System.Collections;
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkInsertCommand : BulkAddAndInsertCommandBase
    {
        private OperationResult _insertBulkResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _insertBulkResult;
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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkInsCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {
                _insertBulkResult = OperationResult.Failure;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                return;
            }

            byte[] dataPackage = null;

            try
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);

                Hashtable insertResult = (Hashtable)nCache.Cache.Insert(cmdInfo.Keys, cmdInfo.Entries, operationContext);
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.BulkInsertResponse bulkInsertResponse = new Alachisoft.NCache.Common.Protobuf.BulkInsertResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.intendedRecipient = cmdInfo.IntendedRecipient;
                bulkInsertResponse.keyExceptionPackage = new Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse();

                //TODO : Package Key Value
                Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysExceptions(insertResult, bulkInsertResponse.keyExceptionPackage);

                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.INSERT_BULK;
                response.bulkInsert = bulkInsertResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

            }
            catch (Exception exc)
            {
                _insertBulkResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkInsCmd.Exec", "cmd executed on cache");

        }

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.PerfStatsCollector collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerUpdBulkAvg(value);
            }
        }
    }
}
