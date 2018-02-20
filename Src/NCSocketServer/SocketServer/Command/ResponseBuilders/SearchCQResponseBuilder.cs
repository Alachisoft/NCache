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

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    // Dated: July 20, 2011
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of SearchResponse command
    /// </summary>
    class SearchCQResponseBuilder : ResponseBuilderBase
    {
        public static Alachisoft.NCache.Common.Protobuf.SearchCQResponse BuildResponse(QueryResultSet resultSet, out int resultCount)
        {
            resultCount = 0;
            Alachisoft.NCache.Common.Protobuf.SearchCQResponse searchResponse = new Alachisoft.NCache.Common.Protobuf.SearchCQResponse();
            try
            {
                searchResponse.queryResultSet = new Alachisoft.NCache.Common.Protobuf.CQResultSet();
                searchResponse.queryResultSet.CQUniqueId = resultSet.CQUniqueId;
                searchResponse.queryResultSet.queryType = Alachisoft.NCache.Common.Protobuf.CQType.SEARCH_CQ_KEYS;
                if (resultSet != null && resultSet.SearchKeysResult != null)
                {
                    resultCount = resultSet.SearchKeysResult.Count;
                }
                Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeys(resultSet.SearchKeysResult.GetEnumerator(), searchResponse.queryResultSet.searchKeyResults);
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
            return searchResponse;
        }
    }
}
