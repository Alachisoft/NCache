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
    public partial class AddCommand : SimpleLease
    {
        #region ILeasable

        public override sealed void ResetLeasable()
        {
            absExpiration = default(long);
            CallbackType = default(int);
            clientID = string.Empty;
            commandID = -1;
            data.Clear();
            datasourceItemAddedCallbackId = default(int);
            extensionObject = default(ProtoBuf.IExtension);
            flag = default(int);
            isAsync = default(bool);
            isResync = default(bool);
            key = string.Empty;
            MethodOverload = 0;
            objectQueryInfoEncrypted.Clear();
            priority = default(int);
            providerName = string.Empty;
          
            removeCallbackId = default(int);
            removeDataFilter = -1;
            requestId = default(long);
            resyncProviderName = string.Empty;
            sldExpiration = default(long);
            subGroup = string.Empty;
            updateCallbackId = default(int);
            updateDataFilter = -1;
            version = string.Empty;
        }

        public override sealed void ReturnLeasableToPool()
        {
      
            PoolManager.GetProtobufAddCommandPool()?.Return(this);
        }

        #endregion
    }
}