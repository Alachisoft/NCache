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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class DeleteQueryCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Query;
            public IDictionary Values;
            
            public long ClientLastViewId;
            public int CommandVersion;
        }

        private OperationResult _deleteQueryResult = OperationResult.Success;
        CommandInfo cmdInfo;

        internal override OperationResult OperationResult
        {
            get
            {
                return _deleteQueryResult;
            }
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "DeleteQuery";
            details.Append("Command Query: " + cmdInfo.Query);
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
                _deleteQueryResult = OperationResult.Failure;
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

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                operationContext.Add(OperationContextFieldName.RemoveQueryOperation, false);

                nCache.Cache.DeleteQuery(cmdInfo.Query, cmdInfo.Values, operationContext);

                stopWatch.Stop();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.DeleteQueryResponse deleteQueryResponse = new Alachisoft.NCache.Common.Protobuf.DeleteQueryResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);

                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.DELETE_QUERY;
                response.deleteQueryResponse = deleteQueryResponse;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                _deleteQueryResult = OperationResult.Failure;
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.ExecuteNonQuery.ToLower());
                        log.GenerateDeleteQueryAPILogItem (cmdInfo.Query, cmdInfo.Values,overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString());
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

            Alachisoft.NCache.Common.Protobuf.DeleteQueryCommand deleteQueryCommand = command.deleteQueryCommand;
            cmdInfo.RequestId = deleteQueryCommand.requestId.ToString();
            cmdInfo.ClientLastViewId = command.clientLastViewId;
            cmdInfo.CommandVersion = command.commandVersion;
            cmdInfo.Query = deleteQueryCommand.query;

            {
                cmdInfo.Values = new Hashtable();
                foreach (Alachisoft.NCache.Common.Protobuf.KeyValue searchValue in deleteQueryCommand.values)
                {
                    string key = searchValue.key;
                    List<Alachisoft.NCache.Common.Protobuf.ValueWithType> valueWithTypes = searchValue.value;
                    Type type = null;
                    object value = null;

                    foreach (Alachisoft.NCache.Common.Protobuf.ValueWithType valueWithType in valueWithTypes)
                    {
                        string typeStr = valueWithType.type;
                        type = Type.GetType(typeStr, true, true);

                        if (valueWithType.value != null)
                        {
                            try
                            {
                                if (type == typeof(System.DateTime))
                                {
                                    // For client we would be sending ticks instead
                                    // of string representation of Date.
                                    value = new DateTime(Convert.ToInt64(valueWithType.value));
                                }
                                else
                                {
                                    value = Convert.ChangeType(valueWithType.value, type);
                                }
                            }
                            catch (Exception)
                            {
                                throw new System.FormatException("Cannot convert '" + valueWithType.value + "' to " + type.ToString());
                            }
                        }

                        if (!cmdInfo.Values.Contains(key))
                        {
                            cmdInfo.Values.Add(key, value);
                        }
                        else
                        {
                            ArrayList list = cmdInfo.Values[key] as ArrayList; // the value is not array list
                            if (list == null)
                            {
                                list = new ArrayList();
                                list.Add(cmdInfo.Values[key]); // add the already present value in the list
                                cmdInfo.Values.Remove(key); // remove the key from hashtable to avoid key already exists exception
                                list.Add(value);// add the new value in the list
                                cmdInfo.Values.Add(key, list);
                            }
                            else
                            {
                                list.Add(value);
                            }
                        }
                    }
                }
            }

            return cmdInfo;
        }
    }
}
