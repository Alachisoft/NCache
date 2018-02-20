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
using Alachisoft.NCache.Common.Monitoring;

using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common;
using System.Text;
using System.Collections;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;


namespace Alachisoft.NCache.SocketServer.Command
{
    class AddCommand : AddAndInsertCommandBase
    {
        private OperationResult _addResult = OperationResult.Success;
        CommandInfo cmdInfo;

        internal override OperationResult OperationResult
        {
            get
            {
                return _addResult;
            }
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

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            string exception = null;
            bool itemUpdated = false;
            bool itemRemove = false;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                serializationContext = nCache.CacheId;
                cmdInfo = base.ParseCommand(command, clientManager, serializationContext);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {
                _addResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            CallbackEntry callbackEntry = null;

            if (cmdInfo.UpdateCallbackId != -1 || cmdInfo.RemoveCallbackId != -1 || (!cmdInfo.RequestId.Equals("-1") && cmdInfo.DoAsync) || cmdInfo.DsItemAddedCallbackId != -1)
            {
                if (cmdInfo.RemoveCallbackId != -1)
                    itemRemove = true;
                if (cmdInfo.UpdateCallbackId != -1)
                    itemUpdated = true;

                callbackEntry = new CallbackEntry(!string.IsNullOrEmpty(cmdInfo.ClientID) ? cmdInfo.ClientID : clientManager.ClientID,
                    Convert.ToInt32(cmdInfo.RequestId),
                    cmdInfo.value,
                    cmdInfo.RemoveCallbackId,
                    cmdInfo.UpdateCallbackId,
                    (short)(cmdInfo.RequestId.Equals("-1") ? -1 : 0),
                    cmdInfo.DsItemAddedCallbackId,
                    cmdInfo.Flag,
                    (EventDataFilter)cmdInfo.UpdateDataFilter,
                    (EventDataFilter)cmdInfo.RemoveDataFilter);
            }

            if (!cmdInfo.DoAsync)
            {
                try
                {
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                    UInt64 itemVersion = 0;
                    if (cmdInfo.ItemVersion == 0)
                        itemVersion = (UInt64)(DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
                    else
                        itemVersion = cmdInfo.ItemVersion;

                    operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
                    operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                    nCache.Cache.Add(cmdInfo.Key,
                        callbackEntry == null ? cmdInfo.value : (object)callbackEntry,
                        cmdInfo.ExpirationHint,
                        cmdInfo.SyncDependency,
                        cmdInfo.EvictionHint,
                        cmdInfo.Group,
                        cmdInfo.SubGroup,
                        cmdInfo.queryInfo,
                        cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, operationContext, null);

                    stopWatch.Stop();

                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.AddResponse addResponse = new Alachisoft.NCache.Common.Protobuf.AddResponse();

                    // retrieve the version added...
                    itemVersion = (ulong)operationContext.GetValueByField(OperationContextFieldName.ItemVersion);

                    addResponse.itemversion = itemVersion;
                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                    response.commandID = command.commandID;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD;
                    response.addResponse = addResponse;

                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                catch (Exception exc)
                {
                    _addResult = OperationResult.Failure;
                    exception = exc.ToString();
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                finally
                {
                    TimeSpan executionTime = stopWatch.Elapsed;
                    try
                    {
                        if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.ADD.ToLower());
                            object toInsert;
                            if (cmdInfo.value is UserBinaryObject)
                            {
                                UserBinaryObject data = (UserBinaryObject)cmdInfo.value;
                                toInsert = data.Length;
                            }
                            else
                                toInsert = cmdInfo.DataFormatValue;
                            Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                            log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, cmdInfo.SyncDependency, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, false, itemUpdated, itemRemove, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                        }
                    }
                    catch
                    {
                    }
                }
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddCmd.Exec", "cmd executed on cache");
            }
            else
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.WriteThru, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
                operationContext.Add(OperationContextFieldName.WriteBehind, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind));

                if (cmdInfo.ProviderName != null)
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.ProviderName);

                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                UInt64 itemVersion = 0;
                if (cmdInfo.ItemVersion == 0)
                    itemVersion = (UInt64)(DateTime.Now - new System.DateTime(2016, 1, 1, 0, 0, 0)).TotalMilliseconds;
                else
                    itemVersion = cmdInfo.ItemVersion;

                operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
                operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                bool onAsyncCall = false;
                if (callbackEntry != null)
                {
                    onAsyncCall = true;
                }

                nCache.Cache.AddAsync(cmdInfo.Key,
                    callbackEntry == null ? (object)cmdInfo.value : (object)callbackEntry,
                    cmdInfo.ExpirationHint,
                    cmdInfo.SyncDependency,
                    cmdInfo.EvictionHint,
                    cmdInfo.Group,
                    cmdInfo.SubGroup,
                    cmdInfo.Flag,
                    cmdInfo.queryInfo, cmdInfo.ProviderName, operationContext);
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
                            UserBinaryObject data = (UserBinaryObject)cmdInfo.value;
                            toInsert = data.Length;
                        }
                        else
                            toInsert = cmdInfo.DataFormatValue;
                        log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, cmdInfo.SyncDependency, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, onAsyncCall, itemUpdated, itemRemove, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }
    }
}
