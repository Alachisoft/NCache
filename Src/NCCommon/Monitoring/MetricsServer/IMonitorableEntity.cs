using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface IMonitorableEntity
    {
        MonitoringEntityType GetEntityType { get; }
        bool IsPrimary { get; }
    }

    

}
