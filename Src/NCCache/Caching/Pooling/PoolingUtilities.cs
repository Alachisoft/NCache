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

using System.Collections.Generic;
using Alachisoft.NCache.Common.Pooling.Lease;

namespace Alachisoft.NCache.Caching.Pooling
{
    public static class PoolingUtilities
    {
        public static void MarkFree(this IEnumerable<ILeasable> leasables, int modulerefId)
        {

        }

        public static void MarkInUse(this IEnumerable<ILeasable> leasables, int modulerefId)
        {

        }

        public static T SwapSimpleLeasables<T>(T leasableOne, T leasableTwo) where T : SimpleLease
        {
            if (ReferenceEquals(leasableOne, leasableTwo))
                return leasableOne;

            leasableOne?.ReturnLeasableToPool();
            return leasableTwo;
        }
    }
}
