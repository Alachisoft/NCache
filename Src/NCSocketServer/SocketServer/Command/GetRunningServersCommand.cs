//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetRunningServersCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsDotNetClient;
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
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                return;
            }

            Alachisoft.NCache.Caching.Cache cache = null;

            try
            {
                string server = ConnectionManager.ServerIpAddress;
                int port = ConnectionManager.ServerPort;

                Dictionary<string, int> runningServers = new Dictionary<string, int>();
                runningServers = ((NCache)clientManager.CmdExecuter).Cache.GetRunningServers(server, port);

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetRunningServersResponse getRunningServerResponse = new Alachisoft.NCache.Common.Protobuf.GetRunningServersResponse();

                //getRunningServerResponse = new List<Common.Protobuf.KeyValuePair>();
                if (runningServers != null)
                {
                    Dictionary<string, int>.Enumerator ide = runningServers.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        Common.Protobuf.KeyValuePair pair = new Common.Protobuf.KeyValuePair();
                        pair.key = ide.Current.Key;
                        pair.value = ide.Current.Value.ToString();
                        getRunningServerResponse.keyValuePair.Add(pair);
                    }
                }

                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.getRunningServer = getRunningServerResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_RUNNING_SERVERS;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }

       
        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetRunningServersCommand getRunningServerCommand = command.getRunningServersCommand;

            cmdInfo.CacheId = getRunningServerCommand.cacheId;
            cmdInfo.IsDotNetClient = getRunningServerCommand.isDotnetClient;
            cmdInfo.Password = getRunningServerCommand.pwd;
            cmdInfo.PasswordBinary = getRunningServerCommand.binaryPassword;
            cmdInfo.RequestId = getRunningServerCommand.requestId.ToString();
            cmdInfo.UserName = getRunningServerCommand.userId;
            cmdInfo.UserNameBinary = getRunningServerCommand.binaryUserId;

            return cmdInfo;
        }
    }
}
