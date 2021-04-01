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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkGetCacheItemCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string[] Keys;
            public BitSet FlagMap;
            public string providerName;
            public long ClientLastViewId;
            public int CommandVersion;
            public string IntendedRecipient;
         
        }

        private OperationResult _getBulkCacheItemResult = OperationResult.Success;
        CommandInfo cmdInfo;
        private readonly int READTHRU_BIT = 16;

        internal override OperationResult OperationResult
        {
            get
            {
                return _getBulkCacheItemResult;
            }

        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "BulkGetCacheItem";
            details.Append("Command Keys: " + cmdInfo.Keys.Length);
            details.Append(" ; ");
           
            return details.ToString();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            int count = 0;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd parsed");

            }
            catch (Exception exc)
            {
                _getBulkCacheItemResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            NCache nCache = clientManager.CmdExecuter as NCache;
            List<CacheEntry> pooledEntries = null;
            try
            {

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);

                operationContext.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                operationContext.CancellationToken = CancellationToken;

                IDictionary getResult = (IDictionary)nCache.Cache.GetBulkCacheItems(cmdInfo.Keys, cmdInfo.FlagMap, operationContext);

                count = getResult.Count;
                stopWatch.Stop();

              
                Alachisoft.NCache.Common.Protobuf.BulkGetCacheItemResponse bulkGetCacheItemResponse = new Alachisoft.NCache.Common.Protobuf.BulkGetCacheItemResponse();

                IDictionaryEnumerator enu = getResult.GetEnumerator();
                while (enu.MoveNext())
                {
                    CacheEntry cacheEntry = null;
                    try
                    {
                        var keyCacheItem = new Alachisoft.NCache.Common.Protobuf.KeyCacheItemPair();

                        keyCacheItem.key = (string)enu.Key;

                        cacheEntry = (CacheEntry)enu.Value;
                        if (cacheEntry != null)
                        {
                            if(cacheEntry.IsFromPool)
                            {
                                if (pooledEntries == null) pooledEntries = new List<CacheEntry>();
                                pooledEntries.Add(cacheEntry);
                            }
                            keyCacheItem.cacheItem = PopulateResponse(cacheEntry, clientManager, nCache.Cache);

                            keyCacheItem.cacheItem.itemType = MiscUtil.EntryTypeToProtoItemType(cacheEntry.Type);

                            bulkGetCacheItemResponse.KeyCacheItemPairs.Add(keyCacheItem);
                        }
                        else
                        {
                            bulkGetCacheItemResponse.KeyCacheItemPairs.Add(null);
                        }
                    }
                    finally
                    {
                        if (cacheEntry != null)
                            cacheEntry.MarkFree(NCModulesConstants.Global);
                     

                    }
                }
                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(bulkGetCacheItemResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(bulkGetCacheItemResponse, Common.Protobuf.Response.Type.BULK_GET_CACHEITEM));
                }
                else
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.BULK_GET_CACHEITEM);
                    response.bulkGetCacheItem = bulkGetCacheItemResponse;
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
                _getBulkCacheItemResult = OperationResult.Failure;
                exception = exc.ToString();
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

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetBulk.ToLower());
                        log.GenerateBulkGetAPILogItem(cmdInfo.Keys.Length, cmdInfo.providerName, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString(), count);

                    }

                    if(pooledEntries != null && pooledEntries.Count >0)
                    {
                        MiscUtil.ReturnEntriesToPool(pooledEntries, clientManager.CacheTransactionalPool);
                    }
                }
                catch
                {

                }
                try
                {
                    cmdInfo.FlagMap?.MarkFree(NCModulesConstants.SocketServer);
                }
                catch
                {

                }
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd executed on cache");

        }

        public override void IncrementCounter(Statistics.StatisticsCounter collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerGetBulkAvg(value);
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.BulkGetCacheItemCommand bulkGetCacheItemCommand = command.bulkGetCacheItemCommand;
            cmdInfo.Keys = bulkGetCacheItemCommand.keys.ToArray();
            cmdInfo.providerName = bulkGetCacheItemCommand.providerName;
            cmdInfo.RequestId = bulkGetCacheItemCommand.requestId.ToString();
            BitSet bitset = BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);
            bitset.Data =((byte)bulkGetCacheItemCommand.flag);
            cmdInfo.FlagMap = bitset;
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;

           

            return cmdInfo;
        }

        private Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse PopulateResponse(CacheEntry entry, ClientManager clientManager, Caching.Cache cache)
        {
            DateTime lockDate = new DateTime();
            Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse response = new Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse();
            response.lockId = string.Empty;
            response.lockTicks = lockDate.Ticks;
            if (entry != null)
            {
                if (entry.ExpirationHint is AggregateExpirationHint)
                {
                    IList<ExpirationHint> hints = ((AggregateExpirationHint)entry.ExpirationHint).Hints;
                    ///All hints are on same level now. There will be no AggregateExpirationHint within an AggregateExpirationHint
                    for (int i = 0; i < hints.Count; i++)
                    {
                        if (hints[i] is FixedExpiration)
                            response.absExp = ((FixedExpiration)hints[i]).AbsoluteTime.Ticks;
                        else if (hints[i] is IdleExpiration)
                            response.sldExp = ((IdleExpiration)hints[i]).SlidingTime.Ticks;
                    }
                }
                else
                {
                    if (entry.ExpirationHint is FixedExpiration)
                        response.absExp = ((FixedExpiration)entry.ExpirationHint).AbsoluteTime.Ticks;
                    else if (entry.ExpirationHint is IdleExpiration)
                        response.sldExp = ((IdleExpiration)entry.ExpirationHint).SlidingTime.Ticks;
                }
                response.itemType = MiscUtil.EntryTypeToProtoItemType(entry.Type);// (Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType)entry.Type;
                                                                                 

                response.priority = (int)entry.Priority;


                if (entry.QueryInfo != null)
                {
                }

                if (entry.ExpirationHint != null)
                {
                    response.hasExpired = entry.ExpirationHint.HasExpired;
                    response.needsResync = entry.ExpirationHint.NeedsReSync;
                }

                response.version = entry.Version;

                response.creationTime = entry.CreationTime.Ticks;
                response.lastModifiedTime = entry.LastModifiedTime.Ticks;
                //response.lockId = (entry.LockId != null ? entry.LockId.ToString() : null);
                //response.lockTicks = entry.LockDate.Ticks;            

                object userValue = entry.Value;

                BitSet flag = entry.Flag;
                response.value.AddRange(((UserBinaryObject)cache.SocketServerDataService.GetClientData(userValue, ref flag, LanguageContext.DOTNET)).DataList);
                response.flag = flag.Data;
            }
            return response;
        }


    }
}
