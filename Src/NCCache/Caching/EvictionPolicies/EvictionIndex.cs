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
using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    internal class EvictionIndex : ISizableIndex
    {
        private HashVector _index = new HashVector();
        private long _head = -1; //min
        private long _tail = -1; //max
        private readonly object _syncLock = new object();
        public ILogger _logger;

        private long _evictionIndexEntriesSize = 0;
        
        private int _keysCount;

        public int KeysCount
        {
            get { return _keysCount; }
            set { _keysCount = value; }
        }

        internal object SyncRoot
        {
            get { return _syncLock; }
        }

        internal bool Contains(long key, object value)
        {
            if (_index.Contains(key))
            {
                EvictionIndexEntry indexEntry = _index[key] as EvictionIndexEntry;
                return indexEntry.Contains(value);
            }
            return false;
        }

        internal void Add(long key, object value)
        {
            if (_index.Count == 0)
            {
               _head = key;
            }

            int add = 0, remove = 0;
            bool incrementKeyCount = true;

            if (_index.Contains(key))
            {
                EvictionIndexEntry indexEntry = (EvictionIndexEntry)_index[key];
                if (indexEntry != null)
                {
                    remove = indexEntry.InMemorySize;

                    if (indexEntry.Contains(value)) incrementKeyCount = false;


                    indexEntry.Insert(value);

                    add = indexEntry.InMemorySize;
                }
            }
            else
            {
                EvictionIndexEntry indexEntry = new EvictionIndexEntry();
                indexEntry.Insert(value);

                add = indexEntry.InMemorySize;

                _index[key] = indexEntry;
                
                EvictionIndexEntry prevEntry = _index[_tail] as EvictionIndexEntry;

                if (prevEntry != null)
                {
                    prevEntry.Next = key;
                }
                indexEntry.Previous = _tail;
                _tail = key;
            }
           
            _evictionIndexEntriesSize -= remove;
            _evictionIndexEntriesSize += add;

            if (incrementKeyCount) _keysCount++;
        }

        /// <summary>
        /// insert at the begining...
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void Insert(long key, object value)
        {
            Insert(key, value,-1);
        }

        /// <summary>
        /// Add method only adds the new node at the tail...
        /// Insert method can add the new nodes in between also....
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void Insert(long key, object value, long currentKey)
        {
            int addSize = 0, removeSize = 0;
            bool incrementKeyCount = true;


            if (_index.Contains(key))
            {
                EvictionIndexEntry indexEntry = (EvictionIndexEntry)_index[key];
                if (indexEntry != null)
                {
                    removeSize = indexEntry.InMemorySize;

                    if (indexEntry.Contains(value)) incrementKeyCount = false;

                    indexEntry.Insert(value);

                    addSize = indexEntry.InMemorySize;
                }
            }
            else
            {
                EvictionIndexEntry currentEntry = (EvictionIndexEntry)_index[currentKey];
                EvictionIndexEntry indexEntry = new EvictionIndexEntry();
                indexEntry.Insert(value);

                addSize = indexEntry.InMemorySize;

                _index[key] = indexEntry;

                if (currentEntry != null)
                {
                    EvictionIndexEntry nextEntry = _index[currentEntry.Next] as EvictionIndexEntry;

                    if (nextEntry != null)
                    {
                        indexEntry.Next = currentEntry.Next;
                        indexEntry.Previous = currentKey;

                        currentEntry.Next = key;
                        nextEntry.Previous = key;
                    }
                    else
                    {
                        currentEntry.Next = key;
                        indexEntry.Previous = currentKey;

                        if (currentKey == _tail)
                            _tail = key;
                    }
                }
                else
                {
                    if (_head == -1)
                    {
                        _head = key;

                        if (_tail == -1)
                        {
                            _tail = key;
                        }
                    }
                    else
                    {
                        EvictionIndexEntry headEntry = (EvictionIndexEntry)_index[_head];

                        indexEntry.Next = _head;
                        headEntry.Previous = key;
                        _head = key;
                    }
                }
            }
           
            _evictionIndexEntriesSize -= removeSize;
            _evictionIndexEntriesSize += addSize;

            if (incrementKeyCount)
                _keysCount++;
        }

        internal void Remove(long key, object value)
        {
            EvictionIndexEntry previousEntry = null;
            EvictionIndexEntry nextEntry = null;

            int addSize = 0, removeSize = 0;

            if (_index.Contains(key))
            {
                EvictionIndexEntry indexEntry = (EvictionIndexEntry)_index[key];
                bool decrementKeyCount = true;
                removeSize = indexEntry.InMemorySize;

                if (!indexEntry.Contains(value)) decrementKeyCount = false;

                if (indexEntry.Remove(value))
                {
                    if (indexEntry.Previous != -1) previousEntry = (EvictionIndexEntry)_index[indexEntry.Previous];
                    if (indexEntry.Next != -1) nextEntry = (EvictionIndexEntry)_index[indexEntry.Next];

                    if (previousEntry != null && nextEntry != null)
                    {
                        previousEntry.Next = indexEntry.Next;
                        nextEntry.Previous = indexEntry.Previous;
                    }
                    else if (previousEntry != null)
                    {
                        previousEntry.Next = indexEntry.Next;
                        _tail = indexEntry.Previous;
                    }
                    else if (nextEntry != null)
                    {
                        nextEntry.Previous = indexEntry.Previous;
                        _head = indexEntry.Next;

                    }
                    else
                    {
                        _tail = _head = -1;
                    }
                    _index.Remove(key);
                }
                else
                {
                    addSize = indexEntry.InMemorySize;
                }

                if (decrementKeyCount)
                    _keysCount--;
            }

            _evictionIndexEntriesSize -= removeSize;
            _evictionIndexEntriesSize += addSize;
        }

        internal void Remove(long key, object value, ref long currentEntry)
        {
            EvictionIndexEntry previousEntry = null;
            EvictionIndexEntry nextEntry = null;

            int addSize = 0, removeSize = 0;

            if (_index.Contains(key))
            {
                EvictionIndexEntry indexEntry = (EvictionIndexEntry)_index[key];

                removeSize = indexEntry.InMemorySize;

                if (indexEntry.Previous != -1) previousEntry = (EvictionIndexEntry)_index[indexEntry.Previous];
                if (indexEntry.Next != -1) nextEntry = (EvictionIndexEntry)_index[indexEntry.Next];

                bool decrementKeyCount = indexEntry.Contains(value);

                if (indexEntry.Remove(value))
                {
                    currentEntry = indexEntry.Previous;

                    if (previousEntry != null && nextEntry != null)
                    {
                        previousEntry.Next = indexEntry.Next;
                        nextEntry.Previous = indexEntry.Previous;
                    }
                    else if (previousEntry != null)
                    {
                        previousEntry.Next = indexEntry.Next;
                        _tail = indexEntry.Previous;
                    }
                    else if (nextEntry != null)
                    {
                        nextEntry.Previous = indexEntry.Previous;
                        _head = indexEntry.Next;
                    }
                    else
                    {
                        _tail = _head = -1;
                    }
                    _index.Remove(key);
                }
                else
                {
                    currentEntry = key;
                    addSize = indexEntry.InMemorySize;
                }

                if (decrementKeyCount) _keysCount--;
            }

            _evictionIndexEntriesSize -= removeSize;
            _evictionIndexEntriesSize += addSize;
        }


        internal void Clear()
        {
            _head = _tail = -1;

            _evictionIndexEntriesSize = 0;
            _keysCount = 0;

            _index = new HashVector();
        }

        internal IList GetSelectedKeys(CacheBase cache, long evictSize, ref long evictedSize)
        {
            EvictionIndexEntry entry;
            ClusteredArrayList selectedKeys = new ClusteredArrayList();
            long totalSize = 0;
            bool selectionCompleted = false;
            long index = _head;
            if (_head != -1)
            {
                do
                {
                    entry = _index[index] as EvictionIndexEntry;
                    IList keys = entry.GetAllKeys();
                    foreach (string key in keys)
                    {
                        int itemSize = cache.GetItemSize(key);
                        if (totalSize + itemSize >= evictSize)
                        {
                            selectedKeys.Add(key);
                            totalSize += itemSize;

                            evictedSize = totalSize;
                            selectionCompleted = true;
                            break;
                        }
                        else
                        {
                            selectedKeys.Add(key);
                            totalSize += itemSize;
                        }
                    }
                    index = entry.Next;
                }
                while (!selectionCompleted && index != -1);
            }
            return selectedKeys;
        }

        #region ISizable Impelementation
        public long IndexInMemorySize
        {
            get
            {
                return (_evictionIndexEntriesSize + EvictionIndexSize);
            }
        }

        private long EvictionIndexSize
        {
            get
            {
                long temp = 0;

                temp += _index.BucketCount * Common.MemoryUtil.NetHashtableOverHead;

                return temp;
            }
        }
        #endregion
    }
}
