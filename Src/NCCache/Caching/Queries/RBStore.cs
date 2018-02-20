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

using System;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    public class RBStore<T> : IIndexStore where T:IComparable
    {
        private RedBlack<T> _rbTree;
        private String _storeDataType = String.Empty;
        private string storeName;

        public string Name
        {
            get { return storeName; }
        }

        public String StoreDataType 
        {
            get { return _storeDataType; }
        }

        public RBStore(string cacheName, string storeDataType, string name, bool haveDuplicateKeys)
        {
            this._storeDataType = storeDataType;
            this.storeName = name;
            _rbTree = new RedBlack<T>(cacheName, Common.MemoryUtil.GetAttributeTypeSize(storeDataType));
            _rbTree.CanHaveDuplicateKeys = haveDuplicateKeys;
        }

        public RBStore(string cacheName,string storeDataType, string name)
        {
            this._storeDataType = storeDataType;
            this.storeName = name;
            _rbTree = new RedBlack<T>(cacheName, Common.MemoryUtil.GetAttributeTypeSize(storeDataType));
        }

       
        public object Add(object key, object value)
        {
            object node = new object();
            if (_rbTree != null)
                node = _rbTree.Add((T)key, value);
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

        public void GetData(object key, ComparisonType comparisonType, IQueryResult result, CollectionOperation mergeType)
        {
            RedBlack<T>.COMPARE compare = RedBlack<T>.COMPARE.EQ;

            if (_rbTree != null)
            {
                switch (comparisonType)
                {
                    case ComparisonType.EQUALS:
                        compare = RedBlack<T>.COMPARE.EQ;
                        break;

                    case ComparisonType.NOT_EQUALS:
                        compare = RedBlack<T>.COMPARE.NE;
                        break;

                    case ComparisonType.LESS_THAN:
                        compare = RedBlack<T>.COMPARE.LT;
                        break;

                    case ComparisonType.GREATER_THAN:
                        compare = RedBlack<T>.COMPARE.GT;
                        break;

                    case ComparisonType.LESS_THAN_EQUALS:
                        compare = RedBlack<T>.COMPARE.LTEQ;
                        break;

                    case ComparisonType.GREATER_THAN_EQUALS:
                        compare = RedBlack<T>.COMPARE.GTEQ;
                        break;

                    case ComparisonType.LIKE:
                        compare = RedBlack<T>.COMPARE.REGEX;
                        break;

                    case ComparisonType.NOT_LIKE:
                        compare = RedBlack<T>.COMPARE.IREGEX;
                        break;
                }
                if (key is T)
                    _rbTree.GetData((T) key, compare, result, mergeType);
                else throw new InvalidCastException("Object must be of type " + typeof (T).Name);
            }
        }

        public int Count
        {
            get { return _rbTree != null ? _rbTree.Count : 0; }
        }

        public void GetTagData(object tag, HashVector finalResult)
        {
            _rbTree.GetTagData((T)tag, finalResult);
        }

        public long IndexInMemorySize
        {
            get { return this._rbTree.IndexInMemorySize; }
        }
    }
}