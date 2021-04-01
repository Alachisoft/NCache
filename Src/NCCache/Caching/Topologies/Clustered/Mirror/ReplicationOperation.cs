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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class ReplicationOperation : IOptimizedQueueOperation
    {
        int _size;
        object _data;
        Array _userPayLoad;
        long _payLoadSize;        

        public ReplicationOperation(object data)
            : this(data, 20)
        {
        }

        public ReplicationOperation(object data, int size)
        {
            _data = data;
            _size = size;
        }

        public ReplicationOperation(object data, int size, Array userPayLoad, long payLoadSize)
            : this(data, size)
        {
            _userPayLoad = userPayLoad;
            _payLoadSize = payLoadSize;            
        }

        public object Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public int Size
        {
            get { return _size; }
            set
            {
                if (value > 0)
                    _size = value;
            }
        }

        public Array UserPayLoad
        {
            get { return _userPayLoad; }
            set { _userPayLoad = value; }
        }

        public long PayLoadSize
        {
            get { return _payLoadSize; }
            set { _payLoadSize = value; }
        }

        public int InMemorySize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void ReturnPooledItemsToPool(PoolManager poolManager)
        {
            if (poolManager == null)
                return;

            if (_data is DictionaryEntry entry)
            {
                if (entry.Value is object[] info)
                {
                    foreach (var infoElem in info)
                    {
                        switch (infoElem)
                        {
                            case CacheEntry cacheEntry:
                                MiscUtil.ReturnEntryToPool(cacheEntry, poolManager);
                                break;

                            case OperationContext operationContext:
                                MiscUtil.ReturnOperationContextToPool(operationContext, poolManager);
                                break;

                            default:
                                // Ignore non-pooled items
                                break;
                        }
                    }
                }
            }
        }
    }
}