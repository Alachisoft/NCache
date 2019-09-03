using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    internal class LocationIdContainer
    {
        public string LocationId { get; set; }
        public HttpContext Context { get; set; }

        public Task OnResponseStarted(object state)
        {
            if (LocationId != null && Context != null && !Context.Response.HasStarted)
            {
                Context.Response.Cookies.Append(NCacheStatics.LocationIdentifier, LocationId);
            }
            return Task.FromResult(0);
        }
    }
}
