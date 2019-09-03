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
