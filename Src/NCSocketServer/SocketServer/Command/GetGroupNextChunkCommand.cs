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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetGroupNextChunkCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public GroupEnumerationPointer Pointer;
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
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));

                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                EnumerationDataChunk nextChunk = nCache.Cache.GetNextChunk(cmdInfo.Pointer, cmdInfo.OperationContext);
                stopWatch.Stop();
              

                Alachisoft.NCache.Common.Protobuf.GetGroupNextChunkResponse getNextChunkResponse = new Alachisoft.NCache.Common.Protobuf.GetGroupNextChunkResponse();
                getNextChunkResponse.keys.AddRange(nextChunk.Data);
                getNextChunkResponse.groupEnumerationPointer = EnumerationPointerConversionUtil.ConvertToProtobufGroupEnumerationPointer(nextChunk.Pointer as GroupEnumerationPointer);

                if (clientManager.ClientVersion >= 5000)
                {
                    ResponseHelper.SetResponse(getNextChunkResponse, command.requestID, command.commandID);
                    _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(getNextChunkResponse, Common.Protobuf.Response.Type.GET_GROUP_NEXT_CHUNK));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    response.getGroupNextChunkResponse = getNextChunkResponse;
                    ResponseHelper.SetResponse(response, command.requestID, command.commandID, Common.Protobuf.Response.Type.GET_GROUP_NEXT_CHUNK);
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exception = exception.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.GetGroupNextChunk.ToLower());
                        log.GenerateGetEnumeratorAPILogItem(1, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());

                        // Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
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
            Alachisoft.NCache.Common.Protobuf.GetGroupNextChunkCommand getNextChunkCommand = command.getGroupNextChunkCommand;
            cmdInfo.RequestId = getNextChunkCommand.requestId.ToString();
            cmdInfo.Pointer = EnumerationPointerConversionUtil.GetFromProtobufGroupEnumerationPointer(getNextChunkCommand.groupEnumerationPointer);

            string intendedRecepient = command.intendedRecipient;
            long lastViewId = command.clientLastViewId;

            cmdInfo.OperationContext = new OperationContext();
            cmdInfo.OperationContext.Add(OperationContextFieldName.IntendedRecipient, intendedRecepient);
            cmdInfo.OperationContext.Add(OperationContextFieldName.ClientLastViewId, lastViewId);
            CommandsUtil.PopulateClientIdInContext(ref cmdInfo.OperationContext, clientManager.ClientAddress);
            return cmdInfo;
        }
    }
}