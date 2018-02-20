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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;

using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;


namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkRemoveCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public object[] Keys;
            public BitSet FlagMap;
            public short DsItemsRemovedId;
            public string ProviderName;
            public long ClientLastViewId;
            public int CommandVersion;
        }

        private OperationResult _removeBulkResult = OperationResult.Success;

        CommandInfo cmdInfo;
            
        internal override OperationResult OperationResult
        {
            get
            {
                return _removeBulkResult;
            }
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "RemoveBulk";
            details.Append("Command Keys: " + cmdInfo.Keys.Length);
            details.Append(" ; ");
            if (cmdInfo.FlagMap != null)
                details.Append("WriteThru: " + cmdInfo.FlagMap.IsBitSet(BitSetConstants.WriteThru));
            return details.ToString();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                _removeBulkResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            byte[] data = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                CallbackEntry cbEnrty = null;
                if (cmdInfo.DsItemsRemovedId != -1)
                {
                    cbEnrty = new CallbackEntry(clientManager.ClientID, -1, null, -1, -1, -1, cmdInfo.DsItemsRemovedId, cmdInfo.FlagMap
                        , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                }
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);

                Hashtable removeResult = (Hashtable)nCache.Cache.Remove(cmdInfo.Keys, cmdInfo.FlagMap, cbEnrty, cmdInfo.ProviderName, operationContext);
                stopWatch.Stop();
                BulkRemoveResponseBuilder.BuildResponse(removeResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets,command.commandID,nCache.Cache);
            }
            catch (Exception exc)
            {
                _removeBulkResult = OperationResult.Failure;
                exception = exc.ToString();
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.REMOVEBULK.ToLower());
                        log.GenerateBulkDeleteAPILogItem(cmdInfo.Keys.Length, cmdInfo.FlagMap, cmdInfo.ProviderName,cmdInfo.DsItemsRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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
                collector.IncrementMsecPerDelBulkAvg(value);
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand bulkRemoveCommand = command.bulkRemoveCommand;
            cmdInfo.Keys = new ArrayList(bulkRemoveCommand.keys).ToArray();
            cmdInfo.DsItemsRemovedId = (short)bulkRemoveCommand.datasourceItemRemovedCallbackId;
            cmdInfo.FlagMap = new BitSet((byte)bulkRemoveCommand.flag);
            cmdInfo.RequestId = bulkRemoveCommand.requestId.ToString();
            cmdInfo.ProviderName = !string.IsNullOrEmpty(bulkRemoveCommand.providerName) ? bulkRemoveCommand.providerName : null;
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }
    }
}
