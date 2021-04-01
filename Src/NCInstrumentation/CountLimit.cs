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
#if !NETCORE
using System.Management.Instrumentation;

namespace Alachisoft.NCache.Instrumentation
{
    /// <summary>
    /// WMI based event fired if the maximum item count limit gets exceeded
    /// </summary>
    public class CountLimit : BaseEvent
    {
        private long _totalCount;
        private long _countLimit;

        CountLimit()
        {
        }

        public CountLimit(long Limit)
        {
            _countLimit = Limit;
        }

        public long TotalCount
        {
            get
            {
                return _totalCount;
            }

            set
            {
                _totalCount = value;
            }
        }
    }
}
#endif