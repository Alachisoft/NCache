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
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class OptimizedQueue
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

        private readonly CacheRuntimeContext _context;

        internal OptimizedQueue(CacheRuntimeContext context)
        {
            _context = context;
        }

        public long Size
        {
            get { lock (_sync_mutex) { return _size; } }
        }

        public long Count
        {
            get { lock (_sync_mutex) { return _count; } }
        }

        /// <summary>
        /// Optimized Enqueue opeartion, adds the opeartion at _tail index and removes 
        /// any previous operations on that key from the queue
        /// </summary>
        /// <param name="operation"></param>
        internal bool Enqueue(object cacheKey, IOptimizedQueueOperation operation)
        {
            bool isNewItem = true;
            try
            {
                lock (_sync_mutex)
                {

                    if (_keyToIndexMap.ContainsKey(cacheKey))    //Optimized Queue, so checking in the map if the current cache key is already mapped to some index of Queue or not
                    {
                        //just update the old operation without chaning it's order in the queue.
                        int index1 = (int)_keyToIndexMap[cacheKey];
                        IOptimizedQueueOperation oldOperation = _queue[index1] as IOptimizedQueueOperation;
                        _queue[index1] = operation;
                        isNewItem = false;
                        _size -= oldOperation.Size; //subtract old operation size
                        _size += operation.Size;
                        oldOperation.ReturnPooledItemsToPool(_context?.TransactionalPoolManager);
                        return isNewItem;
                    }

                    if (_tail == int.MaxValue)     //checks if the _tail value has reached the maxvalue of the long data type, so reinitialize it
                    {
                        _tail = -1;
                        _tailMaxReached = true;
                    }

                    int index = ++_tail;
                    _size += operation.Size;
                    _queue.Add(index, operation);   //Add new opeartion at the tail of the queue
                    _keyToIndexMap[cacheKey] = index;        // update (cache key, queue index) map
                    _indexToKeyMap[index] = cacheKey;
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

        internal IOptimizedQueueOperation Dequeue()
        {
            IOptimizedQueueOperation operation = null;
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
                            operation = _queue[index] as IOptimizedQueueOperation;    //get key on which the operation is to be performed from the head of the queue

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

        protected bool AllowRemoval(object cacheKey)
        {
            lock (_sync_mutex) { return !_keyToIndexMap.ContainsKey(cacheKey); }
        }

        /// <summary>
        /// Clears queue and helping datastructures like map, cache, itemstobereplicated
        /// </summary>
        internal void Clear()
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
        #region IDisposable Members

        public void Dispose()
        {
            lock (_sync_mutex)
            {
            }
        }

        #endregion
    }
}