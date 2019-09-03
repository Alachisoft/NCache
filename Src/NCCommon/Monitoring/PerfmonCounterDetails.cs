using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public struct PerfmonCounterDetails
    {
        public string Category { get; set; }
        public string Instance { get; set; }
        public string Counter { get; set; }
        public double Value { get; set; }
            
    }
}
