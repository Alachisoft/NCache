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
using Alachisoft.NCache.Common.Pooling.Lease;

namespace Alachisoft.NCache.Common.Pooling.Internal
{
    internal sealed class FakeSimpleObjectPool<T> : SimpleObjectPool<T> where T : ILeasable
    {
        public sealed override string PoolName
        {
            get => "FakeSimpleObjectPool";
        }

        internal FakeSimpleObjectPool(PoolManager poolManager, PoolingOptions<T> options)
            : base(poolManager, 0, options.Instantiator)
        {
        }

        public sealed override T Rent(bool initialize = false)
        {
            var instance = _objectInstantiator.Instantiate();
            {
                instance.PoolManager = _poolManager;
            }
            return instance;
        }

        public sealed override T[] Rent(int numberOfItems, bool initialize = false)
        {
            if (numberOfItems < 0)
                throw new ArgumentException("Invalid number of items requested for renting.", nameof(numberOfItems));

            var instances = new T[numberOfItems];

            for (var i = 0; i < numberOfItems; i++)
                instances[i] = Rent(initialize);

            return instances;
        }

        public sealed override void Return(T item)
        {
        }

        public sealed override void Return(IEnumerable<T> items)
        {
        }

        public sealed override void ResetPool()
        {
            // Nothing will happen if base.ResetPool() were called here
            // but we're saving up on executing redundant instructions
        }
    }
}
