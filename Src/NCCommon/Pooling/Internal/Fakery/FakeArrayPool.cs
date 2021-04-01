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

namespace Alachisoft.NCache.Common.Pooling.Internal
{
    internal sealed class FakeArrayPool<T> : ArrayPool<T>
    {
        public sealed override string PoolName
        {
            get => "FakeArrayPool";
        }

        internal FakeArrayPool() : base()
        {
        }

        public sealed override T[] Rent(int length, bool clean = false)
        {
            return new T[length];
        }

        public sealed override void Return(T[] array)
        {
        }

        public sealed override void ResetPool()
        {
            // Nothing will happen if base.ResetPool() were called here
            // but we're saving up on executing redundant instructions
        }
    }
}
