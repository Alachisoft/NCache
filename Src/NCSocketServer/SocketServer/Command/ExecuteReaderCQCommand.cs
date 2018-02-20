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
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ExecuteReaderCQCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Query;
            public IDictionary Values;
            public bool getData;
            public int chunkSize;
            public bool notifyAdd;
            public bool notifyUpdate;
            public bool notifyRemove;
            public string clientUniqueId;
            public string ClientLastViewId;
            public int addDF;
            public int updateDF;
            public int removeDF;

            public int CommandVersion;
        }

        private static char Delimitor = '|';

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
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
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }
            int resultCount = 0;
            try
            {
                NCache cache = clientManager.CmdExecuter as NCache;
                List<ReaderResultSet> resultSetList = null;
                QueryDataFilters datafilters = new QueryDataFilters(cmdInfo.addDF, cmdInfo.updateDF, cmdInfo.removeDF);

                Alachisoft.NCache.Caching.OperationContext operationContext = new Caching.OperationContext(Alachisoft.NCache.Caching.OperationContextFieldName.OperationType, Alachisoft.NCache.Caching.OperationContextOperationType.CacheOperation);
                operationContext.Add(Caching.OperationContextFieldName.ClientId, clientManager.ClientID);
                operationContext.Add(Caching.OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);

                resultSetList = cache.Cache.ExecuteReaderCQ(cmdInfo.Query, cmdInfo.Values, cmdInfo.getData, cmdInfo.chunkSize, cmdInfo.clientUniqueId, clientManager.ClientID, cmdInfo.notifyAdd, cmdInfo.notifyUpdate, cmdInfo.notifyRemove, operationContext, datafilters);
                stopWatch.Stop(); 
                ReaderResponseBuilder.Cache = cache.Cache;
                ReaderResponseBuilder.BuildExecuteReaderCQResponse(resultSetList, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, clientManager.ClientVersion < 4620, out resultCount);
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
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.ExecuteReaderCQ.ToLower());
                        log.GenerateExecuteReaderCQAPILogItem(cmdInfo.Query, cmdInfo.Values, cmdInfo.getData, cmdInfo.chunkSize, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), resultCount);
                    }
                }
                catch
                {
                }
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo commandInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.ExecuteReaderCQCommand executeReaderCQCommand = command.executeReaderCQCommand;
            commandInfo.Query = executeReaderCQCommand.query;
            commandInfo.getData = executeReaderCQCommand.getData;
            commandInfo.chunkSize = (int)executeReaderCQCommand.chunkSize;
            commandInfo.RequestId = executeReaderCQCommand.requestId.ToString();
            commandInfo.notifyAdd = executeReaderCQCommand.notifyAdd;
            commandInfo.notifyRemove = executeReaderCQCommand.notifyRemove;
            commandInfo.notifyUpdate = executeReaderCQCommand.notifyUpdate;
            commandInfo.clientUniqueId = executeReaderCQCommand.clientUniqueId;
            commandInfo.ClientLastViewId = command.clientLastViewId.ToString();
            commandInfo.addDF = executeReaderCQCommand.addDataFilter;
            commandInfo.removeDF = executeReaderCQCommand.remvoeDataFilter;
            commandInfo.updateDF = executeReaderCQCommand.updateDataFilter;

            commandInfo.CommandVersion = command.commandVersion;
            commandInfo.Values = new Hashtable();
            foreach (Alachisoft.NCache.Common.Protobuf.KeyValue keyValuePair in executeReaderCQCommand.values)
            {
                string key = keyValuePair.key;
                List<Alachisoft.NCache.Common.Protobuf.ValueWithType> valueWithTypes = keyValuePair.value;
                Type type = null;
                object value = null;
                foreach (Alachisoft.NCache.Common.Protobuf.ValueWithType valueWithType in valueWithTypes)
                {
                    string typeString = valueWithType.type;
                    if (!clientManager.IsDotNetClient)
                    {
                        typeString = JavaClrTypeMapping.JavaToClr(valueWithType.type);
                    }
                    type = Type.GetType(typeString, true, true);

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

                    if (!commandInfo.Values.Contains(key))
                    {
                        commandInfo.Values.Add(key, value);
                    }
                    else
                    {
                        ArrayList list = commandInfo.Values[key] as ArrayList; // the value is not array list
                        if (list == null)
                        {
                            list = new ArrayList();
                            list.Add(commandInfo.Values[key]); // add the already present value in the list
                            commandInfo.Values.Remove(key); // remove the key from hashtable to avoid key already exists exception
                            list.Add(value);// add the new value in the list
                            commandInfo.Values.Add(key, list);
                        }
                        else
                        {
                            list.Add(value);
                        }
                    }
                }
            }
            return commandInfo;
        }
    }
}
