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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Serialization;
using System.IO;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Threading;
using System.Threading;
using Alachisoft.NCache.Caching.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using System.Text;
using System.Collections;
using Alachisoft.NCache.SocketServer.RequestLogging;
using Alachisoft.NCache.Common.Util;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.CommandLogging;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// An object of this class is used by connection manager to get and send commands
    /// </summary>
    class CommandManager : ICommandManager
    {
        private StatisticsCounter _perfStatsCollector;
        private Bookie bookie;

        private const long MAX_ALLOWED_REQUESTS = 500 * 1000;
        private const long MAX_ALLOWED_REQESTS_PER_SECOND = 200;

        private static readonly ThrottlingManager s_throttleManager = new ThrottlingManager(MAX_ALLOWED_REQESTS_PER_SECOND);

        private static long _totalRequests = 0;

        private static bool _isReported = false;
        private static readonly object _mutex = new object();

     //   private ObjectPoolWithoutBag<InsertCommand> _insertCommandPool;
    
        private Latch _operationModeChangeLatch = new Latch((byte)OperationMode.ONLINE);
 
    
        public StatisticsCounter PerfStatsCollector
        {
            get { return _perfStatsCollector; }
        }

        public Bookie RequestLogger
        {
            get { return bookie; }
        }

        public CommandManager(StatisticsCounter perfStatsCollector)
        {
            _perfStatsCollector = perfStatsCollector;

            bookie = new RequestLogging.Bookie(perfStatsCollector);
           CommandLogManager.InitializeLogger(perfStatsCollector.InstanceName);

            CompactFormatterServices.RegisterCompactType(typeof(Common.DataStructures.StateTransferInfo), 130);
            CompactFormatterServices.RegisterCompactType(typeof(Common.DataStructures.ReplicatorStatusInfo), 131);
            CompactFormatterServices.RegisterCompactType(typeof(OperationContext), 153);
            CompactFormatterServices.RegisterCompactType(typeof(Common.DataStructures.EnumerationPointer), 161);
            CompactFormatterServices.RegisterCompactType(typeof(Common.DataStructures.EnumerationDataChunk), 162);
            if (ServiceConfiguration.EnableRequestCancellation) RequestMonitor.Instance.Initialize();


        }

        private static InsertCommand GenerateInsertCommand()
        {
            return new InsertCommand();
        }

        public object Deserialize(Stream buffer)
        {
            Alachisoft.NCache.Common.Protobuf.Command command = null;
            command = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Command>(buffer);
            buffer.Close();
            return command;
        }

        public virtual void ProcessCommand(ClientManager clientManager, object cmd, short cmdType, long acknowledgementId, UsageStats stats, bool waitforRequests)
        {
            var cache = clientManager.CmdExecuter as NCache;

            bool FiveOrAbove = clientManager.ClientVersion >= 5000;

            Alachisoft.NCache.Common.Protobuf.Command command = cmd as Alachisoft.NCache.Common.Protobuf.Command;

            Common.Protobuf.Command.Type type;

            if (cmdType == 0) type = command.type;
            else type = (Common.Protobuf.Command.Type)cmdType;

            // POTeam
            //HPTimeStats milliSecWatch = new HPTimeStats();
            //milliSecWatch.BeginSample();
            bool clientDisposed = false;
            bool isAsync = false;
            string _methodName = null;// type.ToString();;
            //Stopwatch commandExecution = new Stopwatch();
            //commandExecution.Start();


            CommandBase incommingCmd = null;
            bool isUnsafeCommand = false, doThrottleCommand = true;

            switch (type)
            {

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INIT:
                    Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
                    initCommand.requestId = command.requestID;
                    if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("ConnectionManager.ReceiveCallback", clientManager.ToString()  + initCommand.clientEditionId +  " RequestId :" + command.requestID);
                    incommingCmd = new InitializeCommand(bookie.RequestLoggingEnabled);
                    break;
               
                // Added in server to cater getProductVersion request from client
                case Common.Protobuf.Command.Type.GET_PRODUCT_VERSION:
                    if(FiveOrAbove)
                    {
                        Common.Protobuf.GetProductVersionCommand getProduct = cmd as Common.Protobuf.GetProductVersionCommand;
                        command = new Common.Protobuf.Command()
                        {
                            getProductVersionCommand = getProduct,
                            requestID = getProduct.requestId,
                            type = Common.Protobuf.Command.Type.GET_PRODUCT_VERSION
                        };
                    }
                   else command.getProductVersionCommand.requestId = command.requestID;
                    incommingCmd = new GetProductVersionCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.AddCommand addCmd = cmd as Common.Protobuf.AddCommand;

                        command = Stash.ProtobufCommand;
                        command.addCommand = addCmd;
                        command.commandID = addCmd.commandID;
                        command.requestID = addCmd.requestId;
                        command.version = addCmd.version;
                        command.MethodOverload = addCmd.MethodOverload;
                        command.type = Common.Protobuf.Command.Type.ADD;
                        isAsync = addCmd.isAsync;
                    }
                    else
                    {
                        isAsync = command.addCommand.isAsync;
                        command.addCommand.requestId = command.requestID;
                    }
                    isUnsafeCommand = true;
                    incommingCmd = Stash.SocketServerAddCommand;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkAddCommand bulkAddCmd = cmd as Common.Protobuf.BulkAddCommand;

                        command = new Common.Protobuf.Command()
                        {
                            bulkAddCommand = bulkAddCmd,
                            commandID = bulkAddCmd.commandID,
                            requestID = bulkAddCmd.requestId,
                            version = bulkAddCmd.version,
                            MethodOverload = bulkAddCmd.MethodOverload,
                            type = Common.Protobuf.Command.Type.ADD_BULK
                        };
                    }
                    else
                    {
                        command.bulkAddCommand.requestId = command.requestID;
                    }

                    isUnsafeCommand = true;
                    incommingCmd = new BulkAddCommand(); 
                    break;
                
                case Alachisoft.NCache.Common.Protobuf.Command.Type.CLEAR:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.ClearCommand clearCommand = cmd as Common.Protobuf.ClearCommand;

                        command = new Common.Protobuf.Command()
                        {
                            clearCommand = clearCommand,
                            requestID = clearCommand.requestId,
                            type = Common.Protobuf.Command.Type.CLEAR
                        };
                    }
                    else
                    {
                        command.clearCommand.requestId = command.requestID;
                    }

                    incommingCmd = new ClearCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CONTAINS:
                    command.containsCommand.requestId = command.requestID;
                    incommingCmd = new ContainsCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.CONTAINS_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.ContainsBulkCommand containsBulkCommand = cmd as Common.Protobuf.ContainsBulkCommand;

                        command = new Common.Protobuf.Command()
                        {
                            containsBulkCommand = containsBulkCommand,
                            requestID = containsBulkCommand.requestId,
                            type = Common.Protobuf.Command.Type.CONTAINS_BULK
                        };
                    }
                    else
                    {
                        command.containsBulkCommand.requestId = command.requestID;
                    }
                    incommingCmd = new ContainsCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.COUNT:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.CountCommand countCommand = cmd as Common.Protobuf.CountCommand;

                        command = new Common.Protobuf.Command()
                        {
                            countCommand = countCommand,
                            requestID = countCommand.requestId,
                            type = Common.Protobuf.Command.Type.COUNT
                        };
                    }

                    incommingCmd = new CountCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DISPOSE:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.DisposeCommand dispose = cmd as Common.Protobuf.DisposeCommand;
                        command = new Common.Protobuf.Command()
                        {
                            disposeCommand = dispose,
                            requestID = dispose.requestId,
                            type = Common.Protobuf.Command.Type.DISPOSE
                        };
                    }
                    else
                    {
                        command.disposeCommand.requestId = command.requestID;
                    }
                    incommingCmd = new DisposeCommand();
                    clientDisposed = true;
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetCommand getCmd = cmd as Common.Protobuf.GetCommand;

                        command = Stash.ProtobufCommand;
                        command.getCommand = getCmd;
                        command.commandID = getCmd.commandID;
                        command.requestID = getCmd.requestId;
                        command.MethodOverload = getCmd.MethodOverload;
                        command.type = Common.Protobuf.Command.Type.GET;
                    }
                    else
                    {
                        command.getCommand.requestId = command.requestID;
                    }

                    isUnsafeCommand = true;
                    incommingCmd = Stash.SocketServerGetCommand;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkGetCommand bulkGetCmd = cmd as Common.Protobuf.BulkGetCommand;
                        command = new Common.Protobuf.Command()
                        {
                            bulkGetCommand = bulkGetCmd,
                            commandID = bulkGetCmd.commandID,
                            requestID = bulkGetCmd.requestId,
                            clientLastViewId = bulkGetCmd.clientLastViewId,
                            intendedRecipient = bulkGetCmd.intendedRecipient,
                            commandVersion = bulkGetCmd.commandVersion,
                            MethodOverload = bulkGetCmd.MethodOverload,
                            type = Common.Protobuf.Command.Type.GET_BULK
                        };
                    }
                    else
                    {
                        command.bulkGetCommand.requestId = command.requestID;
                    }
                    incommingCmd = new BulkGetCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_BULK_CACHEITEM:
                    
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkGetCacheItemCommand bulkGetCache = cmd as Common.Protobuf.BulkGetCacheItemCommand;
                        command = new Common.Protobuf.Command()
                        {
                            bulkGetCacheItemCommand = bulkGetCache,
                            requestID = bulkGetCache.requestId,
                            MethodOverload = bulkGetCache.MethodOverload,
                            clientLastViewId=bulkGetCache.clientLastViewId,
                            commandVersion= bulkGetCache.commandVersion,
                            intendedRecipient= bulkGetCache.intendedRecipient,
                            type = Common.Protobuf.Command.Type.GET_BULK_CACHEITEM
                        };
                    }
                    else {
                        command.bulkGetCacheItemCommand.requestId = command.requestID;
                    }
                    incommingCmd = new BulkGetCacheItemCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_ITEM:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetCacheItemCommand getCacheItem = cmd as Common.Protobuf.GetCacheItemCommand;
                        command = new Common.Protobuf.Command()
                        {
                            getCacheItemCommand = getCacheItem,
                            requestID = getCacheItem.requestId,
                            MethodOverload = getCacheItem.MethodOverload,
                            type = Common.Protobuf.Command.Type.GET_CACHE_ITEM
                        };
                    }
                    else
                    {
                        command.getCacheItemCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetCacheItemCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_CACHE_BINDING:
                    command.getCacheBindingCommand.requestId = command.requestID;
                    incommingCmd = new GetCacheBindingCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_COMPACT_TYPES:
                    command.getCompactTypesCommand.requestId = command.requestID;
                    incommingCmd = new GetCompactTypesCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_ENUMERATOR:
                    command.getEnumeratorCommand.requestId = command.requestID;
                    incommingCmd = new GetEnumeratorCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_NEXT_CHUNK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetNextChunkCommand getNextChunkCmd = cmd as Common.Protobuf.GetNextChunkCommand;
                        command = new Common.Protobuf.Command()
                        {
                            getNextChunkCommand = getNextChunkCmd,
                            commandID = getNextChunkCmd.commandID,
                            requestID = getNextChunkCmd.requestId,
                            intendedRecipient = getNextChunkCmd.intendedRecipient,
                            type = Common.Protobuf.Command.Type.GET_NEXT_CHUNK
                        };
                    }
                    else
                    {
                        command.getNextChunkCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetNextChunkCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_GROUP_NEXT_CHUNK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetGroupNextChunkCommand getGroupNextChunkCommand = cmd as Common.Protobuf.GetGroupNextChunkCommand;
                        command = new Common.Protobuf.Command()
                        {
                            getGroupNextChunkCommand = getGroupNextChunkCommand,
                            requestID = getGroupNextChunkCommand.requestId,
                            type = Common.Protobuf.Command.Type.GET_GROUP_NEXT_CHUNK
                        };
                    }
                    else
                    {
                        command.getGroupNextChunkCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetGroupNextChunkCommand();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_HASHMAP:
                    command.getHashmapCommand.requestId = command.requestID;
                    incommingCmd = new GetHashmapCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_LOGGING_INFO:
                    command.getLoggingInfoCommand.requestId = command.requestID;
                    incommingCmd = new GetLogginInfoCommand();
                    break;

#if !(DEVELOPMENT)

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                    command.getOptimalServerCommand.requestId = command.requestID;
                    incommingCmd = new GetOptimalServerCommand();
                    break;

#endif
                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_SERIALIZATION_FORMAT:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetSerializationFormatCommand getSerializationFormatCommand = cmd as Common.Protobuf.GetSerializationFormatCommand;

                        command = new Common.Protobuf.Command()
                        {
                            getSerializationFormatCommand = getSerializationFormatCommand,
                            requestID = getSerializationFormatCommand.requestId,
                            type = Common.Protobuf.Command.Type.GET_SERIALIZATION_FORMAT
                        };
                    }
                    else
                    {
                        command.getSerializationFormatCommand.requestId = command.requestID;
                    }
                    
                    incommingCmd = new GetSerializationFormatCommand();
                    doThrottleCommand = false;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_TYPEINFO_MAP:
                    command.getTypeInfoMapCommand.requestId = command.requestID;
                    incommingCmd = new GetTypeInfoMap();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.InsertCommand insertCmd = cmd as Common.Protobuf.InsertCommand;

                        command = Stash.ProtobufCommand;
                        command.insertCommand = insertCmd;
                        command.commandID = insertCmd.commandID;
                        command.requestID = insertCmd.requestId;
                        command.version = insertCmd.version;
                        command.MethodOverload = insertCmd.MethodOverload;
                        command.type = Common.Protobuf.Command.Type.INSERT;

                        isAsync = insertCmd.isAsync;
                    }
                    else
                    {
                        isAsync = command.insertCommand.isAsync;
                    }

                    isUnsafeCommand = true;
                    incommingCmd = Stash.SocketServerInsertCommand;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkInsertCommand bulkInsertCmd = cmd as Common.Protobuf.BulkInsertCommand;

                        command = new Common.Protobuf.Command()
                        {
                            bulkInsertCommand = bulkInsertCmd,
                            commandID = bulkInsertCmd.commandID,
                            requestID = bulkInsertCmd.requestId,
                            clientLastViewId = bulkInsertCmd.clientLastViewId,
                            intendedRecipient = bulkInsertCmd.intendedRecipient,
                            version = bulkInsertCmd.version,
                            MethodOverload = bulkInsertCmd.MethodOverload,
                            type = Common.Protobuf.Command.Type.INSERT_BULK
                        };

                    }
                    incommingCmd = new BulkInsertCommand();
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.ISLOCKED:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.IsLockedCommand isLocked = cmd as Common.Protobuf.IsLockedCommand;

                        command = new Common.Protobuf.Command()
                        {
                            isLockedCommand = isLocked,
                            requestID = isLocked.requestId,
                            type = Common.Protobuf.Command.Type.ISLOCKED
                        };

                    }
                    else
                    {
                        command.isLockedCommand.requestId = command.requestID;
                    }
                    incommingCmd = new IsLockedCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.LOCK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.LockCommand lockCommand = cmd as Common.Protobuf.LockCommand;

                        command = new Common.Protobuf.Command()
                        {
                            lockCommand = lockCommand,
                            requestID = lockCommand.requestId,
                            MethodOverload = lockCommand.MethodOverload,
                            type = Common.Protobuf.Command.Type.LOCK
                        };

                    }
                    else
                    {
                        command.lockCommand.requestId = command.requestID;
                    }
                    incommingCmd = new LockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.LOCK_VERIFY:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.LockVerifyCommand lockVerifyCommand = cmd as Common.Protobuf.LockVerifyCommand;

                        command = new Common.Protobuf.Command()
                        {
                            lockVerifyCommand = lockVerifyCommand,
                            requestID = lockVerifyCommand.requestId,
                            type = Common.Protobuf.Command.Type.LOCK_VERIFY
                        };

                    }
                    else
                    {
                        command.lockVerifyCommand.requestId = command.requestID;
                    }
                    incommingCmd = new VerifyLockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.RAISE_CUSTOM_EVENT:
                   
                        if (FiveOrAbove)
                        {
                            Common.Protobuf.RaiseCustomEventCommand raiseCustomEvent = cmd as Common.Protobuf.RaiseCustomEventCommand;

                            command = new Common.Protobuf.Command()
                            {
                                raiseCustomEventCommand = raiseCustomEvent,
                                requestID = raiseCustomEvent.requestId,
                                MethodOverload=raiseCustomEvent.MethodOverload,
                                type = Common.Protobuf.Command.Type.RAISE_CUSTOM_EVENT
                            };

                        }
                             
                    else
                        command.raiseCustomEventCommand.requestId = command.requestID;
                    isUnsafeCommand = true;
                    incommingCmd = new RaiseCustomNotifCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.RegisterBulkKeyNotifCommand registerBulkKeyNotifCommand = cmd as Common.Protobuf.RegisterBulkKeyNotifCommand;

                        command = new Common.Protobuf.Command()
                        {
                            registerBulkKeyNotifCommand = registerBulkKeyNotifCommand,
                            requestID = registerBulkKeyNotifCommand.requestId,
                            type = Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF
                        };
                    }
                    else command.registerBulkKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterBulkKeyNotifcationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF:
                    if (FiveOrAbove) {
                        Common.Protobuf.RegisterKeyNotifCommand registerKeyNotifCommand = cmd as Common.Protobuf.RegisterKeyNotifCommand;

                        command = new Common.Protobuf.Command()
                        {
                            registerKeyNotifCommand = registerKeyNotifCommand,
                            requestID = registerKeyNotifCommand.requestId,
                            type = Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF
                        };
                    }
                    else command.registerKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterKeyNotifcationCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REGISTER_NOTIF:
                    command.registerNotifCommand.requestId = command.requestID;
                    incommingCmd = new NotificationRegistered();
                    break;

                case Common.Protobuf.Command.Type.REGISTER_POLLING_NOTIFICATION:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.RegisterPollingNotificationCommand registerNotifCommand = cmd as Common.Protobuf.RegisterPollingNotificationCommand;

                        command = new Common.Protobuf.Command()
                        {
                            registerPollNotifCommand = registerNotifCommand,
                            requestID = registerNotifCommand.requestId,
                            clientLastViewId=registerNotifCommand.clientLastViewId,
                            type = Common.Protobuf.Command.Type.REGISTER_POLLING_NOTIFICATION
                        };
                    }
                    else command.registerPollNotifCommand.requestId = command.requestID;
                    incommingCmd = new RegisterPollingNotificationCommand();
                    break;

                case Common.Protobuf.Command.Type.POLL:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.PollCommand poll = cmd as Common.Protobuf.PollCommand;
                        command = new Common.Protobuf.Command()
                        {
                            pollCommand = poll,
                            requestID = poll.requestId,
                            clientLastViewId= poll.clientLastViewId,
                            commandVersion= poll.commandVersion,
                            type = Common.Protobuf.Command.Type.POLL
                        };
                    }
                    else command.pollCommand.requestId = command.requestID;
                    incommingCmd = new PollCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.RemoveCommand removeCommand = cmd as Common.Protobuf.RemoveCommand;

                        command = Stash.ProtobufCommand;
                        command.removeCommand = removeCommand;
                        command.commandID = removeCommand.commandID;
                        command.requestID = removeCommand.requestId;
                        command.MethodOverload = removeCommand.MethodOverload;
                        command.type = Common.Protobuf.Command.Type.REMOVE;
                    }
                    else
                    {
                        command.removeCommand.requestId = command.requestID;
                    }

                    incommingCmd = Stash.SocketServerRemoveCommand;
                    isUnsafeCommand = true;
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.DeleteCommand deleteCommand = cmd as Common.Protobuf.DeleteCommand;

                        command = new Common.Protobuf.Command()
                        {
                            deleteCommand = deleteCommand,
                            commandID = deleteCommand.commandID,
                            requestID = deleteCommand.requestId,
                            MethodOverload = deleteCommand.MethodOverload,
                            type = Common.Protobuf.Command.Type.DELETE
                        };
                    }
                    else
                    {
                        command.deleteCommand.requestId = command.requestID;
                    }

                    incommingCmd = new DeleteCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkRemoveCommand removeCommand = cmd as Common.Protobuf.BulkRemoveCommand;

                        command = new Common.Protobuf.Command()
                        {
                            bulkRemoveCommand = removeCommand,
                            commandVersion = removeCommand.commandVersion,
                            clientLastViewId=removeCommand.clientLastViewId,
                            intendedRecipient=removeCommand.intendedRecipient,
                            requestID = removeCommand.requestId,
                            MethodOverload = removeCommand.MethodOverload,
                            type = Common.Protobuf.Command.Type.REMOVE_BULK
                        };
                    }
                    else
                    {
                        command.bulkRemoveCommand.requestId = command.requestID;
                    }
                    isUnsafeCommand = true;
                    incommingCmd = new BulkRemoveCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.DELETE_BULK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.BulkDeleteCommand deleteCommand = cmd as Common.Protobuf.BulkDeleteCommand;

                        command = new Common.Protobuf.Command()
                        {
                            bulkDeleteCommand = deleteCommand,
                            intendedRecipient = deleteCommand.intendedRecipient,
                            clientLastViewId=deleteCommand.clientLastViewId,
                            requestID = deleteCommand.requestId,
                            MethodOverload = deleteCommand.MethodOverload,
                            type = Common.Protobuf.Command.Type.DELETE_BULK
                        };
                    }
                    else
                    {
                        command.bulkDeleteCommand.requestId = command.requestID;
                    }

                    incommingCmd = new BulkDeleteCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNLOCK:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.UnlockCommand unlockCommand = cmd as Common.Protobuf.UnlockCommand;

                        command = new Common.Protobuf.Command()
                        {
                            unlockCommand = unlockCommand,
                            requestID = unlockCommand.requestId,
                            MethodOverload = unlockCommand.MethodOverload,
                            type = Common.Protobuf.Command.Type.UNLOCK
                        };
                    }
                    else
                    {
                        command.unlockCommand.requestId = command.requestID;
                    }
                    incommingCmd = new UnlockCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_BULK_KEY_NOTIF:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.UnRegisterBulkKeyNotifCommand unRegisterBulk = cmd as Common.Protobuf.UnRegisterBulkKeyNotifCommand;

                        command = new Common.Protobuf.Command()
                        {
                            unRegisterBulkKeyNotifCommand = unRegisterBulk,
                            requestID = unRegisterBulk.requestId,
                            type = Common.Protobuf.Command.Type.UNREGISTER_BULK_KEY_NOTIF
                        };
                    }
                    else command.unRegisterBulkKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new UnRegsisterBulkKeyNotification();
                    break;


                case Alachisoft.NCache.Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.UnRegisterKeyNotifCommand unRegisterKey = cmd as Common.Protobuf.UnRegisterKeyNotifCommand;

                        command = new Common.Protobuf.Command()
                        {
                            unRegisterKeyNotifCommand = unRegisterKey,
                            requestID = unRegisterKey.requestId,
                            type = Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF
                        };
                    }
                    else command.unRegisterKeyNotifCommand.requestId = command.requestID;
                    incommingCmd = new UnRegisterKeyNoticationCommand();
                    break;

