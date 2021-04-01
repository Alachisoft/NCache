//Copyright(c) .NET Foundation.All rights reserved.


//Licensed under the Apache License, Version 2.0 (the "License"); you may not use
//these files except in compliance with the License.You may obtain a copy of the
//License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed
//under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
//CONDITIONS OF ANY KIND, either express or implied. See the License for the
//specific language governing permissions and limitations under the License.

using System.Threading.Tasks;
using Alachisoft.NCache.Web.SessionState.Configuration;
using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    public class SessionEstablisher
    {
        readonly HttpContext _context;
        readonly string _cookieValue;
        readonly NCacheSessionConfiguration _options;

        private SessionEstablisher(HttpContext context, string cookieValue, NCacheSessionConfiguration options)
        {
            _context = context;
            _cookieValue = cookieValue;
            _options = options;
            context.Response.OnStarting(OnStartingCallback, state: this);
        }

        public static SessionEstablisher CreateCallback(HttpContext context, string cookieValue, NCacheSessionConfiguration options)
        {
            return new SessionEstablisher(context, cookieValue, options);
        }

        private static Task OnStartingCallback(object state)
        {
            var establisher = (SessionEstablisher)state;
            establisher.SetCookie();
            return Task.FromResult(0);
        }
        public void SetCookie()
        {
            var cookieOptions = new CookieOptions
            {
                Domain = _options.SessionOptions.CookieDomain,
                HttpOnly = _options.SessionOptions.CookieHttpOnly,
                Path = _options.SessionOptions.CookiePath ?? "/",
            };

            if (_options.SessionOptions.CookieSecure == CookieSecurePolicy.SameAsRequest)
            {
                cookieOptions.Secure = _context.Request.IsHttps;
            }
            else
            {
                cookieOptions.Secure = _options.SessionOptions.CookieSecure == CookieSecurePolicy.Always;
            }
            
            _context.Response.Cookies.Append(_options.SessionOptions.CookieName, _cookieValue, cookieOptions);

            _context.Response.Headers["Cache-Control"] = "no-cache";
            _context.Response.Headers["Pragma"] = "no-cache";
            _context.Response.Headers["Expires"] = "-1";
        }
    }
}
