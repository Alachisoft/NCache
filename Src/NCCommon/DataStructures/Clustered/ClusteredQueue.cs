// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==
/*=============================================================================
**
** Class: QueueMap
**
** Purpose: A 2d-circular-array implementation of a generic queue.
**
** Date: January 28, 2003
**
=============================================================================*/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Transactions;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    // A simple Queue of generic objects.  Internally it is implemented as a 
    // circular two dimensional buffer, so Enqueue can be O(n).  Dequeue is O(1).
    [DebuggerDisplay("Count = {Count}")]
#if !SILVERLIGHT
    [Serializable()]
#endif
    [System.Runtime.InteropServices.ComVisible(false)]
    public class ClusteredQueue<T> : IEnumerable<T>,
        System.Collections.ICollection, ICloneable
    {
        private ClusteredArray<T> _array;
        private int _head;       // First valid element in the queue
        private int _tail;       // Last valid element in the queue
        private int _size;       // Number of elements.
        private int _version;
        [NonSerialized]
        private Transaction _transaction;
#if !SILVERLIGHT
        [NonSerialized]
#endif
        private Object _syncRoot;

        private const int _MinimumGrow = 4;
        private const int _ShrinkThreshold = 32;
        private int _GrowFactor = 200;  // double each time
        private const int _DefaultCapacity = 4;
        static ClusteredArray<T> _emptyArray = new ClusteredArray<T>(0);

        // Creates a queue with room for capacity objects. The default initial
        // capacity and grow factor are used.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Queue"]/*' />
        public ClusteredQueue()
        {
            _array = _emptyArray;
        }

        // Creates a queue with room for capacity objects. The default grow factor
        // is used.
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Queue1"]/*' />
        public ClusteredQueue(int capacity)
        {
            if (capacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);

            _array = new ClusteredArray<T>(capacity);
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        // Creates a queue with room for capacity objects with the given grow factor
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Queue1"]/*' />
        public ClusteredQueue(int capacity, float growFactor)
        {
            if (capacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            if (!(growFactor >= 1.0 && growFactor <= 10.0))
                throw new ArgumentOutOfRangeException("growFactor", ResourceHelper.GetResourceString("ArgumentOutOfRange_QueueGrowFactor"));

            _array = new ClusteredArray<T>(capacity);
            _head = 0;
            _tail = 0;
            _size = 0;
            _GrowFactor = (int)(growFactor * 100);
        }

        // Fills a Queue with the elements of an ICollection.  Uses the enumerator
        // to get each of the elements.
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Queue3"]/*' />
        public ClusteredQueue(IEnumerable<T> collection)
        {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

            _array = new ClusteredArray<T>(_DefaultCapacity);
            _size = 0;
            _version = 0;

            using (IEnumerator<T> en = collection.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    Enqueue(en.Current);
                }
            }
        }

        private void SetSize(int size)
        {
            _size = size;
        }
        private void SetVersion(int version)
        {
            _version = version;
        }

        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Count"]/*' />
        public virtual int Count
        {
            get { return _size; }
        }

        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.IsSynchronized"]/*' />
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        Object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Removes all Objects from the queue.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Clear"]/*' />
        public virtual void Clear()
        {
            ClusteredArray<T> oldArray = null;
            if (_transaction != null)
            {
                 oldArray = _array;
                _array = new ClusteredArray<T>(0);
            }
            else if (_head < _tail)
                ClusteredArray<T>.Clear(_array, _head, _size);
            else
            {
                ClusteredArray<T>.Clear(_array, _head, _array.Length - _head);
                ClusteredArray<T>.Clear(_array, 0, _tail);
            }

            _head = 0;
            _tail = 0;
            _size = 0;
            _version++;

            if (_transaction != null)
            {
                _transaction.AddRollbackOperation(new ClearRollbackOperation(this, oldArray));
            }

        }

        public static ClusteredQueue<T> Synchronized(ClusteredQueue<T> map)
        {
            return new SynchronizedQueueMap<T>(map);
        }

        public object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        // 
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.CopyTo"]/*' />
        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex, ExceptionResource.ArgumentOutOfRange_Index);
            }

            int arrayLen = array.Length;
            if (arrayLen - arrayIndex < _size)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            int numToCopy = (arrayLen - arrayIndex < _size) ? (arrayLen - arrayIndex) : _size;
            if (numToCopy == 0) return;

            int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
            _array.CopyTo(array, arrayIndex, _head, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
            {
                _array.CopyTo(array, arrayIndex + _array.Length - _head, 0, numToCopy);
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            int arrayLen = array.Length;
            if (index < 0 || index > arrayLen)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }

            if (arrayLen - index < _size)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }

            int numToCopy = (arrayLen - index < _size) ? arrayLen - index : _size;
            if (numToCopy == 0) return;

            try
            {
                int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
                _array.CopyTo(array, index, _head, firstPart);
                numToCopy -= firstPart;

                if (numToCopy > 0)
                {
                    _array.CopyTo(array, index + _array.Length - _head, 0, numToCopy);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
            }
        }

        // Adds item to the tail of the queue.
        //
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Enqueue"]/*' />
        public virtual void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                int newcapacity = (int)((long)_array.Length * (long)_GrowFactor / 100);
                if (newcapacity < _array.Length + _MinimumGrow)
                {
                    newcapacity = _array.Length + _MinimumGrow;
                }
                SetCapacity(newcapacity);
            }
            _array[_tail] = item;
            _tail = (_tail + 1) % _array.Length;
            _size++;
            _version++;

            if (_transaction != null)
            {
                _transaction.AddRollbackOperation(new EnqueueRollbackOperation(this, (_tail - 1== -1)? _array.Length-1: _tail - 1));
            }
        }

        // GetEnumerator returns an IEnumerator over this Queue.  This
        // Enumerator will support removing.
        // 
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.GetEnumerator"]/*' />
        public virtual Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.IEnumerable.GetEnumerator"]/*' />
        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        // Removes the object at the head of the queue and returns it. If the queue
        // is empty, this method simply returns null.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Dequeue"]/*' />
        public virtual T Dequeue()
        {
            if (_size == 0)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EmptyQueue);

            T removed = _array[_head];
            _array[_head] = default(T);
            _head = (_head + 1) % _array.Length;
            _size--;
            _version++;
            if (_transaction != null)
            {
                _transaction.AddRollbackOperation(new DequeueRollbackOperation(this, (_head - 1==-1)? _array.Length - 1:_head-1, removed));

            }
            return removed;
        }

        // Returns the object at the head of the queue. The object remains in the
        // queue. If the queue is empty, this method throws an 
        // InvalidOperationException.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Peek"]/*' />
        public virtual T Peek()
        {
            if (_size == 0)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EmptyQueue);

            return _array[_head];
        }

        // Returns true if the queue contains at least one object equal to item.
        // Equality is determined using item.Equals().
        //
        // Exceptions: ArgumentNullException if item == null.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.Contains"]/*' />
        public virtual bool Contains(T item)
        {
            int index = _head;
            int count = _size;

            EqualityComparer<T> c = EqualityComparer<T>.Default;
            while (count-- > 0)
            {
                if (((Object)item) == null)
                {
                    if (((Object)_array[index]) == null)
                        return true;
                }
                else if (_array[index] != null && c.Equals(_array[index], item))
                {
                    return true;
                }
                index = (index + 1) % _array.Length;
            }

            return false;
        }

        public T GetElement(int i)
        {
            return _array[(_head + i) % _array.Length];
        }

        public void SetElement(int i, T item)
        {
            _array[(_head + i) % _array.Length] = item;
        }

        // Iterates over the objects in the queue, returning an array of the
        // objects in the Queue, or an empty array if the queue is empty.
        // The order of elements in the array is first in to last in, the same
        // order produced by successive calls to Dequeue.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="Queue.ToArray"]/*' />
        public virtual T[] ToArray()
        {
            T[] arr = new T[_size];
            if (_size == 0)
                return arr;

            if (_head < _tail)
            {
                _array.CopyTo(arr, 0, _head, _size);
            }
            else
            {
                _array.CopyTo(arr, 0, _head, _array.Length - _head);
                _array.CopyTo(arr, _array.Length - _head, 0, _tail);
            }

            return arr;
        }


        // PRIVATE Grows or shrinks the buffer to hold capacity objects. Capacity
        // must be >= _size.
        private void SetCapacity(int capacity)
        {
            ClusteredArray<T> newarray = new ClusteredArray<T>(capacity);
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    ClusteredArray<T>.Copy(_array, _head, newarray, 0, _size);
                }
                else
                {
                    ClusteredArray<T>.Copy(_array, _head, newarray, 0, _array.Length - _head);
                    ClusteredArray<T>.Copy(_array, 0, newarray, _array.Length - _head, _tail);
                }
            }

            _array = newarray;
            _head = 0;
            _tail = (_size == capacity) ? 0 : _size;
            _version++;
        }

        public void TrimToSize()
        {
            SetCapacity(_size);
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_array.Length) * 0.9);
            if (_size < threshold)
            {
                SetCapacity(_size);
            }
        }

        // Implements an enumerator for a Queue.  The enumerator uses the
        // internal version number of the list to ensure that no modifications are
        // made to the list while an enumeration is in progress.
        /// <include file='doc\Queue.uex' path='docs/doc[@for="QueueEnumerator"]/*' />
