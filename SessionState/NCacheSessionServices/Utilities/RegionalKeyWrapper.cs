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

using System.Collections;
using Alachisoft.NCache.Web.SessionState.Interface;
using Alachisoft.NCache.Web.SessionStateManagement;
using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    internal class RegionalKeyWrapper : ISessionKeyManager
    {
        NCacheSessionStateSettings _affinitySettings;
        readonly ISessionKeyManager _keyManager;
        readonly string _primaryPrefix;

        public RegionalKeyWrapper(NCacheSessionStateSettings affinitySettings, ISessionKeyManager keyManager)
        {
            _affinitySettings = affinitySettings;
            _keyManager = keyManager;
            foreach (DictionaryEntry entry in _affinitySettings.PrimaryCache)
            {
                _primaryPrefix = (string)entry.Key;
            }
        }

        public string GetSessionKey(HttpContext context, out bool isNew)
        {
            var key = _keyManager.GetSessionKey(context, out isNew);
            if (isNew)
                key = _primaryPrefix + key;
            return key;

        }

        public void ApplySessionKey(HttpContext context, string key)
        {
            _keyManager.ApplySessionKey(context, key);
        }
    }
}
