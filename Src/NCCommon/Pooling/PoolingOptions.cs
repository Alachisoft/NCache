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
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Options;

namespace Alachisoft.NCache.Common.Pooling
{
    public sealed class PoolingOptions<T> where T : ILeasable
    {
        public int InitialCapacity
        {
            get; set;
        }

        public float ServingCapabilityBorderline
        {
            get; set;
        }

        public IPooledObjectInstantiator<T> Instantiator
        {
            get;
        }

        public PoolingOptions(IPooledObjectInstantiator<T> objectInstantiator) : this(objectInstantiator, 100, 0.8f)
        {
        }

        public PoolingOptions(IPooledObjectInstantiator<T> objectInstantiator, int initialCapacity, float servingCapabilityBorderline)
        {
            InitialCapacity = initialCapacity;
            ServingCapabilityBorderline = servingCapabilityBorderline;
            Instantiator = objectInstantiator ?? throw new ArgumentNullException(nameof(objectInstantiator));
        }

        public PoolingOptions(IPooledObjectInstantiator<T> objectInstantiator, int initialCapacity = 100)
        {
            InitialCapacity = initialCapacity;
            ServingCapabilityBorderline = 0.8f;
            Instantiator = objectInstantiator ?? throw new ArgumentNullException(nameof(objectInstantiator));
        }
    }
}
