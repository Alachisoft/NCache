using Alachisoft.NCache.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer.Util
{
    internal class CommandsUtil
    {
        public static void PopulateClientIdInContext(ref OperationContext operationContext, IPAddress clientAddress)
        {
            if (operationContext == null)
                operationContext = new OperationContext();
            operationContext.Add(OperationContextFieldName.ClientIpAddress, clientAddress);
        }
    }
}
