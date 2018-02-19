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

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    enum LogMode
    {
        /// <summary>This mode specifies that operation should be logged before an actual
        /// operation is done on the cache. This logging mode is used for state transfer
        /// among replicas</summary>
        LogBeforeActualOperation,

        /// <summary>This mode specifies that operation should be logged after an actual
        /// operation is done on the cache. This logging mode is used for state transfer
        /// among partitions</summary>
        LogBeforeAfterActualOperation
    }
}
