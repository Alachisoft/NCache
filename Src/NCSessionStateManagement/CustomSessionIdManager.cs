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

using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;


namespace Alachisoft.NCache.Web.SessionStateManagement

{
    /// <summary>
    /// 
    /// </summary>
    public class CustomSessionIdManager : SessionIDManager
    {
        private string _sid;

        public CustomSessionIdManager()
        {
            NCacheSessionStateSettings settings = NCacheSessionStateConfigReader.LoadSessionLocationSettings();
            if (settings != null)
            {
                foreach (DictionaryEntry entry in settings.PrimaryCache)
                {
                    this._sid = (string)entry.Key;
                    break;
                }
            }
        }

        public override string CreateSessionID(HttpContext context)
        {
            return this._sid + base.CreateSessionID(context);
        }

        public override bool Validate(string id)
        {
            string sessionId = id;
            if (id.Length > 24)
            {
                sessionId = id.Substring(id.Length - 24);
            }
            return base.Validate(sessionId);
        }
    }
}
