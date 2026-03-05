using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
  public enum ClusterHealthStatus
    {
            Running,
            Stopped,
            InStateTransfer,
            Partial
    }
}
