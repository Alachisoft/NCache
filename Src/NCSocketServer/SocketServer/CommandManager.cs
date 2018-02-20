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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Serialization;
using System.IO;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Threading;
#if JAVA
using Web = Alachisoft.TayzGrid.Web;
#else
using System.Collections;
using Alachisoft.NCache.SocketServer.RequestLogging;
using Alachisoft.NCache.Common.Util;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.CommandLogging;
#endif
namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// An object of this class is used by connection manager to get and send commands
    /// </summary>
    class CommandManager : ICommandManager
    {
        private PerfStatsCollector _perfStatsCollector;
        private Bookie bookie;

        private const long MAX_ALLOWED_REQUESTS = 500 * 1000;
        private const long MAX_ALLOWED_REQESTS_PER_SECOND = 200;

        private static readonly ThrottlingManager s_throttleManager = new ThrottlingManager(MAX_ALLOWED_REQESTS_PER_SECOND);

        private static long _totalRequests = 0;

        private static bool _isReported = false;
        private static readonly object _mutex = new object();

        public PerfStatsCollector PerfStatsCollector
        {
            get { return _perfStatsCollector; }
        }

        public Bookie RequestLogger
        {
            get { return bookie; }
        }

        public CommandManager(PerfStatsCollector perfStatsCollector)
        {
            _perfStatsCollector = perfStatsCollector;
            bookie = new RequestLogging.Bookie(perfStatsCollector);
            CommandLogManager.InitializeLogger(perfStatsCollector.InstanceName);

            CompactFormatterServices.RegisterCompactType(typeof(Web.Synchronization.SyncCache), 124);

            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.StateTransferInfo), 130);
            CompactFormatterServices.RegisterCompactType(typeof(Alachisoft.NCache.Common.DataStructures.ReplicatorStatusInfo), 131);

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
            bool clientDisposed = false;
            bool isAsync = false;
            string _methodName = command.type.ToString(); ;
            Stopwatch commandExecution = new Stopwatch();
            commandExecution.Start();

            CommandBase incommingCmd = null;
            bool isUnsafeCommand = false, doThrottleCommand = true;

            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.INIT:
                    Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
                    initCommand.requestId = command.requestID;
                    if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " RequestId :" + command.requestID);
                    incommingCmd = new InitializeCommand(bookie.RequestLoggingEnabled);
                    doThrottleCommand = false;
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.EXECUTE_READER:
                    Alachisoft.NCache.Common.Protobuf.ExecuteReaderCommand executeReaderCommand = command.executeReaderCommand;
                    executeReaderCommand.requestId = command.requestID;
                    incommingCmd = new ExecuteReaderCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.EXECUTE_READER_CQ:
                    Alachisoft.NCache.Common.Protobuf.ExecuteReaderCQCommand executeReaderCQCommand = command.executeReaderCQCommand;
                    executeReaderCQCommand.requestId = command.requestID;
                    incommingCmd = new ExecuteReaderCQCommand();
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

                // Added in server to cater getProductVersion request from client
                case Common.Protobuf.Command.Type.GET_PRODUCT_VERSION:
                    command.getProductVersionCommand.requestId = command.requestID;
                    incommingCmd = new GetProductVersionCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    command.addCommand.requestId = command.requestID;
                    isAsync = command.addCommand.isAsync;
                    incommingCmd = new AddCommand();
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                    command.bulkAddCommand.requestId = command.requestID;
                    incommingCmd = new BulkAddCommand();
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_DEPENDENCY:
                    command.addDependencyCommand.requestId = command.requestID;
                    incommingCmd = new AddDependencyCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_SYNC_DEPENDENCY:
                    command.addSyncDependencyCommand.requestId = command.requestID;
                    incommingCmd = new AddSyncDependencyCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CLEAR:
                    command.clearCommand.requestId = command.requestID;
                    incommingCmd = new ClearCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CLOSE_STREAM:
                    command.closeStreamCommand.requestId = command.requestID;
                    incommingCmd = new CloseStreamCommand();
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
                    clientDisposed = true;
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

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_GROUP_NEXT_CHUNK:
                    command.getGroupNextChunkCommand.requestId = command.requestID;
                    incommingCmd = new GetGroupNextChunkCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_GROUP:
                    Alachisoft.NCache.Common.Protobuf.GetGroupCommand getGroupCommand = command.getGroupCommand;
                    getGroupCommand.requestId = command.requestID;
                    if (getGroupCommand.getGroupKeys)
                    {
                        incommingCmd = new GetGroupKeys();
                        _methodName = MethodsName.GetGroupKeys;
                    }
                    else
                    {
                        incommingCmd = new GetGroupData();
                        _methodName = MethodsName.GetGroupData;
                    }
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_HASHMAP:
                    command.getHashmapCommand.requestId = command.requestID;
                    incommingCmd = new GetHashmapCommand();
                    doThrottleCommand = false;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_LOGGING_INFO:
                    command.getLoggingInfoCommand.requestId = command.requestID;
                    incommingCmd = new GetLogginInfoCommand();
                    break;

