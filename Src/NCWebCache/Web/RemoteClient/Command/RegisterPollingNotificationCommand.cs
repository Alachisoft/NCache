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

namespace Alachisoft.NCache.Web.Command
{
    class RegisterPollingNotificationCommand : CommandBase
    {
        Common.Protobuf.RegisterPollingNotificationCommand registerPollNotifCommand;

        public RegisterPollingNotificationCommand(short callbackId)
        {
            registerPollNotifCommand = new Common.Protobuf.RegisterPollingNotificationCommand();
            registerPollNotifCommand.callbackId = callbackId;
            registerPollNotifCommand.requestId = base.RequestId;
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }

        internal override CommandType CommandType
        {
            get { return NCache.Web.Command.CommandType.REGISTER_POLLING_NOTIFICATION; }
        }

        protected override void CreateCommand()
        {
            base._command = new Common.Protobuf.Command();
            base._command.registerPollNotifCommand = registerPollNotifCommand;
            base._command.registerPollNotifCommand.requestId = base.RequestId;
            base._command.requestID = base.RequestId;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.type = Common.Protobuf.Command.Type.REGISTER_POLLING_NOTIFICATION;
        }
    }
}