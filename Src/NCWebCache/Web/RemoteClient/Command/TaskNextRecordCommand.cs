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
    internal class TaskNextRecordCommand : CommandBase
    {
        private string clientId;
        private string taskId;
        private short callbackId;
        private string clientIp;
        private int clientPort;
        private string clusterIp;
        private int clusterPort;

        public TaskNextRecordCommand(string clientId, string taskId, short callbackId,
            Common.Net.Address _clientAddress, Common.Net.Address _clusterAddress)
        {
            this.clientId = clientId;
            this.taskId = taskId;
            this.callbackId = callbackId;
            this.clientIp = _clientAddress.IpAddress.ToString();
            this.clientPort = _clientAddress.Port;
            this.clusterIp = _clusterAddress.IpAddress.ToString();
            this.clusterPort = _clusterAddress.Port;
        }


        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override CommandType CommandType
        {
            get { return Command.CommandType.TASK_NEXT_RECORD; }
        }

        protected override void CreateCommand()
        {
            try
            {
                base._command = new Common.Protobuf.Command();

                Common.Protobuf.GetNextRecordCommand nextRecordCommand = new Common.Protobuf.GetNextRecordCommand();
                nextRecordCommand.TaskId = this.taskId;
                nextRecordCommand.CallbackId = this.callbackId;
                nextRecordCommand.ClientId = this.clientId;
                nextRecordCommand.ClientIp = this.clientIp;
                nextRecordCommand.ClientPort = this.clientPort;
                nextRecordCommand.ClusterIp = this.clusterIp;
                nextRecordCommand.ClusterPort = this.clusterPort;

                nextRecordCommand.IntendedRecipient = this.intendedRecipient;

                base._command.requestID = this.RequestId;
                base._command.clientLastViewId = this.ClientLastViewId;

                base._command.NextRecordCommand = nextRecordCommand;
                base._command.type = Common.Protobuf.Command.Type.TASK_NEXT_RECORD;
            }
            catch (Exception ex)
            {
            }
        }
    }
}