#if  (SERVER || DEVELOPMENT || CLIENT)
#endif
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_ATTRIBUTE:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.AddAttributeCommand addCmd = cmd as Common.Protobuf.AddAttributeCommand;

                        command = new Common.Protobuf.Command()
                        {
                            addAttributeCommand = addCmd,
                            requestID = addCmd.requestId,
                            MethodOverload = addCmd.MethodOverload,
                            type = Common.Protobuf.Command.Type.ADD_ATTRIBUTE
                        };
                    }
                    else
                    {
                        command.addAttributeCommand.requestId = command.requestID;
                    }
                    incommingCmd = new AddAttributeCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.SYNC_EVENTS:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.SyncEventsCommand syncEvents = cmd as Common.Protobuf.SyncEventsCommand;

                        command = new Common.Protobuf.Command()
                        {
                            syncEventsCommand = syncEvents,
                            requestID = syncEvents.requestId,
                            type = Common.Protobuf.Command.Type.SYNC_EVENTS
                        };
                    }
                    else command.syncEventsCommand.requestId = command.requestID;
                    incommingCmd = new SyncEventCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INQUIRY_REQUEST:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.InquiryRequestCommand inquiryRequestCommand = cmd as Common.Protobuf.InquiryRequestCommand;

                        command = new Common.Protobuf.Command()
                        {
                            inquiryRequestCommand = inquiryRequestCommand,
                            requestID = inquiryRequestCommand.requestId,
                            type = Common.Protobuf.Command.Type.ADD_ATTRIBUTE
                        };
                    }

                    incommingCmd = new InquiryRequestCommand(bookie);
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_RUNNING_SERVERS:
                    command.getRunningServersCommand.requestId = command.requestID;
                    incommingCmd = new GetRunningServersCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.GET_EXPIRATION:
                    command.getExpirationCommand.requestId = command.requestID;
                    incommingCmd = new GetExpirationCommand();
                    break;

               

                case Common.Protobuf.Command.Type.GET_CONNECTED_CLIENTS:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetConnectedClientsCommand getConnectedClientsCommand = cmd as Common.Protobuf.GetConnectedClientsCommand;

                        command = new Common.Protobuf.Command()
                        {
                            getConnectedClientsCommand = getConnectedClientsCommand,
                            requestID = getConnectedClientsCommand.requestId,
                            type = Common.Protobuf.Command.Type.GET_CONNECTED_CLIENTS
                        };
                    }
                    else
                    {
                        command.getConnectedClientsCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetConnectedClientsCommand();
                    break;

                case Common.Protobuf.Command.Type.TOUCH:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.TouchCommand touch = cmd as Common.Protobuf.TouchCommand;
                        command = new Common.Protobuf.Command()
                        {
                            touchCommand = touch,
                            requestID = touch.requestId,
                            MethodOverload=touch.MethodOverload,
                            type = Common.Protobuf.Command.Type.TOUCH
                        };
                    }
                    else command.touchCommand.requestId = command.requestID;
                    incommingCmd = new TouchCommand();
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.PING:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.PingCommand ping = cmd as Common.Protobuf.PingCommand;
                        command = new Common.Protobuf.Command()
                        {
                            pingCommand = ping,
                            requestID = ping.requestId,
                            MethodOverload=ping.MethodOverload,
                            type = Common.Protobuf.Command.Type.PING
                        };
                    }
                    else command.pingCommand.requestId = command.requestID;
                    incommingCmd = new PingCommand();
                    break;

             


