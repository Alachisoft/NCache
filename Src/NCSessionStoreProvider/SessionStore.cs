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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace Alachisoft.NCache.Web.SessionState
{
    public class SessionStore: SessionStoreBase
    {
        private static EventHandler s_onAppDomainUnload;
        public override object CreateNewStoreData(IAspEnvironmentContext context, int timeOut)
        {
            SessionStateStoreData data = new SessionStateStoreData(new SessionStateItemCollection(),
                                                                    SessionStateUtility.GetSessionStaticObjects(context.Unwrap() as HttpContext),
                                                                    timeOut);
            if (_detailedLogs) LogDebug("New data object created to be used for current request", null);

            if (_isLocationAffinityEnabled)
                UpdateCookies(context);

            return data;
        }

        protected override object CreateEmptySession(IAspEnvironmentContext context, int sessionTimeout)
        {
            ISessionStateItemCollection dummyItems = new SessionStateItemCollection();
            dummyItems["session-locked"] = "true";

            return new SessionStateStoreData(dummyItems, SessionStateUtility.GetSessionStaticObjects(context.Unwrap() as HttpContext), sessionTimeout);

        }

        protected override object DeserializeSession(byte[] buffer,int timeout)
        {
            if (buffer != null)
            {
                return SessionSerializationUtil.Deserialize(buffer);
            }
            else
                return new SessionStateStoreData(new SessionStateItemCollection(), null, timeout);
        }

        protected override byte[] SerializeSession(object sessionData)
        {
            if (sessionData != null)
                return SessionSerializationUtil.Serialize(sessionData as SessionStateStoreData);

            return null;
        }

        private void OnAppDomainUnload(object unusedObject, EventArgs unusedEventArgs)
        {
            System.Threading.Thread.GetDomain().DomainUnload -= s_onAppDomainUnload;
            DisposeCache();
        }

        protected override void OnCacheInitialize()
        {
           s_onAppDomainUnload = new EventHandler(OnAppDomainUnload);
           System.Threading.Thread.GetDomain().DomainUnload += s_onAppDomainUnload;
        }


        public virtual bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }
    }
}
