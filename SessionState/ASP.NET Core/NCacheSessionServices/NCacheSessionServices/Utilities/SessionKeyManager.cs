using Alachisoft.NCache.Web.SessionState.Configuration;
using Alachisoft.NCache.Web.SessionState.Interface;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class SessionKeyManager : ISessionKeyManager
    {
        readonly IOptions<NCacheSessionConfiguration> _options;
        readonly IDataProtector _dataProtector;
        readonly ISessionKeyGenerator _generator;

        public SessionKeyManager(IOptions<NCacheSessionConfiguration> options, IDataProtectionProvider dataProtectionProvider, ISessionKeyGenerator generator)
        {
            _options = options;
            _generator = generator;
            _dataProtector = dataProtectionProvider.CreateProtector(nameof(SessionKeyManager));
        }
        
        public virtual string GetSessionKey(HttpContext context, out bool isNew)
        {
            isNew = false;
            string cookieKey = _options.Value.SessionOptions.CookieName;
            string sessionId = CookieProtection.Unprotect(_dataProtector, context.Request.Cookies[cookieKey]);
            if (string.IsNullOrEmpty(sessionId))
            {
                isNew = true;
                sessionId = _generator.Create();
            }
            //SessionEstablisher.CreateCallback(context, CookieProtection.Protect(_dataProtector, sessionId), _options.Value);
            return sessionId;
        }

        public virtual void ApplySessionKey(HttpContext context, string key)
        {
            var cookieOptions = new CookieOptions
            {
                Domain = _options.Value.SessionOptions.CookieDomain,
                HttpOnly = _options.Value.SessionOptions.CookieHttpOnly,
                Path = _options.Value.SessionOptions.CookiePath ?? "/",
            };

            if (_options.Value.SessionOptions.CookieSecure == CookieSecurePolicy.SameAsRequest)
            {
                cookieOptions.Secure = context.Request.IsHttps;
            }
            else
            {
                cookieOptions.Secure = _options.Value.SessionOptions.CookieSecure == CookieSecurePolicy.Always;
            }

            context.Response.Cookies.Append(_options.Value.SessionOptions.CookieName, CookieProtection.Protect(_dataProtector, key), cookieOptions);

            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "-1";
            //SessionEstablisher.CreateCallback(context, CookieProtection.Protect(_dataProtector, key), _options.Value);
            //SessionEstablisher.CreateCallback(context, CookieProtection.Protect(_dataProtector, key), _options.Value).SetCookie();
        }
    }
}
