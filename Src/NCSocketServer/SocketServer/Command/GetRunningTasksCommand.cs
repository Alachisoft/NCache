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
using Alachisoft.NCache.Common.Util;
using System.Collections;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetRunningTasksCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            requestId = command.requestID;

            try
            {
                ICommandExecuter tmpVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tmpVar is NCache) ? tmpVar : null);

                // the Actual Call.
                ArrayList runningTasks = nCache.Cache.RunningTasks;

                // Build response
                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                reponse.commandID = command.commandID;
                reponse.RunningTasksResponse = new Common.Protobuf.GetRunningTasksResponse();
                List<string> list = new List<string>(runningTasks.Count);
                foreach (string inst in runningTasks)
                    list.Add(inst);
                reponse.RunningTasksResponse.runningTasks.AddRange(list);
                reponse.responseType = Common.Protobuf.Response.Type.RUNNING_TASKS;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));

            }
            catch (Exception ex)
            {
                exception = ex.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetRunningTasks.ToLower());
                        log.GenerateGetRunningTasksAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }
    }
}
