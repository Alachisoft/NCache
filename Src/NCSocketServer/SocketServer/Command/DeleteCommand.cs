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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Runtime.Events;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;


namespace Alachisoft.NCache.SocketServer.Command
{
    class DeleteCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public bool DoAsync;

            public string RequestId;
            public string Key;
            public BitSet FlagMap;
            public short DsItemRemovedId;
            public object LockId;
            public ulong Version;
            public string ProviderName;
            public LockAccessType LockAccessType;
        }

        private OperationResult _removeResult = OperationResult.Success;
        CommandInfo cmdInfo;

        internal override OperationResult OperationResult
        {
            get
            {
                return _removeResult;
            }
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "Delete";
            details.Append("Command Keys: " + cmdInfo.Key);
            details.Append(" ; ");

            if (cmdInfo.FlagMap != null)
                details.Append("WriteThru: " + cmdInfo.FlagMap.IsBitSet(BitSetConstants.WriteThru));

            return details.ToString();
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RemCmd.Exec", "cmd parsed");
            }
            catch (Exception exc)
            {
                _removeResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }
            NCache nCache = clientManager.CmdExecuter as NCache;
            if (!cmdInfo.DoAsync)
            {
                try
                {
                    CallbackEntry cbEntry = null;
                    if (cmdInfo.DsItemRemovedId != -1)
                    {
                        cbEntry = new CallbackEntry(clientManager.ClientID, -1, null, -1, -1, -1, cmdInfo.DsItemRemovedId, cmdInfo.FlagMap
                            , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                    }

                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                    operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                    operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);

                    nCache.Cache.Delete(cmdInfo.Key, cmdInfo.FlagMap, cbEntry, cmdInfo.LockId, cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName, operationContext);
                    stopWatch.Stop();
               
                    
                    //PROTOBUF:RESPONSE
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.DeleteResponse removeResponse = new Alachisoft.NCache.Common.Protobuf.DeleteResponse();
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.DELETE;
                    response.deleteResponse = removeResponse;
                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                    response.commandID = command.commandID;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                catch (Exception exc)
                {
                    _removeResult = OperationResult.Failure;
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

                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.DELETE.ToLower());
                            log.GenerateDeleteAPILogItem(cmdInfo.Key, cmdInfo.FlagMap, cmdInfo.LockId, (long)cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName,cmdInfo.DsItemRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                    }
                    catch
                    {
                    }
                    if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("RemCmd.Exec", "cmd executed on cache");
                }
            }
            else
            {
                object[] package = null;
                if (!cmdInfo.RequestId.Equals("-1") || cmdInfo.DsItemRemovedId != -1)
                {
                    package = new object[] { cmdInfo.Key, cmdInfo.FlagMap, new CallbackEntry(clientManager.ClientID, 
                        Convert.ToInt32(cmdInfo.RequestId),
                        null,
                        -1,
                        -1,
                        (short)(cmdInfo.RequestId.Equals("-1") ? -1 : 0),
                        cmdInfo.DsItemRemovedId,
                        cmdInfo.FlagMap,
                        EventDataFilter.None, EventDataFilter.None) }; // DataFilter not required
                }
                else
                {
                    package = new object[] { cmdInfo.Key, cmdInfo.FlagMap, null, cmdInfo.ProviderName };
                }

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.MethodOverload, overload);
                nCache.Cache.RemoveAsync(package, operationContext);
                stopWatch.Stop();
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.DELETEASYNC.ToLower());
                        log.GenerateDeleteAPILogItem(cmdInfo.Key, cmdInfo.FlagMap, cmdInfo.LockId, (long)cmdInfo.Version, cmdInfo.LockAccessType, cmdInfo.ProviderName,cmdInfo.DsItemRemovedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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

            Alachisoft.NCache.Common.Protobuf.DeleteCommand removeCommand = command.deleteCommand;
            cmdInfo.DoAsync = removeCommand.isAsync;
            cmdInfo.DsItemRemovedId = (short)removeCommand.datasourceItemRemovedCallbackId;
            cmdInfo.FlagMap = new BitSet((byte)removeCommand.flag);
            cmdInfo.Key = removeCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)removeCommand.lockAccessType;
            cmdInfo.LockId = removeCommand.lockId;
            cmdInfo.RequestId = removeCommand.requestId.ToString();
            cmdInfo.Version = removeCommand.version;
            cmdInfo.ProviderName = !string.IsNullOrEmpty(removeCommand.providerName) ? removeCommand.providerName : null;

            return cmdInfo;
        }
    }
}
