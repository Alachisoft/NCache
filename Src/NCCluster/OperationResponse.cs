using System;
using System.Collections;
using System.IO;

namespace Alachisoft.NGroups
{
    public class OperationResponse
    {
        public Array UserPayload;
        public object SerializablePayload;
        public Stream SerilizationStream;
    }
}