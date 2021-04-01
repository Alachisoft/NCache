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
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class SubscribeTopicCommand : CommandBase
    {
        private Common.Protobuf.SubscribeTopicCommand _command;

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload = 1;
            bool subscribed = false;
            string exceptionMessage = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                if (command.commandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else //NCache 4.1 SP1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                }
                CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                if (nCache != null)
                {
                    _command = command.subscribeTopicCommand;
                    
                    SubscriptionInfo subscriptionInfo = new SubscriptionInfo() { SubscriptionId = _command.subscriptionName, ClientId = clientManager.ClientID, Type = (SubscriptionType)_command.pubSubType,SubPolicyType=(SubscriptionPolicyType)_command.subscriptionPolicy,CreationTime=_command.creationTime,Expiration=_command.expirationTime};
                    SubscriptionOperation subOperation = new SubscriptionOperation(_command.topicName, TopicOperationType.Subscribe, subscriptionInfo);
                    subscribed = nCache.Cache.TopicOpertion(subOperation, operationContext);
                    stopWatch.Stop();
                    //Common.Protobuf.Response response = new Common.Protobuf.Response();
                    Common.Protobuf.SubscribeTopicResponse subscribeTopicResponse = new Common.Protobuf.SubscribeTopicResponse();
                    subscribeTopicResponse.success = subscribed;
                    if (clientManager.ClientVersion >= 5000)
                    {
                        Common.Util.ResponseHelper.SetResponse(subscribeTopicResponse, command.requestID, command.commandID);
                        _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(subscribeTopicResponse, Common.Protobuf.Response.Type.SUBSCRIBE_TOPIC));
                    }
                    else
                    {
                        //PROTOBUF:RESPONSE
                        Common.Protobuf.Response response = new Common.Protobuf.Response();
                        response.subscribeTopicResponse = subscribeTopicResponse;
                        Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.SUBSCRIBE_TOPIC);
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.CreateTopicSubscriptoin);
                        log.GenerateSubscribeToTopicAPILogItem(_command.topicName, (SubscriptionType)_command.pubSubType, executionTime, clientManager.ClientID, clientManager.ClientIP, overload, subscribed, exceptionMessage, APIClassNames.TOPIC);
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

            commandName = "MessagingService.Subscribe";
            details.Append("Topic : " + _command.topicName);
            details.Append(" ; ");
            string subscriptionType = ((SubscriptionType)_command.pubSubType) == SubscriptionType.Publisher ? "DeliveryFailureNotification" : "MessageReceiveNotification";
            details.Append("SubscriptionType : " + subscriptionType);
            details.Append(" ; ");
            details.Append("SubscriptionId : " + _command.subscriptionName);
            details.Append(" ; ");
            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }
    }
}