#region PUB_SUB
                case Common.Protobuf.Command.Type.GET_TOPIC:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetTopicCommand getTopic = cmd as Common.Protobuf.GetTopicCommand;

                        command = new Common.Protobuf.Command()
                        {
                            getTopicCommand = getTopic,
                            requestID = getTopic.requestId,
                            intendedRecipient=getTopic.intendedRecipient,
                            version=getTopic.version,
                            clientLastViewId=getTopic.clientLastViewId,
                            commandVersion=getTopic.commandVersion,
                            type = Common.Protobuf.Command.Type.GET_TOPIC
                        };
                    }
                    else
                    {
                        command.getTopicCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.SUBSCRIBE_TOPIC:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.SubscribeTopicCommand subscribeTopic = cmd as Common.Protobuf.SubscribeTopicCommand;

                        command = new Common.Protobuf.Command()
                        {
                            subscribeTopicCommand = subscribeTopic,
                            requestID = subscribeTopic.requestId,
                            clientLastViewId = subscribeTopic.clientLastViewId,
                            commandVersion = subscribeTopic.commandVersion,
                            version = subscribeTopic.version,
                            intendedRecipient = subscribeTopic.intendedRecipient,
                            type = Common.Protobuf.Command.Type.SUBSCRIBE_TOPIC
                        };
                    }
                   else command.subscribeTopicCommand.requestId = command.requestID;
                    incommingCmd = new SubscribeTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC:
                    if (FiveOrAbove) {
                        Common.Protobuf.UnSubscribeTopicCommand unSubscribeTopic = cmd as Common.Protobuf.UnSubscribeTopicCommand;

                        command = new Common.Protobuf.Command()
                        {
                            unSubscribeTopicCommand = unSubscribeTopic,
                            requestID = unSubscribeTopic.requestId,
                            clientLastViewId=unSubscribeTopic.clientLastViewId,
                            commandVersion=unSubscribeTopic.commandVersion,
                            version=unSubscribeTopic.version,
                            intendedRecipient=unSubscribeTopic.intendedRecipient,
                            type = Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC
                        };
                    }
                    else command.unSubscribeTopicCommand.requestId = command.requestID;
                    incommingCmd = new UnSubscribeTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.REMOVE_TOPIC:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.RemoveTopicCommand removeTopic = cmd as Common.Protobuf.RemoveTopicCommand;
                        command = new Common.Protobuf.Command()
                        {
                            removeTopicCommand = removeTopic,
                            requestID = removeTopic.requestId,
                            intendedRecipient = removeTopic.intendedRecipient,
                            version = removeTopic.version,
                            clientLastViewId = removeTopic.clientLastViewId,
                            commandVersion = removeTopic.commandVersion,
                            type = Common.Protobuf.Command.Type.REMOVE_TOPIC
                        };
                    }
                   else
                        command.removeTopicCommand.requestId = command.requestID;

                    incommingCmd = new RemoveTopicCommand();
                    break;

                case Common.Protobuf.Command.Type.MESSAGE_PUBLISH:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.MessagePublishCommand messagePublish = cmd as Common.Protobuf.MessagePublishCommand;
                        command = new Common.Protobuf.Command()
                        {
                            messagePublishCommand = messagePublish,
                            requestID = messagePublish.requestId,
                            intendedRecipient = messagePublish.intendedRecipient,
                            version = messagePublish.version,
                            clientLastViewId = messagePublish.clientLastViewId,
                            commandVersion = messagePublish.commandVersion,
                            type = Common.Protobuf.Command.Type.MESSAGE_PUBLISH
                        };
                    }
                    else
                    {
                        command.messagePublishCommand.requestId = command.requestID;
                    }
                    incommingCmd = new MessagePublishCommand();
                    break;

                case Common.Protobuf.Command.Type.GET_MESSAGE:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.GetMessageCommand getMessage = cmd as Common.Protobuf.GetMessageCommand;
                        command = new Common.Protobuf.Command()
                        {
                            getMessageCommand = getMessage,
                            requestID = getMessage.requestId,
                            intendedRecipient = getMessage.intendedRecipient,
                            version = getMessage.version,
                            clientLastViewId = getMessage.clientLastViewId,
                            commandVersion = getMessage.commandVersion,
                            type = Common.Protobuf.Command.Type.GET_MESSAGE
                        };
                    }
                    else
                    {
                         command.getMessageCommand.requestId = command.requestID;
                    }
                    incommingCmd = new GetMessageCommand();
                    break;

                case Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT:
                    if (FiveOrAbove)
                    {
                       Common.Protobuf.MesasgeAcknowledgmentCommand getMessage = cmd as Common.Protobuf.MesasgeAcknowledgmentCommand;
                        command = new Common.Protobuf.Command()
                        {
                            mesasgeAcknowledgmentCommand = getMessage,
                            requestID = getMessage.requestId,
                            intendedRecipient = getMessage.intendedRecipient,
                            version = getMessage.version,
                            clientLastViewId = getMessage.clientLastViewId,
                            commandVersion = getMessage.commandVersion,
                            type = Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT
                        };
                    }
                    else command.mesasgeAcknowledgmentCommand.requestId = command.requestID;
                    incommingCmd = new MessageAcknowledgementCommand();
                    break;
                case Alachisoft.NCache.Common.Protobuf.Command.Type.MESSAGE_COUNT:
                    if (FiveOrAbove)
                    {
                        Common.Protobuf.MessageCountCommand messageCount = cmd as Common.Protobuf.MessageCountCommand;
                        command = new Common.Protobuf.Command()
                        {
                            messageCountCommand = messageCount,
                            requestID = messageCount.requestId,
                            type = Common.Protobuf.Command.Type.MESSAGE_COUNT
                        };
                    }
                    else
                    {
                        command.messageCountCommand.requestId = command.requestID;
                    }
                    incommingCmd = new MessageCountCommand();
                    break;
