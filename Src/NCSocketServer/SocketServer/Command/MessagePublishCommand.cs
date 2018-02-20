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
using System.Globalization;
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Protobuf;
using System.Text;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class MessagePublishCommand : CommandBase
    {
        private Common.Protobuf.MessagePublishCommand _command;
        private object _value = null;
        private  Hashtable _metaData = new Hashtable();

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload = 1;
             string exceptionMessage = null;

            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                if (nCache != null)
                {
                    _command = command.messagePublishCommand;
                    var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    if (command.commandVersion < 1)
                    {
                        operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                    }
                    else 
                    {
                        operationContext.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                    }

                    var flag = new BitSet((byte)_command.flag);
                 
                    foreach(KeyValuePair pair in _command.keyValuePair)
                    {
                        _metaData.Add(pair.key, pair.value);
                    }

                    ICollection dataList = _command.data as ICollection;
                    if (dataList != null)
                    {
                        _value = UserBinaryObject.CreateUserBinaryObject(dataList);
                    }
                    else
                    {
                        _value = _command.data;
                    }

                    nCache.Cache.PublishMessage(_command.messageId,
                       _value,
                       _command.creationTime,
                       _command.expiration,
                       _metaData,
                       flag,
                       operationContext
                       );

                    Response response = new Response();
                    MessagePublishResponse messagePublishResponse = new MessagePublishResponse();
                    response.requestId = Convert.ToInt64(_command.requestId);
                    response.commandID = command.commandID;
                    response.responseType = Response.Type.MESSAGE_PUBLISH;
                    response.messagePublishResponse = messagePublishResponse;
                    _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (System.Exception exc)
            {
                exceptionMessage = exc.ToString();
                _serializedResponsePackets.Add(Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.PublishMessageOnTopic);
                        int size = _value != null ? ((UserBinaryObject)_value).Size : 0;
                        bool notifyOnFailure = bool.Parse(_metaData[TopicConstant.NotifyOption] as string);
                        DeliveryOption deliveryOption = (DeliveryOption)(int.Parse(_metaData[TopicConstant.DeliveryOption] as string));
                        string topicName = _metaData[TopicConstant.TopicName] as string;
                        topicName = topicName.Split(TopicConstant.TopicSeperator)[1];

                        log.GeneratePublishTopicMessageAPILogItem(topicName, _command.messageId,size, deliveryOption, notifyOnFailure,_command.expiration, executionTime, clientManager.ClientID, clientManager.ClientIP, overload, exceptionMessage);
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

            string topicName = _metaData[TopicConstant.TopicName] as string;
            topicName = topicName.Split(TopicConstant.TopicSeperator)[1];
            
            commandName = "MessagingService.Publish";
            details.Append("Topic : " + topicName);
            details.Append(" ; ");
            string size = _value != null ? ((UserBinaryObject)_value).Size.ToString() : "0";
            details.Append("Size : " + size);
            details.Append(" ; ");
            TimeSpan expirationTime = new TimeSpan(_command.expiration);
            string expiration = expirationTime != TimeSpan.MaxValue ? expirationTime.ToString() : "NO_EXPIRATION";
            details.Append("Expiratoin : " + expiration);
            details.Append(" ; ");

            bool notifyOnFailure = bool.Parse(_metaData[TopicConstant.NotifyOption] as string);
            DeliveryOption deliveryOption = (DeliveryOption)(int.Parse(_metaData[TopicConstant.DeliveryOption] as string));

            details.Append("DeliveryOption : " + deliveryOption);
            details.Append(" ; ");
            details.Append("NotifyOnDeliveryFailure : " + notifyOnFailure);
            details.Append(" ; ");

            return details.ToString();
        }
    }
}