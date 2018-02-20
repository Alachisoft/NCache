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
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetGroupData : GetGroupCommandBase
    {
        //PROTOBUF
        //TODO:KeyPackage
        CommandInfo cmdInfo;
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "GroupData";
            details.Append("Command Group/SubGroup: " + cmdInfo.Group + "/" + cmdInfo.SubGroup);
            return details.ToString();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int result = 0;
            try
            {
                overload = command.MethodOverload;
                cmdInfo = base.ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }

            byte[] data = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                if (cmdInfo.CommandVersion < 1)
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else // NCache 4.1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                }

                HashVector groupResult = nCache.Cache.GetGroupData(cmdInfo.Group, cmdInfo.SubGroup, operationContext);
                stopWatch.Stop();
                result = groupResult.Count;
                GetGroupDataResponseBuilder.BuildResponse(groupResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, nCache.Cache);
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetGroupData.ToLower());
                        log.GenerateGetGroupDataAPILogItem(cmdInfo.Group, cmdInfo.SubGroup, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), result);
                    }
                }
                catch
                {
                }
            }
        }

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.PerfStatsCollector collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerGetBulkAvg(value);
            }
        }
    }
}
