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
using System.Collections;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddDependencyCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public ExpirationHint ExpHint;
            public bool _isResync;
        }

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
                cmdInfo = ParseCommand(command, clientManager);
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
            try
            {
                //PROTOBUF:RESPONSE
                bool result = nCache.Cache.AddExpirationHint(cmdInfo.Key, cmdInfo.ExpHint, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                //PROTOBUF:RESPONSE
                stopWatch.Stop();
               
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.AddDependencyResponse addDependencyResponse = new Alachisoft.NCache.Common.Protobuf.AddDependencyResponse();
                addDependencyResponse.success = result;
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.ADD_DEPENDENCY;
                response.addDep = addDependencyResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                //PROTOBUF:RESPONSE
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.AddDEPENDENCY.ToLower());
                        Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpHint, null);
                        log.GenerateAddDependencyAPILogItem(cmdInfo.Key,cmdInfo._isResync, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, null, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());
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

            Alachisoft.NCache.Common.Protobuf.AddDependencyCommand addDependencyCommand = command.addDependencyCommand;
            cmdInfo.ExpHint = ProtobufHelper.GetExpirationHintObj(addDependencyCommand.dependency, addDependencyCommand.isResync,serializationContext);
            cmdInfo.Key = addDependencyCommand.key;
            cmdInfo._isResync = addDependencyCommand.isResync;
            cmdInfo.RequestId = addDependencyCommand.requestId.ToString();
            
            return cmdInfo;
        }
    }
}
