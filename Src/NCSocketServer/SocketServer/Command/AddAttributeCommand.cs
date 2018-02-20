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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Collections;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddAttributeCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public ExpirationHint ExpHint;
        }

        private byte[] _resultPacket = null;
        protected string serializationContext;

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            byte[] data = null;

            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                serializationContext = nCache.CacheId;
                cmdInfo = ParseCommand(nCache.Cache, command);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            data = new byte[1];
            try
            {
                bool result = nCache.Cache.AddExpirationHint(cmdInfo.Key, cmdInfo.ExpHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                stopWatch.Stop();
               
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.AddAttributeResponse addAttributeResponse = new Alachisoft.NCache.Common.Protobuf.AddAttributeResponse();
                addAttributeResponse.success = result;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_ATTRIBUTE;
                response.addAttributeResponse = addAttributeResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                stopWatch.Stop();
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.SetAttributes.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpHint, null);
                        log.GenerateAddAttributeAPILogItem(cmdInfo.Key, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        private CommandInfo ParseCommand(Caching.Cache cache,Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.AddAttributeCommand addAttributeCommand = command.addAttributeCommand;
            cmdInfo.ExpHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(cache.Configuration.ExpirationPolicy, addAttributeCommand.dependency, addAttributeCommand.absExpiration, 0, false, serializationContext);
            cmdInfo.Key = addAttributeCommand.key;
            cmdInfo.RequestId = addAttributeCommand.requestId.ToString();

            return cmdInfo;
        }
    }
}
