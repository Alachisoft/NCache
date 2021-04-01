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
using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Configuration;
using Alachisoft.NCache.Web.SessionStateManagement;

namespace Alachisoft.NCache.Web.SessionState
{
    public class NSessionStoreProvider : SessionStateStoreProviderBase
    {
        SessionStore _store;

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            _store = new SessionStore();
            var appName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            Configuration cfg = WebConfigurationManager.OpenWebConfiguration(appName);
            SessionStateSection sessionConfig = (SessionStateSection) cfg.GetSection("system.web/sessionState");
            config["defaultSessionTimeout"] = sessionConfig.Timeout.Minutes.ToString();
            _store.Initialize(name, config);
        }

        public override void Dispose()
        {
            _store.Dispose();
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return _store.SetItemExpireCallback(expireCallback);
        }

        public override void InitializeRequest(HttpContext context)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.InitializeRequest(newContext);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked,
            out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            var newContext = WrapContext(context);
            try
            {
                SessionInitializationActions newActions;
                var data = _store.GetItem(newContext, id, out locked, out lockAge, out lockId, out newActions);
                actions = ToSessionStateActions(newActions);

                return (SessionStateStoreData) data;
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked,
            out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            SessionStateStoreData data;
            SessionInitializationActions newActions;
            var newContext = WrapContext(context);
            try
            {
                data = _store.GetItemExclusive(newContext, id, out locked, out lockAge, out lockId, out newActions) as
                    SessionStateStoreData;
                actions = ToSessionStateActions(newActions);
                return data;
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.ReleaseItemExclusive(newContext, id, lockId);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item,
            object lockId, bool newItem)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.SetAndReleaseItemExclusive(newContext, id, item, lockId, newItem, item.Timeout);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.RemoveItem(newContext, id, lockId);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.ResetItemTimeout(newContext, id);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            var newContext = WrapContext(context);
            try
            {
                var data = _store.CreateNewStoreData(newContext, timeout) as SessionStateStoreData;
                return data;
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.CreateUninitializedItem(newContext, id, timeout);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        public override void EndRequest(HttpContext context)
        {
            var newContext = WrapContext(context);
            try
            {
                _store.EndRequest(newContext);
            }
            finally
            {
                newContext.FinalizeContext();
            }
        }

        private ASPEnvironmentContext WrapContext(HttpContext context)
        {
            return new ASPEnvironmentContext(context);
        }

        private SessionStateActions ToSessionStateActions(SessionInitializationActions actions)
        {
            return (SessionStateActions) ((int) actions);
        }
    }
}
