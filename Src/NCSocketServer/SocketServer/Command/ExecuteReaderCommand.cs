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
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ExecuteReaderCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Query;
            public bool GetData;
            public IDictionary Values;
            public int CommandVersion;
            public int ChunkSize;
            public string ClientLastViewId;
        }

        private static char Delimitor = '|';
        //PROTOBUF
        CommandInfo cmdInfo;

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "ExecuteReader";
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
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }
            int resultCount = 0;
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                ClusteredList<ReaderResultSet> resultSetList = null;
                Alachisoft.NCache.Caching.OperationContext operationContext = new Alachisoft.NCache.Caching.OperationContext(Alachisoft.NCache.Caching.OperationContextFieldName.OperationType, Alachisoft.NCache.Caching.OperationContextOperationType.CacheOperation);
                operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                //client id
                operationContext.Add(OperationContextFieldName.ClientId, clientManager.ClientID);
                resultSetList = nCache.Cache.ExecuteReader(cmdInfo.Query, cmdInfo.Values, cmdInfo.GetData, cmdInfo.ChunkSize, false, operationContext);
                stopWatch.Stop();
                ReaderResponseBuilder.Cache = nCache.Cache;
                ReaderResponseBuilder.BuildExecuteReaderResponse(resultSetList, cmdInfo.CommandVersion, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, clientManager.ClientVersion < 4620, out resultCount);
            }
            catch (Exception exc)
            {
                //PROTOBUF:RESPONSE
                exception = exc.ToString();
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID,command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.ExecuteReader.ToLower());
                        log.GenerateExecuteReaderAPILogItem(cmdInfo.Query, cmdInfo.Values, cmdInfo.GetData, cmdInfo.ChunkSize, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(),resultCount);
                    }
                }
                catch
                {
                }
            }
        }

        //PROTOBUF : SearchCommand is used for enteries
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.ExecuteReaderCommand executeReaderCommand = command.executeReaderCommand;
            cmdInfo.Query = executeReaderCommand.query;
            if (clientManager.IsDotNetClient)
            {
                int index = cmdInfo.Query.IndexOf("$Text$");
                if (index != -1)
                {
                    cmdInfo.Query = cmdInfo.Query.Replace("$Text$", "System.String");
                }
                else
                {
                    index = cmdInfo.Query.IndexOf("$TEXT$");
                    if (index != -1)
                    {
                        cmdInfo.Query = cmdInfo.Query.Replace("$TEXT$", "System.String");
                    }
                    else
                    {
                        index = cmdInfo.Query.IndexOf("$text$");
                        if (index != -1)
                        {
                            cmdInfo.Query = cmdInfo.Query.Replace("$text$", "System.String");
                        }
                    }
                }
            }
            else
            {
                int index = cmdInfo.Query.IndexOf("$Text$");
                if (index != -1)
                {
                    cmdInfo.Query = cmdInfo.Query.Replace("$Text$", "java.lang.String");
                }
                else
                {
                    index = cmdInfo.Query.IndexOf("$TEXT$");
                    if (index != -1)
                    {
                        cmdInfo.Query = cmdInfo.Query.Replace("$TEXT$", "java.lang.String");
                    }
                    else
                    {
                        index = cmdInfo.Query.IndexOf("$text$");
                        if (index != -1)
                        {
                            cmdInfo.Query = cmdInfo.Query.Replace("$text$", "java.lang.String");
                        }
                    }
                }
            }
            cmdInfo.RequestId = executeReaderCommand.requestId.ToString();
            cmdInfo.GetData = executeReaderCommand.getData;
            cmdInfo.ChunkSize = executeReaderCommand.chunkSize;

            cmdInfo.CommandVersion = command.commandVersion;
            cmdInfo.ClientLastViewId = command.clientLastViewId.ToString();
                cmdInfo.Values = new Hashtable();
                foreach (Alachisoft.NCache.Common.Protobuf.KeyValue searchValue in executeReaderCommand.values)
                {
                    string key = searchValue.key;
                    List<Alachisoft.NCache.Common.Protobuf.ValueWithType> valueWithTypes = searchValue.value;
                    Type type = null;
                    object value = null;

                    foreach (Alachisoft.NCache.Common.Protobuf.ValueWithType valueWithType in valueWithTypes)
                    {
                        string typeStr = valueWithType.type;
                        if (!clientManager.IsDotNetClient)
                        {
                            typeStr = JavaClrTypeMapping.JavaToClr(valueWithType.type);
                        }
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

            return cmdInfo;
        }

        private object GetValueObject(string value, bool dotNetClient)
        {
            object retVal = null;

            try
            {
                // Now we move data-type along with the value.So extract them here.
                string[] vals = value.Split(Delimitor);
                object valObj = (object)vals[0];
                string typeStr = vals[1];

                // Assuming that its otherwise java client only

                if (!dotNetClient)
                {
                    string type = JavaClrTypeMapping.JavaToClr(typeStr);
                    if (type != null) // Only if it is not null, otherwise let it go...
                    {
                        typeStr = type;
                    }
                }

                Type objType = System.Type.GetType(typeStr);
                retVal = Convert.ChangeType(valObj, objType);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }
    }
}
