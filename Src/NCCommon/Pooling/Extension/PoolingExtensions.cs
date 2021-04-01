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

using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Protobuf;
using ProtoBuf.Serializers.Pooling;

namespace Alachisoft.NCache.Common.Pooling.Extension
{
    internal static class PoolingExtensions
    {
        public static ProtoPoolBase<BitSet> GetBitSetPool(this PoolManager manager)
        {
            return manager?.GetPool<BitSet>(ObjectPoolType.BitSet);
        }

        public static ProtoPoolBase<SmallUserBinaryObject> GetSmallUserBinaryObjectPool(this PoolManager manager)
        {
            return manager?.GetPool<SmallUserBinaryObject>(ObjectPoolType.SmallUserBinaryObject);
        }

        public static ProtoPoolBase<LargeUserBinaryObject> GetLargeUserBinaryObjectPool(this PoolManager manager)
        {
            return manager?.GetPool<LargeUserBinaryObject>(ObjectPoolType.LargeUserBinaryObject);
        }

        #region ------------------------------- [Protobuf Commands] -------------------------------
        
        public static ProtoPoolBase<Command> GetProtobufCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Command>(ObjectPoolType.ProtobufCommand);
        }

        public static ProtoPoolBase<AddCommand> GetProtobufAddCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<AddCommand>(ObjectPoolType.ProtobufAddCommand);
        }

        public static ProtoPoolBase<GetCommand> GetProtobufGetCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<GetCommand>(ObjectPoolType.ProtobufGetCommand);
        }

        public static ProtoPoolBase<InsertCommand> GetProtobufInsertCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<InsertCommand>(ObjectPoolType.ProtobufInsertCommand);
        }

        public static ProtoPoolBase<RemoveCommand> GetProtobufRemoveCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<RemoveCommand>(ObjectPoolType.ProtobufRemoveCommand);
        }

        #endregion

        #region ------------------------------ [Protobuf Responses] -------------------------------

        public static ProtoPoolBase<Response> GetProtobufResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Response>(ObjectPoolType.ProtobufResponse);
        }

        public static ProtoPoolBase<AddResponse> GetProtobufAddResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<AddResponse>(ObjectPoolType.ProtobufAddResponse);
        }

        public static ProtoPoolBase<GetResponse> GetProtobufGetResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<GetResponse>(ObjectPoolType.ProtobufGetResponse);
        }

        public static ProtoPoolBase<InsertResponse> GetProtobufInsertResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<InsertResponse>(ObjectPoolType.ProtobufInsertResponse);
        }

        public static ProtoPoolBase<RemoveResponse> GetProtobufRemoveResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<RemoveResponse>(ObjectPoolType.ProtobufRemoveResponse);
        }

        #endregion

        #region -------------------------------- [Protobuf Objects] -------------------------------

        public static ProtoPoolBase<LockInfo> GetProtobufLockInfoPool(this PoolManager manager)
        {
            return manager?.GetPool<LockInfo>(ObjectPoolType.ProtobufLockInfo);
        }

       

        #endregion

        public static ArrayPool<byte> GetByteArrayPool(this PoolManager manager)
        {
            return manager?.GetPool<byte>(ArrayPoolType.Byte);
        }
    }
}
