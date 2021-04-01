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
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Statistics;
using System.Collections;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.SocketServer
{
    class ServiceCommandManager : CommandManager
    {

        public ServiceCommandManager(StatisticsCounter perfStatsCollector)
            : base(perfStatsCollector)
        {
        }

        public override void ProcessCommand(ClientManager clientManager, object cmd, short cmdType, long acknowledgementId, UsageStats stats, bool waitforResponse)
        {
            Alachisoft.NCache.Common.Protobuf.Command command = cmd as Alachisoft.NCache.Common.Protobuf.Command;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "enter");
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "" + command);
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " COMMAND to be executed : " + command.type.ToString() + " RequestId :" + command.requestID);

            bool clientDisposed = false;

            CommandBase incommingCmd = null;
            bool isUnsafeCommand = false;

            switch (command.type) 
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.INIT:
                    Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
                    initCommand.requestId = command.requestID;
                    if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " RequestId :" + command.requestID);
                        incommingCmd = new ServiceInitializeCommand(base.RequestLogger.RequestLoggingEnabled, acknowledgementId);
                 break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                    command.getOptimalServerCommand.requestId = command.requestID;
                    incommingCmd = new ServiceGetOptimalServerCommand(acknowledgementId);
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_BINDING:
                    command.getCacheBindingCommand.requestId = command.requestID;
                    incommingCmd = new ServiceCacheBindingCommand(acknowledgementId);
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_SERVER_MAPPING:
                    command.getServerMappingCommand.requestId = command.requestID;
                    incommingCmd = new GetServerMappingCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_MANAGEMENT_PORT:
                    command.getCacheManagementPortCommand.requestId = command.requestID;
                    incommingCmd = new GetCacheManagementPortCommand();
                    break;
            }


            if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.MsecPerCacheOperationBeginSample();
            try
            {
                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                {
                    if (clientDisposed)
                        base.RequestLogger.RemoveClientAccount(clientManager.ClientID);
                    else
                        base.RequestLogger.RegisterRequest(clientManager.ClientID, command.requestID, command.commandID,
                            acknowledgementId);

                }
                incommingCmd.ExecuteCommand(clientManager, command);
            }
            catch (Exception ex)
            {
                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                    base.RequestLogger.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID,
                        Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                throw;
            }
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + command.type.ToString() + " RequestId :" + command.requestID);


            if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.MsecPerCacheOperationEndSample();


#if SERVER
            if (clientManager != null && incommingCmd.OperationResult == OperationResult.Success)
            {
                if (clientManager.CmdExecuter != null)
                {
                    clientManager.CmdExecuter.UpdateSocketServerStats(new SocketServerStats(clientManager.ClientsRequests, clientManager.ClientsBytesSent, clientManager.ClientsBytesRecieved));
                }
            }
#endif
            if (isUnsafeCommand && clientManager.SupportAcknowledgement)
            {
                if (clientManager != null && clientManager.IsDisposed &&
                    incommingCmd.OperationResult == OperationResult.Failure)
                    base.RequestLogger.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID, Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                else
                    base.RequestLogger.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID,
                        Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_AND_EXECUTED, incommingCmd.SerializedResponsePackets);
            }


            if (clientManager != null && incommingCmd.SerializedResponsePackets != null && !clientManager.IsCacheStopped)
            {
                if (SocketServer.IsServerCounterEnabled) base.PerfStatsCollector.IncrementResponsesPerSecStats(1);

                foreach (IList reponse in incommingCmd.SerializedResponsePackets)
                {
                    ConnectionManager.AssureSendSync(clientManager, reponse);
                }
            }

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");

        }
    }
}
