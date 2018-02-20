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
    internal class GetRunningTasksCommand : CommandBase
    {
        internal override RequestType CommandRequestType
        {
            get { return RequestType.NonKeyBulkRead; }
        }

        internal override CommandType CommandType
        {
            get { return Command.CommandType.RUNNING_TASKS; }
        }

        protected override void CreateCommand()
        {
            try
            {
                Common.Protobuf.GetRunningTasksCommand runningTasksCommand =
                    new Common.Protobuf.GetRunningTasksCommand();

                base._command = new Common.Protobuf.Command();
                base._command.requestID = this.RequestId;
                base._command.RunningTasksCommand = runningTasksCommand;
                base._command.type = Common.Protobuf.Command.Type.RUNNING_TASKS;
            }
            catch (Exception ex)
            {
            }
        }
    }
}