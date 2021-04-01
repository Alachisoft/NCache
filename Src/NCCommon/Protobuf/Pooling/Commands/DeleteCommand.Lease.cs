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

using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Extension;

namespace Alachisoft.NCache.Common.Protobuf
{
    public partial class DeleteCommand : SimpleLease
    {
        #region ILeasable

        public override sealed void ResetLeasable()
        {
            key = string.Empty;
            requestId = default(long);
            flag = default(int);
            isAsync = default(bool);
            datasourceItemRemovedCallbackId = default(int);
            lockId = string.Empty;
            lockAccessType = default(int);
            version = default(ulong);
            providerName = string.Empty;
            MethodOverload = 0;
            commandID = -1;
            extensionObject = default(ProtoBuf.IExtension);
        }

        public override sealed void ReturnLeasableToPool()
        {
            // PoolManager.GetProtobufDeleteCommandPool()?.Return(this);
        }

        #endregion
    }
}