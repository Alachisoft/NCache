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
using System.Globalization;

using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using System.Text;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetTopicCommand : CommandBase
    {
        private Common.Protobuf.GetTopicCommand _command;
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload =1;
            bool isTopicCreated = false;
            string exceptionMessage = null;
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                if (command.commandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else //NCache 4.1 SP1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                }
                if (nCache != null)
                {
                    Common.Protobuf.GetTopicCommand getTopicCommand = command.getTopicCommand;
                    _command = getTopicCommand;

                    

                    TopicOperation topicOperation = new TopicOperation(getTopicCommand.topicName, (TopicOperationType)getTopicCommand.type);
                    isTopicCreated = nCache.Cache.TopicOpertion(topicOperation, operationContext);
                    stopWatch.Stop();
                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    Common.Protobuf.GetTopicResponse getTopicResponse = new Common.Protobuf.GetTopicResponse();
                    getTopicResponse.success = isTopicCreated;

                    if (clientManager.ClientVersion >= 5000)
                    {
                        
                        Common.Util.ResponseHelper.SetResponse(getTopicResponse, command.requestID, command.commandID);
                        _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(getTopicResponse, Common.Protobuf.Response.Type.GET_TOPIC));
                    }
                    else
                    {
                        //PROTOBUF:RESPONSE
                        response.getTopicResponse = getTopicResponse;
                        Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.GET_TOPIC);
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }

                }
            }
            catch (Exception exc)
            {
                exceptionMessage = exc.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        string methodName = null;

                        if ((TopicOperationType)_command.type == TopicOperationType.Get)
                            methodName = MethodsName.GetTopic;
                        else 
                            methodName = MethodsName.CreateTopic;

                        APILogItemBuilder log = new APILogItemBuilder(methodName);
                        log.GenerateGetCreateOrDeleteTopicAPILogItem(_command.topicName, executionTime,clientManager.ClientID,clientManager.ClientIP,overload, isTopicCreated, exceptionMessage);

                        // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                    }
                }
                catch
                {

                }
            }
        }

        

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
             
            commandName = (TopicOperationType)_command.type == TopicOperationType.Get?  "MessagingService.GetTopic" : "MessagingService.Create";
            details.Append("topic : " + _command.topicName);
            details.Append(" ; ");
            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }
    }
}