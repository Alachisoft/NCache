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
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Processor;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Caching;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class InvokeEntryProcessorCommand : CommandBase
    {
        public struct CommandInfo
        {
            public long requestId;
            public long clientLastViewId;
            public string CommandVersion;

            public string[] keys;
            public IEntryProcessor entryProcessor;
            public object[] arguments;
            public BitSet readOptionFlag;
            public string defaultReadThru;
            public BitSet writeOptionFlag;
            public string defaultWriteThru;

            public string intendedRecipient;
        }

        private OperationResult invokeEntryProcessorResult = OperationResult.Success;

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo = new CommandInfo();
            int overload;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager, clientManager.CmdExecuter.ID);
            }
            catch (Exception exc)
            {
                invokeEntryProcessorResult = Command.OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }
            NCache nCache = clientManager.CmdExecuter as NCache;

            try
            {
                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.clientLastViewId);
                operationContext.Add(OperationContextFieldName.ReadThru, cmdInfo.readOptionFlag.IsBitSet(BitSetConstants.ReadThru));
                if (cmdInfo.defaultReadThru != null)
                {
                    operationContext.Add(OperationContextFieldName.ReadThruProviderName, cmdInfo.defaultReadThru);
                }

                if (!string.IsNullOrEmpty(cmdInfo.intendedRecipient))
                {
                    operationContext.Add(OperationContextFieldName.IntendedRecipient, cmdInfo.intendedRecipient);
                }

                Hashtable getResult = (Hashtable)nCache.Cache.InvokeEntryProcessor(cmdInfo.keys, cmdInfo.entryProcessor, cmdInfo.arguments, cmdInfo.writeOptionFlag, cmdInfo.defaultWriteThru, operationContext);

                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.InvokeEntryProcessorResponse invokeEntryProcessorResponse = new Alachisoft.NCache.Common.Protobuf.InvokeEntryProcessorResponse();
                response.requestId = Convert.ToInt64(cmdInfo.requestId);

                Alachisoft.NCache.Common.Protobuf.InvokeEPKeyValuePackageResponse invokeEPKeyValuePackage = new Common.Protobuf.InvokeEPKeyValuePackageResponse();
                IDictionaryEnumerator dictEnumerator = getResult.GetEnumerator();
                while(dictEnumerator.MoveNext())
                {
                    invokeEPKeyValuePackage.keys.Add((string)dictEnumerator.Key);
                    invokeEPKeyValuePackage.values.Add(CompactBinaryFormatter.ToByteBuffer(dictEnumerator.Value, nCache.CacheId));
                }
                invokeEntryProcessorResponse.keyValuePackage = invokeEPKeyValuePackage;

                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.INVOKE_ENTRY_PROCESSOR;
                response.invokeEntryProcessorResponse = invokeEntryProcessorResponse;

                response.commandID = command.commandID;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                invokeEntryProcessorResult = OperationResult.Failure;
                //PROTOBUF:RESPONSE
                exception = exc.ToString();
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.Invoke.ToLower());
                        log.GenerateEntryProcessorAPILogItem(cmdInfo.keys.Length, cmdInfo.entryProcessor.ToString(), cmdInfo.readOptionFlag, cmdInfo.defaultReadThru, cmdInfo.writeOptionFlag, cmdInfo.defaultWriteThru, cmdInfo.arguments, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
                    }
                }
                catch
                {
                }
            }
        }

        protected CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager, string cacheId)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Common.Protobuf.InvokeEntryProcessorCommand invokeEntryProcessorCommand = command.invokeEntryProcessorCommand;

            cmdInfo.keys = invokeEntryProcessorCommand.keys.ToArray();
            cmdInfo.entryProcessor = (IEntryProcessor)CompactBinaryFormatter.FromByteBuffer(invokeEntryProcessorCommand.entryprocessor, cacheId);
            cmdInfo.arguments = new object[invokeEntryProcessorCommand.arguments.Count];
            int counter = 0;
            List<byte[]> arguments = invokeEntryProcessorCommand.arguments;
            if (arguments.Count != 0)
            {
                foreach (byte[] argument in invokeEntryProcessorCommand.arguments)
                {
                    cmdInfo.arguments[counter] = CompactBinaryFormatter.FromByteBuffer(argument, cacheId);
                    counter++;
                }
            }
            else
                cmdInfo.arguments = null;
            cmdInfo.readOptionFlag = new BitSet((byte)invokeEntryProcessorCommand.dsReadOption);
            cmdInfo.defaultReadThru = invokeEntryProcessorCommand.defaultReadThru;
            cmdInfo.writeOptionFlag = new BitSet((byte)invokeEntryProcessorCommand.dsWriteOption);
            cmdInfo.defaultWriteThru = invokeEntryProcessorCommand.defaultWriteThru;

            cmdInfo.requestId = command.requestID;
            cmdInfo.clientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.version;

            return cmdInfo;
        }
    }
}
