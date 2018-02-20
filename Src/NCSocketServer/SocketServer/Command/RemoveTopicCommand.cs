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
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RemoveTopicCommand : CommandBase
    {
        private Common.Protobuf.RemoveTopicCommand _command;

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            int overload = 1;
            string exceptionMessage = null;
            bool result = false;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                var operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                if (command.commandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else 
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, command.clientLastViewId.ToString(CultureInfo.InvariantCulture));
                }
                if (nCache != null)
                {
                    _command = command.removeTopicCommand;
                    TopicOperation topicOperation = new TopicOperation(_command.topicName, TopicOperationType.Remove);

                    result = nCache.Cache.TopicOpertion(topicOperation, operationContext);

                    Common.Protobuf.Response response = new Common.Protobuf.Response();
                    Common.Protobuf.RemoveTopicResponse removeTopicResponse = new Common.Protobuf.RemoveTopicResponse();
                    response.requestId = _command.requestId;
                    response.commandID = command.commandID;
                    removeTopicResponse.success = result; 
                    response.responseType = Common.Protobuf.Response.Type.REMOVE_TOPIC;
                    response.removeTopicResponse = removeTopicResponse;
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exceptionMessage = exc.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.DeleteTopic);
                        log.GenerateGetCreateOrDeleteTopicAPILogItem(_command.topicName, executionTime, clientManager.ClientID, clientManager.ClientIP, overload, result, exceptionMessage);
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

            commandName = "MessagingService.RemoveTopic";
            details.Append("topic : " + _command.topicName);
            details.Append(" ; ");
            return details.ToString();
        }
    }
}