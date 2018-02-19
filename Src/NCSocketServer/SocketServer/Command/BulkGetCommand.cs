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
using System.Collections;
using System.Text;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkGetCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string[] Keys;
            public BitSet FlagMap;
            public long ClientLastViewId;
            public int CommandVersion;
            public string IntendedRecipient;
        }

        private OperationResult _getBulkResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _getBulkResult;
            }
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd parsed");

            }
            catch (Exception exc)
            {
                _getBulkResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            byte[] data = null;

            NCache nCache = clientManager.CmdExecuter as NCache;
            try
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);

                Hashtable getResult = (Hashtable)nCache.Cache.GetBulk(cmdInfo.Keys, cmdInfo.FlagMap, operationContext);

                BulkGetResponseBuilder.BuildResponse(getResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, cmdInfo.IntendedRecipient);
            }
            catch (Exception exc)
            {
                _getBulkResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd executed on cache");

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

            Alachisoft.NCache.Common.Protobuf.BulkGetCommand bulkGetCommand = command.bulkGetCommand;
            cmdInfo.Keys = bulkGetCommand.keys.ToArray();
            cmdInfo.RequestId = bulkGetCommand.requestId.ToString();
            cmdInfo.FlagMap = new BitSet((byte)bulkGetCommand.flag);
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }
    }
}
