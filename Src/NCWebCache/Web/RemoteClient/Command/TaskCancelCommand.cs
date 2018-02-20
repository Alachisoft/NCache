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

namespace Alachisoft.NCache.Web.Command
{
    internal class TaskCancelCommand : CommandBase
    {
        private string _taskId;
        private bool _cancelAll = false;

        public TaskCancelCommand(string taskId, bool cancelall)
        {
            this._taskId = taskId;
            this._cancelAll = cancelall;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return CommandType.CANCEL_TASK; }
        }

        protected override void CreateCommand()
        {
            try
            {
                base._command = new Common.Protobuf.Command();

                Common.Protobuf.TaskCancelCommand taskCancelCommand = new Common.Protobuf.TaskCancelCommand();
                taskCancelCommand.taskId = this._taskId;
                taskCancelCommand.cancelAll = this._cancelAll;

                base._command.requestID = this.RequestId;
                base._command.TaskCancelCommand = taskCancelCommand;
                base._command.type = Common.Protobuf.Command.Type.CANCEL_TASK;
            }
            catch (Exception ex)
            {
            }
        }
    }
}