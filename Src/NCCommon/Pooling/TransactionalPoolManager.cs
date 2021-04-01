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

using Alachisoft.NCache.Common.Pooling.Internal;
using Alachisoft.NCache.Common.Pooling.Stats;

namespace Alachisoft.NCache.Common.Pooling
{
    public sealed class TransactionalPoolManager : PoolManager
    {
        public IStringPool StringPool
        {
            get;
        }

        public sealed override string PoolManagerName
        {
            get
            {
                return IsUsingFakePools ? "FakeTransactionalPoolManager" : "TransactionalPoolManager";
            }
        }

        public TransactionalPoolManager(bool hardCreateFakePools) : base(hardCreateFakePools)
        {
            StringPool = new StringPool(100);
        }

        public sealed override void Clear()
        {
            base.Clear();
            StringPool?.ResetPool();
        }

        public sealed override PoolStats GetStats(PoolStatsRequest request)
        {
            var stats = base.GetStats(request);
            stats.StringPoolStats = StringPool.GetStats();

            return stats;
        }
    }
}
