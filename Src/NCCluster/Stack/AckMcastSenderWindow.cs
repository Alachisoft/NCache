// $Id: AckMcastSenderWindow.java,v 1.5 2004/07/05 14:17:32 belaban Exp $
using System;

using Alachisoft.NGroups;
using Alachisoft.NGroups.Util;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NGroups.Stack
{
	/// <summary> Keeps track of ACKs from receivers for each message. When a new message is
	/// sent, it is tagged with a sequence number and the receiver set (set of
	/// members to which the message is sent) and added to a hashtable
	/// (key = sequence number, val = message + receiver set). Each incoming ACK
	/// is noted and when all ACKs for a specific sequence number haven been
	/// received, the corresponding entry is removed from the hashtable. A
	/// retransmission thread periodically re-sends the message point-to-point to
	/// all receivers from which no ACKs have been received yet. A view change or
	/// suspect message causes the corresponding non-existing receivers to be
	/// removed from the hashtable.
	/// <p>
	/// This class may need flow control in order to avoid needless
	/// retransmissions because of timeouts.
	/// 
	/// </summary>
	/// <author>  Bela Ban June 9 1999
	/// </author>
	/// <author>  John Georgiadis May 8 2001
	/// </author>
	/// <version>  $Revision: 1.5 $
	/// </version>
	internal class AckMcastSenderWindow
	{

		/// <summary> The retransmit task executed by the scheduler in regular intervals</summary>
		private abstract class Task : TimeScheduler.Task
		{
			private Interval intervals;
			private bool cancelled_Renamed_Field;
			
			protected internal Task(long[] intervals)
			{
				this.intervals = new Interval(intervals);
				this.cancelled_Renamed_Field = false;
			}
			public virtual long GetNextInterval()
			{
				return (intervals.next());
			}
			public virtual void  cancel()
			{
				cancelled_Renamed_Field = true;
			}
			public virtual bool IsCancelled()
			{
				return (cancelled_Renamed_Field);
			}

			public virtual void Run(){}
			public string getName()
			{
				return "AckMcastSenderWindow.Task";
			}
		}
		
		
		/// <summary> The entry associated with a pending msg</summary>
		private class Entry:Task
		{
			private void  InitBlock(AckMcastSenderWindow enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private AckMcastSenderWindow enclosingInstance;
			public AckMcastSenderWindow Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			/// <summary>The msg sequence number </summary>
			public long seqno;
			/// <summary>The msg to retransmit </summary>
			public Message msg = null;
			/// <summary>destination addr -> boolean (true = received, false = not) </summary>
			public System.Collections.Hashtable senders = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
			/// <summary>How many destinations have received the msg </summary>
			public int num_received = 0;
			
			public Entry(AckMcastSenderWindow enclosingInstance, long seqno, Message msg, System.Collections.ArrayList dests, long[] intervals):base(intervals)
			{
				InitBlock(enclosingInstance);
				this.seqno = seqno;
				this.msg = msg;
				for (int i = 0; i < dests.Count; i++)
					senders[dests[i]] = false;
			}
			
			internal virtual bool allReceived()
			{
				return (num_received >= senders.Count);
			}
			
			/// <summary>Retransmit this entry </summary>
			public override  void  Run()
			{
				Enclosing_Instance._retransmit(this);
			}
			
			public override string ToString()
			{
				System.Text.StringBuilder buff = new System.Text.StringBuilder();
				buff.Append("num_received = " + num_received + ", received msgs = " + Global.CollectionToString(senders));
				return (buff.ToString());
			}
		}
		
		
		
		
		/// <returns> a copy of stable messages, or null (if non available). Removes
		/// all stable messages afterwards
		/// </returns>
		virtual public System.Collections.ArrayList StableMessages
		{
			get
			{
				System.Collections.ArrayList retval;
				
				lock (stable_msgs.SyncRoot)
				{
					retval = (stable_msgs.Count > 0)?(System.Collections.ArrayList) stable_msgs.Clone():null;
					if (stable_msgs.Count > 0)
						stable_msgs.Clear();
				}
				
				return (retval);
			}
			
		}
		/// <summary> Called by retransmitter thread whenever a message needs to be re-sent
		/// to a destination. <code>dest</code> has to be set in the
		/// <code>dst</code> field of <code>msg</code>, as the latter was sent
		/// multicast, but now we are sending a unicast message. Message has to be
		/// copied before sending it (as headers will be appended and therefore
		/// the message changed!).
		/// </summary>
		internal interface RetransmitCommand
		{
			/// <summary> Retranmit the given msg
			/// 
			/// </summary>
			/// <param name="seqno">the sequence number associated with the message
			/// </param>
			/// <param name="msg">the msg to retransmit (it should be a copy!)
			/// </param>
			/// <param name="dest">the msg destination
			/// </param>
			void  retransmit(long seqno, Message msg, Address dest);
		}
		
		
		
		private const long SEC = 1000;
		/// <summary>Default retransmit intervals (ms) - exponential approx. </summary>
		private static readonly long[] RETRANSMIT_TIMEOUTS = new long[]{2 * SEC, 3 * SEC, 5 * SEC, 8 * SEC};
		/// <summary>Default retransmit thread suspend timeout (ms) </summary>
		private const long SUSPEND_TIMEOUT = 30 * 1000;
		
			
		// Msg tables related
		/// <summary>Table of pending msgs: seqno -> Entry </summary>
		private System.Collections.Hashtable msgs = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		
		/// <summary>List of recently suspected members. Used to cease retransmission to suspected members </summary>
		private System.Collections.ArrayList suspects = new System.Collections.ArrayList();
		
		/// <summary>Max number in suspects list </summary>
		private int max_suspects = 20;
		
		/// <summary> List of acknowledged msgs since the last call to
		/// <code>getStableMessages()</code>
		/// </summary>
		private System.Collections.ArrayList stable_msgs = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		/// <summary>Whether a call to <code>waitUntilAcksReceived()</code> is still active </summary>
		private bool waiting = false;
		
		// Retransmission thread related
		/// <summary>Whether retransmitter is externally provided or owned by this object </summary>
		private bool retransmitter_owned;
		/// <summary>The retransmission scheduler </summary>
		private TimeScheduler retransmitter = null;
		/// <summary>Retransmission intervals </summary>
		private long[] retransmit_intervals;
		/// <summary>The callback object for retransmission </summary>
		private AckMcastSenderWindow.RetransmitCommand cmd = null;

        ILogger _ncacheLog;
        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
		
		/// <param name="entry">the record associated with the msg to retransmit. It
		/// contains the list of receivers that haven't yet ack reception
		/// </param>
		private void  _retransmit(Entry entry)
		{
			Address sender;
			bool received;
			
			lock (entry)
			{
				for (System.Collections.IEnumerator e = entry.senders.Keys.GetEnumerator(); e.MoveNext(); )
				{
					sender = (Address) e.Current;
					received = ((System.Boolean) entry.senders[sender]);
					if (!received)
					{
						if (suspects.Contains(sender))
						{
							NCacheLog.Warn("AckMcastSenderWindow", "removing " + sender + " from retransmit list as it is in the suspect list");
							
							remove(sender);
							continue;
						}

                        NCacheLog.Warn("AckMcastSenderWindow", "--> retransmitting msg #" + entry.seqno + " to " + sender);
						
						cmd.retransmit(entry.seqno, entry.msg.copy(), sender);
					}
				}
			}
		}
		
		
		/// <summary> Setup this object's state
		/// 
		/// </summary>
		/// <param name="cmd">the callback object for retranmissions
		/// </param>
		/// <param name="retransmit_timeout">the interval between two consecutive
		/// retransmission attempts
		/// </param>
		/// <param name="sched">the external scheduler to use to schedule retransmissions
		/// </param>
		/// <param name="sched_owned">if true, the scheduler is owned by this object and
		/// can be started/stopped/destroyed. If false, the scheduler is shared
		/// among multiple objects and start()/stop() should not be called from
		/// within this object
		/// 
		/// </param>
		/// <throws>  IllegalArgumentException if <code>cmd</code> is null </throws>
		private void  init(AckMcastSenderWindow.RetransmitCommand cmd, long[] retransmit_intervals, TimeScheduler sched, bool sched_owned)
		{
			if (cmd == null)
			{
				NCacheLog.Error("AckMcastSenderWindow.init", "command is null. Cannot retransmit " + "messages !");
				throw new System.ArgumentException("Command is null.");
			}
			
			retransmitter_owned = sched_owned;
			retransmitter = sched;
			this.retransmit_intervals = retransmit_intervals;
			this.cmd = cmd;
			
			start();
		}
		
		/// <summary> Create and <b>start</b> the retransmitter
		/// 
		/// </summary>
		/// <param name="cmd">the callback object for retranmissions
		/// </param>
		/// <param name="sched">the external scheduler to use to schedule retransmissions
		/// 
		/// </param>
		/// <throws>  IllegalArgumentException if <code>cmd</code> is null </throws>
		public AckMcastSenderWindow(AckMcastSenderWindow.RetransmitCommand cmd, TimeScheduler sched, ILogger NCacheLog)
		{
            this._ncacheLog = NCacheLog;
			init(cmd, RETRANSMIT_TIMEOUTS, sched, false);
		}
		
		
		
		/// <summary> Create and <b>start</b> the retransmitter
		/// 
		/// </summary>
		/// <param name="cmd">the callback object for retranmissions
		/// </param>
		/// <param name="retransmit_timeout">the interval between two consecutive
		/// retransmission attempts
		/// 
		/// </param>
		/// <throws>  IllegalArgumentException if <code>cmd</code> is null </throws>
        public AckMcastSenderWindow(AckMcastSenderWindow.RetransmitCommand cmd, long[] retransmit_intervals, ILogger NCacheLog)
		{
            this._ncacheLog = NCacheLog;
			init(cmd, retransmit_intervals, new TimeScheduler(SUSPEND_TIMEOUT), true);
		}
		
		/// <summary> Adds a new message to the hash table.
		/// 
		/// </summary>
		/// <param name="seqno">The sequence number associated with the message
		/// </param>
		/// <param name="msg">The message (should be a copy!)
		/// </param>
		/// <param name="receivers">The set of addresses to which the message was sent
		/// and from which consequently an ACK is expected
		/// </param>
		public virtual void  add(long seqno, Message msg, System.Collections.ArrayList receivers)
		{
			Entry e;
			
			if (waiting)
				return ;
			if (receivers.Count == 0)
				return ;
			
			lock (msgs.SyncRoot)
			{
				if (msgs[(long) seqno] != null)
					return ;
				e = new Entry(this, seqno, msg, receivers, retransmit_intervals);
				msgs[(long) seqno] = e;
				retransmitter.AddTask(e);
			}
		}
		
		
		/// <summary> An ACK has been received from <code>sender</code>. Tag the sender in
		/// the hash table as 'received'. If all ACKs have been received, remove
		/// the entry all together.
		/// 
		/// </summary>
		/// <param name="seqno"> The sequence number of the message for which an ACK has
		/// been received.
		/// </param>
		/// <param name="sender">The sender which sent the ACK
		/// </param>
		public virtual void  ack(long seqno, Address sender)
		{
			Entry entry;
			
			lock (msgs.SyncRoot)
			{
				entry = (Entry) msgs[(long) seqno];
				if (entry == null)
					return ;
				
				lock (entry)
				{
					Object temp = entry.senders[sender];
					if(temp == null)
						return;

					System.Boolean received = (System.Boolean) temp;
					if (received)
						return ;
					
					// If not yet received
					entry.senders[sender] = true;
					entry.num_received++;
					if (!entry.allReceived())
						return ;
				}
				
				lock (stable_msgs.SyncRoot)
				{
					entry.cancel();
					msgs.Remove((long) seqno);
					stable_msgs.Add((long) seqno);
				}
				// wake up waitUntilAllAcksReceived() method
				System.Threading.Monitor.Pulse(msgs.SyncRoot);
			}
		}
		
		
		/// <summary> Remove <code>obj</code> from all receiver sets and wake up
		/// retransmission thread.
		/// 
		/// </summary>
		/// <param name="obj">the sender to remove
		/// </param>
		public virtual void  remove(Address obj)
		{
			System.Int64 key;
			Entry entry;
			
			lock (msgs.SyncRoot)
			{
				for (System.Collections.IEnumerator e = msgs.Keys.GetEnumerator(); e.MoveNext(); )
				{
					key = (System.Int64) e.Current;
					entry = (Entry) msgs[key];
					lock (entry)
					{
						object tempObject;
						tempObject = entry.senders[obj];
						entry.senders.Remove(obj);
						if (tempObject == null)
							continue; // suspected member not in entry.senders ?

						System.Boolean received = (System.Boolean) tempObject;
						if (received)
							entry.num_received--;
						if (!entry.allReceived())
							continue;
					}
					lock (stable_msgs.SyncRoot)
					{
						entry.cancel();
						msgs.Remove(key);
						stable_msgs.Add(key);
						e = msgs.Keys.GetEnumerator();
					}
					// wake up waitUntilAllAcksReceived() method
					System.Threading.Monitor.Pulse(msgs.SyncRoot);
				}
			}
		}
		
		
		/// <summary> Process with address <code>suspected</code> is suspected: remove it
		/// from all receiver sets. This means that no ACKs are expected from this
		/// process anymore.
		/// 
		/// </summary>
		/// <param name="suspected">The suspected process
		/// </param>
		public virtual void  suspect(Address suspected)
		{
            NCacheLog.Warn("AckMcastSenderWindow", "suspect is " + suspected);
			
			remove(suspected);
			suspects.Add(suspected);
			if (suspects.Count >= max_suspects)
				suspects.RemoveAt(0);
		}
		
		
		public virtual void  clearStableMessages()
		{
			lock (stable_msgs.SyncRoot)
			{
				stable_msgs.Clear();
			}
		}
		
		
		/// <returns> the number of currently pending msgs
		/// </returns>
		public virtual long size()
		{
			lock (msgs.SyncRoot)
			{
				return (msgs.Count);
			}
		}
		
		
		/// <summary>Returns the number of members for a given entry for which acks have to be received </summary>
		public long getNumberOfResponsesExpected(long seqno)
		{
			Entry entry = (Entry) msgs[(long) seqno];
			if (entry != null)
				return entry.senders.Count;
			else
				return - 1;
		}
		
		/// <summary>Returns the number of members for a given entry for which acks have been received </summary>
		public long getNumberOfResponsesReceived(long seqno)
		{
			Entry entry = (Entry) msgs[(long) seqno];
			if (entry != null)
				return entry.num_received;
			else
				return - 1;
		}
		
		/// <summary>Prints all members plus whether an ack has been received from those members for a given seqno </summary>
		public string printDetails(long seqno)
		{
			Entry entry = (Entry) msgs[(long) seqno];
			if (entry != null)
			{
				return entry.ToString();
			}
			else
				return null;
		}
		
		
		/// <summary> Waits until all outstanding messages have been ACKed by all receivers.
		/// Takes into account suspicions and view changes. Returns when there are
		/// no entries left in the hashtable. <b>While waiting, no entries can be
		/// added to the hashtable (they will be discarded).</b>
		/// 
		/// </summary>
		/// <param name="timeout">Miliseconds to wait. 0 means wait indefinitely.
		/// </param>
		public void  waitUntilAllAcksReceived(long timeout)
		{
			long time_to_wait, start_time, current_time;
			Address suspect;
			
			// remove all suspected members from retransmission
			for (System.Collections.IEnumerator it = suspects.GetEnumerator(); it.MoveNext(); )
			{
				suspect = (Address) it.Current;
				remove(suspect);
			}
			
			time_to_wait = timeout;
			waiting = true;
			if (timeout <= 0)
			{
				lock (msgs.SyncRoot)
				{
					while (msgs.Count > 0)
						try
						{
							System.Threading.Monitor.Wait(msgs.SyncRoot);
						}
						catch (System.Threading.ThreadInterruptedException e)
						{
							NCacheLog.Error("AckMcastSenderWindow.waitUntilAllAcksReceived()", e.ToString());
						}
				}
			}
			else
			{
				start_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
				lock (msgs.SyncRoot)
				{
					while (msgs.Count > 0)
					{
						current_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
						time_to_wait = timeout - (current_time - start_time);
						if (time_to_wait <= 0)
							break;
						
						try
						{
							System.Threading.Monitor.Wait(msgs.SyncRoot, TimeSpan.FromMilliseconds(time_to_wait));
						}
						catch (System.Threading.ThreadInterruptedException ex)
						{
							NCacheLog.Error("AckMcastSenderWindow.waitUntilAllAcksReceived", ex.ToString());							
						}
					}
				}
			}

			// SAL:
			if (time_to_wait < 0)
			{
                NCacheLog.Fatal("[Timeout]AckMcastSenderWindow.waitUntillAllAcksReceived:" + time_to_wait);
			}

			waiting = false;
		}
		
		
		
		
		/// <summary> Start the retransmitter. This has no effect, if the retransmitter
		/// was externally provided
		/// </summary>
		public void  start()
		{
			if (retransmitter_owned)
				retransmitter.Start();
		}
		
		
		/// <summary> Stop the rentransmition and clear all pending msgs.
		/// <p>
		/// If this retransmitter has been provided an externally managed
		/// scheduler, then just clear all msgs and the associated tasks, else
		/// stop the scheduler. In this case the method blocks until the
		/// scheduler's thread is dead. Only the owner of the scheduler should
		/// stop it.
		/// </summary>
		public void  stop()
		{
			Entry entry;
			
			// i. If retransmitter is owned, stop it else cancel all tasks
			// ii. Clear all pending msgs and notify anyone waiting
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
						NCacheLog.Error("AckMcastSenderWindow.stop()",ex.ToString());
					}
				}
				else
				{
					for (System.Collections.IEnumerator e = msgs.Values.GetEnumerator(); e.MoveNext(); )
					{
						entry = (Entry) e.Current;
						entry.cancel();
					}
				}
				msgs.Clear();
				// wake up waitUntilAllAcksReceived() method
				System.Threading.Monitor.Pulse(msgs.SyncRoot);
			}
		}
		
		
		/// <summary> Remove all pending msgs from the hashtable. Cancel all associated
		/// tasks in the retransmission scheduler
		/// </summary>
		public void  reset()
		{
			Entry entry;
			
			if (waiting)
				return ;
			
			lock (msgs.SyncRoot)
			{
				for (System.Collections.IEnumerator e = msgs.Values.GetEnumerator(); e.MoveNext(); )
				{
					entry = (Entry) e.Current;
					entry.cancel();
				}
				msgs.Clear();
				System.Threading.Monitor.Pulse(msgs.SyncRoot);
			}
		}
		
		
		public override string ToString()
		{
			System.Text.StringBuilder ret;
			Entry entry;
			System.Int64 key;
			
			ret = new System.Text.StringBuilder();
			lock (msgs.SyncRoot)
			{
				ret.Append("msgs: (" + msgs.Count + ')');
				for (System.Collections.IEnumerator e = msgs.Keys.GetEnumerator(); e.MoveNext(); )
				{
					key = (System.Int64) e.Current;
					entry = (Entry) msgs[key];
					ret.Append("key = " + key + ", value = " + entry + '\n');
				}
				lock (stable_msgs.SyncRoot)
				{
					ret.Append("\nstable_msgs: " + Global.CollectionToString(stable_msgs));
				}
			}
			
			return (ret.ToString());
		}
	}
}