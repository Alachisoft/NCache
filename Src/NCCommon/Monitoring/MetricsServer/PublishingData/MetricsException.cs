using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    class MetricsException
    {
        public int ErrorCode { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime LastLogTime { get; set; }
        public Exception Exception { get; set; }

        public bool CompareAndUpdateifRequired()
        {
            if ((DateTime.UtcNow - LastLogTime).TotalSeconds >= 60)
                return true;
            return false;
        }
    }
}
