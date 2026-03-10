using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Cache.Caching.Statistics
{
    [Serializable]
    public class CumulativeCounters
    {
        public CumulativeCounters() 
        {
            //Default values are -1 to signify that counters have not yet been successfully fetched
            RunningCacheCount = -1;
            CumulativeClientCount = -1;
            CumulativeReqRate = -1;
            CumulativeCacheSize = -1;
        }


        public int RunningCacheCount { get; set; }
        public int CumulativeClientCount { get; set; }
        public int CumulativeReqRate { get; set; }
        public long CumulativeCacheSize { get; set; }

    }
}
