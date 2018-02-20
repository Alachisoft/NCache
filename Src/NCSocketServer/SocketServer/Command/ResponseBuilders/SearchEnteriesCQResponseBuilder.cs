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
using Alachisoft.NCache.Caching.Queries;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    // Dated: July 20, 2011
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of SearchEnteriesCQ command
    /// </summary>
    class SearchEnteriesCQResponseBuilder : ResponseBuilderBase
    {
        public static IList BuildResponse(QueryResultSet resultSet, string RequestId, IList _serializedResponse, int commandID, Caching.Cache cache, out int resultCount)
        {
            Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.Cache = cache;
            long requestId = Convert.ToInt64(RequestId);
            resultCount = 0;
            try
            {
                int sequenceId = 1;

                Alachisoft.NCache.Common.Protobuf.SearchEntriesCQResponse searchEntriesResponse = new Alachisoft.NCache.Common.Protobuf.SearchEntriesCQResponse();
                searchEntriesResponse.queryResultSet = new Alachisoft.NCache.Common.Protobuf.CQResultSet();
                searchEntriesResponse.queryResultSet.CQUniqueId = resultSet.CQUniqueId;
                searchEntriesResponse.queryResultSet.queryType = Alachisoft.NCache.Common.Protobuf.CQType.SEARCH_CQ_ENTRIES;

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                response.requestId = requestId;
                response.commandID = commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.SEARCH_ENTRIES_CQ;

                IList keyValuesPackageChuncks = Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysValues(resultSet.SearchEntriesResult);
                response.numberOfChuncks = keyValuesPackageChuncks.Count;
                foreach (Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse package in keyValuesPackageChuncks)
                {
                    response.sequenceId = sequenceId++;
                    searchEntriesResponse.queryResultSet.searchKeyEnteriesResult = package;
                    response.searchEntriesCQResponse = searchEntriesResponse;
                    _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }

                if (resultSet != null && resultSet.SearchEntriesResult != null)
                {
                    resultCount = resultSet.SearchEntriesResult.Count;
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
                }
            }

            return _serializedResponse;
        }
    }
}
