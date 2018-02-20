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

using System.Collections.Generic;
using Alachisoft.NCache.Common.Protobuf;
using ClientInfo = Alachisoft.NCache.Runtime.Caching.ClientInfo;
using Exception = System.Exception;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetConnectedClientsCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            short callbackId;
            string taskId;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                stopWatch.Start();
                IEnumerable<ClientInfo> connectedClients = nCache.Cache.GetConnectedClientInfos();
                Common.Protobuf.GetConnectedClientsResponse clientsResponse = new Common.Protobuf.GetConnectedClientsResponse();
                Common.Protobuf.Response response = new Common.Protobuf.Response();
                response.getConnectedClientsResponse = clientsResponse;
                response.requestId = command.getConnectedClientsCommand.requestId;
                response.commandID = command.commandID;
                response.responseType = Response.Type.GET_CONNECTED_CLIENTS;
                if (connectedClients != null)
                {
                    foreach (var connectedClient in connectedClients)
                    {
                        clientsResponse.connectedClients.Add(new Common.Protobuf.ClientInfo
                        {
                            clientId = connectedClient.ClientID,
                            processId = connectedClient.ProcessID,
                            appName = connectedClient.AppName,
                            ipAddress = connectedClient.IPAddress.ToString(),
                            machineName = connectedClient.MachineName
                        });
                    }
                }
                stopWatch.Stop();
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(response));

            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetConnectedClientList.ToLower());
                        log.GenerateGetConnectedClientsAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }
    }
}
