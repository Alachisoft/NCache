

using System.Collections.Generic;

namespace Alachisoft.NCache.Web.SessionState
{
    public class NCacheSessionData
    {
        public IDictionary<string, byte[]> Items { get; set; } = new Dictionary<string, byte[]>();
    }
}
