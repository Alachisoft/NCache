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
// limitations under the License.

using System;

namespace Alachisoft.NCache.Runtime.Caching
{
    public class CacheItemAttributes
    {
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
    }


}

