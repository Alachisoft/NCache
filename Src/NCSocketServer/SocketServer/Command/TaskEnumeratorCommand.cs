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
using System.Collections.Generic;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class TaskEnumeratorCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            string taskId;
            short callbackId;
            long clientLastViewId;

            Common.Protobuf.TaskEnumeratorCommand taskEnumeratorCommand = command.TaskEnumeratorCommand;
            taskId = taskEnumeratorCommand.TaskId;
            requestId = command.requestID;
            callbackId= (short)taskEnumeratorCommand.CallbackId;
            clientLastViewId = command.clientLastViewId;
            try
            {
                ICommandExecuter tempVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tempVar is NCache) ? tempVar : null);

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, clientLastViewId);
                
                List<Common.MapReduce.TaskEnumeratorResult> enumerators = 
                                        nCache.Cache.GetTaskEnumerator(new Common.MapReduce.TaskEnumeratorPointer(
                                                        clientManager.ClientID, taskId, callbackId), operationContext);

                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                reponse.commandID = command.commandID;
                Common.Protobuf.TaskEnumeratorResponse taskEnumResponse = new Common.Protobuf.TaskEnumeratorResponse();
                foreach (Common.MapReduce.TaskEnumeratorResult rslt in enumerators)
                {
                    taskEnumResponse.TaskEnumeratorResult.Add(Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(rslt, nCache.Cache.Name));
                }

                reponse.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.TASK_ENUMERATOR;
                reponse.TaskEnumeratorResponse = taskEnumResponse;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));

            }
            catch (Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
             
            
        }
    }
}
