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
using System.Collections;
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkAddCommand : BulkAddAndInsertCommandBase
    {
        private OperationResult _addBulkResult = OperationResult.Success;
        CommandInfo cmdInfo;

        internal override OperationResult OperationResult
        {
            get
            {
                return _addBulkResult;
            }
        }
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "AddBulk";
            details.Append("Command Keys: " + cmdInfo.Keys.Length);
            details.Append(" ; ");
            details.Append("Command Values: " + cmdInfo.Entries.Length);
            details.Append(" ; ");
            details.Append("WriteThru: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
            return details.ToString();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {

            ClientId = clientManager.ClientID; 

            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            Hashtable queryInfo = null;
            ExpirationHint expHint = null;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                serailizationContext = nCache.CacheId;
                cmdInfo = base.ParseCommand(command, clientManager, serailizationContext);
            }
            catch (System.Exception exc)
            {
                _addBulkResult = OperationResult.Failure;
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            try
            {
                //PROTOBUF:RESPONSE
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                operationContext.Add(OperationContextFieldName.WriteThru, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
                operationContext.Add(OperationContextFieldName.WriteBehind, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind));
                operationContext.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                operationContext.CancellationToken = CancellationToken;
                
                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);
                queryInfo = cmdInfo.Entries[0].QueryInfo;
                expHint = cmdInfo.Entries[0].ExpirationHint;

                IDictionary itemVersions = null;

                Hashtable addResult = (Hashtable)nCache.Cache.Add(cmdInfo.Keys, cmdInfo.Entries, cmdInfo.Flag, cmdInfo.ProviderName, out itemVersions, operationContext);
                stopWatch.Stop();
                BulkAddResponse addResponse = new BulkAddResponse();

                
                addResponse.keyExceptionPackage = new Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse();
                addResponse.keyVersionPackage = new Alachisoft.NCache.Common.Protobuf.KeyVersionPackageResponse();
                Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysExceptions(addResult, addResponse.keyExceptionPackage);

                if (cmdInfo.returnVersion)
                    Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysVersion(itemVersions, addResponse.keyVersionPackage);

                if (clientManager.ClientVersion >= 5000)
                {
                    addResponse.intendedRecipient = cmdInfo.IntendedRecipient;
                    ResponseHelper.SetResponse(addResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(addResponse, Response.Type.ADD_BULK));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Response response = new Response();
                    response.intendedRecipient = cmdInfo.IntendedRecipient;
                    response.bulkAdd = addResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.ADD_BULK);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

            }
            catch (OperationCanceledException ex)
            {
                exception = ex.ToString();
                Dispose();

            }
            catch (System.Exception exc)
            {
                _addBulkResult = OperationResult.Failure;
                exception = exc.ToString();
               _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }

            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.ADDBULK.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(expHint, queryInfo);
                        log.GenerateADDInsertBulkAPILogItem(cmdInfo.Keys.Length, cmdInfo.Entries.Length, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.Entries[0].Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.Group != null ? cmdInfo.Group : null, cmdInfo.SubGroup != null ? cmdInfo.SubGroup : null, cmdInfo.Entries[0].Flag, cmdInfo.Entries[0].ProviderName, cmdInfo.Entries[0].ResyncProviderName, false, cmdInfo.Entries[0].HasQueryInfo, (long)cmdInfo.Entries[0].Version, cmdInfo.onUpdateCallbackId, cmdInfo.OnDsItemsAddedCallback, false, false, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {

                }
                if (cmdInfo.Entries != null)
                {
                    foreach (var entry in cmdInfo.Entries)
                    {
                        entry.Flag?.MarkFree(NCModulesConstants.SocketServer);
                    }
                }

                if (cmdInfo.Entries != null)
                {
                    cmdInfo.Entries.MarkFree(NCModulesConstants.SocketServer);
                    MiscUtil.ReturnEntriesToPool(cmdInfo.Entries, clientManager.CacheTransactionalPool);
                }
            }

        }

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.StatisticsCounter collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerAddBulkAvg(value);
            }
        }
    }
}
