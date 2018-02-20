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
using System.Collections.Generic;
using System.Text;
#if JAVA
using Alachisoft.TayzGrid.Runtime.Dependencies;
#else
using Alachisoft.NCache.Runtime.Dependencies;
#endif

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Caching
#else
namespace Alachisoft.NCache.Runtime.Caching
#endif
{
    /// <remark>
    /// This Feature is Not Available in Express
    /// </remark>
    public class CacheItemAttributes 
    {
        /// <summary> Dependency for the object.</summary>
        private CacheDependency _d;

        /// <summary> Absolute expiration for the object. </summary>
        private DateTime _absoluteExpiration = DateTime.MaxValue.ToUniversalTime();

        /// <summary>
        /// Gets/Sets absolute expiration.
        /// </summary>
        public DateTime AbsoluteExpiration
        {
            get { return _absoluteExpiration; }
            set { _absoluteExpiration = value; }
        }


        /// <summary>The file or cache key dependencies for the item. 
        /// When any dependency changes, the object becomes invalid and is removed from 
        /// the cache. If there are no dependencies, this property contains a null 
        /// reference (Nothing in Visual Basic).</summary>
        /// <remarks></remarks>
        public CacheDependency Dependency
        {
            get { return _d; }
            set { _d = value; }
        }


    }
}
