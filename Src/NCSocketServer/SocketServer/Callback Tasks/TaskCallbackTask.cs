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

using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Caching;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.CallbackTasks
{
    public class TaskCallbackTask : ICallbackTask
    {
        string clientId;
        string taskId;
        short taskStatus;
        short callbackId;
        string taskFailureReason;
        EventContext _eventContext;

        public TaskCallbackTask(string clientid, string taskid, short taskstatus, string taskFailureReason, short callbackid, EventContext context)
        {
            this.clientId = clientid;
            this.taskId = taskid;
            this.taskStatus = taskstatus;
            this.callbackId = callbackid;
            this._eventContext = context;
            this.taskFailureReason = taskFailureReason;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) 
                clientManager = (ClientManager)ConnectionManager.ConnectionTable[clientId];

            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();

                response.TaskCallbackResponse = EventHelper.GetTaskCallbackResponse(_eventContext, taskId, taskStatus, taskFailureReason, callbackId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.TASK_CALLBACK;

                IList serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Low);
            }
        }
    }
}
