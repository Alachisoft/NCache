using System.Collections;

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
		
		private Address sender = null;
		private System.Collections.ArrayList msgs = new System.Collections.ArrayList();
		private Retransmitter.RetransmitCommand cmd = null;
		private bool retransmitter_owned;
		private TimeScheduler retransmitter = null;
		
		
		/// <summary>Retransmit command (see Gamma et al.) used to retrieve missing messages </summary>
		internal interface RetransmitCommand
		{
			/// <summary> Get the missing messages between sequence numbers
			/// <code>first_seqno</code> and <code>last_seqno</code>. This can either be done by sending a
			/// retransmit message to destination <code>sender</code> (nak-based scheme), or by
			/// retransmitting the missing message(s) to <code>sender</code> (ack-based scheme).
			/// </summary>
			/// <param name="first_seqno">The sequence number of the first missing message
			/// </param>
			/// <param name="last_seqno"> The sequence number of the last missing message
			/// </param>
			/// <param name="sender">The destination of the member to which the retransmit request will be sent
			/// (nak-based scheme), or to which the message will be retransmitted (ack-based scheme).
			/// </param>
			void  retransmit(long first_seqno, long last_seqno, Address sender);
		}
		
		
		/// <summary> Create a new Retransmitter associated with the given sender address</summary>
		/// <param name="sender">the address from which retransmissions are expected or to which retransmissions are sent
		/// </param>
		/// <param name="cmd">the retransmission callback reference
		/// </param>
		/// <param name="sched">retransmissions scheduler
		/// </param>
		public Retransmitter(Address sender, Retransmitter.RetransmitCommand cmd, TimeScheduler sched)
		{
			init(sender, cmd, sched, false);
		}
		
		
		/// <summary> Create a new Retransmitter associated with the given sender address</summary>
		/// <param name="sender">the address from which retransmissions are expected or to which retransmissions are sent
		/// </param>
		/// <param name="cmd">the retransmission callback reference
		/// </param>
		public Retransmitter(Address sender, Retransmitter.RetransmitCommand cmd)
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
			Entry e;
			
			if (first_seqno > last_seqno)
			{
				long tmp = first_seqno;
				first_seqno = last_seqno;
				last_seqno = tmp;
			}
			lock (msgs.SyncRoot)
			{
				e = new Entry(this, first_seqno, last_seqno, RETRANSMIT_TIMEOUTS);
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
					Entry e = (Entry) msgs[index];
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
					Entry entry = (Entry) msgs[index];
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
						Entry e = (Entry) msgs[index];
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
		private void  init(Address sender, Retransmitter.RetransmitCommand cmd, TimeScheduler sched, bool sched_owned)
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
		
		
		/// <summary> The entry associated with an initial group of missing messages
		/// with contiguous sequence numbers and with all its subgroups.<br>
		/// E.g.
		/// - initial group: [5-34]
		/// - msg 12 is acknowledged, now the groups are: [5-11], [13-34]
		/// <p>
		/// Groups are stored in a list as long[2] arrays of the each group's
		/// bounds. For speed and convenience, the lowest & highest bounds of
		/// all the groups in this entry are also stored separately
		/// </summary>
		private class Entry:Task
		{
			private Retransmitter enclosingInstance;
			public System.Collections.ArrayList list;
			public long low;
			public long high;
			
			public Entry(Retransmitter enclosingInstance, long low, long high, long[] intervals):base(intervals)
			{
				this.enclosingInstance = enclosingInstance;
				this.low = low;
				this.high = high;
				list = new System.Collections.ArrayList();
				list.Add(low);
				list.Add(high);
			}
			
			/// <summary> Remove the given seqno and resize or partition groups as
			/// necessary. The algorithm is as follows:<br>
			/// i. Find the group with low <= seqno <= high
			/// ii. If seqno == low,
			/// a. if low == high, then remove the group
			/// Adjust global low. If global low was pointing to the group
			/// deleted in the previous step, set it to point to the next group.
			/// If there is no next group, set global low to be higher than
			/// global high. This way the entry is invalidated and will be removed
			/// all together from the pending msgs and the task scheduler
			/// iii. If seqno == high, adjust high, adjust global high if this is
			/// the group at the tail of the list
			/// iv. Else low < seqno < high, break [low,high] into [low,seqno-1]
			/// and [seqno+1,high]
			/// 
			/// </summary>
			/// <param name="seqno">the sequence number to remove
			/// </param>
			public virtual void  remove(long seqno)
			{
				int i;
				long loBound = -1;
				long hiBound = -1;

				lock (this)
				{
					for (i = 0; i < list.Count; i+=2)
					{
						loBound = (long) list[i];
						hiBound = (long) list[i+1];

						if (seqno < loBound || seqno > hiBound)
							continue;
						break;
					}
					if (i == list.Count)
						return ;
					
					if (seqno == loBound)
					{
						if (loBound == hiBound)
						{
							list.RemoveAt(i);
							list.RemoveAt(i);
						}
						else
							list[i] = ++loBound;

						if (i == 0)
							low = list.Count == 0 ? high + 1:loBound;
					}
					else if (seqno == hiBound)
					{
						list[i+1] = --hiBound;
						if (i == list.Count - 1)
							high = hiBound;
					}
					else
					{
						list[i+1] = seqno - 1;

						list.Insert(i + 2, hiBound);
						list.Insert(i + 2, seqno + 1);
					}
				}
			}
			
			/// <summary> Retransmission task:<br>
			/// For each interval, call the retransmission callback command
			/// </summary>
			public override void  Run()
			{
				ArrayList cloned;
				lock (this)
				{
					cloned = (ArrayList) list.Clone();
				}
				for (int i = 0; i < cloned.Count; i+=2)
				{
					long loBound = (long) cloned[i];
					long hiBound = (long) cloned[i+1];
					enclosingInstance.cmd.retransmit(loBound, hiBound, enclosingInstance.sender);
				}
			}
			
			public override string ToString()
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				if (low == high)
					sb.Append(low);
				else
					sb.Append(low).Append(':').Append(high);
				return sb.ToString();
			}
		} // end class Entry
		
		
		internal static void  sleep(long timeout)
		{
			Util.Util.sleep(timeout);
		}
	}
}
