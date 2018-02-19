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



namespace Alachisoft.NCache.SocketServer.Util
{
    public static class CommandHelper
    {
        public static bool Queable(object command)
        {
            if (!(command is Common.Protobuf.Command))
            {
                return false;
            }

            Common.Protobuf.Command cmd = (Common.Protobuf.Command)command;

            switch (cmd.type)
            {
                case Common.Protobuf.Command.Type.ADD:
                case Common.Protobuf.Command.Type.INSERT:
                case Common.Protobuf.Command.Type.REMOVE:
                case Common.Protobuf.Command.Type.DELETE:
                case Common.Protobuf.Command.Type.CONTAINS:
                case Common.Protobuf.Command.Type.GET:
                case Common.Protobuf.Command.Type.COUNT:
                    return true;
            }

            return false;
        }
    }
}
