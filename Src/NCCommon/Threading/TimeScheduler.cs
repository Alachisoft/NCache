using System;
using System.Threading;

namespace Alachisoft.NCache.Common.Threading
{
	/// <remarks>
	/// The scheduler supports varying scheduling intervals by asking the task
	/// every time for its next preferred scheduling interval. Scheduling can
	/// either be <i>fixed-delay</i> or <i>fixed-rate</i>. 
	/// In fixed-delay scheduling, the task's new schedule is calculated
	/// as:<br></br>
	/// new_schedule = time_task_starts + scheduling_interval
	/// <p>
	/// In fixed-rate scheduling, the next schedule is calculated as:<br></br>
	/// new_schedule = time_task_was_supposed_to_start + scheduling_interval</p>
	/// <p>
	/// The scheduler internally holds a queue of tasks sorted in ascending order
	/// according to their next execution time. A task is removed from the queue
	/// if it is cancelled, i.e. if <tt>TimeScheduler.Task.isCancelled()</tt>
	/// returns true.
	/// </p>
	/// <p>
	/// Initially, the scheduler is in <tt>SUSPEND</tt>ed mode, <tt>start()</tt>
	/// need not be called: if a task is added, the scheduler gets started
	/// automatically. Calling <tt>start()</tt> starts the scheduler if it's
	/// suspended or stopped else has no effect. Once <tt>stop()</tt> is called,
	/// added tasks will not restart it: <tt>start()</tt> has to be called to
	/// restart the scheduler.
	/// </p>
	/// </remarks>
	/// <summary>
	/// Fixed-delay and fixed-rate single thread scheduler
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public class TimeScheduler: IDisposable
	{
		/// <summary>
		/// The interface that submitted tasks must implement
		/// </summary>
		public interface Task 
		{
			/// <summary>
			/// Returns true if task is cancelled and shouldn't be scheduled again
			/// </summary>
			/// <returns></returns>
			bool IsCancelled();

			/// <summary>
			/// The next schedule interval
			/// </summary>
			/// <returns>The next schedule interval</returns>
			long GetNextInterval();

			/// <summary>
			/// Execute the task
			/// </summary>
			void Run();
		}


		/// <remarks>
		/// Needed in case all tasks have been
		/// cancelled and we are still waiting on the schedule time of the task
		/// at the top
		/// </remarks>
		/// <summary>Regular wake-up intervals for scheduler</summary>
		private const long TICK_INTERVAL = 1000;

		enum State
		{
			/// <summary>State Constant</summary>
			RUN = 0,
			/// <summary>State Constant</summary>
			SUSPEND = 1,
			/// <summary>State Constant</summary>
			STOPPING = 2,
			/// <summary>State Constant</summary>
			STOP = 3,
			/// <summary>State Constant</summary>
			DISPOSED = 4
		}

		/// <summary>TimeScheduler thread name</summary>
		private const String THREAD_NAME = "TimeScheduler.Thread";

		/// <summary>The scheduler thread</summary>
		private Thread thread = null;
		/// <summary>The thread's running state</summary>
		private State thread_state = State.SUSPEND;
		
		/// <summary>Time that task queue is empty before suspending the scheduling thread</summary>
		private long suspend_interval;

		/// <summary>Sorted list of <code>IntTask</code>s </summary>
		private EventQueue queue;

	    bool _concurrent;

        public int QueueCount
        {
            get
            {
                if (queue == null) return 0;
                return queue.Count;
            }
        }

		/// <summary>
		/// Set the thread state to running, create and start the thread
		/// </summary>
		private void _start() 
		{
			lock(this)
			{
				if(thread_state != State.DISPOSED)
				{
					thread_state = State.RUN;
					thread = new Thread(new ThreadStart(_run));
					thread.Name = THREAD_NAME;
					thread.IsBackground = true;
					thread.Start();
				}
			}
		}

		/// <summary>
		/// Restart the suspended thread
		/// </summary>
		private void _unsuspend() 
		{
			_start();
		}

		/// <summary>
		/// Set the thread state to suspended
		/// </summary>
		private void _suspend() 
		{
			lock(this)
			{
				if(thread_state != State.DISPOSED)
				{
					thread_state = State.SUSPEND;
					thread = null;
				}
			}
		}

		/// <summary>
		/// Set the thread state to stopping
		/// </summary>
		private void _stopping() 
		{
			lock(this)
			{
				if(thread_state != State.DISPOSED)
				{
					thread_state = State.STOPPING;
				}
			}
		}

		/// <summary>
		/// Set the thread state to stopped
		/// </summary>
		private void _stop() 
		{
			lock(this)
			{
				if(thread_state != State.DISPOSED)
				{
					thread_state = State.STOP;
					thread = null;
				}
			}
		}


		/// <remarks>
		/// Get the first task, if the running time hasn't been
		/// reached then wait a bit and retry. Else reschedule the task and then
		/// run it. 
		/// </remarks>
		/// <summary>
		/// If the task queue is empty, sleep until a task comes in or if slept
		/// for too long, suspend the thread.
		/// </summary>
		private void _run() 
		{
			long    elapsedTime;
			try 
			{
				while(true) 
				{
					lock(this) 
					{ 
						if (thread == null) return; 
					}

					Task    task = null;
                    bool lockReAcquired = true;
					lock(queue) 
					{
                       
						if(queue.IsEmpty) 
							lockReAcquired = Monitor.Wait(queue, (int)suspend_interval);

                        if (lockReAcquired)
                        {
                            QueuedEvent e = queue.Peek();
                            if (e != null)
                            {
                                lock (e)
                                {
                                    long interval = e.Interval;
                                    task = e.Task;
                                    if (task.IsCancelled())
                                    {
                                        queue.Pop();
                                        continue;
                                    }

                                    elapsedTime = e.ElapsedTime;
                                    if (elapsedTime >= interval)
                                    {
                                        // Reschedule the task
                                        queue.Pop();
                                        if (e.ReQueue())
                                        {
                                            queue.Push(e);
                                        }
                                    }
                                }
                                if (elapsedTime < e.Interval)
                                {
                                    Monitor.Wait(queue, (int)(e.Interval - elapsedTime));
                                    continue;
                                }
                            }
                        }
					}

                    lock (this)
                    {
                        if (queue.IsEmpty && !lockReAcquired)
                        {
                            _suspend();
                            return;
                        }
                    }
					try 
					{
					    if (task != null)
					    {
                            if(_concurrent)
                                ThreadPool.QueueUserWorkItem(ExecuterCallback, task);
                            else
                                task.Run();
                        }
                    } 
					catch(Exception ex) 
					{
                        Trace.error("TimeScheduler._run()", ex.ToString());
					}
				}
			}
			catch(ThreadInterruptedException ex) 
			{
				Trace.error("TimeScheduler._run()",ex.ToString());
			}
		
		}

        private void ExecuterCallback(object state)
        {
            var task = state as Task;
            if (task != null)
            {
                task.Run();
            }
        }

        /// <summary>
        /// Create a scheduler that executes tasks in dynamically adjustable
        /// intervals
        /// </summary>
        /// <param name="suspend_interval">
        /// The time that the scheduler will wait for
        /// at least one task to be placed in the task queue before suspending
        /// the scheduling thread
        /// </param>
        public TimeScheduler(long suspend_interval) 
		{
			queue = new EventQueue();
			this.suspend_interval = suspend_interval;
		}

		/// <summary>
		/// Create a scheduler that executes tasks in dynamically adjustable
		/// intervals
		/// </summary>
		public TimeScheduler() : this(2000){}

        public TimeScheduler(bool concurrent): this(concurrent, 2000) { }
        

        public TimeScheduler(bool concurrent, long interval) : this(interval)
        {
            _concurrent = concurrent;
        }

        /// <remarks>
        /// <b>Relative Scheduling</b>
        /// <tt>true</tt>:<br></br>
        /// Task is rescheduled relative to the last time it <i>actually</i>
        /// started execution
        ///	<p>
        /// <tt>false</tt>:<br></br>
        /// Task is scheduled relative to its <i>last</i> execution schedule. This
        /// has the effect that the time between two consecutive executions of
        /// the task remains the same.
        /// </p>
        /// </remarks>
        /// <summary>
        /// Add a task for execution at adjustable intervals
        /// </summary>
        /// <param name="t">The task to execute</param>
        /// <param name="relative">Use relative scheduling</param>
        public void AddTask(Task t, bool relative) 
		{
			long interval;
			lock(this) 
			{
				if(thread_state == State.DISPOSED) return;
				if((interval = t.GetNextInterval()) < 0) return;

                queue.Push(new QueuedEvent(t));

				switch(thread_state) 
				{
					case State.RUN: break; //Monitor.PulseAll(queue); 
					case State.SUSPEND: _unsuspend(); break;
					case State.STOPPING: break;
					case State.STOP: break;
				}
			}
		}

		/// <summary>
		/// Add a task for execution at adjustable intervals
		/// </summary>
		/// <param name="t">The task to execute</param>
		public void AddTask(Task t) { AddTask(t, true); }

		/// <summary>
		/// Start the scheduler, if it's suspended or stopped
		/// </summary>
		public void Start() 
		{
			lock(this) 
			{
				switch(thread_state) 
				{
					case State.DISPOSED: break;
					case State.RUN: break;
					case State.SUSPEND: _unsuspend(); break;
					case State.STOPPING: break;
					case State.STOP: _start(); break;
				}
			}
		}


		/// <summary>
		/// Stop the scheduler if it's running. Switch to stopped, if it's
		/// suspended. Clear the task queue.
		/// </summary>
		public void Stop()
		{
			// i. Switch to STOPPING, interrupt thread
			// ii. Wait until thread ends
			// iii. Clear the task queue, switch to STOPPED,
			lock(this) 
			{
				switch(thread_state) 
				{
					case State.RUN: _stopping(); break;
					case State.SUSPEND: _stop(); return;
					case State.STOPPING: return;
					case State.STOP: return;
					case State.DISPOSED: return;
				}
				thread.Interrupt();
			}
			thread.Join();
			lock(this) 
			{
				queue.Clear();
				_stop();
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
			Thread tmp = null;
			lock(this)
			{
				if(thread_state == State.DISPOSED) return;

				tmp = thread;
				thread_state = State.DISPOSED;
				thread = null;
				if(tmp != null)
				{
					tmp.Interrupt();
				}
			}
            queue.Clear();
           
		}
	}
}
