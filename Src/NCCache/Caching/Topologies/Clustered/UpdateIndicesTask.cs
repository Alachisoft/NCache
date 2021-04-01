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
using System;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class UpdateIndicesTask : AsyncProcessor.IAsyncTask
    {
        ClusterCacheBase _cache;
        object _key;

        public UpdateIndicesTask(ClusterCacheBase cache, object key)
        {
            _cache = cache;
            _key = key;

        }

        #region IAsyncTask Members

        public void Process()
        {
            if (_cache != null && _key != null)
            {
                try
                {
                    _cache.UpdateIndices(_key, new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation));
                }
                catch (Exception e)
                {
                }
            }

        }

        #endregion
    }
}