using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class NCacheSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }
}
