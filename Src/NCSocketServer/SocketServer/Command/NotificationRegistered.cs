// Copyright (c) 2017 Alachisoft
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
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    class NotificationRegistered : CommandBase
    {

        private struct CommandInfo
        {
            public string RequestId;
            public int RegNotifs;
            public int datafilter;
            public int sequence;
        }

        //PROTOBUF

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                NotificationsType notif = (NotificationsType)cmdInfo.RegNotifs;
                //Will only register those which are == null i.e. not initialized
                nCache.RegisterNotification(notif);

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RegisterNotifResponse registerNotifResponse = new Alachisoft.NCache.Common.Protobuf.RegisterNotifResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REGISTER_NOTIF;
                response.registerNotifResponse = registerNotifResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            //HACK:notifMask
            Alachisoft.NCache.Common.Protobuf.RegisterNotifCommand registerNotifCommand = command.registerNotifCommand;
            cmdInfo.RegNotifs = registerNotifCommand.notifMask;
            cmdInfo.RequestId = registerNotifCommand.requestId.ToString();
            cmdInfo.datafilter = registerNotifCommand.datafilter;
            cmdInfo.sequence = registerNotifCommand.sequence;
            return cmdInfo;
        }      
    }
}
