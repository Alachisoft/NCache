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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Collections;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.Util;

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
            catch (System.Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            data = new byte[1];
            try
            {
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                //PROTOBUF:RESPONSE
                bool result = nCache.Cache.AddExpirationHint(cmdInfo.Key, cmdInfo.ExpHint, operationContext);
                //PROTOBUF:RESPONSE
                stopWatch.Stop();

                AddAttributeResponse addResponse = new AddAttributeResponse();
                addResponse.success = result;
                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(addResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(addResponse, Response.Type.ADD_ATTRIBUTE));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Response response = new Response();
                    response.addAttributeResponse = addResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.ADD_ATTRIBUTE);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (System.Exception exc)
            {
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                //PROTOBUF:RESPONSEexception = exc.ToString();
                exception = exc.ToString();
                //_resultPacket = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID);
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
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
                catch { }
            }

        }

        private CommandInfo ParseCommand(Caching.Cache cache,Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.AddAttributeCommand addAttributeCommand = command.addAttributeCommand;
            cmdInfo.ExpHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(cache.Context.FakeObjectPool, null, addAttributeCommand.absExpiration, 0, false, serializationContext);
            cmdInfo.Key = addAttributeCommand.key;
            cmdInfo.RequestId = addAttributeCommand.requestId.ToString();

            return cmdInfo;
        }

    }
}
