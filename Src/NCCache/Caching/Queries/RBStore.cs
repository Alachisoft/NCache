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
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.Caching.Queries
{
    public class RBStore : IIndexStore
    {
        private RedBlack _rbTree;
        private String _storeDataType = String.Empty;
        public String StoreDataType
        {
            get { return _storeDataType; }
        }

        public RBStore(string cacheName, string storeDataType)
        {
            this._storeDataType = storeDataType;
            _rbTree = new RedBlack(cacheName, Common.MemoryUtil.GetAttributeTypeSize(storeDataType));
        }

        public object Add(object key, object value)
        {
            object node = new object();
            if (_rbTree != null)
                node = _rbTree.Add(key as IComparable, value);
            return node;
        }

        public bool Remove(object value, object indexPosition)
        {
            bool isNodeRemoved = false;
            if (_rbTree != null)
                isNodeRemoved = _rbTree.Remove(value, indexPosition);
            return isNodeRemoved;
        }

        public void Clear()
        {
            if (_rbTree != null)
                _rbTree.Clear();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (_rbTree != null)
                return _rbTree.GetEnumerator();

            return new RedBlackEnumerator();
        }

        public ArrayList GetData(object key, ComparisonType comparisonType)
        {
            RedBlack.COMPARE compare = RedBlack.COMPARE.EQ;
            ArrayList result = new ArrayList();

            if (_rbTree != null)
            {
                switch (comparisonType)
                {
                    case ComparisonType.EQUALS:
                        compare = RedBlack.COMPARE.EQ;
                        break;

                    case ComparisonType.NOT_EQUALS:
                        compare = RedBlack.COMPARE.NE;
                        break;

                    case ComparisonType.LESS_THAN:
                        compare = RedBlack.COMPARE.LT;
                        break;

                    case ComparisonType.GREATER_THAN:
                        compare = RedBlack.COMPARE.GT;
                        break;

                    case ComparisonType.LESS_THAN_EQUALS:
                        compare = RedBlack.COMPARE.LTEQ;
                        break;

                    case ComparisonType.GREATER_THAN_EQUALS:
                        compare = RedBlack.COMPARE.GTEQ;
                        break;

                    case ComparisonType.LIKE:
                        compare = RedBlack.COMPARE.REGEX;
                        break;

                    case ComparisonType.NOT_LIKE:
                        compare = RedBlack.COMPARE.IREGEX;
                        break;
                }

                result = _rbTree.GetData(key as IComparable, compare) as ArrayList;
            }

            return result;
        }

        public int Count
        {
            get { return _rbTree != null ? _rbTree.Count : 0; }
        }

        public long IndexInMemorySize
        {
            get { return this._rbTree.IndexInMemorySize; }
        }

    }
}
