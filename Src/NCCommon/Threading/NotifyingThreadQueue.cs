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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace Alachisoft.NCache.Common.Threading
{
    [Serializable]
    public class NotifyingThreadQueue<T>
    {
        #region my events
        /// <summary>
        /// signals when the start of the queue changes
        /// </summary>
        public event QueueStateChangedHandler<T> QueueStateChanged;
        /// <summary>
        /// signals when the current operation is finished processing
        /// </summary>
        public event ThreadFinishedHandler<T> ThreadFinished;
        /// <summary>
        /// signals when the current operation had an error OR when an 
        /// enqueued item was removed from the queue prior to processing
        /// </summary>
        public event ThreadErrorHandler<T> ThreadError;
        #endregion

        #region my vars
        private object syncobj;
        private object _extraInfo;
        private int maxthreads;
        private int currentthreads;
        private QueueState qs;
        private Queue<KeyValuePair<T, QueueOperationHandler<T>>> queue;
        private QueueOperationHandler<T> defaultop;
        #endregion

        #region constructors
        /// <summary>
        /// constructs the NotifyingThreadQueue. sets the state to QueueState.Idle
        /// </summary>
        /// <param name="defaultoperation">the default operation to perform on an enqueued item</param>
        /// <exception cref="System.ArgumentNullException">defaultoperation is null</exception>
        public NotifyingThreadQueue(QueueOperationHandler<T> defaultoperation)
            : this(int.MaxValue, defaultoperation)
        {
        }

        /// <summary>
        /// constructs the NotifyingThreadQueue. sets the state to QueueState.Idle
        /// </summary>
        /// <param name="defaultoperation">the default operation to perform on an enqueued item</param>
        /// <exception cref="System.ArgumentNullException">defaultoperation is null</exception>
        public NotifyingThreadQueue(QueueOperationHandler<T> defaultoperation, object extraInfo)
            : this(int.MaxValue, defaultoperation)
        {
            _extraInfo = extraInfo;
        }

        /// <summary>
        /// constructs the NotifyingThreadQueue. sets the state to QueueState.Idle
        /// </summary>
        /// <param name="maxthreads">sets the maximum number of simultaneous operations</param>
        /// <param name="defaultoperation">the default operation to perform on an enqueued item</param>
        /// <exception cref="System.ArgumentException">maxthreads is less than or equal to 0</exception>
        /// <exception cref="System.ArgumentNullException">defaultoperation is null</exception>
        public NotifyingThreadQueue(int maxthreads, QueueOperationHandler<T> defaultoperation)
        {
            if (maxthreads <= 0)
                throw new ArgumentException("maxthreads can not be <= 0");
            if (defaultoperation == null)
                throw new ArgumentNullException("defaultoperation can not be null");

            this.qs = QueueState.Idle;
            this.syncobj = new object();
            this.currentthreads = 0;
            this.maxthreads = maxthreads;
            this.queue = new Queue<KeyValuePair<T, QueueOperationHandler<T>>>();
            this.defaultop = defaultoperation;
        }
        #endregion

        #region Properties
        public object ExtraInfo
        {
            get
            {
                return _extraInfo;
            }
            set
            {
                _extraInfo = value;
            }
        } 
        #endregion

        #region control ops
        /// <summary>
        /// pauses the execution of future operations. the current operations are allowed to finish.
        /// </summary>
        public void Pause()
        {
            lock (syncobj)
            {
                if (qs == QueueState.Idle)
                {
                    /// this is a judgment call if you pause this when you 
                    /// don’t have any elements in it then you can go directly 
                    /// to paused and this means that you basically want to 
                    /// keep queuing until something happens                    
                    qs = QueueState.Paused;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Idle, QueueState.Paused));
                }
                else if (qs == QueueState.Running)
                {
                    qs = QueueState.Pauseing;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Running, QueueState.Pauseing));

                    /// running means you had some active threads so you couldn’t 
                    /// get to paused right away
                }
                else if (qs == QueueState.Stopping)
                {
                    ThreadErrorInternal(default(T), new ThreadStateException("Once the queue is stopping  its done processing"));
                }
                /// if we are already paused or pausing we dont need to do anything


            }
        }
        /// <summary>
        /// stops the execution of future operations. clears out all pending operations. 
        /// no further operations are allowed to be enqueued. the current operations are 
        /// allowed to finish.
        /// </summary>
        public void Stop()
        {
            lock (syncobj)
            {
                if ((qs == QueueState.Idle) || (qs == QueueState.Stopping))
                {
                    /// do nothing idle has nothing to stop and stopping  
                    /// is already working on stopping 
                    return;
                }
                else if (qs == QueueState.Paused)
                {
                    qs = QueueState.Stopping;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Paused, QueueState.Stopping));

                    /// if we are already paused then we have no threads running 
                    /// so just drop all the extra items in the queue
                    while (queue.Count != 0)
                        ThreadErrorInternal(queue.Dequeue().Key, new ThreadStateException("the Queue is stopping . no processing done"));

                    /// ensure proper event flow paused-> stopping -> idle
                    qs = QueueState.Idle;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Stopping, QueueState.Idle));
                }
                else
                {
                    QueueState temState = this.QueueState;
                    qs = QueueState.Stopping;
                    QueueStateChangedInternal(this, new QueueChangedState(temState, QueueState.Stopping));

                    /// why are we not dequeuing everything? that’s b/c if we have threads 
                    /// left they have to finish in their own good time so they can go 
                    /// through the process of getting rid of all the others. both ways work
                    if (currentthreads == 0)
                    {
                        qs = QueueState.Idle;
                        QueueStateChangedInternal(this, new QueueChangedState(QueueState.Stopping, QueueState.Idle));
                    }
                }
            }
        }
        /// <summary>
        /// continues the execution of enqueued operations after a pause.
        /// </summary>
        public void Continue()
        {
            lock (syncobj)
            {
                if ((qs == QueueState.Pauseing) || (qs == QueueState.Paused))
                {
                    qs = QueueState.Running;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Paused, QueueState.Running));

                    //while (currentthreads < maxthreads)
                    {
                        TryNewThread();
                        //Queuestate may have changed, so return if doing nothing
                        //if (qs == QueueState.Idle || qs == QueueState.Paused) { return; }
                    }
                }
                else if ((qs == QueueState.Idle) || (qs == QueueState.Running))
                {
                    /// Continuing to process while the queue is idle is meaning 
                    /// less just ignore the command
                    return;
                }
                else if (qs == QueueState.Stopping)
                {
                    ThreadErrorInternal(default(T), new ThreadStateException("Once the queue is stopping  its done processing"));
                }
            }
        }
        #endregion

        #region data axcessors
        /// <summary>
        /// gets the current QueueState
        /// </summary>
        public QueueState QueueState
        {
            get
            {
                lock (syncobj)
                {
                    return qs;
                }
            }
        }
        /// <summary>
        /// gets the maximum number of operations that can be executed at once
        /// </summary>
        public int MaxThreads
        {
            get
            {
                return maxthreads;
            }
        }
        /// <summary>
        /// gets the current number of current ongoing operations
        /// </summary>
        public int CurrentRunningThreads
        {
            get
            {
                lock (syncobj)
                {
                    return currentthreads;
                }

            }
        }

        public int Count
        {
            get { return this.queue.Count; }
        }
        #endregion

        #region enque ops
        /// <summary>
        /// Adds the item to the queue to process asynchronously.
        /// </summary>
        /// <param name="item">the item to enqueue</param>
        public void EnQueue(T item)
        {
            EnQueue(item, defaultop);
        }
        /// <summary>
        /// Adds the item to the queue to process asynchronously and 
        /// uses the different operation instead  of the default.
        /// </summary>
        /// <param name="item">the item to enqueue</param>
        /// <param name="opp">the new operation that overrides the default</param>
        /// <exception cref="System.ArgumentNullException">opp is null</exception>
        public void EnQueue(T item, QueueOperationHandler<T> opp)
        {
            if (opp == null)
                throw new ArgumentNullException("operation can not be null");

            lock (syncobj)
            {
                if (qs == QueueState.Idle)
                {
                    #region idle
                    qs = QueueState.Running;
                    QueueStateChangedInternal(this, new QueueChangedState(QueueState.Idle, QueueState.Running));

                    /// the problem with generics is that sometimes the fully 
                    /// descriptive name goes on for a while
                    KeyValuePair<T, QueueOperationHandler<T>> kvp = new KeyValuePair<T, QueueOperationHandler<T>>(item, opp);

                    /// thread demands that its ParameterizedThreadStart take an object not a generic type
                    /// one might have resonably thought that there would be a generic constructor that 
                    /// took a strongly typed value but there is not one
                    currentthreads++;
                    new Thread(new ParameterizedThreadStart(RunOpp)).Start(kvp);

                    #endregion
                }
                else if ((qs == QueueState.Paused) || (qs == QueueState.Pauseing))
                {
                    #region pause
                    /// in the case that we are pausing or currently paused we just add the value to the
                    /// queue we dont try to run the process 
                    queue.Enqueue(new KeyValuePair<T, QueueOperationHandler<T>>(item, opp));
                    #endregion
                }
                else if (qs == QueueState.Running)
                {
                    #region running
                    /// you have to enqueue the item then try to execute the first item in the process
                    /// always enqueue first as this ensures that you get the oldest item first since 
                    /// that is what you wanted to do you did not want a stack
                    queue.Enqueue(new KeyValuePair<T, QueueOperationHandler<T>>(item, opp));
                    TryNewThread();
                    #endregion
                }
                else if (qs == QueueState.Stopping)
                {
                    #region stopping
                    /// when you are stopping the queue i assume that you wanted to stop it not pause it this 
                    /// means that if you try to enqueue something it will throw an exception since you 
                    /// shouldnt be enqueueing anything since when the queue gets done all its current 
                    /// threads it clears the rest out so why bother enqueueing it. at this point we have 
                    /// a choice we can make the notifyer die or we can use the error event we already 
                    /// have built in to tell the sender. i chose the later. also try to pick an appropriate 
                    /// exception not just the base
                    ThreadErrorInternal(item, new ThreadStateException("the Queue is stopping . no processing done"));
                    #endregion
                }
            }
        }

        #region tools
        private void RunOpp(object o)
        {
            KeyValuePair<T, QueueOperationHandler<T>> kvp = (KeyValuePair<T, QueueOperationHandler<T>>)o;

            try
            {
                kvp.Value(kvp.Key);
                ThreadFinishedInternal(kvp.Key);
            }
            catch (Exception ex)
            {
                ThreadErrorInternal(kvp.Key, new ThreadStateException("error processing. partial processing done.", ex));
            }
            finally
            {
                lock (syncobj)
                {
                    currentthreads--;
                }
                TryNewThread();
            }
        }
        private void TryNewThread()
        {
            lock (syncobj)
            {
                if (qs == QueueState.Running)
                {
                    #region Running
                    if (queue.Count != 0)
                    {
                        if (currentthreads < maxthreads)
                        {
                            currentthreads++;
                            new Thread(new ParameterizedThreadStart(RunOpp)).Start(queue.Dequeue());
                        }
                    }
                    else
                    {
                        if (currentthreads == 0)
                        {
                            qs = QueueState.Idle;
                            QueueStateChangedInternal(this, new QueueChangedState(QueueState.Running, QueueState.Idle));
                        }
                    }
                    #endregion
                }
                else if (qs == QueueState.Stopping)
                {
                    #region stopping
                    /// normally when we stop a queue we can just clear out the remaining 
                    /// values and let the threads peter out. however, we made the decision 
                    /// to throw an exception by way of our exception handler. it is therefore 
                    /// important to keep with that and get rid of all the queue items in 
                    /// that same way
                    while (queue.Count != 0)
                        ThreadErrorInternal(queue.Dequeue().Key, new ThreadStateException("the Queue is stopping . no processing done"));

                    /// all threads come through here so its up to us to single the change 
                    /// from stopping to idle
                    if (currentthreads == 0)
                    {
                        qs = QueueState.Idle;
                        QueueStateChangedInternal(this, new QueueChangedState(QueueState.Stopping, QueueState.Idle));
                    }
                    #endregion
                }
                else if (qs == QueueState.Pauseing)
                {
                    #region Pauseing
                    if (currentthreads == 0)
                    {
                        qs = QueueState.Paused;
                        QueueStateChangedInternal(this, new QueueChangedState(QueueState.Pauseing, QueueState.Paused));
                    }
                    #endregion
                }
                else
                {
                    #region Idle / Paused
                    /// there should be no way to got in here while your idle or paused
                    /// this is just an error check
                    ThreadErrorInternal(default(T), new Exception("internal state bad"));
                    #endregion
                }
            }
        }
        #endregion
        #endregion

        #region event fowarders
        /// during testing i found out that it is likely that the events will be 
        /// updating controls and that this means that if care is not taken then 
        /// the events throw exceptions this is probably not desirable so i decided 
        /// to fix this by just dumping these errors for you silently. this is a 
        /// double edged sword though. if things are not appearing in your controls 
        /// this might be the culprit
        private void QueueStateChangedInternal(object sender, QueueChangedState qs)
        {
            try
            {
                if (QueueStateChanged != null)
                    QueueStateChanged(sender, qs);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("Cross-thread operation not valid"))
                    Console.WriteLine(ex.Message);
                else
                    throw ex;
            }
        }
        private void ThreadFinishedInternal(T finisheditem)
        {
            try
            {
                if (ThreadFinished != null)
                    ThreadFinished(finisheditem);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("Cross-thread operation not valid"))
                    Console.WriteLine(ex.Message);
                else
                    throw ex;
            }
        }
        private void ThreadErrorInternal(T unfinisheditem, Exception ex)
        {
            try
            {
                if (ThreadError != null)
                    ThreadError(unfinisheditem, ex);
            }
            catch (InvalidOperationException ex2)
            {
                if (ex.Message.StartsWith("Cross-thread operation not valid"))
                    Console.WriteLine(ex2.Message);
                else
                    throw ex2;
            }
        }
        #endregion
    }
}