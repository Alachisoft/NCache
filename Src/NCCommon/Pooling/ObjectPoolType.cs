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

namespace Alachisoft.NCache.Common.Pooling
{
    public enum ObjectPoolType : int
    {
        AggregateExpirationHint,
        BitSet,
        CacheEntry,
        CollectionNotifications,
        CompressedValueEntry,
        CounterHint,
        DbCacheDependency,
        ExtensibleDependency,
        FileDependency,
        FixedExpiration,
        FixedIdleExpiration,
        GroupInfo,
        IdleExpiration,
        IndexInformation,
        LargeUserBinaryObject,
        NodeExpiration,
        Notifications,
        PriorityEvictionHint,
        SmallUserBinaryObject,
        TimestampHint,
        TTLExpiration,
        CacheInsResultWithEntry,
        OperationContext,
        OperationId,
        ProtobufAddCommand,
        ProtobufAddResponse,
        ProtobufCommand,
        ProtobufGetCommand,
        ProtobufGetResponse,
        ProtobufInsertCommand,
        ProtobufInsertResponse,
        ProtobufLockInfo,
        ProtobufObjectQueryInfo,
        ProtobufRemoveCommand,
        ProtobufRemoveResponse,
        ProtobufResponse,
        SocketServerAddCommand,
        SocketServerGetCommand,
        SocketServerInsertCommand,
        SocketServerRemoveCommand,
    }
}
