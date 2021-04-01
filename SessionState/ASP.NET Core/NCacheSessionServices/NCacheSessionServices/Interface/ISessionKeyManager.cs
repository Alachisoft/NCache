using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Interface
{
    public interface ISessionKeyManager
    {
        string GetSessionKey(HttpContext context, out bool isNew);
        void ApplySessionKey(HttpContext context, string key);
    }
}
