// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.SocketServer.Command.ResponseBuilders;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.SocketServer.Command
{
    class SearchCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Query;
            public IDictionary Values;
            public int CommandVersion;
            public string ClientLastViewId;
        }

        private static char Delimitor = '|'; //Asif Imam

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
                QueryResultSet resultSet = null;
                Alachisoft.NCache.Caching.OperationContext operationContext = new Alachisoft.NCache.Caching.OperationContext(Alachisoft.NCache.Caching.OperationContextFieldName.OperationType, Alachisoft.NCache.Caching.OperationContextOperationType.CacheOperation);
                if (cmdInfo.CommandVersion <= 1) //NCache 3.8 SP4 and previous
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, forcedViewId);
                }
                else //NCache 4.1 SP1 or later
                {
                    operationContext.Add(OperationContextFieldName.ClientLastViewId, cmdInfo.ClientLastViewId);
                }

                resultSet = nCache.Cache.Search(cmdInfo.Query, cmdInfo.Values, operationContext);

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.search = SearchResponseBuilder.BuildResponse(resultSet, cmdInfo.CommandVersion);
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH;

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }


        //PROTOBUF : SearchCommand
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.SearchCommand searchCommand = command.searchCommand;
            cmdInfo.Query = searchCommand.query;

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

            cmdInfo.RequestId = searchCommand.requestId.ToString();
            cmdInfo.CommandVersion = command.commandVersion;
            cmdInfo.ClientLastViewId = command.clientLastViewId.ToString();

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

                    type = Type.GetType(typeStr, true, true);

                    if (valueWithType.value != null)
                    {
                        try
                        {
                            if (type == typeof(System.DateTime))
                            {
                                ///For client we would be sending ticks instead
                                ///of string representation of Date.
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

        private object GetValueObject(string value)
        {
            object retVal = null;

            try
            {
                //Added by Asif Imam:: Now we move data-type along with the value.So extract them here.
                string[] vals = value.Split(Delimitor);
                object valObj = (object)vals[0];
                string typeStr = vals[1];

                Type objType = System.Type.GetType(typeStr);
                if (objType == typeof(System.DateTime))
                {
                    System.Globalization.CultureInfo enUs = new System.Globalization.CultureInfo("en-US");
                    retVal = Convert.ChangeType(valObj, objType, enUs);
                }
                else
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
