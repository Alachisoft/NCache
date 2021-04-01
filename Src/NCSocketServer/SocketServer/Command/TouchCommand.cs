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
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class TouchCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public List<string> Keys;
        }
        CommandInfo cmdInfo;
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            string exception = null;
            int overload;
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
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                nCache.Cache.Touch(cmdInfo.Keys, operationContext);
                stopWatch.Stop();

              
                Common.Protobuf.TouchResponse touchResponse = new Common.Protobuf.TouchResponse();

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(touchResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(touchResponse, Common.Protobuf.Response.Type.TOUCH));
                }
                else
                {
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.touchResponse = touchResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.TOUCH);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Touch.ToLower());
                        log.GenerateTouchCommandAPILogItem(cmdInfo.Keys.Count, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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

            Alachisoft.NCache.Common.Protobuf.TouchCommand touchCommand = command.touchCommand;
            cmdInfo.Keys = touchCommand.keys;
            cmdInfo.RequestId = touchCommand.requestId.ToString();
            return cmdInfo;
        }
      
    }
}
