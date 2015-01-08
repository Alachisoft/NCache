// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Queries
{
    public class IndexInformation : ISizableIndex
    {
        
        private List<IndexStoreInformation> _nodeInfo;
        private int _indexInformationSize;
        private int _nodeInfoMaxCount;

        public IndexInformation()
        {
            _nodeInfo = new List<IndexStoreInformation>();    
        }

        public List<IndexStoreInformation> IndexStoreInformations
        {
            get { return _nodeInfo; }
            set { _nodeInfo = value; }
        }

        public void Add(string storeName,IIndexStore store, RedBlackNodeReference node)
        {
            IndexStoreInformation ni = new IndexStoreInformation(storeName, store, node);
            _nodeInfo.Add(ni);

            if (_nodeInfo.Count > _nodeInfoMaxCount)
                _nodeInfoMaxCount = _nodeInfo.Count;

            _indexInformationSize += (int)ni.IndexInMemorySize;

        }

        public long IndexInMemorySize
        {
            get { return (_indexInformationSize + (_nodeInfoMaxCount * Common.MemoryUtil.NetListOverHead)); }
        }
      
    }
}
