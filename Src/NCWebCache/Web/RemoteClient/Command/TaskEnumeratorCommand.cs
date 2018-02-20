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
    internal class TaskEnumeratorCommand : CommandBase
    {
        private string taskId;
        private short callbackId;

        public TaskEnumeratorCommand(string taskId, short callbackId)
        {
            this.taskId = taskId;
            this.callbackId = callbackId;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return Command.CommandType.TASK_ENUMERATOR; }
        }

        protected override void CreateCommand()
        {
            try
            {
                base._command = new Common.Protobuf.Command();

                Common.Protobuf.TaskEnumeratorCommand taskEnumeratorCommand =
                    new Common.Protobuf.TaskEnumeratorCommand();
                taskEnumeratorCommand.TaskId = this.taskId;
                taskEnumeratorCommand.CallbackId = this.callbackId;
                base._command.requestID = this.RequestId;
                base._command.clientLastViewId = this.ClientLastViewId;

                base._command.TaskEnumeratorCommand = taskEnumeratorCommand;
                base._command.type = Common.Protobuf.Command.Type.TASK_ENUMERATOR;
            }
            catch (Exception ex)
            {
            }
        }
    }
}