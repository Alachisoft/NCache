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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    // Dated: July 20, 2011
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of GetGroupData command
    /// </summary>
    class GetGroupDataResponseBuilder : ResponseBuilderBase
    {
        public static IList BuildResponse(HashVector groupResult, int commandVersion, string RequestId, IList _serializedResponse, int commandID, Caching.Cache cache)
        {
            long requestId = Convert.ToInt64(RequestId);

            Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.Cache = cache;
            switch (commandVersion)
            {
                case 0: // Versions earlier than NCache 4.1 because all of them expect responses as one chunck
                    {
                        Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                        Alachisoft.NCache.Common.Protobuf.GetGroupDataResponse getGroupDataResponse = new Alachisoft.NCache.Common.Protobuf.GetGroupDataResponse();
                        response.requestId = requestId;
                        response.commandID = commandID;
                        getGroupDataResponse.keyValuePackage = Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysValues(groupResult, getGroupDataResponse.keyValuePackage);
                        response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_GROUP_DATA;
                        response.getGroupData = getGroupDataResponse;
                        _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }
                    break;
                case 1: // Verion 4.1 or later
                    {
                        IList keyValuesPackageChuncks = Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysValues(groupResult);
                        int sequenceId = 1;
                        Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                        Alachisoft.NCache.Common.Protobuf.GetGroupDataResponse getGroupDataResponse = new Alachisoft.NCache.Common.Protobuf.GetGroupDataResponse();
                        response.requestId = requestId;
                        response.commandID = commandID;
                        response.numberOfChuncks = keyValuesPackageChuncks.Count;
                        response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_GROUP_DATA;

                        foreach (Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse package in keyValuesPackageChuncks)
                        {
                            response.sequenceId = sequenceId++;
                            getGroupDataResponse.keyValuePackage = package;
                            response.getGroupData = getGroupDataResponse;
                            _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                        }
                    }
                    break;
            }
            return _serializedResponse;
        }
    }
}
