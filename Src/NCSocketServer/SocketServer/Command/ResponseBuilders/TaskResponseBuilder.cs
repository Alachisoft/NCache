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

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    internal class TaskResponseBuilder : ResponseBuilderBase
    {
        public static void BuildTaskResponse(long requestId, IList _serializedResponse)
        {
            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.MapReduceTaskResponse taskResponse = new Alachisoft.NCache.Common.Protobuf.MapReduceTaskResponse();
            response.requestId = Convert.ToInt64(requestId);
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.MAP_REDUCE_TASK;
            response.mapReduceTaskResponse = taskResponse;

            _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
        }
    }
}
