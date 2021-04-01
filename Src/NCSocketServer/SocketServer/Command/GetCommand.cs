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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetCommand : CommandBase
    {
        private struct CommandInfo
        {
            public long RequestId;
            public string Key;
            public BitSet FlagMap;
            public LockAccessType LockAccessType;
            public object LockId;
            public TimeSpan LockTimeout;
            public ulong CacheItemVersion;
            public string ProviderName;
            public int ThreadId;
        }

        
        private OperationResult _getResult = OperationResult.Success;
        CommandInfo cmdInfo;

        private readonly BitSet _bitSet;
        private readonly OperationContext _operationContext;
        private readonly Common.Protobuf.GetResponse _getResponse;

        internal override OperationResult OperationResult
        {
            get
            {
                return _getResult;
            }
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public GetCommand()
        {
            _bitSet = new BitSet();
            _operationContext = new OperationContext();
            _getResponse = new Common.Protobuf.GetResponse();
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "Get";
            details.Append("Command Keys: " + cmdInfo.Key);
            details.Append(" ; ");
            return details.ToString();
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int dataLength = 0;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
			try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "GetCommand", "command: " + command + " Error" + arEx);
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(arEx, command.requestID,command.commandID, clientManager.ClientVersion));
                return;
            }
            catch (Exception exc)
            {
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }
            Alachisoft.NCache.Common.Protobuf.GetResponse getResponse = null;
            CompressedValueEntry flagValueEntry = null;
            OperationContext operationContext = null;
            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                object lockId = cmdInfo.LockId;
                ulong version = cmdInfo.CacheItemVersion;
                DateTime lockDate = new DateTime();

                operationContext = _operationContext;
                operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                if (cmdInfo.LockAccessType == LockAccessType.ACQUIRE)
                {
                    operationContext.Add(OperationContextFieldName.ClientThreadId, clientManager.ClientID);
                    operationContext.Add(OperationContextFieldName.ClientThreadId, cmdInfo.ThreadId);
                    operationContext.Add(OperationContextFieldName.IsRetryOperation, command.isRetryCommand);
                }
               
                flagValueEntry = nCache.Cache.GetGroup(cmdInfo.Key, cmdInfo.FlagMap, null,null, ref version, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType,  operationContext);

                stopWatch.Stop();
                UserBinaryObject ubObj = null;

                getResponse = _getResponse;

                if (flagValueEntry != null)
                {
                    if (flagValueEntry.Value is UserBinaryObject)
                        ubObj = (UserBinaryObject)flagValueEntry.Value;
                    else
                    {
                        var flag = flagValueEntry.Flag;
                        ubObj = (UserBinaryObject)nCache.Cache.SocketServerDataService.GetClientData(flagValueEntry.Value, ref flag, LanguageContext.DOTNET);
                    }
                    if(flagValueEntry.Value!=null)
                    {
                        getResponse.itemType = MiscUtil.EntryTypeToProtoItemType(flagValueEntry.Type);// (Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType)flagValueEntry.Type;
                    }
                }
                if (ubObj != null)
                    dataLength = ubObj.Length;

                if (clientManager.ClientVersion >= 5000)
                {
                    if (lockId != null)
                    {
                        getResponse.lockId = lockId.ToString();
                    }
                    getResponse.requestId = cmdInfo.RequestId;
                    getResponse.commandID = command.commandID;
                    getResponse.lockTime = lockDate.Ticks;
                    getResponse.version = version;
                    if (ubObj == null)
                    {
                        //  response.get = getResponse;
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(getResponse, Common.Protobuf.Response.Type.GET));
                    }
                    else
                    {
                        //_dataPackageArray = ubObj.Data;
                        getResponse.flag = flagValueEntry.Flag.Data;
                        getResponse.data.AddRange(ubObj.DataList);
                        //  response.get = getResponse;
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(getResponse, Common.Protobuf.Response.Type.GET));
                    }
                }
                else
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = Stash.ProtobufResponse;

                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                    response.commandID = command.commandID;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET;
                    if (lockId != null)
                    {
                        getResponse.lockId = lockId.ToString();
                    }
                    getResponse.lockTime = lockDate.Ticks;
                    getResponse.version = version;
                    if (ubObj == null)
                    {
                        response.get = getResponse;
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }
                    else
                    {
                        getResponse.flag = flagValueEntry.Flag.Data;
                        getResponse.data.AddRange(ubObj.DataList);
                        response.get = getResponse;
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _getResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                operationContext?.MarkFree(NCModulesConstants.SocketServer);

                TimeSpan executionTime = stopWatch.Elapsed;

               
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        int resutlt = 0;
                        if (getResponse != null)
                        {
                            resutlt = dataLength;
                        }
                        string methodName = null;
                        if (cmdInfo.LockAccessType == LockAccessType.ACQUIRE)
                            methodName = MethodsName.GET.ToLower();
                       
                        else
                            methodName = MethodsName.GET.ToLower();
                        APILogItemBuilder log = new APILogItemBuilder(methodName);
                        log.GenerateGetCommandAPILogItem(cmdInfo.Key, null,null, (long)cmdInfo.CacheItemVersion, cmdInfo.LockAccessType, cmdInfo.LockTimeout, cmdInfo.LockId, cmdInfo.ProviderName, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), resutlt);

                    }
                }
                catch
                {

                }
                if(flagValueEntry != null)
                {
                    MiscUtil.ReturnCompressedEntryToPool(flagValueEntry, clientManager.CacheTransactionalPool);
                    MiscUtil.ReturnEntryToPool(flagValueEntry.Entry, clientManager.CacheTransactionalPool);
                }
            }
			//}
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GetCmd.Exec", "cmd executed on cache");

        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCommand getCommand = command.getCommand;

            cmdInfo.CacheItemVersion = getCommand.version;
            BitSet bitset = _bitSet;
            bitset.Data =((byte)getCommand.flag);
            cmdInfo.FlagMap = bitset;

            cmdInfo.Key = clientManager.CacheTransactionalPool.StringPool.GetString(getCommand.key);
            cmdInfo.LockAccessType = (LockAccessType)getCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCommand.lockInfo.lockTimeout);
			cmdInfo.ProviderName = getCommand.providerName.Length == 0 ? null : getCommand.providerName;
            cmdInfo.RequestId = getCommand.requestId;
            cmdInfo.ThreadId = getCommand.threadId;
         
            return cmdInfo;
        }


  
        public sealed override void ResetLeasable()
        {
            base.ResetLeasable();

            _bitSet.ResetLeasable();
            _getResponse.ResetLeasable();
            _operationContext.ResetLeasable();
        }
    }
}
