//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Monitoring;
using System.IO;
using Alachisoft.NCache.SocketServer.RequestLogging;
using System.Collections;
using System;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.SocketServer
{
    internal class ManagementCommandManager : ICommandManager
    {
        public ManagementCommandManager() { }

        public object Deserialize(Stream buffer)
        {
            Common.Protobuf.ManagementCommand command = null;
            command = ProtoBuf.Extended.Serializer.Deserialize<Common.Protobuf.ManagementCommand>(buffer);
            buffer.Close();
            return command;
        }

        public void ProcessCommand(ClientManager clientManager, object command, short cmdType, long acknowledgementId, UsageStats stats, bool waitforResponse)
        {
            
            Common.Protobuf.ManagementCommand cmd = command as Common.Protobuf.ManagementCommand;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "enter");
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "" + cmd);

            NCManagementCommandBase incommingCmd = null;
            incommingCmd = new ManagementCommand();

            incommingCmd.ExecuteCommand(clientManager, cmd);


#if SERVER
            if (clientManager != null &&
                incommingCmd.OperationResult == OperationResult.Success)
            {
                if (clientManager.CmdExecuter != null)
                {
                    clientManager.CmdExecuter.UpdateSocketServerStats(new SocketServerStats(clientManager.ClientsRequests, clientManager.ClientsBytesSent, clientManager.ClientsBytesRecieved));
                }
            }
#endif

            if (clientManager != null && incommingCmd.SerializedResponsePackets != null && !clientManager.IsCacheStopped)
            {
                foreach (IList reponse in incommingCmd.SerializedResponsePackets)
                {
                    ConnectionManager.AssureSend(clientManager, reponse, false);
                }
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");
            


        }
        private static void ExecuteTLSCommandSync(ClientManager clientManager, Common.Protobuf.ManagementCommand cmd)
        {
            clientManager.SecureConnectionEnabled = true;

            Alachisoft.NCache.Common.Protobuf.ManagementResponse response = new Alachisoft.NCache.Common.Protobuf.ManagementResponse();

            response.methodName = cmd.methodName;
            response.version = cmd.commandVersion;
            response.requestId = cmd.requestId;

            ClusteredArrayList list = ResponseHelper.SerializeResponse(response);

            if (clientManager != null && list != null)
            {
                ConnectionManager.AssureSend(clientManager, list.ToArray(), false);
            }
        }
        public Common.DataStructures.RequestStatus GetRequestStatus(string clientId, long requestId, long commandId)
        {
            return null;
        }

        public void RegisterOperationModeChangeEvent()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }

        public Bookie RequestLogger
        {
            get { return null; }
        }
    }
}
