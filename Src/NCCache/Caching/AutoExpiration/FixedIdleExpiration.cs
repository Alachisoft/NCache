using System;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// Fixed time expiry and Idle Time to live based derivative of ExpirationHint. 
    /// Combines the effect of both.
    /// </summary>
    [Serializable]
    public class FixedIdleExpiration : AggregateExpirationHint
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="idleTime">sliding expiration hint</param>
        /// <param name="absoluteTime">fixed expiration hint</param>
        public FixedIdleExpiration(TimeSpan idleTime, DateTime absoluteTime):base(new FixedExpiration(absoluteTime), new IdleExpiration(idleTime))
        {
            _hintType = ExpirationHintType.FixedIdleExpiration;
        }

        public FixedIdleExpiration()         
        {
            _hintType = ExpirationHintType.FixedIdleExpiration;
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}