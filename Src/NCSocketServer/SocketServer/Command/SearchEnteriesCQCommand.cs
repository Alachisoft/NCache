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
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.SocketServer.Command
{
    class SearchEnteriesCQCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Query;
            public IDictionary Values;
            public bool notifyAdd;
            public bool notifyUpdate;
            public bool notifyRemove;
            public string clientUniqueId;
            public string ClientLastViewId;
            public int CommandVersion;
            public int addDF;
            public int updateDF;
            public int removeDF;
        }

        private static char Delimitor = '|';

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
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                }
                return;
            }

            byte[] data = null;
            int resultCount = 0;
            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                QueryResultSet resultSet = null;
                QueryDataFilters datafilters = new QueryDataFilters(cmdInfo.addDF, cmdInfo.updateDF, cmdInfo.removeDF);

                Alachisoft.NCache.Caching.OperationContext operationContext = new Alachisoft.NCache.Caching.OperationContext(Alachisoft.NCache.Caching.OperationContextFieldName.OperationType, Alachisoft.NCache.Caching.OperationContextOperationType.CacheOperation);
                if (Convert.ToInt64(cmdInfo.ClientLastViewId) != -1)
                    operationContext.Add(Alachisoft.NCache.Caching.OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                resultSet = nCache.Cache.SearchEntriesCQ(cmdInfo.Query, cmdInfo.Values, cmdInfo.clientUniqueId, clientManager.ClientID, cmdInfo.notifyAdd, cmdInfo.notifyUpdate, cmdInfo.notifyRemove, operationContext, datafilters);
                stopWatch.Stop();
                SearchEnteriesCQResponseBuilder.BuildResponse(resultSet, cmdInfo.RequestId, _serializedResponsePackets, command.commandID, nCache.Cache, out resultCount);
            }
            catch (Exception exc)
            {
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

                        APILogItemBuilder log = new APILogItemBuilder(MethodsName.SearchCQ.ToLower());
                        log.GenerateSearchEntriesCQAPILogItem(cmdInfo.Query, cmdInfo.Values, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), resultCount);
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

            Alachisoft.NCache.Common.Protobuf.SearchCQCommand searchCommand = command.searchCQCommand;
            cmdInfo.Query = searchCommand.query;
            cmdInfo.RequestId = searchCommand.requestId.ToString();
            cmdInfo.notifyAdd = searchCommand.notifyAdd;
            cmdInfo.notifyUpdate = searchCommand.notifyUpdate;
            cmdInfo.notifyRemove = searchCommand.notifyRemove;
            cmdInfo.clientUniqueId = searchCommand.clientUniqueId;
            cmdInfo.ClientLastViewId = command.clientLastViewId.ToString();
            cmdInfo.CommandVersion = command.commandVersion;
            cmdInfo.addDF = searchCommand.addDataFilter;
            cmdInfo.updateDF = searchCommand.updateDataFilter;
            cmdInfo.removeDF = searchCommand.remvoeDataFilter;

            if (searchCommand.searchEntries == true)
            {
                cmdInfo.Values = new Hashtable();
                foreach (Alachisoft.NCache.Common.Protobuf.KeyValue searchValue in searchCommand.values)
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
