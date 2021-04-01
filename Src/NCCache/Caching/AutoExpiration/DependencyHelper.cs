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
using System.Text;
using System.Collections;
using System.Data;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class DependencyHelper
    {
        
        private static long TICKS = 10000000;
      

        internal static ExpirationHint GetExpirationHint(PoolManager poolManager, object dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            ExpirationHint hint = GetExpirationHint(poolManager, absoluteExpiration, slidingExpiration);

            if (hint == null)
                return GetExpirationHint(poolManager);

            ExpirationHint hint2 = GetExpirationHint(poolManager);

            if (hint2 == null)
                return hint;


            AggregateExpirationHint aggregateHint = null;

            if (hint2 is AggregateExpirationHint)
            {
                aggregateHint = hint2 as AggregateExpirationHint;
                aggregateHint.Add(hint);

                return aggregateHint;
            }

            aggregateHint = AggregateExpirationHint.Create(poolManager);
            aggregateHint.Add(hint);
            aggregateHint.Add(hint2);

            return aggregateHint;
        }

        internal static ExpirationHint GetExpirationHint(PoolManager poolManager)
        {
            AggregateExpirationHint aggregateHint = AggregateExpirationHint.Create(poolManager);
            return GetExpirationHint(poolManager, aggregateHint);
        }

        private static ExpirationHint GetExpirationHint(PoolManager poolManager, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            ExpirationHint hint = null;

            if (ExpirationConstants.AbsoluteNoneExpiration.ToUniversalTime().Equals(absoluteExpiration) && ExpirationConstants.SlidingNoneExpiration.Equals(slidingExpiration))
                return null;

            if (!ExpirationConstants.AbsoluteNoneExpiration.ToUniversalTime().Equals(absoluteExpiration.ToUniversalTime()))
            {
                absoluteExpiration = absoluteExpiration.ToUniversalTime();
                hint = FixedExpiration.Create(poolManager, absoluteExpiration);
            }
            else if (!ExpirationConstants.SlidingNoneExpiration.Equals(slidingExpiration))

            {
                hint = IdleExpiration.Create(poolManager, slidingExpiration);
            }

            return hint;
        }

        private static ExpirationHint GetExpirationHint(PoolManager poolManager, AggregateExpirationHint aggregateHint)
        {
            return null;
        }
    }
}
