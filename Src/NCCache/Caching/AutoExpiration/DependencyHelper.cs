// Copyright (c) 2015 Alachisoft
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
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Data;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class DependencyHelper
    {
        public static void GetActualCacheDependency(ExpirationHint hint, ref DateTime absoluteExpiration, ref TimeSpan slidingExpiration)
        {
            if (hint != null)
            {
                if (hint is FixedExpiration)
                {
                    absoluteExpiration = ((FixedExpiration)hint).AbsoluteTime;
                }
                else if (hint is IdleExpiration)
                {
                    slidingExpiration = ((IdleExpiration)hint).SlidingTime;
                }
            }
        }

        public static ExpirationHint GetExpirationHint(DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            ExpirationHint hint = null;

            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration) && TimeSpan.Zero.Equals(slidingExpiration))
                return null;

            if (DateTime.MaxValue.ToUniversalTime().Equals(absoluteExpiration))
                hint = new IdleExpiration(slidingExpiration);
            else
            {
                absoluteExpiration = absoluteExpiration.ToUniversalTime();
                hint = new FixedExpiration(absoluteExpiration);
            }

            return hint;
        }
    }
}
