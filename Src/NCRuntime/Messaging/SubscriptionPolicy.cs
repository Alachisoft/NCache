using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// Defines the policy used in case of Durable subscription.
    /// </summary>
    internal enum SubscriptionPolicy
    {
        /// <summary>
        /// Shared subscription policy is for multiple subscribers on a single subscription. In this case messages are sent to any of the topic subscribers. This policy provides better load division over clients subscribing to a subscription.
        /// </summary>
        Shared,

        /// <summary>
        /// Exclusive subscription policy is for a single subscriber on a single subscription. In this case messages are recieved by the single subscriber only.
        /// </summary>
        Exclusive
    }
}
