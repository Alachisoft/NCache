using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public interface ICancellableRequest
    {
        bool IsCancelled { get; }
        bool HasTimedout { get; }
        bool Cancel();
    }
}
