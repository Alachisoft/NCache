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

namespace Alachisoft.NCache.Common.Pooling.ArrayPool
{
    internal sealed class ArrayWrapperBucketedInstantiator<T> : ArrayWrapperInstantiatorBase<T>
    {
        private readonly ObjectPool<ArrayWrapper<T>> _arrayWrapperPool;

        public ArrayWrapperBucketedInstantiator(ObjectPool<ArrayWrapper<T>> arrayWrapperPool, int arrayLength) : base(arrayLength)
        {
            _arrayWrapperPool = arrayWrapperPool;
        }

        public override ArrayWrapper<T> Instantiate()
        {
            var wrapper = _arrayWrapperPool.Rent(initialize: false);
            wrapper.ArrayLength = _arrayLength;

            return wrapper;
        }
    }
}
