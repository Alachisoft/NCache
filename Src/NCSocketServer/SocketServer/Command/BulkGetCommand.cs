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
using System.Collections;
using System.Text;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkGetCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string[] Keys;
            public BitSet FlagMap;
            public string providerName;
            public long ClientLastViewId;
            public int CommandVersion;
            public string IntendedRecipient;
           
        }

        private OperationResult _getBulkResult = OperationResult.Success;
        CommandInfo cmdInfo;
        private readonly int READTHRU_BIT = 16; 

        internal override OperationResult OperationResult
        {
            get
            {
                return _getBulkResult;
            }

        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "BulkGet";
            details.Append("Command Keys: " + cmdInfo.Keys.Length);
            details.Append(" ; ");
            return details.ToString();
        }



        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch(); 
            int count = 0;
            stopWatch.Start();

            try
            {
                try
                {
                    overload = command.MethodOverload;
                    cmdInfo = ParseCommand(command, clientManager);
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd parsed");

                }
                catch (Exception exc)
                {
                    _getBulkResult = OperationResult.Failure;
                    if (!base.immatureId.Equals("-2"))
                    {
                        //PROTOBUF:RESPONSE
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    }
                    return;
                }

                byte[] data = null;

                NCache nCache = clientManager.CmdExecuter as NCache;
                HashVector getResult = null;

                try
                {
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                    CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                    if (!string.IsNullOrEmpty(cmdInfo.IntendedRecipient))
                        operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.IntendedRecipient);

                    operationContext.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                    operationContext.CancellationToken = CancellationToken;
                     getResult = (HashVector)nCache.Cache.GetBulk(cmdInfo.Keys, cmdInfo.FlagMap, operationContext);
                    stopWatch.Stop();

                    count = getResult.Count;
                    BulkGetResponseBuilder.BuildResponse(getResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, cmdInfo.IntendedRecipient, command.commandID, clientManager, nCache.Cache);
                }
                catch (OperationCanceledException ex)
                {
                    exception = ex.ToString();
                    Dispose();

                }
                catch (Exception exc)
                {
                    _getBulkResult = OperationResult.Failure;
                    exception = exc.ToString();
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                finally
                {
                    TimeSpan executionTime = stopWatch.Elapsed;
                    if (getResult != null)
                    {
                        foreach (CompressedValueEntry compressedValueEntry in getResult.Values)
                        {
                            MiscUtil.ReturnEntryToPool(compressedValueEntry.Entry, clientManager.CacheTransactionalPool);
                            MiscUtil.ReturnCompressedEntryToPool(compressedValueEntry, clientManager.CacheTransactionalPool);
                        }
                    }
                    try
                    {
                        if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                        {

                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetBulk.ToLower());
                            log.GenerateBulkGetAPILogItem(cmdInfo.Keys.Length, cmdInfo.providerName,  overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString(), count);

                        }
                    }
                    catch
                    {

                    }
                }
            }
            finally
            {
                cmdInfo.FlagMap.MarkFree(NCModulesConstants.SocketServer);
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("BulkGetCmd.Exec", "cmd executed on cache");

        }

        public override void IncrementCounter(Statistics.StatisticsCounter collector, long value)
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
            cmdInfo.providerName = bulkGetCommand.providerName;
            cmdInfo.RequestId = bulkGetCommand.requestId.ToString();
            BitSet bitset = BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);

            bitset.Data =((byte)bulkGetCommand.flag);

            cmdInfo.FlagMap = bitset;
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
          
            return cmdInfo;
        }

    }
}
