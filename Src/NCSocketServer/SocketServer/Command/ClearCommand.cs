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
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using System.Diagnostics;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ClearCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public bool DoAsync;
            public string RequestId;
            public BitSet FlagMap;
            public short DsClearedId;
            public string providerName;
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
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
                    if (cmdInfo.DsClearedId != -1)
                        cbEntry = new CallbackEntry(clientManager.ClientID, -1, null, -1, -1, -1, cmdInfo.DsClearedId, cmdInfo.FlagMap
                            , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                    OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                    operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, cmdInfo.providerName);

                    nCache.Cache.Clear(cmdInfo.FlagMap, cbEntry, operationContext);
                    stopWatch.Stop();

                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.ClearResponse clearResponse = new Alachisoft.NCache.Common.Protobuf.ClearResponse();
                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);

                    response.commandID = command.commandID;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.CLEAR;
                    response.clearResponse = clearResponse;

                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                catch (Exception exc)
                {
                    //PROTOBUF:RESPONSE
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
                            APILogItemBuilder log = new APILogItemBuilder(MethodsName.Clear.ToLower());
                            log.GenerateClearAPILogItem(cmdInfo.FlagMap, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                        }
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.ReadThruProviderName, cmdInfo.providerName);

                nCache.Cache.ClearAsync(cmdInfo.FlagMap, new CallbackEntry(clientManager.ClientID,
                    Convert.ToInt32(cmdInfo.RequestId),
                    null,
                    -1,
                    -1,
                    0,
                    cmdInfo.DsClearedId,
                    cmdInfo.FlagMap,
                    EventDataFilter.None, EventDataFilter.None)

                    , operationContext);
                stopWatch.Stop();
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.ClearAsync.ToLower());
                        log.GenerateClearAsyncAPILogItem(cmdInfo.FlagMap, cmdInfo.DsClearedId, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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

            Alachisoft.NCache.Common.Protobuf.ClearCommand clearCommand = command.clearCommand;

            cmdInfo.DoAsync = clearCommand.isAsync;
            cmdInfo.DsClearedId = (short)clearCommand.datasourceClearedCallbackId;
            cmdInfo.FlagMap = new BitSet((byte)clearCommand.flag);
            cmdInfo.RequestId = clearCommand.requestId.ToString();
            cmdInfo.providerName = clearCommand.providerName;

            return cmdInfo;
        }
    }
}
