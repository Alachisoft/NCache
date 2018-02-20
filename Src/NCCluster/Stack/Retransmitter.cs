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
using System.Threading;

using Alachisoft.NGroups.Util;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Stack
{
	/// <summary> Maintains a pool of sequence numbers of messages that need to be retransmitted. Messages
	/// are aged and retransmission requests sent according to age (linear backoff used). If a
	/// TimeScheduler instance is given to the constructor, it will be used, otherwise Reransmitter
	/// will create its own. The retransmit timeouts have to be set first thing after creating an instance.
	/// The <code>add()</code> method adds a range of sequence numbers of messages to be retransmitted. The
	/// <code>remove()</code> method removes a sequence number again, cancelling retransmission requests for it.
	/// Whenever a message needs to be retransmitted, the <code>RetransmitCommand.retransmit()</code> method is called.
	/// It can be used e.g. by an ack-based scheme (e.g. AckSenderWindow) to retransmit a message to the receiver, or
	/// by a nak-based scheme to send a retransmission request to the sender of the missing message.
	/// 
	/// </summary>
	/// <author>  John Giorgiadis
	/// </author>
	/// <author>  Bela Ban
	/// </author>
	/// <version>  $Revision: 1.4 $
	/// </version>
	internal class Retransmitter
	{
		virtual public long[] RetransmitTimeouts
		{
			set
			{
				if (value != null)
					RETRANSMIT_TIMEOUTS = value;
			}
			
		}
		
		private const long SEC = 1000;
		/// <summary>Default retransmit intervals (ms) - exponential approx. </summary>
		private static long[] RETRANSMIT_TIMEOUTS = new long[]{2 * SEC, 3 * SEC, 5 * SEC, 8 * SEC};
		/// <summary>Default retransmit thread suspend timeout (ms) </summary>
		private const long SUSPEND_TIMEOUT = 2000;
		
		internal Address sender = null;
		private System.Collections.ArrayList msgs = new System.Collections.ArrayList();
		internal RetransmitCommand cmd = null;
		private bool retransmitter_owned;
		private TimeScheduler retransmitter = null;
		
		
		/// <summary> Create a new Retransmitter associated with the given sender address</summary>
		/// <param name="sender">the address from which retransmissions are expected or to which retransmissions are sent
		/// </param>
		/// <param name="cmd">the retransmission callback reference
		/// </param>
		/// <param name="sched">retransmissions scheduler
		/// </param>
		public Retransmitter(Address sender, RetransmitCommand cmd, TimeScheduler sched)
		{
			init(sender, cmd, sched, false);
		}
		
		
		/// <summary> Create a new Retransmitter associated with the given sender address</summary>
		/// <param name="sender">the address from which retransmissions are expected or to which retransmissions are sent
		/// </param>
		/// <param name="cmd">the retransmission callback reference
		/// </param>
		public Retransmitter(Address sender, RetransmitCommand cmd)
		{
			init(sender, cmd, new TimeScheduler(30 * 1000), true);
		}
		
		
		/// <summary> Add the given range [first_seqno, last_seqno] in the list of
		/// entries eligible for retransmission. If first_seqno > last_seqno,
		/// then the range [last_seqno, first_seqno] is added instead
		/// <p>
		/// If retransmitter thread is suspended, wake it up
		/// TODO:
		/// Does not check for duplicates !
		/// </summary>
		public virtual void  add(long first_seqno, long last_seqno)
		{
            RetransmitterEntry e;
			
			if (first_seqno > last_seqno)
			{
				long tmp = first_seqno;
				first_seqno = last_seqno;
				last_seqno = tmp;
			}
			lock (msgs.SyncRoot)
			{
				e = new RetransmitterEntry(this, first_seqno, last_seqno, RETRANSMIT_TIMEOUTS);
				msgs.Add(e);
				retransmitter.AddTask(e);
			}
		}
		
		/// <summary> Remove the given sequence number from the list of seqnos eligible
		/// for retransmission. If there are no more seqno intervals in the
		/// respective entry, cancel the entry from the retransmission
		/// scheduler and remove it from the pending entries
		/// </summary>
		public virtual void  remove(long seqno)
		{
			lock (msgs.SyncRoot)
			{
				for (int index = 0; index < msgs.Count; index++)
				{
					RetransmitterEntry e = (RetransmitterEntry) msgs[index];
					lock (e)
					{
						if (seqno < e.low || seqno > e.high)
							continue;
						e.remove(seqno);
						if (e.low > e.high)
						{
							e.cancel();
							msgs.RemoveAt(index);
						}
					}
					break;
				}
			}
		}
		
		/// <summary> Reset the retransmitter: clear all msgs and cancel all the
		/// respective tasks
		/// </summary>
		public virtual void  reset()
		{
			lock (msgs.SyncRoot)
			{
				for (int index = 0; index < msgs.Count; index++)
				{
                    RetransmitterEntry entry = (RetransmitterEntry) msgs[index];
					entry.cancel();
				}
				msgs.Clear();
			}
		}
		
		/// <summary> Stop the rentransmition and clear all pending msgs.
		/// <p>
		/// If this retransmitter has been provided  an externally managed
		/// scheduler, then just clear all msgs and the associated tasks, else
		/// stop the scheduler. In this case the method blocks until the
		/// scheduler's thread is dead. Only the owner of the scheduler should
		/// stop it.
		/// </summary>
		public virtual void  stop()
		{
			// i. If retransmitter is owned, stop it else cancel all tasks
			// ii. Clear all pending msgs
			lock (msgs.SyncRoot)
			{
				if (retransmitter_owned)
				{
					try
					{
						retransmitter.Dispose();
					}
					catch (System.Threading.ThreadInterruptedException ex)
					{
					}
				}
				else
				{
					for (int index = 0; index < msgs.Count; index++)
					{
                        RetransmitterEntry e = (RetransmitterEntry) msgs[index];
						e.cancel();
					}
				}
				msgs.Clear();
			}
		}
		
		
		public override string ToString()
		{
			return (msgs.Count + " messages to retransmit: (" + Global.CollectionToString(msgs) + ')');
		}
		
		
		
		
		
		/* ------------------------------- Private Methods -------------------------------------- */
		
		/// <summary> Init this object
		/// 
		/// </summary>
		/// <param name="sender">the address from which retransmissions are expected
		/// </param>
		/// <param name="cmd">the retransmission callback reference
		/// </param>
		/// <param name="sched">retransmissions scheduler
		/// </param>
		/// <param name="sched_owned">whether the scheduler parameter is owned by this
		/// object or is externally provided
		/// </param>
		private void  init(Address sender, RetransmitCommand cmd, TimeScheduler sched, bool sched_owned)
		{
			this.sender = sender;
			this.cmd = cmd;
			retransmitter_owned = sched_owned;
			retransmitter = sched;
		}
		
		
		/* ---------------------------- End of Private Methods ------------------------------------ */
		
		
		
		/// <summary> The retransmit task executed by the scheduler in regular intervals</summary>
		private abstract class Task : TimeScheduler.Task
		{
			private Interval intervals;
			private bool isCancelled;
			
			protected internal Task(long[] intervals)
			{
				this.intervals = new Interval(intervals);
				this.isCancelled = false;
			}
			
			public virtual long GetNextInterval()
			{
				return (intervals.next());
			}
			
			public virtual bool IsCancelled()
			{
				return (isCancelled);
			}
			
			public virtual void  cancel()
			{
				isCancelled = true;
			}

			public virtual void Run() {}
		}
		
		
		
		
		internal static void  sleep(long timeout)
		{
			Util.Util.sleep(timeout);
		}
	}
}
