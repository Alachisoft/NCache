// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using System.Collections;
using Alachisoft.NCache.Common;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetCacheItemCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public BitSet FlagMap;
            public LockAccessType LockAccessType;
            public object LockId;
            public TimeSpan LockTimeout;
        }


        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        //PROTOBUF

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                return;
            }

            NCache nCache = clientManager.CmdExecuter as NCache;
            
            try
            {
                object lockId = cmdInfo.LockId;
                DateTime lockDate = new DateTime();
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);             
                CacheEntry entry = (CacheEntry)nCache.Cache.GetCacheEntry(cmdInfo.Key, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType, operationContext);

                Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse getCacheItemResponse = new Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_CACHE_ITEM;

                if (entry == null)
                {
                    getCacheItemResponse.lockId = lockId == null ? string.Empty : lockId.ToString();
                    getCacheItemResponse.lockTicks = lockDate.Ticks;
                    response.getItem = getCacheItemResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    return;
                }

                getCacheItemResponse = PopulateResponse(entry, getCacheItemResponse, clientManager);
                response.getItem = getCacheItemResponse;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }    

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCacheItemCommand getCacheItemCommand = command.getCacheItemCommand;

            cmdInfo.FlagMap = new BitSet((byte)getCacheItemCommand.flag);

            cmdInfo.Key = getCacheItemCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)getCacheItemCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCacheItemCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCacheItemCommand.lockInfo.lockTimeout);
            cmdInfo.RequestId = getCacheItemCommand.requestId.ToString();


            return cmdInfo;
        }
       
        private Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse PopulateResponse(CacheEntry entry, Alachisoft.NCache.Common.Protobuf.GetCacheItemResponse response, ClientManager clientManager)
        {
            if (entry.ExpirationHint is FixedExpiration) response.absExp = ((FixedExpiration)entry.ExpirationHint).AbsoluteTime.Ticks;
            else if (entry.ExpirationHint is IdleExpiration) response.sldExp = ((IdleExpiration)entry.ExpirationHint).SlidingTime.Ticks;
            
            response.priority = (int)entry.Priority;
            

            if (entry.ExpirationHint != null)
            {
                response.hasExpired = entry.ExpirationHint.HasExpired;
                response.needsResync = entry.ExpirationHint.NeedsReSync;
            }
            response.flag = entry.Flag.Data;
            response.creationTime = entry.CreationTime.Ticks;
            response.lastModifiedTime = entry.LastModifiedTime.Ticks;
            response.lockId = (entry.LockId != null ? entry.LockId.ToString() : null);
            response.lockTicks = entry.LockDate.Ticks;  

            object userValue = entry.Value;
            if (userValue is CallbackEntry)
            {
                userValue = ((CallbackEntry)userValue).Value;
            }
            response.value.AddRange(((UserBinaryObject)userValue).DataList);

            return response;
        }
    }
}
