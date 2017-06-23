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

using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Serialization;
using System.IO;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// An object of this class is used by connection manager to get and send commands
    /// </summary>
    class CommandManager : ICommandManager
    {
        private PerfStatsCollector _perfStatsCollector;
        public PerfStatsCollector PerfStatsCollector
        {
            get { return _perfStatsCollector; }
        }

        public CommandManager(PerfStatsCollector perfStatsCollector)
        {
            _perfStatsCollector = perfStatsCollector;

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Caching.OperationContext), 153);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.EnumerationPointer), 161);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.EnumerationDataChunk), 162);

        }

        public object Deserialize(Stream buffer)
        {
            Alachisoft.NCache.Common.Protobuf.Command command = null;
            command = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Command>(buffer);
            buffer.Close();
            return command;
        }

        public virtual void ProcessCommand(ClientManager clientManager, object cmd, long acknowledgementId, UsageStats stats)
        {
            Alachisoft.NCache.Common.Protobuf.Command command = cmd as Alachisoft.NCache.Common.Protobuf.Command;
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "enter");
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "" + command);
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " COMMAND to be executed : " + command.type.ToString() + " RequestId :" + command.requestID);                 

            HPTimeStats milliSecWatch = new HPTimeStats();
            milliSecWatch.BeginSample();

            CommandBase incommingCmd = null;
           
            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.INIT:
                    Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
                    initCommand.requestId = command.requestID;
                    incommingCmd = new InitializeCommand();
                   
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.EXECUTE_READER:
                    Alachisoft.NCache.Common.Protobuf.ExecuteReaderCommand executeReaderCommand = command.executeReaderCommand;
                    executeReaderCommand.requestId = command.requestID;
                    incommingCmd = new ExecuteReaderCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_READER_CHUNK:
                    Alachisoft.NCache.Common.Protobuf.GetReaderNextChunkCommand getReaderChunkCommand = command.getReaderNextChunkCommand;
                    getReaderChunkCommand.requestId = command.requestID;
                    incommingCmd = new GetReaderChunkCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DISPOSE_READER:
                    Alachisoft.NCache.Common.Protobuf.DisposeReaderCommand disposeReaderCommand = command.disposeReaderCommand;
                    disposeReaderCommand.requestId = command.requestID;
                    incommingCmd = new DisposeReaderCommand();
                    break;
                    // added in server to cater getProductVersion request from client
                case Common.Protobuf.Command.Type.GET_PRODUCT_VERSION:
                    command.getProductVersionCommand.requestId = command.requestID;
                    incommingCmd = new GetProductVersionCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    command.addCommand.requestId = command.requestID;
                    incommingCmd = new AddCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                    command.bulkAddCommand.requestId = command.requestID;
                    incommingCmd = new BulkAddCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CLEAR:
                    command.clearCommand.requestId = command.requestID;
                    incommingCmd = new ClearCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CONTAINS:
                    command.containsCommand.requestId = command.requestID;
                    incommingCmd = new ContainsCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.COUNT:
                    command.countCommand.requestId = command.requestID;
                    incommingCmd = new CountCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DISPOSE:
                    command.disposeCommand.requestId = command.requestID;
                    incommingCmd = new DisposeCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET:
                    command.getCommand.requestId = command.requestID;
                    incommingCmd = new GetCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_BULK:
                    command.bulkGetCommand.requestId = command.requestID;
                    incommingCmd = new BulkGetCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_ITEM:
                    command.getCacheItemCommand.requestId = command.requestID;
                    incommingCmd = new GetCacheItemCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_BINDING:
                    command.getCacheBindingCommand.requestId = command.requestID;
                    incommingCmd = new GetCacheBindingCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_ENUMERATOR:
                    command.getEnumeratorCommand.requestId = command.requestID;
                    incommingCmd = new GetEnumeratorCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_NEXT_CHUNK:
                    command.getNextChunkCommand.requestId = command.requestID;
                    incommingCmd = new GetNextChunkCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_HASHMAP:
                    command.getHashmapCommand.requestId = command.requestID;
                    incommingCmd = new GetHashmapCommand();
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                    command.getOptimalServerCommand.requestId = command.requestID;
                    incommingCmd = new GetOptimalServerCommand();
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_RUNNING_SERVERS:
                    command.getRunningServersCommand.requestId = command.requestID;
                    incommingCmd = new GetRunningServersCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_LOGGING_INFO:
                    command.getLoggingInfoCommand.requestId = command.requestID;
                    incommingCmd = new GetLogginInfoCommand();
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_TYPEINFO_MAP:
                    command.getTypeInfoMapCommand.requestId = command.requestID;
                    incommingCmd = new GetTypeInfoMap();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT:
                    command.insertCommand.requestId = command.requestID;
                    incommingCmd = new InsertCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                    command.bulkInsertCommand.requestId = command.requestID;
                    incommingCmd = new BulkInsertCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.ISLOCKED:
                    command.isLockedCommand.requestId = command.requestID;
                    incommingCmd = new IsLockedCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.LOCK:
                    command.lockCommand.requestId = command.requestID;
                    incommingCmd = new LockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.LOCK_VERIFY:
                    command.lockVerifyCommand.requestId = command.requestID;
                    incommingCmd = new VerifyLockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF:
                    command.registerKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterKeyNotifcationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_NOTIF:
                    command.registerNotifCommand.requestId = command.requestID;
                    incommingCmd = new NotificationRegistered();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE:
                    command.removeCommand.requestId = command.requestID;
                    incommingCmd = new RemoveCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE:
                    command.deleteCommand.requestId = command.requestID;
                    incommingCmd = new DeleteCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BULK:
                    command.bulkRemoveCommand.requestId = command.requestID;
                    incommingCmd = new BulkRemoveCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE_BULK:
                    command.bulkDeleteCommand.requestId = command.requestID;
                    incommingCmd = new BulkDeleteCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH:
                    Alachisoft.NCache.Common.Protobuf.SearchCommand searchCommand = command.searchCommand;
                    searchCommand.requestId = command.requestID;

                    if (searchCommand.searchEntries)
                        incommingCmd = new SearchEnteriesCommand();
                    else
                        incommingCmd = new SearchCommand();

                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNLOCK:
                    command.unlockCommand.requestId = command.requestID;
                    incommingCmd = new UnlockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF:
                    command.unRegisterKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new UnRegisterKeyNoticationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_ATTRIBUTE:
                    command.addAttributeCommand.requestId = command.requestID;
                    incommingCmd = new AddAttributeCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF:
                    command.registerBulkKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterBulkKeyNotifcationCommand();
                    break;

            }
            if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationBeginSample();
       
            incommingCmd.ExecuteCommand(clientManager, command);/**/
           
            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + command.type.ToString() + " RequestId :" + command.requestID);


            if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationEndSample();

            if (clientManager != null && incommingCmd.SerializedResponsePackets != null && !clientManager.IsCacheStopped)
            {
                if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.IncrementResponsesPerSecStats(1);

                foreach (byte[] reponse in incommingCmd.SerializedResponsePackets)
                {
                    ConnectionManager.AssureSend(clientManager, reponse, Alachisoft.NCache.Common.Enum.Priority.Normal);
                }
            }
            
            if (stats != null)
            {
                stats.EndSample();
                if (incommingCmd != null)
                {
                    incommingCmd.IncrementCounter(_perfStatsCollector, stats.Current);
                }
            }

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");
        }
      
    }
}
