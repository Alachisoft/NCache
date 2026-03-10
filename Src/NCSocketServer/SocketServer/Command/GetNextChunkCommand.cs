//  Copyright (c) 2026 Alachisoft
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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.ResponseSerialization;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetNextChunkCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public EnumerationPointer Pointer;
            public OperationContext OperationContext;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
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
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Exception = exc,
                        RequestId = command.requestID,
                        CommandId = command.commandID,
                    };

                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
                }

                return;
            }

            int count = 0;
            string keyPackage = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                EnumerationDataChunk nextChunk = nCache.Cache.GetNextChunk(cmdInfo.Pointer, cmdInfo.OperationContext);
                stopWatch.Stop();
                if (!clientManager.EnumerationPointers.ContainsKey(cmdInfo.Pointer.Id))
                {
                    clientManager.EnumerationPointers.Add(cmdInfo.Pointer.Id, cmdInfo.Pointer);
                }
                else
                {
                    clientManager.EnumerationPointers[cmdInfo.Pointer.Id] = cmdInfo.Pointer;
                }

                if (clientManager.ClientVersion >= 5000)
                {
                    Alachisoft.NCache.Common.Protobuf.GetNextChunkResponse getNextChunkResponse = new Alachisoft.NCache.Common.Protobuf.GetNextChunkResponse();
                    getNextChunkResponse.requestId = Convert.ToInt64(cmdInfo.RequestId);

                    getNextChunkResponse.commandID = command.commandID;
                    getNextChunkResponse.intendedRecipient = cmdInfo.OperationContext.GetValueByField(OperationContextFieldName.IntendedRecipient).ToString();

                    if (nextChunk.Data != null)
                        getNextChunkResponse.keys.AddRange(nextChunk.Data);

                    getNextChunkResponse.enumerationPointer = EnumerationPointerConversionUtil.ConvertToProtobufEnumerationPointer(nextChunk.Pointer);

                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = getNextChunkResponse,
                        ResponseType = Common.Protobuf.Response.Type.GET_NEXT_CHUNK
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }
                else
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.GetNextChunkResponse getNextChunkResponse = new Alachisoft.NCache.Common.Protobuf.GetNextChunkResponse();
                    response.requestId = Convert.ToInt64(cmdInfo.RequestId);

                    response.commandID = command.commandID;
                    response.intendedRecipient = cmdInfo.OperationContext.GetValueByField(OperationContextFieldName.IntendedRecipient).ToString();
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_NEXT_CHUNK;
                    response.getNextChunkResponse = getNextChunkResponse;

                    if (nextChunk.Data != null)
                        getNextChunkResponse.keys.AddRange(nextChunk.Data);

                    getNextChunkResponse.enumerationPointer = EnumerationPointerConversionUtil.ConvertToProtobufEnumerationPointer(nextChunk.Pointer);

                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = response,
                        ResponseType = Common.Protobuf.Response.Type.GET_NEXT_CHUNK
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                ResponseOptions responseOptions = new ResponseOptions()
                {
                    Exception = exc,
                    RequestId = command.requestID,
                    CommandId = command.commandID,
                };

                _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetNextChunk.ToLower());
                        log.GenerateGetEnumeratorAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                    }
                }
                catch
                {

                }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.GetNextChunkCommand getNextChunkCommand = command.getNextChunkCommand;
            cmdInfo.RequestId = getNextChunkCommand.requestId.ToString();
            cmdInfo.Pointer = EnumerationPointerConversionUtil.GetFromProtobufEnumerationPointer(getNextChunkCommand.enumerationPointer);

            string intendedRecepient = command.intendedRecipient;
            long lastViewId = command.clientLastViewId;

            cmdInfo.OperationContext = new OperationContext();
            cmdInfo.OperationContext.Add(OperationContextFieldName.IntendedRecipient, intendedRecepient);
            cmdInfo.OperationContext.Add(OperationContextFieldName.ClientLastViewId, lastViewId);

            return cmdInfo;
        }
    }
}


