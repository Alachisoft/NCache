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
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class TaskProgressCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            long requestId;
            string taskId;

            Common.Protobuf.TaskProgressCommand taskProgressCommand = command.TaskProgressCommand;
            taskId = taskProgressCommand.taskId;
            requestId = command.requestID;

            try
            {
                ICommandExecuter tmpVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tmpVar is NCache) ? tmpVar : null);

                Runtime.MapReduce.TaskStatus taskStatus = nCache.Cache.TaskStatus(taskId);

                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                reponse.TaskProgressResponse = new Common.Protobuf.TaskProgressResponse();
                reponse.TaskProgressResponse.progresses = Serialization.Formatters.CompactBinaryFormatter.ToByteBuffer(taskStatus, nCache.Cache.Name);
                reponse.responseType = Common.Protobuf.Response.Type.TASK_PROGRESS;
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
