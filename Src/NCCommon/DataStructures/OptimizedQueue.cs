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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections.Generic;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class OptimizedQueue<TKey,TQueueITem> where TQueueITem :ISizable
    {
        private HashVector _queue = new HashVector(1000);
        private HashVector _keyToIndexMap = new HashVector(1000);
        private HashVector _indexToKeyMap = new HashVector(1000);

        private int _tail = -1;
        private int _head = -1;
        private bool _tailMaxReached = false;
        private object _sync_mutex = new object();
        private long _size;
        private long _count;

        public long Size
        {
            get { lock (_sync_mutex) { return _size; } }
        }

        public long Count
        {
            get { lock (_sync_mutex) { return _count; } }
        }

        public OptimizedQueue():this(10,null)
        {

        }
        public OptimizedQueue(int size) : this(size, null)
        {

        }
        public OptimizedQueue(int size,IEqualityComparer keyComparer)
        {
            _queue = new HashVector(size);
            _keyToIndexMap = new HashVector(size, keyComparer);
            _indexToKeyMap = new HashVector(size);
        }

        /// <summary>
        /// Optimized Enqueue opeartion, adds the opeartion at _tail index and removes 
        /// any previous operations on that key from the queue
        /// </summary>
        /// <param name="item"></param>
        public bool Enqueue(string key, TQueueITem item)
        {
            bool isNewItem = true;
            try
            {
                lock (_sync_mutex)
                {

                    if (_keyToIndexMap.ContainsKey(key))    //Optimized Queue, so checking in the map if the current cache key is already mapped to some index of Queue or not
                    {
                        //just update the old operation without chaning it's order in the queue.
                        int index1 = (int)_keyToIndexMap[key];
                        TQueueITem oldOperation = (TQueueITem)_queue[index1];
                        _queue[index1] = item;
                        isNewItem = false;
                        _size -= oldOperation.Size; //subtract old operation size
                        _size += item.Size;
                        return isNewItem;
                    }

                    if (_tail == int.MaxValue)     //checks if the _tail value has reached the maxvalue of the long data type, so reinitialize it
                    {
                        _tail = -1;
                        _tailMaxReached = true;
                    }

                    int index = ++_tail;
                    _size += item.Size;
                    _queue.Add(index, item);   //Add new opeartion at the tail of the queue
                    _keyToIndexMap[key] = index;        // update (cache key, queue index) map
                    _indexToKeyMap[index] = key;
                    if (isNewItem) _count++;
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
            finally
            {
            }
            return isNewItem;
        }

        public TQueueITem Dequeue()
        {
            TQueueITem operation = default(TQueueITem);
            try
            {
                lock (_sync_mutex)
                {
                    int index = 0;
                    do               //fetch the next valid operation from the queue
                    {
                        if (_head < _tail || _tailMaxReached)  //or contition checks if the _tail has reached max long value and _head has not yet reached there , so in this case _head<_tail will fail bcz _tail has been reinitialized
                        {
                            if (_head == int.MaxValue)     //checks if _head has reached the max long value, so reinitialize _head and make _tailMaxReached is set to false as _head<_tail is now again valid
                            {
                                _head = -1;
                                _tailMaxReached = false;
                            }

                            index = ++_head;
                            operation = (TQueueITem)_queue[index] ;    //get key on which the operation is to be performed from the head of the queue

                            if (operation != null)
                            {
                                string cacheKey = _indexToKeyMap[index] as string;
                                _keyToIndexMap.Remove(cacheKey);               //update map 
                                _indexToKeyMap.Remove(index);
                                _queue.Remove(index);               //update queue
                                _size -= operation.Size;
                                _count--;
                            }
                        }
                        else
                            break;
                    } while (operation == null);
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
            return operation;
        }

        // Returns the object at the head of the queue. The object remains in the
        // queue. If the queue is empty, this method throws an 
        // InvalidOperationException.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Peek"]/*' />
        public bool Peek(out TQueueITem item)
        {
            item = default(TQueueITem);
            try
            {
                lock (_sync_mutex)
                {
                    int index = 0;
                    int peekHead = _head;
                    do               //fetch the next valid operation from the queue
                    {
                        if (peekHead < _tail)  //or contition checks if the _tail has reached max long value and _head has not yet reached there , so in this case _head<_tail will fail bcz _tail has been reinitialized
                        {
                            index = ++peekHead;
                            item = (TQueueITem)_queue[index];    //get key on which the operation is to be performed from the head of the queue

                            if (item != null)
                            {
                                return true;
                            }
                            else
                            {
                                if (_head == int.MaxValue)     //checks if _head has reached the max long value, so reinitialize _head and make _tailMaxReached is set to false as _head<_tail is now again valid
                                {
                                    _head = -1;
                                    _tailMaxReached = false;
                                }

                                _head++;
                            }

                                     
                        }
                        else
                            break;
                    } while (item == null);
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
            return false;
        }

        public bool TryGetValue(TKey key,out TQueueITem value)
        {
            value = default(TQueueITem);
            lock (_sync_mutex)
            {
                if (_keyToIndexMap.ContainsKey(key))
                {
                    int index = (int)_keyToIndexMap[key];
                    value = (TQueueITem)_queue[index];
                    return true;
                }
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            lock (_sync_mutex)
            {
                return _keyToIndexMap.ContainsKey(key);
            }
        }

        public bool AllowRemoval(string key)
        {
            lock (_sync_mutex) { return !_keyToIndexMap.ContainsKey(key); }
        }

        public TQueueITem Remove(string key)
        {
            lock (_sync_mutex)
            {
                if (_keyToIndexMap.ContainsKey(key))
                {
                    int index = (int)_keyToIndexMap[key];
                    TQueueITem operation = (TQueueITem)_queue[index];
                    _keyToIndexMap.Remove(key);
                    _indexToKeyMap.Remove(index);
                    _queue.Remove(index);
                    _size -= operation.Size;
                    _count--;

                    if (index == _head)
                    {
                        if (_head == int.MaxValue)     //checks if _head has reached the max long value, so reinitialize _head and make _tailMaxReached is set to false as _head<_tail is now again valid
                        {
                            _head = -1;
                            _tailMaxReached = false;
                        }

                        _head++;
                    }
                    
                    return operation;
                }
            }
            return default(TQueueITem);
        }

        public bool Contains(string cacheKey)
        {
            lock (_sync_mutex)
            {
                return _keyToIndexMap.ContainsKey(cacheKey);
            }
        }

        public TQueueITem this[string key]
        {
            get
            {
                lock (_sync_mutex)
                {
                    if (_keyToIndexMap.ContainsKey(key))
                    {
                        int index = (int)_keyToIndexMap[key];
                        TQueueITem operation = (TQueueITem)_queue[index] ;
                        return operation;
                    }
                }
                return default(TQueueITem);
            }
        }

        /// <summary>
        /// Clears queue and helping datastructures like map, cache, itemstobereplicated
        /// </summary>
        public void Clear()
        {
            try
            {
                lock (_sync_mutex)
                {
                    _queue.Clear();
                    _keyToIndexMap.Clear();
                    _indexToKeyMap.Clear();
                    _tail = _head = -1;
                    _tailMaxReached = false;
                    _size = 0;
                    _count = 0;
                }

            }
            catch (Exception exp)
            {
                throw exp;
            }

        }
        public bool IsEmpty
        {
            get { return _count == 0; }
        }

        public IEnumerator<TQueueITem> GetEnumerator()
        {
            lock (_sync_mutex)
            {
                return new ValueEnumerator(this);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (_sync_mutex)
            {
            }
        }

        #endregion

        class ValueEnumerator : IEnumerator<TQueueITem>
        {
            private OptimizedQueue<TKey, TQueueITem> _parent;
            private int _head, _tail;
            private long _count;
            private bool _tailMaxReached;
            private TQueueITem _current = default(TQueueITem);
            private bool _isValid = false;

            public ValueEnumerator(OptimizedQueue<TKey,TQueueITem> parent)
            {
                _parent = parent;
                Reset();
            }

            public TQueueITem Current
            {
                get
                {
                    if (!_isValid) throw new InvalidOperationException("Enumerator is invalid");
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (!_isValid) throw new InvalidOperationException("Enumerator is invalid");
                    return _current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _current = default(TQueueITem);
                bool hasItem = false;

                if (_count > 0)
                {
                    lock (_parent._sync_mutex)
                    {
                        int index = 0;
                        do               //fetch the next valid operation from the queue
                        {
                            if (_head < _tail || _tailMaxReached)  //or contition checks if the _tail has reached max long value and _head has not yet reached there , so in this case _head<_tail will fail bcz _tail has been reinitialized
                            {
                                if (_head == int.MaxValue)     //checks if _head has reached the max long value, so reinitialize _head and make _tailMaxReached is set to false as _head<_tail is now again valid
                                {
                                    _head = -1;
                                    _tailMaxReached = false;
                                }

                                index = ++_head;
                                _current = (TQueueITem)_parent._queue[index];    //get key on which the operation is to be performed from the head of the queue

                                if (_current != null)
                                {
                                    _count--;
                                    hasItem = true;
                                }
                            }
                            else
                                break;
                        } while (_current == null);
                    }
                }
                _isValid = hasItem;
                return hasItem;
            }

            public void Reset()
            {
                _head = _parent._head;
                _tail = _parent._tail;
                _count = _parent._count;
                _tailMaxReached = _parent._tailMaxReached;
            }
        }
    }
}