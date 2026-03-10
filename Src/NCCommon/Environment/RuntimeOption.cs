using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.RuntimeEnvironment
{
    public class RuntimeOption
    {
        public string Location { get; set; }
        public string Command { get; set; }
        public string Argument { get; set; }
        public string Name { get; set; }
        public string CacheName { get; set; }
    }
}
