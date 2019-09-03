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
