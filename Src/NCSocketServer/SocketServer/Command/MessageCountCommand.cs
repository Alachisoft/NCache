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
using System.Diagnostics;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class MessageCountCommand : CommandBase
    {
        private bool _operationSuccessStatus;

        internal MessageCountCommand()
        {
            _operationSuccessStatus = false;
        }

        internal override OperationResult OperationResult
        {
            get
            {
                return _operationSuccessStatus ? OperationResult.Success : OperationResult.Failure;
            }
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            // int overload;
            string exception = null;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            long messageCount = 0;


            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;

                if (nCache != null)
                {
                    OperationContext operationContext = null;
                    CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                    messageCount = nCache.Cache.GetMessageCount(command.messageCountCommand.topicName, operationContext);
                    _operationSuccessStatus = true;
                }
                stopwatch.Stop();

                Common.Protobuf.MessageCountResponse messageCountResponse = new Common.Protobuf.MessageCountResponse();
                messageCountResponse.messageCount = messageCount;


                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(messageCountResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(messageCountResponse, Common.Protobuf.Response.Type.MESSAGE_COUNT));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    response.messageCountResponse = messageCountResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.MESSAGE_COUNT);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception e)
            {
                exception = e.ToString();
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeExceptionResponseWithType(e, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopwatch.Elapsed;
                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.MessageCount.ToLower());
                        log.GenerateCacheCountAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), messageCount);
                    }
                }
                catch { }
            }
        }
    }
}
