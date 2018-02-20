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
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetTagCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string[] Tags;
            public TagComparisonType ComparisonType;
            public long ClientLastViewId;
            public int CommandVersion;
        }

        private OperationResult _getResult = OperationResult.Success;
        CommandInfo cmdInfo;
        private long resultCount = 0;

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
            commandName = "GetTag";
            details.Append("Command Tags: ");
            foreach (string tag in cmdInfo.Tags)
            {
                details.Append(tag + ",");
            }
            details.Append(" ; ");
            details.Append("Result count: " + resultCount);
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
            int responseResult = 0;
            byte[] data = null;

            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("GetCommand", "command: " + command + " Error" + arEx);
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID, command.commandID));
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
                HashVector result = null;
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                if (cmdInfo.CommandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else // NCache 4.1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                }

                result = (HashVector)nCache.Cache.GetByTag(cmdInfo.Tags, cmdInfo.ComparisonType, operationContext);
                stopWatch.Stop();
                resultCount = result.Count;
                responseResult = result.Count;
                GetTagResponseBuilder.BuildResponse(result, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, nCache.Cache);

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
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetByAllTags.ToLower());
                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            log.GenerateGetByAllTagsAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), responseResult);
                        }
                        else if (cmdInfo.ComparisonType == TagComparisonType.BY_TAG)
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetByTag.ToLower());
                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            log.GenerateGetByTagAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), responseResult);
                        }
                        else if (cmdInfo.ComparisonType == TagComparisonType.ANY_MATCHING_TAG)
                        {
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetByAnyTag.ToLower());
                            Hashtable tags = new Hashtable();
                            foreach (string tag in cmdInfo.Tags)
                            {
                                tags.Add(tag, tag);
                            }
                            log.GenerateGetByAnyTagAPILogItem(tags, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), responseResult);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public override void IncrementCounter(Statistics.PerfStatsCollector collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerGetBulkAvg(value);
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetTagCommand getTagCommand = command.getTagCommand;

            cmdInfo.ComparisonType = (TagComparisonType)getTagCommand.tagComparisonType;
            cmdInfo.RequestId = getTagCommand.requestId.ToString();
            cmdInfo.Tags = getTagCommand.tags.ToArray();
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;

            return cmdInfo;
        }
    }
}
