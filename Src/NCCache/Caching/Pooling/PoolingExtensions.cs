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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using ProtoBuf.Serializers.Pooling;

namespace Alachisoft.NCache.Caching.Pooling
{
    public static class PoolingExtensions
    {
        public static ProtoPoolBase<BitSet> GetBitSetPool(this PoolManager manager)
        {
            return manager?.GetPool<BitSet>(ObjectPoolType.BitSet);
        }

        public static ProtoPoolBase<GroupInfo> GetGroupInfoPool(this PoolManager manager)
        {
            return manager?.GetPool<GroupInfo>(ObjectPoolType.GroupInfo);
        }

        public static ProtoPoolBase<CacheEntry> GetCacheEntryPool(this PoolManager manager)
        {
            return manager?.GetPool<CacheEntry>(ObjectPoolType.CacheEntry);
        }

        public static ProtoPoolBase<CounterHint> GetCounterHintPool(this PoolManager manager)
        {
            return manager?.GetPool<CounterHint>(ObjectPoolType.CounterHint);
        }

        public static ProtoPoolBase<OperationID> GetOperationIdPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<OperationID>(ObjectPoolType.OperationId);
        }
        
        public static ProtoPoolBase<Notifications> GetNotificationsPool(this PoolManager manager)
        {
            return manager?.GetPool<Notifications>(ObjectPoolType.Notifications);
        }

        public static ProtoPoolBase<TimestampHint> GetTimestampHintPool(this PoolManager manager)
        {
            return manager?.GetPool<TimestampHint>(ObjectPoolType.TimestampHint);
        }

        public static ProtoPoolBase<TTLExpiration> GetTTLExpirationPool(this PoolManager manager)
        {
            return manager?.GetPool<TTLExpiration>(ObjectPoolType.TTLExpiration);
        }

        public static ProtoPoolBase<IdleExpiration> GetIdleExpirationPool(this PoolManager manager)
        {
            return manager?.GetPool<IdleExpiration>(ObjectPoolType.IdleExpiration);
        }

        public static ProtoPoolBase<FixedExpiration> GetFixedExpirationPool(this PoolManager manager)
        {
            return manager?.GetPool<FixedExpiration>(ObjectPoolType.FixedExpiration);
        }


        public static ProtoPoolBase<OperationContext> GetOperationContextPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<OperationContext>(ObjectPoolType.OperationContext);
        }
        
        public static ProtoPoolBase<FixedIdleExpiration> GetFixedIdleExpirationPool(this PoolManager manager)
        {
            return manager?.GetPool<FixedIdleExpiration>(ObjectPoolType.FixedIdleExpiration);
        }

        public static ProtoPoolBase<CompressedValueEntry> GetCompressedValueEntryPool(this PoolManager manager)
        {
            return manager?.GetPool<CompressedValueEntry>(ObjectPoolType.CompressedValueEntry);
        }

       

        public static ProtoPoolBase<PriorityEvictionHint> GetPriorityEvictionHintPool(this PoolManager manager)
        {
            return manager?.GetPool<PriorityEvictionHint>(ObjectPoolType.PriorityEvictionHint);
        }

        
        public static ProtoPoolBase<SmallUserBinaryObject> GetSmallUserBinaryObjectPool(this PoolManager manager)
        {
            return manager?.GetPool<SmallUserBinaryObject>(ObjectPoolType.SmallUserBinaryObject);
        }

        public static ProtoPoolBase<LargeUserBinaryObject> GetLargeUserBinaryObjectPool(this PoolManager manager)
        {
            return manager?.GetPool<LargeUserBinaryObject>(ObjectPoolType.LargeUserBinaryObject);
        }

        public static ProtoPoolBase<AggregateExpirationHint> GetAggregateExpirationHintPool(this PoolManager manager)
        {
            return manager?.GetPool<AggregateExpirationHint>(ObjectPoolType.AggregateExpirationHint);
        }

       
        internal static ProtoPoolBase<NodeExpiration> GetNodeExpirationPool(this PoolManager manager)
        {
            return manager?.GetPool<NodeExpiration>(ObjectPoolType.NodeExpiration);
        }

        internal static ProtoPoolBase<CacheInsResultWithEntry> GetCacheInsResultWithEntryPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<CacheInsResultWithEntry>(ObjectPoolType.CacheInsResultWithEntry);
        }

        public static ArrayPool<byte> GetByteArrayPool(this PoolManager manager)
        {
            return manager?.GetPool<byte>(ArrayPoolType.Byte);
        }
    }
}
