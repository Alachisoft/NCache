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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Pooling;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Util;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RemoveCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public bool DoAsync;
            public long RequestId;
            public string Key;
            public BitSet FlagMap;
            public short DsItemRemovedId;
            public object LockId;
            public ulong Version;
            public string ProviderName;
            public LockAccessType LockAccessType;
        }
        CommandInfo cmdInfo;

        private readonly BitSet _bitSet;
        private readonly RemoveResponse _removeResponse;
        private readonly OperationContext _operationContext;

        private OperationResult _removeResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _removeResult;
            }
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public RemoveCommand()
        {
            _bitSet = new BitSet();
            _removeResponse = new RemoveResponse();
            _operationContext = new OperationContext();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (System.Exception exc)
            {
                _removeResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            CompressedValueEntry flagValueEntry = null;
            NCache nCache = clientManager.CmdExecuter as NCache;

            if (!cmdInfo.DoAsync)
            {
                OperationContext operationContext = null;

                try
                {
                    Notifications notification = null;
                    if (cmdInfo.DsItemRemovedId != -1)
                    {
                        notification = new Notifications(clientManager.ClientID, -1, -1, -1, -1, cmdInfo.DsItemRemovedId
                            , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                    }

                    operationContext = _operationContext;
                    operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                    operationContext.Add(OperationContextFieldName.MethodOverload, overload);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                    CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                    flagValueEntry = nCache.Cache.Remove(cmdInfo.Key, cmdInfo.FlagMap, notification, cmdInfo.LockId, cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName, operationContext);
                    stopWatch.Stop();
                    UserBinaryObject ubObject = null;
                    if (flagValueEntry != null)
                    {
                        if (flagValueEntry.Value is UserBinaryObject)
                            ubObject = (UserBinaryObject)flagValueEntry.Value;
                        else
                        {
                            var flag = flagValueEntry.Flag;
                            ubObject = (flagValueEntry == null) ? null : (UserBinaryObject)nCache.Cache.SocketServerDataService.GetClientData(flagValueEntry.Value, ref flag, LanguageContext.DOTNET);
                        }
                    }


                    //PROTOBUF:RESPONSE
                    RemoveResponse removeResponse = _removeResponse;

                    if (ubObject != null)
                    {
                        removeResponse.value.AddRange(ubObject.DataList);
                        removeResponse.flag = flagValueEntry.Flag.Data;
                        removeResponse.itemType = MiscUtil.EntryTypeToProtoItemType(flagValueEntry.Type);
                    }

                    if (clientManager.ClientVersion >= 5000)
                    {
                        ResponseHelper.SetResponse(removeResponse, command.requestID, command.commandID);
                        _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(removeResponse, Response.Type.REMOVE));
                    }
                    else
                    {
                        Response response = Stash.ProtobufResponse;
                        {
                            response.remove = removeResponse;
                        };

                        ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.REMOVE);
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }
                }
                catch (System.Exception exc)
                {
                    _removeResult = OperationResult.Failure;
                    exception = exc.ToString();

                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                finally
                {
                    operationContext?.MarkFree(NCModulesConstants.SocketServer);

                    cmdInfo.FlagMap?.MarkFree(NCModulesConstants.SocketServer);
                    TimeSpan executionTime = stopWatch.Elapsed;

                    if (flagValueEntry != null)
                    {
                        MiscUtil.ReturnEntryToPool(flagValueEntry.Entry, clientManager.CacheTransactionalPool);
                        MiscUtil.ReturnCompressedEntryToPool(flagValueEntry, clientManager.CacheTransactionalPool);
                    }

                    try
                    {
                        if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                        {

                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.REMOVE.ToLower());
                            log.GenerateRemoveAPILogItem(cmdInfo.Key, cmdInfo.FlagMap, cmdInfo.LockId, (long)cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName, cmdInfo.DsItemRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                    }
                    catch
                    {
                    }
                }
            }

            else
            {
                OperationContext operationContext = null;

                try
                {
                    cmdInfo.FlagMap = new BitSet { Data = cmdInfo.FlagMap.Data };

                    object[] package = null;
                    if (cmdInfo.RequestId != -1 || cmdInfo.DsItemRemovedId != -1)
                    {
                        package = new object[] { cmdInfo.Key, cmdInfo.FlagMap, new Notifications(clientManager.ClientID,
                        Convert.ToInt32(cmdInfo.RequestId),
                        -1,
                        -1,
                        (short)(cmdInfo.RequestId == -1 ? -1 : 0),
                        cmdInfo.DsItemRemovedId
                        ,Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None),cmdInfo.ProviderName}; //DataFilter not required
                    }
                    else
                    {
                        package = new object[] { cmdInfo.Key, cmdInfo.FlagMap, null, cmdInfo.ProviderName };
                    }

                    operationContext = OperationContext.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);
                    operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                    operationContext.Add(OperationContextFieldName.MethodOverload, overload);
                    nCache.Cache.RemoveAsync(package, operationContext);
                    stopWatch.Stop();
                }
                finally
                {
                    operationContext?.MarkFree(NCModulesConstants.SocketServer);

                    TimeSpan executionTime = stopWatch.Elapsed;
                    try
                    {
                        if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.RemoveAsync.ToLower());
                            log.GenerateRemoveAPILogItem(cmdInfo.Key, cmdInfo.FlagMap, cmdInfo.LockId, (long)cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName, cmdInfo.DsItemRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                    }
                    catch { }
                }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.RemoveCommand removeCommand = command.removeCommand;
            cmdInfo.DoAsync = removeCommand.isAsync;
            cmdInfo.DsItemRemovedId = (short)removeCommand.datasourceItemRemovedCallbackId;

            BitSet bitset = BitSet.CreateAndMarkInUse(clientManager.CacheTransactionalPool, NCModulesConstants.SocketServer);
            bitset.Data =((byte)removeCommand.flag);
            cmdInfo.FlagMap = bitset;
            cmdInfo.Key = clientManager.CacheTransactionalPool.StringPool.GetString(removeCommand.key);
            cmdInfo.LockAccessType = (LockAccessType)removeCommand.lockAccessType;
            cmdInfo.LockId = removeCommand.lockId;
            cmdInfo.RequestId = removeCommand.requestId;
            cmdInfo.Version = removeCommand.version;
            cmdInfo.ProviderName = !string.IsNullOrEmpty(removeCommand.providerName) ? removeCommand.providerName : null;

            return cmdInfo;
        }

        public sealed override void ResetLeasable()
        {
            base.ResetLeasable();

            _bitSet.ResetLeasable();
            _removeResponse.ResetLeasable();
            _operationContext.ResetLeasable();
        }
    }
}
