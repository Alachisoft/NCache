using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management
{
    public class MetricsTransporterFactory : IMetricsTransporterFactory
    {
      
        public IMetricsTransporter Create()
        {
            return new MetricsTransporter();
        }

        public void Initialize(string endPoint)
        {
            
        }
    }

    
}