#endregion
                
            }

            if (_operationModeChangeLatch.IsAnyBitsSet((byte)OperationMode.OFFLINE))
                incommingCmd = null;
              if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationBeginSample();


          
            
            ///*****************************************************************/
            ///**/incommingCmd.ExecuteCommand(clientManager, command, value);/**/
            ///*****************************************************************/
            //PROTOBUF
            /*****************************************************************/
            /**/

            try
            {
                if (incommingCmd != null)
                {
                    incommingCmd.RequestTimeout = clientManager.RequestTimeout;
                   if (IsMonitoringCommand(command) && ServiceConfiguration.EnableRequestCancellation && ((NCache)clientManager.CmdExecuter != null))
                    {
                        incommingCmd.StartWatch();
                        RequestMonitor.Instance.RegisterClientrequestsInLedger(clientManager.ClientID, ((NCache)clientManager.CmdExecuter).Cache.NCacheLog, command.requestID, incommingCmd);
                    }

                }

                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                {
                    if (clientDisposed)
                        bookie.RemoveClientAccount(clientManager.ClientID);
                    else
                        bookie.RegisterRequest(clientManager.ClientID, command.requestID, command.commandID,
                            acknowledgementId);

                }
                
                {
                    incommingCmd.ExecuteCommand(clientManager, command);
                }

                if (command.type == Alachisoft.NCache.Common.Protobuf.Command.Type.INIT && ServiceConfiguration.EnableRequestCancellation && clientManager.CmdExecuter !=null)
                {
                    RequestMonitor.Instance.RegisterClientLedger(clientManager.ClientID, ((NCache)clientManager.CmdExecuter).Cache.NCacheLog);
                }

                if (SocketServer.IsServerCounterEnabled)
                    clientManager.ConnectionManager.PerfStatsColl.IncrementRequestsPerSecStats(1);
            }
            catch (Exception ex)
            {
                if (isUnsafeCommand && clientManager.SupportAcknowledgement)
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID,
                        Alachisoft.NCache.Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                throw;
            }
            finally
            {
                if (IsMonitoringCommand(command) && ServiceConfiguration.EnableRequestCancellation)
                {
                    incommingCmd.Dispose();
                    RequestMonitor.Instance.UnRegisterClientRequests(clientManager.ClientID, command.requestID);
                }
            }


           if (SocketServer.IsServerCounterEnabled) _perfStatsCollector.MsecPerCacheOperationEndSample();

            if (isUnsafeCommand && clientManager.SupportAcknowledgement)
            {
                if (clientManager != null && clientManager.IsDisposed && incommingCmd.OperationResult == OperationResult.Failure)
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID, Common.Enum.RequestStatus.RECEIVED_WITH_ERROR, null);
                else
                    bookie.UpdateRequest(clientManager.ClientID, command.requestID, command.commandID, Common.Enum.RequestStatus.RECEIVED_AND_EXECUTED, incommingCmd.SerializedResponsePackets);
            }
            try
            {
                if (clientManager != null && !clientManager.IsCacheStopped)
                {
                    if (incommingCmd.SerializedResponsePackets != null && !incommingCmd.IsCancelled && incommingCmd.SerializedResponsePackets.Count > 0)
                    {

                        for (int i = 0; i < incommingCmd.SerializedResponsePackets.Count; i++)
                        {
                            IList response = (IList)incommingCmd.SerializedResponsePackets[i];

                            ConnectionManager.AssureSend(clientManager, response, waitforRequests);
                        }

                        incommingCmd.SerializedResponsePackets.Clear();
                    }
                   
                }
            }
            finally
            {
                if (cache != null && command != null)
                {
                    switch (command.type)
                    {
                        case Common.Protobuf.Command.Type.ADD:
                            command.addCommand.ReturnLeasableToPool();
                            break;

                        case Common.Protobuf.Command.Type.GET:
                            command.getCommand.ReturnLeasableToPool();
                            break;

                        case Common.Protobuf.Command.Type.INSERT:
                            command.insertCommand.ReturnLeasableToPool();
                            break;

                        case Common.Protobuf.Command.Type.REMOVE:
                            command.removeCommand.ReturnLeasableToPool();
                            break;
                    }
                }
            }
           
        }

        private byte[] SerializeResponse(Common.Protobuf.InsertResponse insertResponse, Alachisoft.NCache.Common.Protobuf.Response.Type type)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                short value = (short)type;
                byte[] responseTypeBytes = BitConverter.GetBytes(value);
                stream.Write(responseTypeBytes, 0, responseTypeBytes.Length);

                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize(stream, insertResponse);

                int messageLen = (int)stream.Length - (size.Length + responseTypeBytes.Length);
                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Position += responseTypeBytes.Length;
                stream.Write(size, 0, size.Length);

                return stream.ToArray();
            }
        }

        public Alachisoft.NCache.Common.DataStructures.RequestStatus GetRequestStatus(string clientId, long requestId, long commandId)
        {
            return bookie.GetRequestStatus(clientId, requestId, commandId);
        }

        

        public void RegisterOperationModeChangeEvent()
        {
            Alachisoft.NCache.Caching.Cache _cache = CacheProvider.Provider.GetCache("");
            if (_cache !=null )
                _cache.OperationModeChanged += new OperationModeChangedCallback(OperationModeChanged);
        }

        private bool IsMonitoringCommand (Alachisoft.NCache.Common.Protobuf.Command command)
        {
            switch (command.type)
            {
                case Common.Protobuf.Command.Type.INIT:
                case Common.Protobuf.Command.Type.GET_RUNNING_SERVERS:
                case Common.Protobuf.Command.Type.GET_HASHMAP:
                case Common.Protobuf.Command.Type.REGISTER_NOTIF:
                case Common.Protobuf.Command.Type.GET_COMPACT_TYPES:
                case Common.Protobuf.Command.Type.GET_TYPEINFO_MAP:
                case Common.Protobuf.Command.Type.GET_EXPIRATION:
                case Common.Protobuf.Command.Type.GET_OPTIMAL_SERVER:
                case Common.Protobuf.Command.Type.INSERT:



                    return false;
                default:
                    return true;
            }

            return true;
        }

      

        private void OperationModeChanged(OperationMode mode)
        {
            switch (mode)
            {
                case OperationMode.OFFLINE:
                    _operationModeChangeLatch.SetStatusBit((byte)OperationMode.OFFLINE, (byte)OperationMode.ONLINE);
                    break;
                case OperationMode.ONLINE:
                default:
                    _operationModeChangeLatch.SetStatusBit((byte)OperationMode.ONLINE, (byte)OperationMode.OFFLINE);
                    break;
            }

        }

        public void Dispose()
        {
            if (RequestMonitor.Instance != null && ServiceConfiguration.EnableRequestCancellation)
                RequestMonitor.Instance.Dispose();

        }
    }
}
