using Alachisoft.NCache.Common.Protobuf;
using System.IO;

namespace Alachisoft.NCache.Common.ResponseSerialization
{
    public class ResponseOptions
    {
        public Response.Type ResponseType { get; set; }
        public object Response { get; set; }
        public bool WriteRequestIdInResponse { get; set; } = true;
        public ManagementResponse ManagementResponse { get;  set; }
        public System.Exception Exception { get;  set; }
        public long RequestId { get;  set; }
        public int CommandId { get;  set; }
        public Stream Stream { get;  set; }
        public byte[] GetBytes { get;  set; }
    }
}
