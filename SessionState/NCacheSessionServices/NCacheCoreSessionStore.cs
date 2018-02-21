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

using Alachisoft.NCache.Web.SessionState.Serialization;

namespace Alachisoft.NCache.Web.SessionState
{
    internal class NCacheCoreSessionStore: SessionStoreBase
    {
        protected override object DeserializeSession(byte[] buffer, int timeout)
        {
            return SessionSerializer.Deserialize(buffer);
        }

        protected override byte[] SerializeSession(object sessionData)
        {
            NCacheSessionData session = sessionData as NCacheSessionData;
            return SessionSerializer.Serialize(session);
        }

        public override object CreateNewStoreData(IAspEnvironmentContext context, int timeOut)
        {
            return new NCacheSessionData();
        }

        protected override object CreateEmptySession(IAspEnvironmentContext context, int sessionTimeout)
        {
            var session = new NCacheSessionData();
            session.Items.Add(NCacheStatics.EmptySessionFlag, null);
            return session;
        }
    }
}
