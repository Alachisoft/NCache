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
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class TaskNextRecordCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            string taskId;
            short callbackId;
            string clientId;
            string clientIp;
            int clientPort;
            string clusterIp;
            int clusterPort;
            long clientLastViewId;

            string intendedRecipient;

            Common.Protobuf.GetNextRecordCommand nextRecordCommand = command.NextRecordCommand;
            taskId = nextRecordCommand.TaskId;
            requestId = command.requestID;
            callbackId = (short)nextRecordCommand.CallbackId;
            clientId = nextRecordCommand.ClientId;
            clientIp = nextRecordCommand.ClientIp;
            clientPort = nextRecordCommand.ClientPort;
            clusterIp = nextRecordCommand.ClusterIp;
            clusterPort = nextRecordCommand.ClusterPort;

            intendedRecipient = nextRecordCommand.IntendedRecipient;

            clientLastViewId = command.clientLastViewId;
            try
            {
                ICommandExecuter tempVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tempVar is NCache) ? tempVar : null);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, clientLastViewId);

                Common.MapReduce.TaskEnumeratorPointer pntr = new Common.MapReduce.TaskEnumeratorPointer(clientId, taskId, callbackId);
                pntr.ClientAddress = new Common.Net.Address(clientIp, clientPort);
                pntr.ClusterAddress = new Common.Net.Address(clusterIp, clusterPort);
                
                Common.MapReduce.TaskEnumeratorResult enumerator =
                                        nCache.Cache.GetTaskNextRecord(pntr, operationContext);

                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                Common.Protobuf.GetNextRecordResponse nextRecordResponse = new Common.Protobuf.GetNextRecordResponse();
                
                nextRecordResponse.Key = Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(enumerator.RecordSet.Key, nCache.Cache.Name);
                nextRecordResponse.Value= Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(enumerator.RecordSet.Value, nCache.Cache.Name);
                nextRecordResponse.IsLastResult = enumerator.IsLastResult;
                nextRecordResponse.NodeAddress = enumerator.NodeAddress;
                
                reponse.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.TASK_NEXT_RECORD;
                reponse.NextRecordResponse = nextRecordResponse;
                reponse.commandID = command.commandID;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));

            }
            catch (Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
        }
    }
}
