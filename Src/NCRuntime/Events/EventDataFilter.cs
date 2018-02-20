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
namespace Alachisoft.TayzGrid.Runtime.Events
#else
namespace Alachisoft.NCache.Runtime.Events
#endif
{
    /// <summary>
    /// This enum is to describe when registering an event, upon raise how much data is 
    /// retrieved from cache when the event is raised.
    /// Only one value can be set
    /// </summary>
    public enum EventDataFilter
    {
        /// <summary>
        /// No Data or meta data on fire of events required 
        /// </summary>
        None = 0x0, 

        /// <summary>
        /// Only meta data of cache item is required
        /// </summary>
	    Metadata = 0x1 ,

        /// <summary>
        /// Item value with cache item required
        /// </summary>
        DataWithMetadata = 0x3
        
    }
}
