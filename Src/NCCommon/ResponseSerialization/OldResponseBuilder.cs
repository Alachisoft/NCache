using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Common.Protobuf;
using System;
using System.Collections;
using System.Text;
using System.IO;

namespace Alachisoft.NCache.Common.ResponseSerialization
{
    class OldResponseBuilder : ResponseBaseBuilder, IResponseBuilder, IManagementResponseBuilder
    {
        public IList BuildResponse(ResponseOptions responseOptions)
        {


            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize(stream, responseOptions.Response);

                int messageLen = (int)stream.Length - size.Length;
                size = Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);

                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public IList BuildManagementResponse(ResponseOptions responseOptions)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize(stream, responseOptions.ManagementResponse);
                int messageLen = (int)stream.Length - size.Length;

                size = Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);
                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public IList BuildExceptionResponse(ResponseOptions responseOptions)
        {
            Response response = GetExceptionResponse(responseOptions.Exception, responseOptions.RequestId, responseOptions.CommandId);

            var responseBuilder = new ResponseOptions()
            {
                Response = response
            };

            return BuildResponse(responseBuilder);
        }

        public Response DeserializeResponse(ResponseOptions responseOptions)
        {
            byte[] bytes = responseOptions.GetBytes;
            Response response = null;
            byte[] length = new byte[10];
            Array.Copy(bytes, 0, length, 0, length.Length);
            int size = Convert.ToInt32(Encoding.UTF8.GetString(length));
            byte[] responseBytes = new byte[size];
            Array.Copy(bytes, 10, responseBytes, 0, size);
            using (MemoryStream ms = new MemoryStream(responseBytes))
            {
                response = Serializer.Deserialize<Response>(ms);
            }
            return response;
        }
       
    }
}
