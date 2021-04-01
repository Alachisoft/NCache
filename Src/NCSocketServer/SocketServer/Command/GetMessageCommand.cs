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
using System.Globalization;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetMessageCommand : CommandBase
    {
        protected string SerializationContext;
        private Common.Protobuf.GetMessageCommand _command;
        private string _clientId;
        IDictionary<string, IList<object>> _messages;
        List<string> _patterns;
        public override bool CanHaveLargedata
        {
            get { return true; }
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload = 1;
            string exceptionMessage = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                if (nCache != null)
                {
                    _command = command.getMessageCommand;
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
                    _clientId = clientManager.ClientID;
                    SubscriptionInfo subInfo = new SubscriptionInfo() { ClientId = clientManager.ClientID  };
                   
                    MessageResponse response = nCache.Cache.GetAssignedMessages(subInfo, operationContext);
                    stopWatch.Stop();
                    //filter event messages for older clients that use pubsub
                    if (clientManager.ClientVersion < 5000)
                    {
                        if (response.AssignedMessages.ContainsKey(TopicConstant.ItemLevelEventsTopic))
                            response.AssignedMessages.Remove(TopicConstant.ItemLevelEventsTopic);
                        if (response.AssignedMessages.ContainsKey(TopicConstant.CollectionEventsTopic))
                            response.AssignedMessages.Remove(TopicConstant.CollectionEventsTopic);
                    }

                    GetMessageResponseBuilder.BuildResponse(response.AssignedMessages, command.commandVersion, _command.requestId.ToString(CultureInfo.InvariantCulture), _serializedResponsePackets, command.commandID,command.requestID, nCache, _clientId, clientManager);

                }
            }
            catch (System.Exception exc)
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetTopicMessages);
                        log.GenerateGetTopicMessagesAPILogItem(executionTime, clientManager.ClientID, clientManager.ClientIP, overload, _messages, exceptionMessage);

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

            commandName = "MessagingService.GetAssignedMessages";
            details.Append("ClientID : " + _clientId);
            details.Append(" ; ");

            if(_messages !=null)
            {
                details.Append("Results : [");

                foreach(var pair in _messages)
                {
                    if (pair.Key != null && pair.Value != null)
                        details.Append(pair.Key).Append(" : " + pair.Value.Count).Append(" , ") ;
                }
                details.Append("]; ");
            }

            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }
    }
}
