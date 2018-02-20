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
using Alachisoft.NCache.Caching;
using System.Collections;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class RemoveByTagCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string[] Tags;
            public TagComparisonType ComparisonType;
            public long ClientLastViewId;
        }

        private OperationResult _getResult = OperationResult.Success;
        CommandInfo cmdInfo;
        internal override OperationResult OperationResult
        {
            get
            {
                return _getResult;
            }
        }
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "RemoveByTag";
            details.Append("Command Tags: ");
            foreach (string tag in cmdInfo.Tags)
            {
                details.Append(tag + ",");
            }
            return details.ToString();
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            byte[] data = null;

            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "GetCommand", "command: " + command + " Error" + arEx);
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID,command.commandID));
                return;
            }
            catch (Exception exc)
            {
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                nCache.Cache.RemoveByTag(cmdInfo.Tags, cmdInfo.ComparisonType, operationContext);
                stopWatch.Stop();

                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.RemoveByTagResponse removeTagResponse = new Alachisoft.NCache.Common.Protobuf.RemoveByTagResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;

                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.REMOVE_TAG;
                response.removeTagResponse = removeTagResponse;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _getResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;

                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        if (cmdInfo.ComparisonType == TagComparisonType.ALL_MATCHING_TAGS)
                        {
                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.RemoveByAllTags.ToLower());
                            log.GenerateRemoveByAllTagsAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                        else if (cmdInfo.ComparisonType == TagComparisonType.BY_TAG)
                        {

                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            if (tags != null)
                            {
                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.RemoveByTag.ToLower());
                                log.GenerateRemoveByTagAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                            }
                        }
                        else if (cmdInfo.ComparisonType == TagComparisonType.ANY_MATCHING_TAG)
                        {
                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            if (tags != null)
                            {
                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.RemoveByAnyTag.ToLower());
                                log.GenerateRemoveByAnyTagAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                            }
                        }
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

            Alachisoft.NCache.Common.Protobuf.RemoveByTagCommand removeTagCommand = command.removeByTagCommand;

            cmdInfo.RequestId = removeTagCommand.requestId.ToString();
            cmdInfo.Tags = removeTagCommand.tags.ToArray();
            cmdInfo.ComparisonType = (TagComparisonType)removeTagCommand.tagComparisonType;
            cmdInfo.ClientLastViewId = command.clientLastViewId;

            return cmdInfo;
        }
    }
}
