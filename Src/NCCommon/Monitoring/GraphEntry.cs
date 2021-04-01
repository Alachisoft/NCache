using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public struct GraphEntry
    {
        public string CounterName;
        public string NodeIp;
        public List<object> Values;
    }
}
