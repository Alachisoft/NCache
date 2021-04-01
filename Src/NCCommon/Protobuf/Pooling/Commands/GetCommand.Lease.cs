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
    public partial class GetCommand : SimpleLease
    {
        #region ILeasable

        public override sealed void ResetLeasable()
        {
            commandID = -1;
            extensionObject = default(ProtoBuf.IExtension);
            flag = default(int);
            key = string.Empty;
            lockInfo = null;
            MethodOverload = 0;
            providerName = string.Empty;
            readMode = -1;
            requestId = default(long);
            threadId = -1;
            version = default(ulong);
        }

        public override sealed void ReturnLeasableToPool()
        {
            lockInfo?.ReturnLeasableToPool();
            PoolManager.GetProtobufGetCommandPool()?.Return(this);
        }

        #endregion
    }
}
