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

using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Command;
using ProtoBuf.Serializers.Pooling;

namespace Alachisoft.NCache.SocketServer.Pooling
{
    public static class PoolingExtensions
    {
        internal static ProtoPoolBase<AddCommand> GetSocketServerAddCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<AddCommand>(ObjectPoolType.SocketServerAddCommand);
        }

        internal static ProtoPoolBase<GetCommand> GetSocketServerGetCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<GetCommand>(ObjectPoolType.SocketServerGetCommand);
        }

        internal static ProtoPoolBase<InsertCommand> GetSocketServerInsertCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<InsertCommand>(ObjectPoolType.SocketServerInsertCommand);
        }

        internal static ProtoPoolBase<RemoveCommand> GetSocketServerRemoveCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<RemoveCommand>(ObjectPoolType.SocketServerRemoveCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.Command> GetProtobufCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.Command>(ObjectPoolType.ProtobufCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.Response> GetProtobufResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.Response>(ObjectPoolType.ProtobufResponse);
        }

        public static ProtoPoolBase<Common.Protobuf.AddCommand> GetProtobufAddCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.AddCommand>(ObjectPoolType.ProtobufAddCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.GetCommand> GetProtobufGetCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.GetCommand>(ObjectPoolType.ProtobufGetCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.AddResponse> GetProtobufAddResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.AddResponse>(ObjectPoolType.ProtobufAddResponse);
        }

        public static ProtoPoolBase<Common.Protobuf.GetResponse> GetProtobufGetResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.GetResponse>(ObjectPoolType.ProtobufGetResponse);
        }

        public static ProtoPoolBase<Common.Protobuf.InsertCommand> GetProtobufInsertCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.InsertCommand>(ObjectPoolType.ProtobufInsertCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.RemoveCommand> GetProtobufRemoveCommandPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.RemoveCommand>(ObjectPoolType.ProtobufRemoveCommand);
        }

        public static ProtoPoolBase<Common.Protobuf.InsertResponse> GetProtobufInsertResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.InsertResponse>(ObjectPoolType.ProtobufInsertResponse);
        }

        public static ProtoPoolBase<Common.Protobuf.RemoveResponse> GetProtobufRemoveResponsePool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.RemoveResponse>(ObjectPoolType.ProtobufRemoveResponse);
        }

        public static ProtoPoolBase<Common.Protobuf.LockInfo> GetProtobufLockInfoPool(this PoolManager manager)
        {
            return manager?.GetSimplePool<Common.Protobuf.LockInfo>(ObjectPoolType.ProtobufLockInfo);
        }

       
    }
}