#if !(DEVELOPMENT)

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                    command.getOptimalServerCommand.requestId = command.requestID;
                    incommingCmd = new GetOptimalServerCommand();
                    doThrottleCommand = false;
                    break;
#endif
                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_STREAM_LENGTH:
                    command.getStreamLengthCommand.requestId = command.requestID;
                    incommingCmd = new GetStreamLengthCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_TAG:
                    command.getTagCommand.requestId = command.requestID;
                    incommingCmd = new GetTagCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BY_TAG:
                    command.removeByTagCommand.requestId = command.requestID;
                    incommingCmd = new RemoveByTagCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_KEYS_TAG:
                    command.getKeysByTagCommand.requestId = command.requestID;
                    incommingCmd = new GetKeysByTagCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_TYPEINFO_MAP:
                    command.getTypeInfoMapCommand.requestId = command.requestID;
                    incommingCmd = new GetTypeInfoMap();
                    doThrottleCommand = false;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT:
                    command.insertCommand.requestId = command.requestID;
                    incommingCmd = new InsertCommand();
                    isAsync = command.insertCommand.isAsync;
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                    command.bulkInsertCommand.requestId = command.requestID;
                    incommingCmd = new BulkInsertCommand();
                    isUnsafeCommand = true;
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

                case Alachisoft.NCache.Common.Protobuf.Command.Type.OPEN_STREAM:
                    command.openStreamCommand.requestId = command.requestID;
                    incommingCmd = new OpenStreamCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.RAISE_CUSTOM_EVENT:
                    command.raiseCustomEventCommand.requestId = command.requestID;
                    incommingCmd = new RaiseCustomNotifCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.READ_FROM_STREAM:
                    command.readFromStreamCommand.requestId = command.requestID;
                    incommingCmd = new ReadFromStreamCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF:
                    command.registerBulkKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterBulkKeyNotifcationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF:
                    command.registerKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterKeyNotifcationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_NOTIF:
                    command.registerNotifCommand.requestId = command.requestID;
                    incommingCmd = new NotificationRegistered();
                    doThrottleCommand = false;
                    break;

                case Common.Protobuf.Command.Type.REGISTER_POLLING_NOTIFICATION:
                    command.registerPollNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterPollingNotificationCommand();
                    break;

                case Common.Protobuf.Command.Type.POLL:
                    command.pollCommand.requestId = command.requestID;
                    incommingCmd = new PollCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE:
                    command.removeCommand.requestId = command.requestID;
                    incommingCmd = new RemoveCommand();
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE:
                    command.deleteCommand.requestId = command.requestID;
                    incommingCmd = new DeleteCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BULK:
                    command.bulkRemoveCommand.requestId = command.requestID;
                    incommingCmd = new BulkRemoveCommand();
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE_BULK:
                    command.bulkDeleteCommand.requestId = command.requestID;
                    incommingCmd = new BulkDeleteCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_GROUP:
                    command.removeGroupCommand.requestId = command.requestID;
                    incommingCmd = new RemoveGroupCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH:
                    Alachisoft.NCache.Common.Protobuf.SearchCommand searchCommand = command.searchCommand;
                    searchCommand.requestId = command.requestID;

                    if (searchCommand.searchEntries)
                    {
                        incommingCmd = new SearchEnteriesCommand();
                        _methodName = "SearchEnteries";
                    }
                    else
                    {
                        incommingCmd = new SearchCommand();
                        _methodName = "Search";
                    }
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.SEARCH_CQ:
                    Alachisoft.NCache.Common.Protobuf.SearchCQCommand searchCQCommand = command.searchCQCommand;
                    searchCQCommand.requestId = command.requestID;

                    if (searchCQCommand.searchEntries)
                    {
                        _methodName = "SearchCQEnteries";
                        incommingCmd = new SearchEnteriesCQCommand();
                    }
                    else
                    {
                        _methodName = "SearchCQ";
                        incommingCmd = new SearchCQCommand();
                    }
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_CQ:
                    command.unRegisterCQCommand.requestId = command.requestID;
                    incommingCmd = new UnRegisterCQCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_CQ:
                    command.registerCQCommand.requestId = command.requestID;
                    incommingCmd = new RegisterCQCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETEQUERY:
                    Alachisoft.NCache.Common.Protobuf.DeleteQueryCommand deleteQueryCommand = command.deleteQueryCommand;
                    deleteQueryCommand.requestId = command.requestID;

                    if (deleteQueryCommand.isRemove)
                    {
                        incommingCmd = new RemoveQueryCommand();
                        _methodName = "RemoveQuery";
                    }
                    else
                    {
                        incommingCmd = new DeleteQueryCommand();
                        _methodName = "DeleteQuery";
                    }
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNLOCK:
                    command.unlockCommand.requestId = command.requestID;
                    incommingCmd = new UnlockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_BULK_KEY_NOTIF:
                    command.unRegisterBulkKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new UnRegsisterBulkKeyNotification();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF:
                    command.unRegisterKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new UnRegisterKeyNoticationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.WRITE_TO_STREAM:
                    command.writeToStreamCommand.requestId = command.requestID;
                    incommingCmd = new WriteToStreamCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_ATTRIBUTE:
                    command.addAttributeCommand.requestId = command.requestID;
                    incommingCmd = new AddAttributeCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.SYNC_EVENTS:
                    command.syncEventsCommand.requestId = command.requestID;
                    incommingCmd = new SyncEventCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INQUIRY_REQUEST:
                    incommingCmd = new InquiryRequestCommand(bookie);
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.MAP_REDUCE_TASK:
                    command.mapReduceTaskCommand.requestId = command.requestID;
                    incommingCmd = new MapReduceTaskCommand();
                    break;

                case Common.Protobuf.Command.Type.TASK_CALLBACK:
                    command.TaskCallbackCommand.requestId = command.requestID;
                    incommingCmd = new TaskCallbackCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CANCEL_TASK:
                    incommingCmd = new TaskCancelCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.RUNNING_TASKS:
                    command.RunningTasksCommand.requestId = command.requestID;
                    incommingCmd = new GetRunningTasksCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.TASK_PROGRESS:
                    command.TaskProgressCommand.requestId = command.requestID;
                    incommingCmd = new TaskProgressCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.TASK_ENUMERATOR:
                    incommingCmd = new TaskEnumeratorCommand();
                    break;

                case Common.Protobuf.Command.Type.TASK_NEXT_RECORD:
                    command.NextRecordCommand.RequestId = command.requestID;
                    incommingCmd = new TaskNextRecordCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INVOKE_ENTRY_PROCESSOR:
                    incommingCmd = new InvokeEntryProcessorCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_RUNNING_SERVERS:
                    command.getRunningServersCommand.requestId = command.requestID;
                    incommingCmd = new GetRunningServersCommand();
                    doThrottleCommand = false;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_EXPIRATION:
                    command.getExpirationCommand.requestId = command.requestID;
                    incommingCmd = new GetExpirationCommand();
                    doThrottleCommand = false;
                    break;

                case Common.Protobuf.Command.Type.GET_CONNECTED_CLIENTS:
                    command.getConnectedClientsCommand.requestId = command.requestID;
                    incommingCmd = new GetConnectedClientsCommand();
                    break;

                case Common.Protobuf.Command.Type.TOUCH:
                    command.touchCommand.requestId = command.requestID;
                    incommingCmd = new TouchCommand();
                    break;

              

                #region PUB_SUB
                case Common.Protobuf.Command.Type.GET_TOPIC:
                    command.getTopicCommand.requestId = command.requestID;
                    incommingCmd = new GetTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.SUBSCRIBE_TOPIC:
                    command.subscribeTopicCommand.requestId = command.requestID;
                    incommingCmd = new SubscribeTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC:
                    command.unSubscribeTopicCommand.requestId = command.requestID;
                    incommingCmd = new UnSubscribeTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.REMOVE_TOPIC:
                    command.removeTopicCommand.requestId = command.requestID;
                    incommingCmd = new RemoveTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.MESSAGE_PUBLISH:
                    command.messagePublishCommand.requestId = command.requestID;
                    incommingCmd = new MessagePublishCommand();
                    break;
                case Common.Protobuf.Command.Type.GET_MESSAGE:
                    command.getMessageCommand.requestId = command.requestID;
                    incommingCmd = new GetMessageCommand();
                    break;
                case Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT:
                    command.mesasgeAcknowledgmentCommand.requestId = command.requestID;
                    incommingCmd = new MessageAcknowledgementCommand();
                    break;
                    #endregion
            }


            if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationBeginSample();

            try
            {
                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                {
                    if (clientDisposed)
                        bookie.RemoveClientAccount(clientManager.ClientID);
                    else
                        bookie.RegisterRequest(clientManager.ClientID, command.requestID, command.commandID,
                            acknowledgementId);
                }
                incommingCmd.ExecuteCommand(clientManager, command);
            }
            catch (Exception ex)
            {
                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID,
                        Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                throw;
            }

            if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString() + " after executing COMMAND : " + command.type.ToString() + " RequestId :" + command.requestID);

            if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationEndSample();

