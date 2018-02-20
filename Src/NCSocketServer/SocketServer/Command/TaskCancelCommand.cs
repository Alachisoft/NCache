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
    internal class TaskCancelCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            string taskId = null;
            long requestId;

            try
            {
                Common.Protobuf.TaskCancelCommand comm = command.TaskCancelCommand;

                if (comm.taskId != null || !string.IsNullOrEmpty(comm.taskId))
                    taskId = comm.taskId;

                requestId = command.requestID;

            }
            catch (Exception ex)
            {
                if (base.immatureId != "-2")
                    _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
                return;
            }

            try
            {
                ICommandExecuter tmpVar = clientManager.CmdExecuter;
                NCache nCache = (NCache)((tmpVar is NCache) ? tmpVar : null);

                nCache.Cache.CancelTask(taskId);
                Common.Protobuf.Response reponse = new Common.Protobuf.Response();
                reponse.requestId = requestId;
                reponse.commandID = command.commandID;
                reponse.TaskCallbackResponse = new Common.Protobuf.TaskCallbackResponse();
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(reponse));

            }
            catch (Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID, command.commandID));
            }
        }
    }
}
