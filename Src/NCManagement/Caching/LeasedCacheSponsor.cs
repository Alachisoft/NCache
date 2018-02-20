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

using System;
using System.Runtime.Remoting.Lifetime;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Sponsor used to extend lifetime of cache.
    /// </summary>
    class LeasedCacheSponsor : ISponsor
    {
        /// <summary>
        /// Requests a sponsoring client to renew the lease for the specified object.
        /// </summary>
        /// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
        /// <returns>The additional lease time for the specified object.</returns>
        public TimeSpan Renewal(ILease lease)
        {
            return TimeSpan.FromMinutes(10);
        }
    }
}