#if !SILVERLIGHT
        [Serializable()]
#endif
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "not an expected scenario")]
        public struct Enumerator : IEnumerator<T>,
            System.Collections.IEnumerator
        {
            private ClusteredQueue<T> _q;
            private int _index;   // -1 = not started, -2 = ended/disposed
            private int _version;
            private T _currentElement;

            internal Enumerator(ClusteredQueue<T> q)
            {
                _q = q;
                _version = _q._version;
                _index = -1;
                _currentElement = default(T);
            }
            /// <include file='doc\Queue.uex' path='docs/doc[@for="QueueEnumerator.Dispose"]/*' />
            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
            }

            /// <include file='doc\Queue.uex' path='docs/doc[@for="QueueEnumerator.MoveNext"]/*' />
            public bool MoveNext()
            {
                if (_version != _q._version) ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);

                if (_index == -2)
                    return false;

                _index++;

                if (_index == _q._size)
                {
                    _index = -2;
                    _currentElement = default(T);
                    return false;
                }

                _currentElement = _q.GetElement(_index);
                return true;
            }

            /// <include file='doc\Queue.uex' path='docs/doc[@for="QueueEnumerator.Current"]/*' />
            public T Current
            {
                get
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
                        else
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
                    }
                    return _currentElement;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
                        else
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
                    }
                    return _currentElement;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_version != _q._version) ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                _index = -1;
                _currentElement = default(T);
            }
        }

        // Implements a synchronization wrapper around a queue.
        [Serializable]
        private class SynchronizedQueueMap<T> : ClusteredQueue<T>
        {
            private ClusteredQueue<T> _q;
            private Object root;

            internal SynchronizedQueueMap(ClusteredQueue<T> q)
            {
                this._q = q;
                root = _q._syncRoot;
            }

            public bool IsSynchronized
            {
                get { return true; }
            }

            public new Object SyncRoot
            {
                get
                {
                    return root;
                }
            }

            public override int Count
            {
                get
                {
                    lock (root)
                    {
                        return _q.Count;
                    }
                }
            }

            public override void Clear()
            {
                lock (root)
                {
                    _q.Clear();
                }
            }

            public override Object Clone()
            {
                lock (root)
                {
                    return new SynchronizedQueueMap<T>((ClusteredQueue<T>)_q.Clone());
                }
            }

            public override bool Contains(T obj)
            {
                lock (root)
                {
                    return _q.Contains(obj);
                }
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                lock (root)
                {
                    _q.CopyTo(array, arrayIndex);
                }
            }

            public override void Enqueue(T value)
            {
                lock (root)
                {
                    _q.Enqueue(value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Thread safety problems with precondition - can't express the precondition as of Dev10.
            public override T Dequeue()
            {
                lock (root)
                {
                    return _q.Dequeue();
                }
            }

            public override Enumerator GetEnumerator()
            {
                lock (root)
                {
                    return _q.GetEnumerator();
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Thread safety problems with precondition - can't express the precondition as of Dev10.
            public override T Peek()
            {
                lock (root)
                {
                    return _q.Peek();
                }
            }

            public override T[] ToArray()
            {
                lock (root)
                {
                    return _q.ToArray();
                }
            }

        }


        public virtual object Clone()
        {
            ClusteredQueue<T> newMap = new ClusteredQueue<T>();
            newMap._array = (ClusteredArray<T>)_array.Clone();
            newMap._GrowFactor = _GrowFactor;
            newMap._head = _head;
            newMap._size = _size;
            newMap._version = _version;
            return newMap;
        }
        #region /*         Transactions */
        private void EnqueueRollback(int index)
        {
            _array[index] = default(T);
            _tail = (index) % _array.Length;
            _size--;
        }
        private void DequeueRollback(int index,T item)
        {
            _array[index] = item;
            _head = (index) % _array.Length;
            _size++;
        }
        public bool BeginTransaction()
        {
            if (_transaction == null)
                _transaction = new Transaction();
            return true;
        }
        public void CommitTransaction()
        {
            if (_transaction != null) _transaction.Commit();
        }
        public void RollbackTransaction()
        {
            if (_transaction != null) _transaction.Rollback();
        }
        #endregion

        #region /                       ---- Rollback Operations ----                           /

        class EnqueueRollbackOperation : IRollbackOperation
        {
            private int _index;
            private ClusteredQueue<T> _parent;

            public EnqueueRollbackOperation(ClusteredQueue<T> parent, int index)
            {
                _index = index;
                _parent = parent;
            }

            public void Execute()
            {
                _parent.EnqueueRollback(_index);
            }
        }
        class DequeueRollbackOperation : IRollbackOperation
        {
            private int _index;
            private T _item;
            private ClusteredQueue<T> _parent;

            public DequeueRollbackOperation(ClusteredQueue<T> parent, int index,T item)
            {
                _index = index;
                _parent = parent;
                _item = item;
            }

            public void Execute()
            {
                _parent.DequeueRollback(_index,_item);
            }
        }
        class ClearRollbackOperation : IRollbackOperation
        {
            private ClusteredArray<T> _items;
            private ClusteredQueue<T> _parent;

            public ClearRollbackOperation(ClusteredQueue<T> parent, ClusteredArray<T> items)
            {
                _items = items;
                _parent = parent;
            }
            public void Execute()
            {
                _parent._array = _items;
            }
        }
        #endregion
    }
}
