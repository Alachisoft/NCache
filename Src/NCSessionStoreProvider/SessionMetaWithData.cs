using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Web.SessionState;

namespace Alachisoft.NCache.Web.SessionStoreProvider
{
    [Serializable]
    public class SessionMetaWithData
    {
        private byte[] sessionData;
        private int timeout;
        private SessionInitializationActions actionKey;

        public byte[] SessionData
        {
            get { return sessionData; }
            set { sessionData = value; }
        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public SessionInitializationActions ActionKey
        {
            get { return actionKey; }
            set { actionKey = value; }
        }

    }
}
