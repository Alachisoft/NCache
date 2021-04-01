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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;
using Runtime = Alachisoft.NCache.Runtime;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkRemoveCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public long RequestId;
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
            //details.AppendLine("Dependency: " + cmdInfo. != null ? "true" : "false");
            return details.ToString();
        }


        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            //while(true)
            //{ 
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
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
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                        //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, base.immatureId), base.ParsingExceptionMessage(exc));
                    }
                    return;
                }

                //TODO
                byte[] data = null;
                Hashtable removeResult = null;

                try
                {
                    NCache nCache = clientManager.CmdExecuter as NCache;
                    Notifications cbEnrty = null;
                    if (cmdInfo.DsItemsRemovedId != -1)
                    {
                        cbEnrty = new Notifications(clientManager.ClientID, -1, -1, -1, -1, cmdInfo.DsItemsRemovedId
                            , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                    }
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                    CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);

                    operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                    operationContext.Add(OperationContextFieldName.ClientOperationTimeout, clientManager.RequestTimeout);
                    operationContext.CancellationToken = CancellationToken;

                    removeResult = (Hashtable)nCache.Cache.Remove(cmdInfo.Keys, cmdInfo.FlagMap, cbEnrty, cmdInfo.ProviderName, operationContext);
                    stopWatch.Stop();
                    BulkRemoveResponseBuilder.BuildResponse(removeResult, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, command.requestID, nCache.Cache, clientManager);
                }
                catch (OperationCanceledException ex)
                {
                    exception = ex.ToString();
                    Dispose();

                }
                catch (Exception exc)
                {
                    _removeBulkResult = OperationResult.Failure;
                    exception = exc.ToString();
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    //_resultPacket = clientManager.ReplyPacket(base.ExceptionPacket(exc, cmdInfo.RequestId), base.ExceptionMessage(exc));
                }
                finally
                {
                    TimeSpan executionTime = stopWatch.Elapsed;
                    try
                    {
                        if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                        {

                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.REMOVEBULK.ToLower());
                            // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                            log.GenerateBulkDeleteAPILogItem(cmdInfo.Keys.Length, cmdInfo.FlagMap, cmdInfo.ProviderName, cmdInfo.DsItemsRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                        }
                    }
                    catch
                    {

                    }

                    if (removeResult?.Count > 0)
                    {
                        foreach (DictionaryEntry removeEntry in removeResult)
                        {
                            if (removeEntry.Value is CompressedValueEntry compressedValueEntry)
                            {
                                MiscUtil.ReturnEntryToPool(compressedValueEntry.Entry, clientManager.CacheTransactionalPool);
                                MiscUtil.ReturnCompressedEntryToPool(compressedValueEntry, clientManager.CacheTransactionalPool);
                            }
                        }
                    }
                }
            }
            finally
            {
                cmdInfo.FlagMap.MarkFree(NCModulesConstants.SocketServer);
            }
        //}
        }

        public override void IncrementCounter(Alachisoft.NCache.SocketServer.Statistics.StatisticsCounter collector, long value)
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

            BitSet bitset = BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);
            bitset.Data = (byte)bulkRemoveCommand.flag;
            cmdInfo.FlagMap = bitset;
            cmdInfo.RequestId = bulkRemoveCommand.requestId;
            cmdInfo.ProviderName = !string.IsNullOrEmpty(bulkRemoveCommand.providerName) ? bulkRemoveCommand.providerName : null;
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            return cmdInfo;
        }

        //private CommandInfo ParseCommand(ref string command)
        //{
        //    CommandInfo cmdInfo = new CommandInfo();

        //    int beginQuoteIndex = 0, endQuoteIndex = 0;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);

        //    int size = Convert.ToInt32(command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1));
            
        //    cmdInfo.Keys = new object[size];

        //    for (int i = 0; i < size; i++)
        //    {
        //        base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //        //if (beginQuoteIndex + 1 == endQuoteIndex) throw new ArgumentNullException("keys[" + i.ToString() + "]");
        //        cmdInfo.Keys[i] = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
        //    }

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.RequestId = command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
        //    base.immatureId = cmdInfo.RequestId;

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.FlagMap = new BitSet(Convert.ToByte(command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1)));

        //    base.UpdateDelimIndexes(ref command, '"', ref beginQuoteIndex, ref endQuoteIndex);
        //    cmdInfo.DsItemsRemovedId = Convert.ToInt16(command.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1));

        //    return cmdInfo;
        //}
    }
}
