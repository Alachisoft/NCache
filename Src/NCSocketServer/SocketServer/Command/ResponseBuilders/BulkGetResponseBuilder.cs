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
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of BulkGet command
    /// </summary>
    class BulkGetResponseBuilder 
    {
        public static IList<byte[]> BuildResponse(Hashtable getResult, int commandVersion, string RequestId, IList<byte[]> _serializedResponse, string intendedRecepient)
        {
            long requestId = Convert.ToInt64(RequestId);
            switch (commandVersion)
            {
                case 0: //Versions earlier than NCache 4.1 because all of them expect responses as one chunck
                {
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.BulkGetResponse bulkGetResponse = new Alachisoft.NCache.Common.Protobuf.BulkGetResponse();
                    response.requestId = requestId;
                    response.intendedRecipient = intendedRecepient;

                    bulkGetResponse.keyValuePackage = Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysValues(getResult, bulkGetResponse.keyValuePackage);

                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_BULK;
                    response.bulkGet = bulkGetResponse;
                    _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                break;
                case 1: //Verion 4.1 or later
                {
                    List<Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse> keyValuesPackageChuncks = Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeysValues(getResult);
                    int sequenceId = 1;
                    Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                    Alachisoft.NCache.Common.Protobuf.BulkGetResponse bulkGetResponse = new Alachisoft.NCache.Common.Protobuf.BulkGetResponse();
                    response.requestId = requestId;
                    response.numberOfChuncks = keyValuesPackageChuncks.Count;
                    response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_BULK;
                    foreach (Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse package in keyValuesPackageChuncks)
                    {
                        response.sequenceId = sequenceId++;
                        bulkGetResponse.keyValuePackage = package;
                        response.bulkGet = bulkGetResponse;
                        _serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    }
                }
                break;

            }
            return _serializedResponse;
        }
    }
}
