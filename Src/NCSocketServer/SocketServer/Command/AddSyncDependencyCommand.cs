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
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddSyncDependencyCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public Alachisoft.NCache.Caching.CacheSynchronization.CacheSyncDependency SyncDependency;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            byte[] data = null;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            try
            {
                stopWatch.Start();
                cmdInfo = ParseCommand(command, clientManager);
                overload = command.MethodOverload;
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            data = new byte[1];
            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                bool result = nCache.Cache.AddSyncDependency(cmdInfo.Key, cmdInfo.SyncDependency, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));

                stopWatch.Stop();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.AddSyncDependencyResponse addSyncDependencyResponse = new Alachisoft.NCache.Common.Protobuf.AddSyncDependencyResponse();
                addSyncDependencyResponse.success = result;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_SYNC_DEPENDENCY;
                response.addSyncDependencyResponse = addSyncDependencyResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.AddDEPENDENCY.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(null, null);

                        log.GenerateCacheSyncDependencyAPILogItem(cmdInfo.SyncDependency.CacheId, cmdInfo.Key,   expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, false, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.AddSyncDependencyCommand addSyncDependencyCommand = command.addSyncDependencyCommand;
            cmdInfo.Key = addSyncDependencyCommand.key;
            cmdInfo.RequestId = addSyncDependencyCommand.requestId.ToString();
            cmdInfo.SyncDependency = base.GetCacheSyncDependencyObj(addSyncDependencyCommand.syncDependency);

            return cmdInfo;
        }
    }
}