#if COMMUNITY
            if (clientManager != null && clientManager.CmdExecuter != null && incommingCmd.OperationResult == OperationResult.Success)
            {
                clientManager.CmdExecuter.UpdateSocketServerStats(new SocketServerStats(clientManager.ClientsRequests, clientManager.ClientsBytesSent, clientManager.ClientsBytesRecieved));
            }
#endif
            if (isUnsafeCommand && clientManager.SupportAcknowledgement)
            {
                if (clientManager != null && clientManager.IsDisposed && incommingCmd.OperationResult == OperationResult.Failure)
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID, Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                else
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID, Common.Enum.RequestStatus.RECEIVED_AND_EXECUTED, incommingCmd.SerializedResponsePackets);
            }
            if (clientManager != null && !clientManager.IsCacheStopped)
            {
                if (incommingCmd.SerializedResponsePackets != null)
                {
                    if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.IncrementResponsesPerSecStats(1);

                    foreach (IList reponse in incommingCmd.SerializedResponsePackets)
                    {
                        ConnectionManager.AssureSend(clientManager, reponse, Alachisoft.NCache.Common.Enum.Priority.Normal);
                    }
                }
                commandExecution.Stop();
                if (!isAsync && command.type != Common.Protobuf.Command.Type.PING && (incommingCmd.SerializedResponsePackets == null || incommingCmd.SerializedResponsePackets.Count <= 0))
                {
                    try
                    {
                        if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                        {
                            APILogItemBuilder log = new APILogItemBuilder();
                            log.GenerateCommandManagerLog(_methodName, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), commandExecution.Elapsed, "Serialized Response Packets for " + _methodName + " command is null or empty.");
                        }
                    }
                    catch
                    {
                    }
                }
            }
            double commandElapsedSeconds = commandExecution.Elapsed.TotalSeconds;

            if (ServiceConfiguration.EnableCommandThresholdLogging && commandElapsedSeconds > ServiceConfiguration.CommandExecutionThreshold)
            {
                try
                {
                    string commandName;
                    string details = incommingCmd.GetCommandParameters(out commandName);
                    string[] clientIdParts = clientManager.ClientID.Split(':');
                    string clientipid = "CLIENT";
                    try
                    {
                        clientipid = clientIdParts[clientIdParts.Length - 2] + ":" + clientIdParts[clientIdParts.Length - 1];
                    }
                    catch { }

                    CommandLogManager.LogInfo(clientipid, commandElapsedSeconds.ToString(), commandName, details);
                }
                catch (Exception ex)
                {
                }
            }

            if (stats != null)
            {
                stats.EndSample();
                if (incommingCmd != null)
                {
                    // Increment Counter
                    incommingCmd.IncrementCounter(_perfStatsCollector, stats.Current);
                }
            }

            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("CmdMgr.PrsCmd", "exit");
        }


        /// <summary>
        /// Get the command from command string by checking the starting letters of string
        /// and initializes a command of that type 
        /// </summary>
        /// <param name="command">command string</param>
        /// <returns>command base</returns>
        public CommandBase GetCommandObject(string command)
        {
            CommandBase incommingCmd = null;

            if (command.StartsWith("INIT ") || command.StartsWith("INIT_NEW "))
                incommingCmd = new InitializeCommand(bookie.RequestLoggingEnabled);

            if (command.StartsWith("INITSECONDARY "))
                incommingCmd = new InitSecondarySocketCommand();

#if !(DEVELOPMENT)

            else if (command.StartsWith("GETOPTIMALSERVER"))
                incommingCmd = new GetOptimalServerCommand();

#endif
            else if (command.StartsWith("GETCACHEBINDING"))
                incommingCmd = new GetCacheBindingCommand();

            else if (command.StartsWith("ADD "))
            {
                incommingCmd = new AddCommand();
            }

            else if (command.StartsWith("INSERT "))
            {
                incommingCmd = new InsertCommand();
            }

            else if (command.StartsWith("GET "))
            {
                incommingCmd = new GetCommand();
            }

            else if (command.StartsWith("GETTAG "))
                incommingCmd = new GetTagCommand();

            else if (command.StartsWith("REMOVE "))
            {
                incommingCmd = new RemoveCommand();
            }

            else if (command.StartsWith("REMOVEGROUP "))
                incommingCmd = new RemoveGroupCommand();

            else if (command.StartsWith("CONTAINS "))
                incommingCmd = new ContainsCommand();

            else if (command.StartsWith("COUNT "))
                incommingCmd = new CountCommand();

            else if (command.StartsWith("CLEAR "))
                incommingCmd = new ClearCommand();

            else if (command.StartsWith("NOTIF "))
                incommingCmd = new NotificationRegistered();

            else if (command.StartsWith("RAISECUSTOMNOTIF "))
                incommingCmd = new RaiseCustomNotifCommand();

            else if (command.StartsWith("ADDBULK "))
            {
                incommingCmd = new BulkAddCommand();
            }

            else if (command.StartsWith("INSERTBULK "))
            {
                incommingCmd = new BulkInsertCommand();
            }

            else if (command.StartsWith("GETBULK "))
            {
                incommingCmd = new BulkGetCommand();
            }

            else if (command.StartsWith("REMOVEBULK "))
            {
                incommingCmd = new BulkRemoveCommand();
            }

            else if (command.StartsWith("UNLOCK "))
                incommingCmd = new UnlockCommand();

            else if (command.StartsWith("LOCK "))
                incommingCmd = new LockCommand();

            else if (command.StartsWith("ISLOCKED "))
                incommingCmd = new IsLockedCommand();

            else if (command.StartsWith("GETCACHEITEM "))
                incommingCmd = new GetCacheItemCommand();

            else if (command.StartsWith("GETGROUPKEYS "))
                incommingCmd = new GetGroupKeys();

            else if (command.StartsWith("GETGROUPDATA "))
                incommingCmd = new GetGroupData();

            else if (command.StartsWith("ADDDEPENDENCY "))
                incommingCmd = new AddDependencyCommand();

            else if (command.StartsWith("ADDSYNCDEPENDENCY "))
                incommingCmd = new AddSyncDependencyCommand();

            else if (command.StartsWith("GETENUM "))
                incommingCmd = new GetEnumeratorCommand();

            else if (command.StartsWith("REGKEYNOTIF "))
                incommingCmd = new RegisterKeyNotifcationCommand();

            else if (command.StartsWith("UNREGKEYNOTIF "))
                incommingCmd = new UnRegisterKeyNoticationCommand();

            else if (command.StartsWith("GETTYPEINFOMAP "))
                incommingCmd = new GetTypeInfoMap();

            else if (command.StartsWith("GETHASHMAP "))
                incommingCmd = new GetHashmapCommand();

            else if (command.StartsWith("SEARCH "))
                incommingCmd = new SearchCommand();

            else if (command.StartsWith("SEARCHENTERIES "))
                incommingCmd = new SearchEnteriesCommand();

            else if (command.StartsWith("BULKREGKEYNOTIF "))
                incommingCmd = new RegisterBulkKeyNotifcationCommand();

            else if (command.StartsWith("BULKUNREGKEYNOTIF "))
                incommingCmd = new UnRegsisterBulkKeyNotification();

            else if (command.StartsWith("DISPOSE "))
                incommingCmd = new DisposeCommand();
#if COMMUNITY

            else if (command.StartsWith("OPENSTREAM "))
                incommingCmd = new OpenStreamCommand();

            else if (command.StartsWith("CLOSESTREAM "))
                incommingCmd = new CloseStreamCommand();

            else if (command.StartsWith("READFROMSTREAM "))
                incommingCmd = new ReadFromStreamCommand();

            else if (command.StartsWith("WRITETOSTREAM "))
                incommingCmd = new WriteToStreamCommand();

            else if (command.StartsWith("GETSTREAMLENGTH "))
                incommingCmd = new GetStreamLengthCommand();
#endif
            else if (command.StartsWith("GETLOGGINGINFO "))
                incommingCmd = new GetLogginInfoCommand();

            return incommingCmd;
        }

        public Alachisoft.NCache.Common.DataStructures.RequestStatus GetRequestStatus(string clientId, long requestId, long commandId)
        {
            return bookie.GetRequestStatus(clientId, requestId, commandId);
        }

        private const string EVAL_LIMIT_MESSAGE = "You've hit the maximum requests for cache '{0}' that can be made to a cache server under pre-evaluation. Please request a trial license key at sales@alachisoft.com to remove this limitation.";

        static string CACHE_NAME = "";

        /// <summary>This method throttles and increments the total requests if the License is in Evaluation mode. </summary>
        /// <param name="exception">Sets an exception if the number of performed requests are greater than the limit.</param>
        /// <returns>True indicating to continue execution, else stop...</returns>
        private bool CheckContinuationAndThrottle(ClientManager clientManager, out Exception exception)
        {
            exception = null;
            return true;
        }
    }
}
