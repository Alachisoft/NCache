using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer;

namespace Alachisoft.NCache.Common.Monitoring
{
    interface IClusterHealthMonitorableEntity:IMonitorableEntity
    {
        ClusterHealthData Data { get; }
    }
}
