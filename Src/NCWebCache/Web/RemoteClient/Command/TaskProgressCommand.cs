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
    internal class TaskProgressCommand : CommandBase
    {
        private string _taskId;

        public TaskProgressCommand(string taskId)
        {
            this._taskId = taskId;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return Command.CommandType.TASK_PROGRESS; }
        }

        protected override void CreateCommand()
        {
            try
            {
                base._command = new Common.Protobuf.Command();

                Common.Protobuf.TaskProgressCommand taskProgressCommand = new Common.Protobuf.TaskProgressCommand();
                taskProgressCommand.taskId = this._taskId;
                base._command.requestID = this.RequestId;

                base._command.TaskProgressCommand = taskProgressCommand;
                base._command.type = Common.Protobuf.Command.Type.TASK_PROGRESS;
            }
            catch (Exception ex)
            {
            }
        }
    }
}