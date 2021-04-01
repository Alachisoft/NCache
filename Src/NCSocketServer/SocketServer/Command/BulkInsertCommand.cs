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
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkInsertCommand : BulkAddAndInsertCommandBase
    {
        CommandInfo cmdInfo;
        private OperationResult _insertBulkResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _insertBulkResult;
            }
        }
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "InsertBulk";
            details.Append("Command Keys: " + cmdInfo.Keys.Length);
            details.Append(" ; ");
            details.Append("Command Values: " + cmdInfo.Entries.Length);
            details.Append(" ; ");
            if (cmdInfo.Flag != null)
                details.Append("WriteThru: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            ClientId = clientManager.ClientID; 
            Hashtable queryInfo = null;
            ExpirationHint expHint = null;
            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            bool itemUpdated = false;
            bool itemRemove = false;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                serailizationContext = nCache.CacheId; 
                overload = command.MethodOverload;
                cmdInfo = base.ParseCommand(command, clientManager, serailizationContext);
            }
            catch (Exception exc)
            {
                _insertBulkResult = OperationResult.Failure;
                exception = exc.ToString();
                //if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                 //   _resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                }
                return;
            }

            //TODO
            byte[] dataPackage = null;

            try
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);

                operationContext.Add(OperationContextFieldName.WriteThru, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
                operationContext.Add(OperationContextFieldName.WriteBehind, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind));
                if (cmdInfo.ProviderName != null)
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.ProviderName);
                
                operationContext.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                operationContext.CancellationToken = CancellationToken;

                queryInfo = cmdInfo.Entries[0].QueryInfo;
                expHint = cmdInfo.Entries[0].ExpirationHint;

                IDictionary itemVersions = null;

                Hashtable insertResult = (Hashtable)nCache.Cache.Insert(cmdInfo.Keys, cmdInfo.Entries, cmdInfo.Flag, cmdInfo.ProviderName, null, out itemVersions, operationContext);
                stopWatch.Stop();

                Alachisoft.NCache.Common.Protobuf.BulkInsertResponse bulkInsertResponse = new Alachisoft.NCache.Common.Protobuf.BulkInsertResponse();
                

               
                bulkInsertResponse.intendedRecipient = cmdInfo.IntendedRecipient;
                bulkInsertResponse.keyExceptionPackage = new Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse();
                bulkInsertResponse.keyVersionPackage = new Alachisoft.NCache.Common.Protobuf.KeyVersionPackageResponse();

                //TODO : Package Key Value
                Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysExceptions(insertResult, bulkInsertResponse.keyExceptionPackage);

                if (cmdInfo.returnVersion)
                    Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysVersion(itemVersions, bulkInsertResponse.keyVersionPackage);



                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(bulkInsertResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(bulkInsertResponse, Common.Protobuf.Response.Type.INSERT_BULK));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.bulkInsert = bulkInsertResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.INSERT_BULK);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (OperationCanceledException ex)
            {
                exception = ex.ToString();
                Dispose();

            }
            catch (Exception exc)
            {
                _insertBulkResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.INSERTBULK.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(expHint, queryInfo);
                        log.GenerateADDInsertBulkAPILogItem(cmdInfo.Keys.Length, cmdInfo.Entries.Length, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.Entries[0].Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.Group != null ? cmdInfo.Group : null, cmdInfo.SubGroup != null ? cmdInfo.SubGroup : null, cmdInfo.Entries[0].Flag, cmdInfo.ProviderName, cmdInfo.Entries[0].ResyncProviderName, false, cmdInfo.Entries[0].HasQueryInfo, (long)cmdInfo.Entries[0].Version, cmdInfo.onUpdateCallbackId, cmdInfo.OnDsItemsAddedCallback, false, false, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {

                }
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

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.StatisticsCounter collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerUpdBulkAvg(value);
            }
        }
        
    }
}
