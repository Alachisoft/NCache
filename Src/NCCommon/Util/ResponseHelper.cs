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

using System.Collections.Generic;
using System.Text;
using System.IO;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;

using Alachisoft.NCache.Runtime.Exceptions;
using Exception = System.Exception;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Common.Util
{
    public class ResponseHelper
    {
        public static byte[] SerializeResponse(Alachisoft.NCache.Common.Protobuf.Response command)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Response>(stream, command);
                int messageLen = (int)stream.Length - size.Length;

                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);

                byte[] result = stream.ToArray();
                stream.Close();

                return result;
            }
        }
        public static byte[] SerializeResponse(Alachisoft.NCache.Common.Protobuf.ManagementResponse command)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.ManagementResponse>(stream, command);
                int messageLen = (int)stream.Length - size.Length;

                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);

                byte[] result = stream.ToArray();
                stream.Close();

                return result;
            }
        }

        private static Protobuf.Exception GetExceptionResponse(Exception exc)
        {
            Alachisoft.NCache.Common.Protobuf.Exception ex = new Alachisoft.NCache.Common.Protobuf.Exception();
            ex.message = exc.Message;
            ex.exception = exc.ToString();

            if (exc is OperationFailedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.OPERATIONFAILED;
            else if (exc is Runtime.Exceptions.AggregateException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.AGGREGATE;
            else if (exc is ConfigurationException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATION;
            else if (exc is OperationNotSupportedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.NOTSUPPORTED;
            else if (exc is TypeIndexNotDefined)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.TYPE_INDEX_NOT_FOUND;
            else if (exc is AttributeIndexNotDefined)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.ATTRIBUTE_INDEX_NOT_FOUND;
            else if (exc is StateTransferInProgressException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STATE_TRANSFER_EXCEPTION;
            else
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;
            return ex;
        }

		public static byte[] SerializeExceptionResponse(Exception exc, long requestId)
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
            else if (exc is OperationNotSupportedException)
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.NOTSUPPORTED;

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
            response.exception = ex;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.EXCEPTION;

            return SerializeResponse(response);
        }

        public static byte[] SerializeManagementExceptionResponse(Exception exc, long requestId)
        {
            Alachisoft.NCache.Common.Protobuf.Exception ex = GetExceptionResponse(exc);

            Alachisoft.NCache.Common.Protobuf.ManagementResponse response  = new ManagementResponse();
            response.requestId = requestId;
            response.exception = ex;

            return SerializeResponse(response);
        }
    }
}
