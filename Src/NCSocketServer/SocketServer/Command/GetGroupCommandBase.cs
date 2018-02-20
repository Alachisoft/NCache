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

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetGroupCommandBase : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Group;
            public string SubGroup;
            public long ClientLastViewId;
            public int CommandVersion;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
        }

        //PROTOBUF
        protected CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            
            Alachisoft.NCache.Common.Protobuf.GetGroupCommand getGroupCommand = command.getGroupCommand;

            cmdInfo.Group = getGroupCommand.group;
            cmdInfo.RequestId = getGroupCommand.requestId.ToString();
            cmdInfo.SubGroup = getGroupCommand.subGroup.Length == 0 ? null : getGroupCommand.subGroup;
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }
    }
}
