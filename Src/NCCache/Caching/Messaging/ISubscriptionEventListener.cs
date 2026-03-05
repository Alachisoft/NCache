using Alachisoft.NCache.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.Messaging
{
    interface ISubscriptionEventListener
    {

        void OnSubscriptionInstanceRemoved(SubscriptionIdentifier[] keys,string clientId);
        void OnSubscritionRefresh(SubscriptionIdentifier subscription);
    }
}
