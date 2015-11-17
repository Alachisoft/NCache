// Copyright (c) 2015 Alachisoft
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
using System.Net.Sockets;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Serialization;
using System.IO;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;

namespace Alachisoft.NCache.SocketServer
{
    internal class ManagementCommandManager : ICommandManager
    {
        public ManagementCommandManager()
        {
        }

        public object Deserialize(Stream stream)
        {
            Alachisoft.NCache.Common.Protobuf.ManagementCommand command = null;
            command = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.ManagementCommand>(stream);
            stream.Close();

            return command;
        }

        public void ProcessCommand(ClientManager clientManager, object command)
        {
            Alachisoft.NCache.Common.Protobuf.ManagementCommand cmd = command as Alachisoft.NCache.Common.Protobuf.ManagementCommand;

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "enter");
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "" + cmd);
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " COMMAND to be executed : " + "Management Command" + " RequestId :" + cmd.requestId);

            NCManagementCommandBase incommingCmd = null;
            incommingCmd = new ManagementCommand();

            //PROTOBUF
            /*****************************************************************/
            /**/
            incommingCmd.ExecuteCommand(clientManager, cmd);/**/
            /*****************************************************************/
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + "Management Command" + " RequestId :" + cmd.requestId);

            if (clientManager != null && incommingCmd.SerializedResponsePackets != null && !clientManager.IsCacheStopped)
            {
                foreach (byte[] reponse in incommingCmd.SerializedResponsePackets)
                {
                    ConnectionManager.AssureSend(clientManager, reponse,Common.Enum.Priority.Normal);
                }

            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");

        }
    }
}
