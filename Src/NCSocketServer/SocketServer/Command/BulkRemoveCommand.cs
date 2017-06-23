// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkRemoveCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public object[] Keys;
            public BitSet FlagMap;
            public long ClientLastViewId;
            public int CommandVersion;
        }

        private OperationResult _removeBulkResult = OperationResult.Success;

        internal override OperationResult OperationResult
        {
            get
            {
                return _removeBulkResult;
            }
        }


        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            
            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                _removeBulkResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            //TODO
            byte[] data = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                CallbackEntry cbEnrty = null;
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                Hashtable removeResult = (Hashtable)nCache.Cache.Remove(cmdInfo.Keys, cmdInfo.FlagMap, cbEnrty, operationContext);               
                BulkRemoveResponseBuilder.BuildResponse(removeResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets);

            }
            catch (Exception exc)
            {
                _removeBulkResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
            }
        }

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.PerfStatsCollector collector, long value)
        {
            if (collector != null)
            {
                collector.IncrementMsecPerDelBulkAvg(value);
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand bulkRemoveCommand = command.bulkRemoveCommand;
            cmdInfo.Keys = new ArrayList(bulkRemoveCommand.keys).ToArray();
            cmdInfo.FlagMap = new BitSet((byte)bulkRemoveCommand.flag);
            cmdInfo.RequestId = bulkRemoveCommand.requestId.ToString();
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }
    }
}
