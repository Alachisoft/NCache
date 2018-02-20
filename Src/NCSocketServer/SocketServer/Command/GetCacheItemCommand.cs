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
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Collections;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using System.Diagnostics;

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
            if (cmdInfo.FlagMap != null)
                details.Append("ReadThru: " + cmdInfo.FlagMap.IsBitSet(BitSetConstants.ReadThru));
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
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            NCache nCache = clientManager.CmdExecuter as NCache;
            CacheEntry entry = null;
            try
            {
                object lockId = cmdInfo.LockId;
                DateTime lockDate = new DateTime();
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ReadThru, cmdInfo.FlagMap.IsBitSet(BitSetConstants.ReadThru));
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
                if (cmdInfo.ProviderName != null)
                {
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, cmdInfo.ProviderName);
                }
                
                entry = (CacheEntry)nCache.Cache.GetCacheEntry(cmdInfo.Key, cmdInfo.Group, cmdInfo.SubGroup, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType, operationContext,ref cmdInfo.CacheItemVersion);
                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse getCacheItemResponse = new Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_CACHE_ITEM;


                getCacheItemResponse.lockId = lockId == null ? string.Empty : lockId.ToString();
                getCacheItemResponse.lockTicks = lockDate.Ticks;

                if (entry == null)
                {
                    response.getItem = getCacheItemResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    return;
                }

                getCacheItemResponse = PopulateResponse(entry, getCacheItemResponse, clientManager, nCache.Cache);
                response.getItem = getCacheItemResponse;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
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
                        int result = 0;
                        if (entry != null)
                            result = 1;
                        else
                            result = 0;

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetCacheItem.ToLower());
                        log.GenerateGetCacheItemAPILogItem(cmdInfo.Key, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.FlagMap, (long)cmdInfo.CacheItemVersion, cmdInfo.LockAccessType, cmdInfo.LockTimeout, cmdInfo.LockId.ToString(), cmdInfo.ProviderName, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), result);
                    }
                }
                catch
                {
                }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand getCacheItemCommand = command.getCacheItemCommand;

            cmdInfo.CacheItemVersion = getCacheItemCommand.version;
            cmdInfo.FlagMap = new BitSet((byte)getCacheItemCommand.flag);

            cmdInfo.Group = getCacheItemCommand.group.Length == 0 ? null : getCacheItemCommand.group;
            cmdInfo.Key = getCacheItemCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)getCacheItemCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCacheItemCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCacheItemCommand.lockInfo.lockTimeout);
            cmdInfo.ProviderName = getCacheItemCommand.providerName.Length == 0 ? null : getCacheItemCommand.providerName;
            cmdInfo.RequestId = getCacheItemCommand.requestId.ToString();
            cmdInfo.SubGroup = getCacheItemCommand.subGroup.Length == 0 ? null : getCacheItemCommand.subGroup;

            return cmdInfo;
        }
       
        private Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse PopulateResponse(CacheEntry entry, Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse response, ClientManager clientManager, Caching.Cache cache)
        {
            if (entry.ExpirationHint is AggregateExpirationHint)
            {
                IList<ExpirationHint> hints = ((AggregateExpirationHint)entry.ExpirationHint).Hints;
                // All hints are on same level now. There will be no AggregateExpirationHint within an AggregateExpirationHint
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
            // Fixed and Idle expiration hints are not included in making of protobuf dependency object
            response.dependency = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetDependencyObj(entry.ExpirationHint);

            response.priority = (int)entry.Priority;
            
            if (entry.QueryInfo != null)
            {
                if (entry.QueryInfo["tag-info"] != null)
                {
                    response.tagInfo = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetTagInfoObj(entry.QueryInfo["tag-info"] as Hashtable);
                }
                if (entry.QueryInfo["named-tag-info"] != null)
                {
                    response.namedTagInfo = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetNamedTagInfoObj(entry.QueryInfo["named-tag-info"] as Hashtable, clientManager.IsDotNetClient);
                }
            }

            if (entry.ExpirationHint != null)
            {
                response.hasExpired = entry.ExpirationHint.HasExpired;
                response.needsResync = entry.ExpirationHint.NeedsReSync;
            }
           
            response.version = entry.Version;
          
            response.creationTime = entry.CreationTime.Ticks;
            response.lastModifiedTime = entry.LastModifiedTime.Ticks;

            if (entry.GroupInfo != null)
            {
                response.group = entry.GroupInfo.Group;
                response.subGroup = entry.GroupInfo.SubGroup;
            }

            object userValue = entry.Value;
            if (userValue is CallbackEntry)
            {
                userValue = ((CallbackEntry)userValue).Value;
            }

            BitSet flag = entry.Flag;
            response.value.AddRange(((UserBinaryObject)cache.SocketServerDataService.GetClientData(userValue, ref flag, LanguageContext.DOTNET)).DataList);
            response.flag = flag.Data;
            return response;
        }
    }
}
