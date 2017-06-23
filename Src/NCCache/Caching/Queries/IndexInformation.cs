// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    public class IndexInformation : ISizableIndex
    {

        private object _nodeInfo;
        private ushort _indexInformationSize;
        private ushort _nodeInfoMaxCount;

        public IndexInformation()
        { }

        public IList<IndexStoreInformation> IndexStoreInformations
        {
            get
            {
                if (_nodeInfo is IndexStoreInformation)
                {
                    ClusteredList<IndexStoreInformation> _lst = new ClusteredList<IndexStoreInformation>(1);
                    _lst.Add((IndexStoreInformation)_nodeInfo);
                    return _lst;
                }
                else
                    return (ClusteredList<IndexStoreInformation>)_nodeInfo;
            }
        }

        public void Add(string storeName, IIndexStore store, INodeReference node)
        {
            IndexStoreInformation ni = new IndexStoreInformation(storeName, store, node);
            if (_nodeInfo == null)
            {
                _nodeInfo = ni;
                _nodeInfoMaxCount = _nodeInfoMaxCount < 1 ? (ushort)1 : _nodeInfoMaxCount;
            }
            else
            {
                if (_nodeInfo is IndexStoreInformation)
                {
                    ClusteredList<IndexStoreInformation> _list = new ClusteredList<IndexStoreInformation>();
                    _list.Add((IndexStoreInformation)_nodeInfo);
                    _list.Add(ni);
                    _nodeInfo = _list;
                    if (_list.Count > _nodeInfoMaxCount)
                        _nodeInfoMaxCount = (ushort)_list.Count;
                }
                else
                {
                    ClusteredList<IndexStoreInformation> _list = ((ClusteredList<IndexStoreInformation>)_nodeInfo);
                    _list.Add(ni);
                    if (_list.Count > _nodeInfoMaxCount)
                        _nodeInfoMaxCount = (ushort)_list.Count;
                }
            }
            _indexInformationSize += (ushort)ni.IndexInMemorySize;
        }

        public long IndexInMemorySize
        {
            get { return (long)(_indexInformationSize + (_nodeInfoMaxCount * Common.MemoryUtil.NetListOverHead)); }
        }
      
    }
}
