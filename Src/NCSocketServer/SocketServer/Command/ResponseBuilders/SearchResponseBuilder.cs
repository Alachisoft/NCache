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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Serialization.Formatters;
using System.Collections;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    //Dated: July 20, 2011
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of SearchResponse command
    /// </summary>
    class SearchResponseBuilder : ResponseBuilderBase
    {
        public static Alachisoft.NCache.Common.Protobuf.SearchResponse BuildResponse(QueryResultSet resultSet, int commandVersion)
        {
            Alachisoft.NCache.Common.Protobuf.SearchResponse searchResponse = new Alachisoft.NCache.Common.Protobuf.SearchResponse();
            try
            {
                switch (commandVersion)
                {
                    case 0: //Version from NCache 3.8 to NCache 3.8 SP3
                        {
                            Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeys(resultSet.SearchKeysResult.GetEnumerator(), searchResponse.keys);
                        }
                        break;
                    case 1: //From Version 3.8 SP4 onwards // offically announced support in 4.1
                    case 2: //NCache 4.1 SP1
                        {
                            searchResponse.queryResultSet = new Alachisoft.NCache.Common.Protobuf.QueryResultSet();

                            switch (resultSet.Type)
                            {
                                case QueryType.AggregateFunction:
                                    searchResponse.queryResultSet.queryType = Alachisoft.NCache.Common.Protobuf.QueryType.AGGREGATE_FUNCTIONS;
                                    searchResponse.queryResultSet.aggregateFunctionType = (Alachisoft.NCache.Common.Protobuf.AggregateFunctionType)(int)resultSet.AggregateFunctionType;

                                    searchResponse.queryResultSet.aggregateFunctionResult = new Alachisoft.NCache.Common.Protobuf.DictionaryItem();
                                    searchResponse.queryResultSet.aggregateFunctionResult.key = resultSet.AggregateFunctionResult.Key.ToString();
                                    searchResponse.queryResultSet.aggregateFunctionResult.value = resultSet.AggregateFunctionResult.Value != null ? CompactBinaryFormatter.ToByteBuffer(resultSet.AggregateFunctionResult.Value, null) : null;
                                    break;

                                case QueryType.SearchKeys:
                                    searchResponse.queryResultSet.queryType = Alachisoft.NCache.Common.Protobuf.QueryType.SEARCH_KEYS;
                                    Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeys(resultSet.SearchKeysResult.GetEnumerator(), searchResponse.queryResultSet.searchKeyResults);
                                    break;
                            }
                        }
                        break;
                    default:
                        {
                            throw new Exception("Unsupported Command Version");
                        }
                }
            }
            catch (Exception ex)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled)
                {
                    SocketServer.Logger.NCacheLog.Error(ex.ToString());
                    if (resultSet == null)
                    {
                        SocketServer.Logger.NCacheLog.Error("QueryResultSet is null");
                    }
                    else if (resultSet.AggregateFunctionResult.Key == null)
                    {
                        SocketServer.Logger.NCacheLog.Error("QueryResultSet.AggregateFunctionResult.Key is null");
                    }
                    else if (resultSet.AggregateFunctionResult.Value == null)
                    {
                        SocketServer.Logger.NCacheLog.Error("QueryResultSet.AggregateFunctionResult.Value is null");
                    }
                }
            }

            return searchResponse;
        }
    }
}
