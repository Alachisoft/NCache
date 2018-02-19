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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    class ServiceCommandManager : CommandManager
    {

        public ServiceCommandManager(PerfStatsCollector perfStatsCollector)
            : base(perfStatsCollector)
        {
        }

        public override void ProcessCommand(ClientManager clientManager, object cmd, long acknowledgementId, UsageStats stats)
        {
            Common.Protobuf.Command command = cmd as Common.Protobuf.Command;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "enter");
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "" + command);
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " COMMAND to be executed : " + command.type.ToString() + " RequestId :" + command.requestID);

            HPTimeStats milliSecWatch = new HPTimeStats();
            milliSecWatch.BeginSample();

            CommandBase incommingCmd = null;

            switch (command.type)
            {
                case Common.Protobuf.Command.Type.INIT:
                    Common.Protobuf.InitCommand initCommand = command.initCommand;
                    initCommand.requestId = command.requestID;
                    incommingCmd = new ServiceInitializeCommand(acknowledgementId);
                    break;
                case Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                    command.getOptimalServerCommand.requestId = command.requestID;
                    incommingCmd = new ServiceGetOptimalServerCommand(acknowledgementId);
                    break;

                case Common.Protobuf.Command.Type.GET_CACHE_BINDING:
                    command.getCacheBindingCommand.requestId = command.requestID;
                    incommingCmd = new ServiceCacheBindingCommand(acknowledgementId);
                    break;

                case Common.Protobuf.Command.Type.GET_SERVER_MAPPING:
                    command.getServerMappingCommand.requestId = command.requestID;
                    incommingCmd = new GetServerMappingCommand();
                    break;
            }


            if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.MsecPerCacheOperationBeginSample();
            try
            {
                incommingCmd.ExecuteCommand(clientManager, command);
            }
            catch (Exception ex)
            {
                throw;
            }
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + command.type.ToString() + " RequestId :" + command.requestID);


            if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.MsecPerCacheOperationEndSample();


            if (clientManager != null && incommingCmd.SerializedResponsePackets != null && !clientManager.IsCacheStopped)
            {

                if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.IncrementResponsesPerSecStats(1);

                foreach (byte[] reponse in incommingCmd.SerializedResponsePackets)
                {
                    ConnectionManager.AssureSend(clientManager, reponse, Common.Enum.Priority.Normal);
                }
            }

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");

        }
    }
}
