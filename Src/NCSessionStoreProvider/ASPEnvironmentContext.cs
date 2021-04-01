//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System.Web;
using Alachisoft.NCache.Web.SessionState;

namespace Alachisoft.NCache.Web.SessionState
{
    public class ASPEnvironmentContext: IAspEnvironmentContext
    {
        private const string LocationIdentifier = "New_Location";

        HttpContext _context;
        string _locationCookie = null;

        public ASPEnvironmentContext(HttpContext context)
        {
            _context = context;

            if (_context.Request.Cookies[LocationIdentifier] != null)
                _locationCookie = _context.Request.Cookies[LocationIdentifier].Value;

            // Bugfix 12852 - Multi-regional sessions do not work as expected even when session locking is disabled in new version i.e. Merger of asp.net and .net core sessions
            // [Umer] Contains, Get and Indexer when used on HttpCookieCollection will return true irrespective
            // of if item exists or not, and if the item is not present they create an empty cookie so dont use it.
            var responseCookieEnumerator = _context.Response.Cookies.Keys.GetEnumerator();
            while (responseCookieEnumerator.MoveNext())
            {
                if (LocationIdentifier.Equals(responseCookieEnumerator.Current))
                {
                    if (!string.IsNullOrEmpty(_context.Response.Cookies[LocationIdentifier].Value))
                    {
                        _locationCookie = _context.Response.Cookies[LocationIdentifier].Value;
                        break;
                    }
                    else
                    {
                        _context.Response.Cookies.Remove(LocationIdentifier);
                    }
                }
            }

            //if (_context.Response.Cookies[LocationIdentifier] != null)
            //    _locationCookie = _context.Response.Cookies[LocationIdentifier].Value;
        }

        public object Unwrap()
        {
            return _context;
        }

        public object GetItem(object key)
        {
            return _context.Items[key];
        }

        public void StoreItem(object key, object value)
        {
            _context.Items[key] = value;
        }

        public bool ContainsItem(object key)
        {
            return _context.Items.Contains(key);
        }

        public void RemoveItem(object key)
        {
            _context.Items.Remove(key);
        }

        public object this[object key]
        {
            get { return _context.Items[key]; }
            set { _context.Items[key] = value; }
        }

        public string GetLocationCookie()
        {
            return _locationCookie;
        }

        public void SetLocationCookie(string cookieValue)
        {
            _locationCookie = cookieValue;
        }

        public void RemoveLocationCookie()
        {
            _locationCookie = null;
        }

        public void FinalizeContext()
        {
            if (_locationCookie != null)
                _context.Response.Cookies.Set(new HttpCookie(LocationIdentifier, _locationCookie));
        }
    }
}
