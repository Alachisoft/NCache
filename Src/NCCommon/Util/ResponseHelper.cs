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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;
using Exception = System.Exception;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Exceptions;
#else
using Alachisoft.NCache.Runtime.Exceptions;
#endif 
#if JAVA
using Runtime = Alachisoft.TayzGrid.Runtime;
#else
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;
#endif
namespace Alachisoft.NCache.Common.Util
{
    public class ResponseHelper
    {
        public static Response DeserializeResponse(byte[] bytes)
        {
            Response response = null;
            byte[] length = new byte[10];
            Array.Copy(bytes, 0, length, 0, length.Length);
            int size = Convert.ToInt32(UTF8Encoding.UTF8.GetString(length));
            byte[] responseBytes = new byte[size];
            Array.Copy(bytes, 10, responseBytes, 0, size);
            using (MemoryStream ms = new MemoryStream(responseBytes))
            {
                response = Serializer.Deserialize<Response>(ms);
            }
            return response;
        }

        public static IList SerializeResponse(Alachisoft.NCache.Common.Protobuf.Response command)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                //TODO
                
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Response>(stream, command);

                int messageLen = (int)stream.Length - size.Length;
                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);

                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public static ClusteredArrayList SerializeResponse(Alachisoft.NCache.Common.Protobuf.ManagementResponse command)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                //TODO
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);
                
                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.ManagementResponse>(stream, command);
                int messageLen = (int)stream.Length - size.Length;

                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);
                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

       
		public static IList SerializeExceptionResponse(Exception exc, long requestId, int commandID)

        {
            Alachisoft.NCache.Common.Protobuf.Exception ex = new Alachisoft.NCache.Common.Protobuf.Exception();
            ex.message = exc.Message;
            ex.exception = exc.ToString();
            if (exc is InvalidReaderException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.INVALID_READER_EXCEPTION;
            else if (exc is OperationFailedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.OPERATIONFAILED;
            else if (exc is Runtime.Exceptions.AggregateException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.AGGREGATE;
            else if (exc is ConfigurationException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATION;
          
            else if (exc is VersionException)
            {
                VersionException tempEx = (VersionException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATON_EXCEPTION;
                ex.errorCode = tempEx.ErrorCode;
            }
            else if (exc is OperationNotSupportedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.NOTSUPPORTED;
            else if (exc is StreamAlreadyLockedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_ALREADY_LOCKED;
            else if (exc is StreamCloseException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_CLOSED;
            else if (exc is StreamInvalidLockException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_INVALID_LOCK;
            else if (exc is StreamNotFoundException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_NOT_FOUND;
            else if (exc is StreamException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_EXC;
            else if (exc is TypeIndexNotDefined)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.TYPE_INDEX_NOT_FOUND;
            else if (exc is AttributeIndexNotDefined)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.ATTRIBUTE_INDEX_NOT_FOUND;
            else if (exc is StateTransferInProgressException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STATE_TRANSFER_EXCEPTION;
            else 
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            response.requestId = requestId;

		    response.commandID = commandID;
            response.exception = ex;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.EXCEPTION;

            return SerializeResponse(response);
        }
    }
}
