using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    class SystemMetricsMonitor
    {
        private MetricsPublisher _metricPublisher;
        private List<string> systemMetrics = new List<string>();
        private CacheRuntimeContext _context;
        private const string literal = " _usage";

        public SystemMetricsMonitor(MetricsPublisher metricPublisher, CacheRuntimeContext context)
        {
            _context = context;
            _metricPublisher = metricPublisher;
            InitializeMonitor();
        }

        private void InitializeMonitor()
        {
            systemMetrics.Add(ResourceName.CPU+ literal);
            systemMetrics.Add(ResourceName.MEMORY+ literal); 
            systemMetrics.Add(ResourceName.NETWORK+ literal);
            InitializeMerics();
        }

        private void InitializeMerics()
        {
            foreach(var item in systemMetrics)
            {
                ICollector collector = null;
                switch (item)
                {
                    case ResourceName.MEMORY + literal:
                        collector = new MemoryUsageCollector(_context);
                        break;

                    case ResourceName.NETWORK + literal:
                        collector = new NetworkUsageCollector(_context);
                        break;

                    case ResourceName.CPU + literal:
                        collector = new CPUUsageCollector();
                        break;
                }
                           
                _metricPublisher.SystemMetricsDictionary.Add(item, collector);

            }
        }
    }
}
