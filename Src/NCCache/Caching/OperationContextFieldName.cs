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
namespace Alachisoft.NCache.Caching
{
    public enum OperationContextFieldName
    {
        OperationType,
        RaiseCQNotification,
        ReadThruOptions,
        ReadThru,
        ReadThruProviderName,
        ClientLastViewId,
        IntendedRecipient,
        OperationId,
        EventContext,
        GenerateQueryInfo,
        RemoveQueryOperation,
        NotifyRemove,
        ValueDataSize,
        NoGracefulBlock,
        RemoveOnReplica,
        ClientId,
        IndexMetaInfo,
        ReaderBitsetEnum,
        DataFormat,
        IsRetryOperation,
        ClientThreadId,
        DonotRegisterSyncDependency,
        ItemVersion,
        IsClusteredOperation,
        WriteThru,
        WriteBehind,
        WriteThruProviderName,
        GetDataWithReader,
        ClientUpdateCallbackID,
        ClientUpdateCallbackFilter,
        ClientRemoveCallbackID,
        ClientRemoveCallbackFilter,
        ClientDataSourceCallbackID,
        /// <summary>
        /// The operation is only intended for client cache only.
        /// Do not perform on source l2 cache.
        /// </summary>
       
        MethodOverload,
        CallbackType,
        ClientDeleteLockId,
        ClientDeleteLockAccessType,
        DontFireClearNotification,
        GroupInfo,
        InternalOperation,
        DoNotLog,
        ClientOperationTimeout,
        NeedUserPayload,
        CloneCacheEntry,
        UseObjectPool,
        ClientIpAddress
    }
}