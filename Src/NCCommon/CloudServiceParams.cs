using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common
{
    public class CloudServiceParams
    {
        public CloudServiceMethods MethodType { get; set; }
    }
    public enum CloudServiceMethods
    {
        GetSystemStats,
        GetNCacheStats,
        GetResourceUtilizationStats,
        MonitorServices,
        ExecuteScript
     
    }
}
