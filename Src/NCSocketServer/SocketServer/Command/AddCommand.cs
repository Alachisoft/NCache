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
using Alachisoft.NCache.Common.Monitoring;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
using System.Text;
using System.Collections;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddCommand : AddAndInsertCommandBase
    {
        private OperationResult _addResult = OperationResult.Success;
        CommandInfo cmdInfo;

        private readonly AddResponse _addResponse;
        private readonly OperationContext _operationContext;

        internal override OperationResult OperationResult
        {
            get
            {
                return _addResult;
            }
        }

        public AddCommand() : base()
        {
            _addResponse = new AddResponse();
            _operationContext = new OperationContext();
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "Add";
            details.Append("Command Key: " + cmdInfo.Key);
            details.Append(" ; ");

            UserBinaryObject binaryObject = cmdInfo.value as UserBinaryObject;
            if (binaryObject != null)
                details.Append("Command Value Size: " + binaryObject.Size);
            else
                details.Append("Command Value: " + cmdInfo.value);

            if (cmdInfo.Flag != null)
            {
                details.Append(" ; ");
                if (cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru))
                    details.Append("WriteThru: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru) + " ; ");
                if (cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    details.Append("WriteBehind: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind) + " ; ");

            }

            if (cmdInfo.ExpirationHint != null)
                details.Append("Dependency: " + cmdInfo.ExpirationHint.GetType().Name);
            return details.ToString();
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            long dataLength = 0;
            string exception = null;
            bool itemUpdated = false;
            bool itemRemove = false;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                try
                {
                    overload = command.MethodOverload;
                    serializationContext = nCache.CacheId;
                    cmdInfo = base.ParseCommand(command, clientManager, serializationContext);
                }
                catch (System.Exception exc)
                {
                    _addResult = OperationResult.Failure;
                    {
                        //PROTOBUF:RESPONSE
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    }

                    return;
                }

                Notifications callbackEntry = null;

                if (cmdInfo.UpdateCallbackId != -1 || cmdInfo.RemoveCallbackId != -1 || (!cmdInfo.RequestId.Equals("-1") && cmdInfo.DoAsync) || cmdInfo.DsItemAddedCallbackId != -1)
                {
                    if (cmdInfo.RemoveCallbackId != -1)
                        itemRemove = true;
                    if (cmdInfo.UpdateCallbackId != -1)
                        itemUpdated = true;

                    callbackEntry = new Notifications(!string.IsNullOrEmpty(cmdInfo.ClientID) ? cmdInfo.ClientID : clientManager.ClientID,
                        Convert.ToInt32(cmdInfo.RequestId),
                        cmdInfo.RemoveCallbackId,
                        cmdInfo.UpdateCallbackId,
                        (short)(cmdInfo.RequestId == -1 ? -1 : 0),
                        cmdInfo.DsItemAddedCallbackId,
                        (EventDataFilter)cmdInfo.UpdateDataFilter,
                        (EventDataFilter)cmdInfo.RemoveDataFilter);
                }

                UserBinaryObject data = cmdInfo.value as UserBinaryObject;
                if (data != null)
                    dataLength = data.Length;

                if (!cmdInfo.DoAsync)
                {
                    OperationContext operationContext = null;

                    try
                    {
                        operationContext = _operationContext;
                        operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                        CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                        UInt64 itemVersion = 0;
                        if (cmdInfo.ItemVersion == 0)
                            itemVersion = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                        else
                            itemVersion = cmdInfo.ItemVersion;

                        operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
                        operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                        operationContext?.MarkInUse(NCModulesConstants.SocketServer);

                        if (cmdInfo.Group != null)
                            if (string.IsNullOrEmpty(cmdInfo.Type)) cmdInfo.Type = oldClientsGroupType;

                        nCache.Cache.Add(cmdInfo.Key,
                            cmdInfo.value,
                            cmdInfo.ExpirationHint,
                            cmdInfo.EvictionHint,
                            cmdInfo.Group,
                            cmdInfo.SubGroup,
                            cmdInfo.queryInfo,
                            cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, operationContext, null, callbackEntry, cmdInfo.Type);
                        
                        stopWatch.Stop();

                        if(operationContext.Contains(OperationContextFieldName.ItemVersion))
                             itemVersion = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);

                        _addResponse.itemversion = itemVersion;

                        if (clientManager.ClientVersion >= 5000)
                        {
                            ResponseHelper.SetResponse(_addResponse, command.requestID, command.commandID);
                            _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(_addResponse, Response.Type.ADD));
                        }
                        else
                        {
                            //PROTOBUF:RESPONSE
                            var response = Stash.ProtobufResponse;
                            response.addResponse = _addResponse;
                            ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.ADD);

                            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                        }
                    }
                    catch (System.Exception exc)
                    {
                        _addResult = OperationResult.Failure;
                        exception = exc.ToString();
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

                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.ADD.ToLower());
                                object toInsert;
                                if (cmdInfo.value is UserBinaryObject)
                                {
                                    toInsert = dataLength;
                                }
                                else
                                    toInsert = cmdInfo.DataFormatValue;
                                Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                                log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, false, itemUpdated, itemRemove, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                            }
                        }
                        catch { }
                    }
                }

                else
                {
                    OperationContext operationContext = null;

                    try
                    {
                        operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                        
                        operationContext.Add(OperationContextFieldName.WriteThru, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
                        operationContext.Add(OperationContextFieldName.WriteBehind, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind));
                        if (cmdInfo.ProviderName != null)
                            operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.ProviderName);

                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                        UInt64 itemVersion = 0;
                        if (cmdInfo.ItemVersion == 0)
                            itemVersion = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                        else
                            itemVersion = cmdInfo.ItemVersion;


                        operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
                        operationContext.Add(OperationContextFieldName.MethodOverload, overload);
                        operationContext.MarkInUse(NCModulesConstants.SocketServer);

                        bool onAsyncCall = false;
                        if (callbackEntry != null)
                        {
                            onAsyncCall = true;
                        }

                        if (cmdInfo.Group != null)
                            if (string.IsNullOrEmpty(cmdInfo.Type)) cmdInfo.Type = oldClientsGroupType;

                        // Fetching this from pool to avoid value corruption for eviction hint for old 
                        cmdInfo.EvictionHint = Caching.EvictionPolicies.PriorityEvictionHint.Create(
                            clientManager.CacheTransactionalPool, cmdInfo.EvictionHint.Priority
                        );

                        nCache.Cache.AddAsync(cmdInfo.Key,
                            cmdInfo.value,
                            cmdInfo.ExpirationHint,
                            cmdInfo.EvictionHint,
                            cmdInfo.Group,
                            cmdInfo.SubGroup,
                            cmdInfo.Flag,
                            cmdInfo.queryInfo, cmdInfo.ProviderName, operationContext, callbackEntry, cmdInfo.Type);

                        stopWatch.Stop();
                        TimeSpan executionTime = stopWatch.Elapsed;
                        try
                        {
                            if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                            {

                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.ADDASYNC.ToLower());
                                Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                                object toInsert;
                                if (cmdInfo.value is UserBinaryObject)
                                {
                                    toInsert = dataLength;
                                }
                                else
                                    toInsert = cmdInfo.DataFormatValue;
                                log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, onAsyncCall, itemUpdated, itemRemove, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                            }
                        }
                        catch { }
                    }
                    finally
                    {
                        operationContext?.MarkFree(NCModulesConstants.SocketServer);
                    }
                }
            }
            finally
            {
                cmdInfo.Flag?.MarkFree(NCModulesConstants.SocketServer);
            }
        }

        public sealed override void ResetLeasable()
        {
            base.ResetLeasable();

            cmdInfo = default;
            _addResponse.ResetLeasable();
            _operationContext.ResetLeasable();
            _addResult = OperationResult.Success;
        }

        public sealed override void ReturnLeasableToPool()
        {
            _addResponse?.ReturnLeasableToPool();
            PoolManager.GetSocketServerAddCommandPool()?.Return(this);
        }
    }
}
