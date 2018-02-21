// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Configuration
{
    public class NCacheSessionConfiguration
    {
        /// <summary>
        /// Name of the session application, used to differenciate between sessions if 
        /// multiple applications are using the same cache
        /// </summary>
        public string SessionAppId { get; set; }

        /// <summary>
        /// Session cookie related options
        /// </summary>
        public SessionOptions SessionOptions { get; set; } = new SessionOptions
        {
            CookieName = ".NCache.AspNetCore.Session"
        };

        /// <summary>
        /// Mandatory name of the cache
        /// </summary>
        public string CacheName { get; set; }

        /// <summary>
        /// A flag which the user can put in the HTTPContext before fetching the session data 
        /// of current session to signify that the current session is read only and will not be 
        /// committed even if changed
        /// </summary>
        public string ReadOnlyFlag { get; set; } = ".NCache.AspNetCore.IsReadOnly";

        /// <summary>
        /// Enable locking on sessions
        /// </summary>
        public bool? EnableSessionLocking { get; set; }

        /// <summary>
        /// Should any exceptions that occured in the session state module be reported to event log
        /// </summary>
        public bool? WriteExceptionsToEventLog { get; set; }

        /// <summary>
        /// Should NCache session logs be enabled
        /// </summary>
        public bool? EnableLogs { get; set; }

        /// <summary>
        /// Should NCache detailed session logs be enabled
        /// </summary>
        public bool? EnableDetailLogs { get; set; }

        /// <summary>
        /// Should exceptions be thrown to the application if they occur
        /// </summary>
        public bool? ExceptionsEnabled { get; set; }

        /// <summary>
        /// Enable the location affinity feature of NCache Sessions
        /// </summary>
        public bool? EnableLocationAffinity { get; set; }

        /// <summary>
        /// Mapping of caches for location affinity
        /// </summary>
        public CacheAffinity[] AffinityMapping { get; set; }

        /// <summary>
        /// How much retries should be made for a cache operation on which an exception has occured
        /// </summary>
        public int? OperationRetry { get; set; }

        /// <summary>
        /// After how much interval each retry should be made for a cache operation on which an exception has occured
        /// </summary>
        public int? OperationRetryInterval { get; set; }
        public int? InprocDelay { get; set; }

        /// <summary>
        /// Time after which a new request forcefully releases the lock which is being held by an older request
        /// </summary>
        public int RequestTimeout { get; set; } = 120;


    }

    //
    // Summary:
    //     Represents the session state options for the application.
    public class SessionOptions
    {
        //
        // Summary:
        // Domain used to create the cookie. Is not provided by default.
        public string CookieDomain { get; set; }
        //
        // Summary:
        //     Determines if the browser should allow the cookie to be accessed by client-side
        //     JavaScript. The default is true, which means the cookie will only be passed to
        //     HTTP requests and is not made available to script on the page.
        public bool CookieHttpOnly { get; set; }
        //
        // Summary:
        //     Cookie name used to persist the session ID. Defaults to Microsoft.AspNetCore.Session.SessionDefaults.CookieName.
        public string CookieName { get; set; }
        //
        // Summary:
        //     Determines the path used to create the cookie. Defaults to Microsoft.AspNetCore.Session.SessionDefaults.CookiePath.
        public string CookiePath { get; set; }
        //
        // Summary:
        //     Determines if the cookie should only be transmitted on HTTPS requests.
        public CookieSecurePolicy CookieSecure { get; set; }
        //
        // Summary:
        //     The IdleTimeout indicates how long the session can be idle before its contents
        //     are abandoned. Each session access resets the timeout. Note this only applies
        //     to the content of the session, not the cookie.
        public long IdleTimeout { get; set; }
    }
}
