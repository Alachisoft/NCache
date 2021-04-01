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
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using System.Collections;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using System.Diagnostics;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetCacheItemCommand : CommandBase
    {

        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public string Group;
            public string SubGroup;
            public BitSet FlagMap;
            public LockAccessType LockAccessType;
            public object LockId;
            public TimeSpan LockTimeout;
            public ulong CacheItemVersion;
            public string ProviderName;
        }


        CommandInfo cmdInfo;

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        //PROTOBUF
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "GetCacheItem";
            details.Append("Command Keys: " + cmdInfo.Key);
            details.Append(" ; ");
            //details.Append("ReadThru: " + (cmdInfo.ReadMode != ReadMode.None));
            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
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
                if (!base.immatureId.Equals("-2"))
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }

            NCache nCache = clientManager.CmdExecuter as NCache;
            CacheEntry entry = null;
            try
            {
                object lockId = cmdInfo.LockId;
                DateTime lockDate = new DateTime();
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
               // ReadThruOptions readOptions = new ReadThruOptions(cmdInfo.ReadMode, cmdInfo.ProviderName);
                //operationContext.Add(OperationContextFieldName.ReadThruOptions, readOptions);
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                entry = (CacheEntry)nCache.Cache.GetCacheEntry(cmdInfo.Key, cmdInfo.Group, cmdInfo.SubGroup, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType, operationContext, ref cmdInfo.CacheItemVersion);
                stopWatch.Stop();


                GetCacheItemResponse getCacheItemResponse = new GetCacheItemResponse();
                getCacheItemResponse.lockId = lockId == null ? string.Empty : lockId.ToString();
                getCacheItemResponse.lockTicks = lockDate.Ticks;
                if (clientManager.ClientVersion >= 5000)
                {

                    ResponseHelper.SetResponse(getCacheItemResponse, command.requestID, command.commandID);
                    if (entry == null)
                    {
                        _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(getCacheItemResponse, Response.Type.GET_CACHE_ITEM));
                        return;
                    }
                    else
                    {
                        getCacheItemResponse.itemType = MiscUtil.EntryTypeToProtoItemType(entry.Type);
                    }
                    getCacheItemResponse = PopulateResponse(entry, getCacheItemResponse, clientManager, nCache.Cache);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(getCacheItemResponse, Response.Type.GET_CACHE_ITEM));
                }

                else
                {
                    Response response = new Response();
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.GET_CACHE_ITEM);
                    if (entry == null)
                    {
                        response.getItem = getCacheItemResponse;
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                        return;
                    }
                    else
                    {
                        getCacheItemResponse.itemType = MiscUtil.EntryTypeToProtoItemType(entry.Type);
                    }
                    getCacheItemResponse = PopulateResponse(entry, getCacheItemResponse, clientManager, nCache.Cache);
                    response.getItem = getCacheItemResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                
            }
            catch (System.Exception exc)
            {
                exception = exc.ToString();
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {

                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        int result = 0;
                        if (entry != null)
                            result = 1;
                        else
                            result = 0;

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetCacheItem.ToLower());

                    }
                    if (entry != null)
                    {
                        MiscUtil.ReturnEntryToPool(entry, clientManager.CacheTransactionalPool);
                    }
                }
                catch
                {

                }
                if (entry != null)
                {
                    entry.MarkFree(NCModulesConstants.Global);
                }
                cmdInfo.FlagMap?.MarkFree(NCModulesConstants.SocketServer);
            }
        }



        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand getCacheItemCommand = command.getCacheItemCommand;

            cmdInfo.CacheItemVersion = getCacheItemCommand.version;
            BitSet bitset = BitSet.CreateAndMarkInUse(clientManager.CacheTransactionalPool, NCModulesConstants.SocketServer);
            bitset.Data=((byte)getCacheItemCommand.flag);
            cmdInfo.FlagMap = bitset;
            cmdInfo.Group = null;
            cmdInfo.Key = clientManager.CacheTransactionalPool.StringPool.GetString(getCacheItemCommand.key);
            cmdInfo.LockAccessType = (LockAccessType)getCacheItemCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCacheItemCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCacheItemCommand.lockInfo.lockTimeout);
            cmdInfo.ProviderName = getCacheItemCommand.providerName.Length == 0 ? null : getCacheItemCommand.providerName;
            cmdInfo.RequestId = getCacheItemCommand.requestId.ToString();
            MiscUtil.ReturnBitsetToPool(cmdInfo.FlagMap, clientManager.CacheTransactionalPool);

            return cmdInfo;
        }

       
        private Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse PopulateResponse(CacheEntry entry, Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse response, ClientManager clientManager, Caching.Cache cache)
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
            
            response.ResyncProviderName = entry.ResyncProviderName;
            object userValue = entry.Value;

            BitSet flag = entry.Flag;

            object data = cache.SocketServerDataService.GetClientData(userValue, ref flag, LanguageContext.DOTNET);
            if (data != null)
                response.value.AddRange(((UserBinaryObject)(data)).DataList);

            response.flag = flag.Data;

        
            
            return response;
        }

    }
}
