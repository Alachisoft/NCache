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
using System.Threading;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary> Elements are added at the tail and removed from the head. Class is thread-safe in that
    /// 1 producer and 1 consumer may add/remove elements concurrently. The class is not
    /// explicitely designed for multiple producers or consumers.
    /// </summary>
    /// <author> 
    /// </author>
    public class Queue
    {
        protected System.Collections.Queue[] _queues = new System.Collections.Queue[3];

        /*flag to determine the state of the queue*/
        protected bool closed = false;

        /*current size of the queue*/
        protected int size = 0;

        /* Lock object for synchronization. Is notified when element is added */
        protected object mutex = new object();

        /// <summary>Lock object for syncing on removes. It is notified when an object is removed </summary>
        // Object  remove_mutex=new Object();

        /*the number of end markers that have been added*/
        protected int num_markers = 0;

        /// <summary> if the queue closes during the runtime
        /// an endMarker object is added to the end of the queue to indicate that
        /// the queue will close automatically when the end marker is encountered
        /// This allows for a "soft" close.
        /// </summary>
        /// <seealso cref="Queue#close">
        /// </seealso>
        protected static readonly object endMarker = new object();


        /// <summary> creates an empty queue</summary>
        public Queue()
        {
            _queues[(int)Priority.High] = new System.Collections.Queue(11, 1.0f);
            _queues[(int)Priority.Normal] = new System.Collections.Queue(11, 1.0f);
            _queues[(int)Priority.Low] = new System.Collections.Queue(11, 1.0f);
        }

        /// <summary>
        /// Number of Objects in the queue
        /// </summary>
        public int Count
        {
            get { return size - num_markers; }
        }

        /// <summary> returns true if the Queue has been closed
        /// however, this method will return false if the queue has been closed
        /// using the close(true) method and the last element has yet not been received.
        /// </summary>
        /// <returns> true if the queue has been closed
        /// </returns>
        public bool Closed
        {
            get { return closed; }
        }

        /// <summary> adds an object to the tail of this queue
        /// If the queue has been closed with close(true) no exception will be
        /// thrown if the queue has not been flushed yet.
        /// </summary>
        /// <param name="obj">- the object to be added to the queue
        /// </param>
        /// <exception cref=""> QueueClosedException exception if closed() returns true
        /// </exception>
        public void add(object obj)
        {
            add(obj, Priority.Normal);
        }

        /// <summary> adds an object to the tail of this queue
        /// If the queue has been closed with close(true) no exception will be
        /// thrown if the queue has not been flushed yet.
        /// </summary>
        /// <param name="obj">- the object to be added to the queue
        /// </param>
        /// <param name="priority">- the priority of the object
        /// </param>
        /// <exception cref=""> QueueClosedException exception if closed() returns true
        /// </exception>
        public void add(object obj, Priority priority)
        {
            if (obj == null)
            {
                return;
            }
            if (priority == Priority.Critical)
                priority = Priority.High;

            lock (mutex)
            {
                if (closed)
                    throw new QueueClosedException();
                if (this.num_markers > 0)
                    throw new QueueClosedException("Queue.add(): queue has been closed. You can not add more elements. " + "Waiting for removal of remaining elements.");

                _queues[(int)priority].Enqueue(obj);
                size++;
                Monitor.PulseAll(mutex);
            }
        }



        /// <summary> Removes 1 element from head or <B>blocks</B>
        /// until next element has been added or until queue has been closed
        /// </summary>
        /// <returns> the first element to be taken of the queue
        /// </returns>
        public object remove()
        {
            object retval = null;
            try
            {
                retval = remove(Timeout.Infinite);
            }
            catch (Runtime.Exceptions.TimeoutException)
            {
            }
            return retval;
        }


        /// <summary> Removes 1 element from the head.
        /// If the queue is empty the operation will wait for timeout ms.
        /// if no object is added during the timeout time, a Timout exception is thrown
        /// </summary>
        /// <param name="timeout">- the number of milli seconds this operation will wait before it times out
        /// </param>
        /// <returns> the first object in the queue
        /// </returns>
        public object remove(long timeout)
        {
            object retval = null;

            /*lock the queue*/
            lock (mutex)
            {
                /*if the queue size is zero, we want to wait until a new object is added*/
                if (size == 0)
                {
                    if (closed) throw new QueueClosedException();

                    /*release the add_mutex lock and wait no more than timeout ms*/
                    if (!Monitor.Wait(mutex, (int)timeout))
                    {
                        throw new Runtime.Exceptions.TimeoutException();
                    }
                }
                /*check to see if the object closed*/
                if (closed) throw new QueueClosedException();


                /*get the next value*/
                retval = RemoveItemFromQueue();

                /*if we reached an end marker we are going to close the queue*/
                if (retval == endMarker)
                {
                    close(false);
                    throw new QueueClosedException();
                }

                /*at this point we actually did receive a value from the queue, return it*/
                return retval;
            }
        }

        protected virtual Object RemoveItemFromQueue()
        {
            Object retval = null;
            if (_queues[(int)Priority.High].Count > 0)
                retval = removeInternal(_queues[(int)Priority.High]);
            else if (_queues[(int)Priority.Normal].Count > 0)
                retval = removeInternal(_queues[(int)Priority.Normal]);
            else if (_queues[(int)Priority.Low].Count > 0)
                retval = removeInternal(_queues[(int)Priority.Low]);

            return retval;
        }


        /// <summary> returns the first object on the queue, without removing it.
        /// If the queue is empty this object blocks until the first queue object has
        /// been added
        /// </summary>
        /// <returns> the first object on the queue
        /// </returns>
        public object peek()
        {
            object retval = null;
            bool success;
            retval = peek(Timeout.Infinite, out success);
            return retval;
        }


        /// <summary> returns the first object on the queue, without removing it.
        /// If the queue is empty this object blocks until the first queue object has
        /// been added or the operation times out
        /// </summary>
        /// <param name="timeout">how long in milli seconds will this operation wait for an object to be added to the queue
        /// before it times out
        /// </param>
        /// <param name="error">this param is set to false if timeout occurs</param>
        /// <returns> the first object on the queue
        /// </returns>

        public object peek(long timeout, out bool success)
        {
            object retval = null;
            success = true;
            lock (mutex)
            {
                if (size == 0)
                {
                    if (closed)
                        throw new QueueClosedException();

                    if (!Monitor.Wait(mutex, (int)timeout))
                    {
                        success = false;
                        return null;
                    }
                }

                if (closed)
                    throw new QueueClosedException();

                retval = peekInternal();

                if (retval == endMarker)
                {
                    close(false);
                    throw new QueueClosedException();
                }
            }
            return retval;
        }

        private object peekInternal()
        {
            object retval = null;
            if (_queues[(int)Priority.High].Count > 0)
                retval = _queues[(int)Priority.High].Peek();
            else if (_queues[(int)Priority.Normal].Count > 0)
                retval = _queues[(int)Priority.Normal].Peek();
            else if (_queues[(int)Priority.Low].Count > 0)
                retval = _queues[(int)Priority.Low].Peek();
            return retval;
        }

        /// <summary>Marks the queues as closed. When an <code>add</code> or <code>remove</code> operation is
        /// attempted on a closed queue, an exception is thrown.
        /// </summary>
        /// <param name="flush_entries">When true, a end-of-entries marker is added to the end of the queue.
        /// Entries may be added and removed, but when the end-of-entries marker
        /// is encountered, the queue is marked as closed. This allows to flush
        /// pending messages before closing the queue.
        /// </param>
        public virtual void close(bool flush_entries)
        {
            lock (mutex)
            {
                if (flush_entries)
                {
                    try
                    {
                        add(endMarker, Priority.Low);
                        num_markers++;
                    }
                    catch (QueueClosedException ex)
                    {
                    }
                    return;
                }

                closed = true;
                Monitor.PulseAll(mutex);
            }
        }


        /// <summary> resets the queue.
        /// This operation removes all the objects in the queue and marks the queue open
        /// </summary>
        public virtual void reset()
        {
            lock (mutex)
            {
                num_markers = 0;
                if (!closed) close(false);

                size = 0;

                _queues[(int)Priority.High].Clear();
                _queues[(int)Priority.Normal].Clear();
                _queues[(int)Priority.Low].Clear();

                closed = false;
                Monitor.PulseAll(mutex);
            }
        }

        /// <summary> prints the size of the queue</summary>
        public override string ToString()
        {
            return "Queue (" + Count + ") messages";
        }


        /* ------------------------------------- Private Methods ----------------------------------- */


        /// <summary> Removes the first element. Returns null if no elements in queue.
        /// Always called with add_mutex locked (we don't have to lock add_mutex ourselves)
        /// </summary>
        protected object removeInternal(System.Collections.Queue queue)
        {
            object obj = null;
            lock (mutex)
            {
                int count = queue.Count;
                if (count > 0)
                {
                    obj = queue.Dequeue();
                }
                else
                {
                    return null;
                }

                size--;
                if (size < 0)
                    size = 0;

                if (peekInternal() == endMarker)
                    closed = true;
            }

            return obj;
        }
    }

}