// Copyright (c) 2018 Alachisoft
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

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Queries
{
    public class IndexStoreInformation : ISizableIndex
    {
        private IIndexStore _store;
        private INodeReference _rbnodes;
        private string _storeName;

        // Since an object takes 24 bits, it also saves 8 bits of members in these 24 bits. This makes the total size: object size + members size - 8  = 24 + (8*3) -8 = 40
        private const int RefSize = 40; 


        public IndexStoreInformation()
        {
            _rbnodes = new RedBlackNodeReference<int>();
        }

        public IndexStoreInformation(string storeName, IIndexStore store, INodeReference node)
        {
            _rbnodes = node;
            _store = store;
            _storeName = storeName;
        }

        public string StoreName 
        {

            get { return _storeName; }
            set { _storeName = value; }
        }

        public IIndexStore Store
        {
            get { return _store; }
            set { _store = value; }
        }

        public INodeReference IndexPosition
        {
            get { return _rbnodes; }
            set { _rbnodes = value; }
        }

        public long IndexInMemorySize
        {
            get
            {
                return RefSize;
            }
        }

    }
}
