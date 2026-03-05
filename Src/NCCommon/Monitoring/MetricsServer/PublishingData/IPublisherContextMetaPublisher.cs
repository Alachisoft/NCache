using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.MetricsServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
   public interface IPublisherContextMetaPublisher
    {
        void PublishMetadata(IMetricsTransporter transporter);
    }
}
