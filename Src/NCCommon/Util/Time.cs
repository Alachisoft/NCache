using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    public class Time
    {
        public static long MaxUniversalTicks = DateTime.MaxValue.ToUniversalTime().Ticks;

        public static DateTime ReferenceTime = new System.DateTime(2016, 1, 1, 0, 0, 0).ToUniversalTime();
    }
}
