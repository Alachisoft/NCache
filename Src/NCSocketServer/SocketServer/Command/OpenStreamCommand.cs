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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;
using System.Collections;


namespace Alachisoft.NCache.SocketServer.Command
{
    class OpenStreamCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            Alachisoft.NCache.Common.Protobuf.OpenStreamCommand openStreamCommand = command.openStreamCommand;
            NCache nCache = clientManager.CmdExecuter as NCache;
            Caching.Cache cache = nCache.Cache;

            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            overload = command.MethodOverload;

            string lockHandle = null;
            ExpirationHint expHint = null;
            try
            {
                if (clientManager != null)
                {
                    expHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(cache.Configuration.ExpirationPolicy, openStreamCommand.dependency, openStreamCommand.absoluteExpiration, openStreamCommand.slidingExpiration, false, ((NCache)clientManager.CmdExecuter).CacheId);
                    int pr = (int)openStreamCommand.priority;
                    EvictionHint evictionHint = new PriorityEvictionHint((CacheItemPriority) pr);
					if (openStreamCommand.group != null) openStreamCommand.group = openStreamCommand.group.Length == 0 ? null : openStreamCommand.group;
					if (openStreamCommand.subGroup != null) openStreamCommand.subGroup = openStreamCommand.subGroup.Length == 0 ? null : openStreamCommand.subGroup;
                    lockHandle = ((NCache)clientManager.CmdExecuter).Cache.OpenStream(openStreamCommand.key, (Alachisoft.NCache.Common.Enum.StreamModes)openStreamCommand.streamMode, openStreamCommand.group, openStreamCommand.subGroup, expHint, evictionHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                    stopWatch.Stop();
                }
            }
            catch (Exception e)
            {
                //PROTOBUF:RESPONSE
                exception = e.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(e, command.requestID,command.commandID));
                return;
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetCacheStream.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(expHint,null);
                        log.GenerateOpenStreamAPILogItem(openStreamCommand.key, openStreamCommand.streamMode, openStreamCommand.group, openStreamCommand.subGroup, openStreamCommand.priority, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.OpenStreamResponse openStreamResponse = new Alachisoft.NCache.Common.Protobuf.OpenStreamResponse();
            response.requestId = openStreamCommand.requestId;
            response.commandID = command.commandID;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.OPEN_STREAM;
            response.openStreamResponse = openStreamResponse;
			openStreamResponse.lockHandle = lockHandle;

            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
        }
    }
}
