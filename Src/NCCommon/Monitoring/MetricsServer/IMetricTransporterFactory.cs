using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring.MetricsServer
{
    public interface IMetricsTransporterFactory
    {
        void Initialize(string endPoint);
        IMetricsTransporter Create();
    }
}
