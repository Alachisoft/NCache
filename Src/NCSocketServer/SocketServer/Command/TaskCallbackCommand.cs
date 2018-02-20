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
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class TaskCallbackCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            short callbackId;
            string taskId;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            
            Common.Protobuf.TaskCallbackCommand taskCallbackCommand = command.TaskCallbackCommand;
            requestId = command.requestID;
            callbackId = (short)taskCallbackCommand.callbackId;
            taskId = taskCallbackCommand.taskId;
            try
            {
                stopWatch.Start();
                ICommandExecuter tempVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tempVar is NCache) ? tempVar : null);

                nCache.Cache.RegisterTaskNotificationCallback(taskId, new TaskCallbackInfo(clientManager.ClientID, callbackId), new Caching.OperationContext());
                stopWatch.Stop();
                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                reponse.commandID = command.commandID;
                Common.Protobuf.TaskCallbackResponse taskCallbackResp = new Common.Protobuf.TaskCallbackResponse();
                
                reponse.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.TASK_CALLBACK;
                reponse.TaskCallbackResponse = taskCallbackResp;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));

            }
            catch (Exception ex)
            {
                exception = exception.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetTaskResult.ToLower());
                        log.GenerateGetTaskResultAPILogItem(taskId.ToString(),1,exception,executionTime,clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {

                }
            }
        }
    }
}
