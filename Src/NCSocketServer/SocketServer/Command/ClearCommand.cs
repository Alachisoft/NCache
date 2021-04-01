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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;
using System.Diagnostics;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Util;

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
            CommandInfo cmdInfo=default(CommandInfo);
            int overload;
            string exception = null;
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
                    if (!base.immatureId.Equals("-2"))
                    {
                        //PROTOBUF:RESPONSE
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    }
                    return;
                }
                NCache nCache = clientManager.CmdExecuter as NCache;
                if (!cmdInfo.DoAsync)
                {
                    try
                    {
                        Notifications notification = null;
                        if (cmdInfo.DsClearedId != -1)
                            notification = new Notifications(clientManager.ClientID, -1, -1, -1, -1, cmdInfo.DsClearedId
                                , Runtime.Events.EventDataFilter.None, Runtime.Events.EventDataFilter.None); //DataFilter not required
                        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                        operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.providerName);
                        CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                        nCache.Cache.Clear(cmdInfo.FlagMap, notification, operationContext);
                        stopWatch.Stop();

                        Alachisoft.NCache.Common.Protobuf.ClearResponse clearResponse = new Alachisoft.NCache.Common.Protobuf.ClearResponse();

                        if (clientManager.ClientVersion >= 5000)
                        {
                            ResponseHelper.SetResponse(clearResponse, command.requestID, command.commandID);
                            _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(clearResponse, Common.Protobuf.Response.Type.CLEAR));
                        }
                        else
                        {
                            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                            response.clearResponse = clearResponse;
                            ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.CLEAR);

                            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                        }
                    }
                    catch (Exception exc)
                    {
                        //PROTOBUF:RESPONSE
                        exception = exc.ToString();
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
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
                    operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.providerName);

                    nCache.Cache.ClearAsync(cmdInfo.FlagMap, new Notifications(clientManager.ClientID,
                        Convert.ToInt32(cmdInfo.RequestId),
                        -1,
                        -1,
                        0,
                        cmdInfo.DsClearedId,
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
            finally
            {
                cmdInfo.FlagMap?.MarkFree(NCModulesConstants.SocketServer);
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.ClearCommand clearCommand= command.clearCommand;

            cmdInfo.DoAsync = clearCommand.isAsync;
            cmdInfo.DsClearedId = (short) clearCommand.datasourceClearedCallbackId;     
            BitSet bitset= BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer); ;
            bitset.Data =((byte)clearCommand.flag);
            cmdInfo.FlagMap = bitset;
            cmdInfo.RequestId = clearCommand.requestId.ToString();
            cmdInfo.providerName = clearCommand.providerName;

            return cmdInfo;
        }
    }
}
