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
using System.Globalization;
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Protobuf;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class MessageAcknowledgementCommand : CommandBase
    {
        private MesasgeAcknowledgmentCommand _command;
        private string _clientId;
        Dictionary<string, IList<string>> _topicWiseMessageIds;

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            NCache nCache = clientManager.CmdExecuter as NCache;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload = 1;
            string exceptionMessage = null;

            try
            {
                if (nCache != null)
                {
                    var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    if (command.commandVersion < 1)
                    {
                        operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                    }
                    else 
                    {
                        operationContext.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                    }

                    operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                    CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                    _clientId = clientManager.ClientID;
                    _command = command.mesasgeAcknowledgmentCommand;

                    _topicWiseMessageIds = new Dictionary<string, IList<string>>();
                    foreach (KeyValue values in _command.values)
                    {
                        string key = values.key;
                        var messageIdsList = new List<string>();
                        foreach (ValueWithType valueWithType in values.value)
                        {
                            if (valueWithType.value != null)
                            {
                                messageIdsList.Add(valueWithType.value);
                            }
                        }
                        _topicWiseMessageIds.Add(key, messageIdsList);
                    }
                    
                    nCache.Cache.AcknowledgeMessageReceipt(clientManager.ClientID, _topicWiseMessageIds, operationContext);
                    stopWatch.Stop();


                    MessageAcknowledgmentResponse messageAckResponse = new MessageAcknowledgmentResponse();
                    if (clientManager.ClientVersion >= 5000)
                    {
                        ResponseHelper.SetResponse(messageAckResponse, command.requestID, command.commandID);
                        _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(messageAckResponse, Common.Protobuf.Response.Type.MESSAGE_ACKNOWLEDGEMENT));
                    }
                    else
                    {
                        Common.Protobuf.Response response = new Common.Protobuf.Response();
                        response.messageAcknowledgmentResponse = messageAckResponse;
                        ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.MESSAGE_ACKNOWLEDGEMENT);
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }

                }
            }
            catch (System.Exception exc)
            {
                exceptionMessage = exc.ToString();
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.AcknowledgeTopicMessages);
                        log.GenerateAcknowledgeTopicMessagesAPILogItem(executionTime, clientManager.ClientID, clientManager.ClientIP, overload, _topicWiseMessageIds, exceptionMessage);

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

            commandName = "MessagingService.AcknowledgeMessages";
            details.Append("ClientID : " + _clientId);
            details.Append(" ; ");

            if (_topicWiseMessageIds != null)
            {
                details.Append("TopicWiseMessageDetails : [");

                foreach (var pair in _topicWiseMessageIds)
                {
                    if (pair.Key != null && pair.Value != null)
                        details.Append(pair.Key).Append(" : " + pair.Value.Count).Append(" , ");
                }
                details.Append("]; ");
            }

            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }
    }
}
