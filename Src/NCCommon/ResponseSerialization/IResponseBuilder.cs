using Alachisoft.NCache.Common.Protobuf;
using System.Collections;

namespace Alachisoft.NCache.Common.ResponseSerialization
{
    public interface IResponseBuilder
    {
        IList BuildResponse(ResponseOptions responseOptions);
        IList BuildExceptionResponse(ResponseOptions responseOptions);
        Protobuf.Response DeserializeResponse(ResponseOptions responseOptions);
        
    }

    public interface IManagementResponseBuilder
    {
        IList BuildManagementResponse(ResponseOptions responseOptions);
    }
}
