using System;
using System.Collections;

using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NGroups.Stack
{
	/// <summary> ACK-based sliding window for a sender. Messages are added to the window keyed by seqno
	/// When an ACK is received, the corresponding message is removed. The Retransmitter
	/// continously iterates over the entries in the hashmap, retransmitting messages based on their
	/// creation time and an (increasing) timeout. When there are no more messages in the retransmission
	/// table left, the thread terminates. It will be re-activated when a new entry is added to the
	/// retransmission table.
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	internal class AckSenderWindow : Retransmitter.RetransmitCommand
	{
		private void  InitBlock()
		{
			retransmitter = new Retransmitter(null, this);
		}
		
		internal AckSenderWindow.RetransmitCommand retransmit_command = null; // called to request XMIT of msg
		internal System.Collections.Hashtable msgs = new System.Collections.Hashtable(); // keys: seqnos (Long), values: Messages

		internal long[] interval = new long[]{1000, 2000, 3000, 4000};
		internal Retransmitter retransmitter;
        internal Alachisoft.NCache.Common.DataStructures.Queue msg_queue = new Alachisoft.NCache.Common.DataStructures.Queue(); // for storing messages if msgs is full
		internal int window_size = - 1; // the max size of msgs, when exceeded messages will be queued
		
		/// <summary>when queueing, after msgs size falls below this value, msgs are added again (queueing stops) </summary>
		internal int min_threshold = - 1;
		internal bool use_sliding_window = false, queueing = false;
		internal Protocol transport = null; // used to send messages
		
        private ILogger _ncacheLog;

        private ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

		internal interface RetransmitCommand
		{
			void  retransmit(long seqno, Message msg);
		}
		
		/// <summary> Creates a new instance. Thre retransmission thread has to be started separately with
		/// <code>start()</code>.
		/// </summary>
		/// <param name="com">If not null, its method <code>retransmit()</code> will be called when a message
		/// needs to be retransmitted (called by the Retransmitter).
		/// </param>
        public AckSenderWindow(AckSenderWindow.RetransmitCommand com, ILogger NCacheLog)
		{
            _ncacheLog = NCacheLog;

			InitBlock();
			retransmit_command = com;
			retransmitter.RetransmitTimeouts = interval;
		}


        public AckSenderWindow(AckSenderWindow.RetransmitCommand com, long[] interval, ILogger NCacheLog)
		{
            _ncacheLog = NCacheLog;
            InitBlock();
			retransmit_command = com;
			this.interval = interval;
			retransmitter.RetransmitTimeouts = interval;
		}
		
		/// <summary> This constructor whould be used when we want AckSenderWindow to send the message added
		/// by add(), rather then ourselves.
		/// </summary>
        public AckSenderWindow(AckSenderWindow.RetransmitCommand com, long[] interval, Protocol transport, ILogger NCacheLog)
		{
            _ncacheLog = NCacheLog;
            InitBlock();
			retransmit_command = com;
			this.interval = interval;
			this.transport = transport;
			retransmitter.RetransmitTimeouts = interval;
		}
		
		
		public virtual void  setWindowSize(int window_size, int min_threshold)
		{
			this.window_size = window_size;
			this.min_threshold = min_threshold;
			
			// sanity tests for the 2 values:
			if (min_threshold > window_size)
			{
				this.min_threshold = window_size;
				this.window_size = min_threshold;
				NCacheLog.Warn("min_threshold (" + min_threshold + ") has to be less than window_size ( " + window_size + "). Values are swapped");
			}
			if (this.window_size <= 0)
			{
				this.window_size = this.min_threshold > 0?(int) (this.min_threshold * 1.5):500;
                NCacheLog.Warn("window_size is <= 0, setting it to " + this.window_size);
			}
			if (this.min_threshold <= 0)
			{
				this.min_threshold = this.window_size > 0?(int) (this.window_size * 0.5):250;
                NCacheLog.Warn("min_threshold is <= 0, setting it to " + this.min_threshold);
			}


            NCacheLog.Debug("window_size=" + this.window_size + ", min_threshold=" + this.min_threshold);
			use_sliding_window = true;
		}
		
		
		public virtual void  reset()
		{
			lock (msgs.SyncRoot)
			{
				msgs.Clear();
			}
			
			// moved out of sync scope: Retransmitter.reset()/add()/remove() are sync'ed anyway
			// Bela Jan 15 2003
			retransmitter.reset();
		}
		
		
		/// <summary> Adds a new message to the retransmission table. If the message won't have received an ack within
		/// a certain time frame, the retransmission thread will retransmit the message to the receiver. If
		/// a sliding window protocol is used, we only add up to <code>window_size</code> messages. If the table is
		/// full, we add all new messages to a queue. Those will only be added once the table drains below a certain
		/// threshold (<code>min_threshold</code>)
		/// </summary>
		public virtual void  add(long seqno, Message msg)
		{
			lock (msgs.SyncRoot)
			{
				if (msgs.ContainsKey(seqno))
					return ;
				
				if (!use_sliding_window)
				{
					addMessage(seqno, msg);
				}
				else
				{
					// we use a sliding window
					if (queueing)
						addToQueue(seqno, msg);
					else
					{
						if (msgs.Count + 1 > window_size)
						{
							queueing = true;
							addToQueue(seqno, msg);
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("window_size (" + window_size + ") was exceeded, " + "starting to queue messages until window size falls under " + min_threshold);
						}
						else
						{
							addMessage(seqno, msg);
						}
					}
				}
			}
		}
		
		
		/// <summary> Removes the message from <code>msgs</code>, removing them also from retransmission. If
		/// sliding window protocol is used, and was queueing, check whether we can resume adding elements.
		/// Add all elements. If this goes above window_size, stop adding and back to queueing. Else
		/// set queueing to false.
		/// </summary>
		public virtual void  ack(long seqno)
		{
			Entry entry;
			
			lock (msgs.SyncRoot)
			{
				msgs.Remove(seqno);
				retransmitter.remove(seqno);
				
				if (use_sliding_window && queueing)
				{
					if (msgs.Count < min_threshold)
					{
						// we fell below threshold, now we can resume adding msgs
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("number of messages in table fell under min_threshold (" + min_threshold + "): adding " + msg_queue.Count + " messages on queue");
						
						while (msgs.Count < window_size)
						{
							if ((entry = removeFromQueue()) != null)
							{
								addMessage(entry.seqno, entry.msg);
							}
							else
								break;
						}
						
						if (msgs.Count + 1 > window_size)
						{
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("exceeded window_size (" + window_size + ") again, will still queue");
							return ; // still queueing
						}
						else
							queueing = false; // allows add() to add messages again

                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("set queueing to false (table size=" + msgs.Count + ')');
					}
				}
			}
		}
		
		
		public override string ToString()
		{
			return Global.CollectionToString(msgs.Keys) + " (retransmitter: " + retransmitter.ToString() + ')';
		}
		
		/* -------------------------------- Retransmitter.RetransmitCommand interface ------------------- */
		public virtual void  retransmit(long first_seqno, long last_seqno, Address sender)
		{
			Message msg;
			
			if (retransmit_command != null)
			{
				for (long i = first_seqno; i <= last_seqno; i++)
				{
					if ((msg = (Message) msgs[(long) i]) != null)
					{
						// find the message to retransmit
						retransmit_command.retransmit(i, msg);
						//System.out.println("### retr(" + first_seqno + "): tstamp=" + System.currentTimeMillis());
					}
				}
			}
		}
		/* ----------------------------- End of Retransmitter.RetransmitCommand interface ---------------- */
		
		
		
		
		
		/* ---------------------------------- Private methods --------------------------------------- */
		internal virtual void  addMessage(long seqno, Message msg)
		{
			if (transport != null)
				transport.passDown(new Event(Event.MSG, msg));
			msgs[seqno] = msg;
			retransmitter.add(seqno, seqno);
		}
		
		internal virtual void  addToQueue(long seqno, Message msg)
		{
			try
			{
				msg_queue.add(new Entry(this, seqno, msg));
			}
			catch (System.Exception ex)
			{
                NCacheLog.Error("AckSenderWindow.add()",   ex.ToString());				
			}
		}
		
		internal virtual Entry removeFromQueue()
		{
			try
			{
				return msg_queue.Count == 0?null:(Entry) msg_queue.remove();
			}
			catch (System.Exception ex)
			{
                NCacheLog.Error("AckSenderWindow.removeFromQueue()",  ex.ToString());
				
				return null;
			}
		}
		/* ------------------------------ End of Private methods ------------------------------------ */
		
		
		
		
		/// <summary>Struct used to store message alongside with its seqno in the message queue </summary>
		internal class Entry
		{
			private void  InitBlock(AckSenderWindow enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private AckSenderWindow enclosingInstance;
			public AckSenderWindow Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal long seqno;
			internal Message msg;
			
			internal Entry(AckSenderWindow enclosingInstance, long seqno, Message msg)
			{
				InitBlock(enclosingInstance);
				this.seqno = seqno;
				this.msg = msg;
			}
		}
	}
}

