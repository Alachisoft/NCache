// $Id: QUEUE.java,v 1.6 2004/07/23 02:28:01 belaban Exp $

using System.Threading;
using Alachisoft.NGroups.Protocols.pbcast;
using Alachisoft.NGroups.Stack;
using Gms = Alachisoft.NGroups.Protocols.pbcast.GMS;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> Queuing layer. Upon reception of event START_QUEUEING, all events traveling through
    /// this layer upwards/downwards (depending on direction of event) will be queued. Upon
    /// reception of a STOP_QUEUEING event, all events will be released. Finally, the
    /// queueing flag is reset.
    /// When queueing, only event STOP_QUEUEING (received up or downwards) will be allowed
    /// to release queueing.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>

    internal class QUEUE:Protocol
	{
        public QUEUE()
        {
            
        }
		virtual public System.Collections.ArrayList UpVector
		{
			get
			{
				return up_vec;
			}
			
		}
		virtual public System.Collections.ArrayList DownVector
		{
			get
			{
				return dn_vec;
			}
			
		}
		virtual public bool QueueingUp
		{
			get
			{
				return queueing_up;
			}
			
		}
		virtual public bool QueueingDown
		{
			get
			{
				return queueing_dn;
			}
			
		}
		/// <summary>All protocol names have to be unique ! </summary>
		override public System.String Name
		{
			get
			{
				return "QUEUE";
			}
			
		}
		internal System.Collections.ArrayList up_vec = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		internal System.Collections.ArrayList dn_vec = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		internal bool queueing_up = false, queueing_dn = false;
		private ReaderWriterLock queingLock = new ReaderWriterLock();

		public override System.Collections.ArrayList providedUpServices()
		{
			System.Collections.ArrayList ret = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			ret.Add((System.Int32) Event.START_QUEUEING);
			ret.Add((System.Int32) Event.STOP_QUEUEING);
			return ret;
		}
		
		public override System.Collections.ArrayList providedDownServices()
		{
			System.Collections.ArrayList ret = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			ret.Add((System.Int32) Event.START_QUEUEING);
			ret.Add((System.Int32) Event.STOP_QUEUEING);
			return ret;
		}


        public override bool setProperties(System.Collections.Hashtable props)
        {
            if (stack.StackType == ProtocolStackType.TCP)
            {
                this.up_thread = false;
                this.down_thread = false;
                if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(Name + ".setProperties",  "part of TCP stack");
            }
            return true;
        }
		
		/// <summary>Queues or passes up events. No queue sync. necessary, as this method is never called
		/// concurrently.
		/// </summary>
		public override void  up(Event evt)
		{
			switch (evt.Type)
			{
				case Event.MSG:
					Message msg = (Message) evt.Arg;
                    object obj = msg.getHeader(HeaderType.GMS);
					if (obj != null && obj is GMS.HDR)
					{
						GMS.HDR hdr = (GMS.HDR)obj;
						if (hdr.type == GMS.HDR.VIEW || hdr.type == GMS.HDR.JOIN_RSP)
						{
                            queingLock.AcquireWriterLock(Timeout.Infinite);
                            try
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Queue.Up()",   "Received VIEW event, so we start up_queuing");
                                queueing_up = true; // starts up queuing
                            }
                            finally { queingLock.ReleaseWriterLock(); }
						}
						if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Queue.up()",  "Message Headers = " + Global.CollectionToString(msg.Headers));
						passUp(evt);
						return;
					}

					queingLock.AcquireReaderLock(Timeout.Infinite);
                    try
                    {
                        if (queueing_up)
                        {
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("queued up event " + evt.ToString());
                            up_vec.Add(evt);
                            return;
                        }
                    }
                    finally { queingLock.ReleaseReaderLock(); }
                       
					break;
				}
		
			passUp(evt); // Pass up to the layer above us
		}
		
		private void deliverUpQueuedEvts(Event evt)
		{
			Event e;
			if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("replaying up events");

            queingLock.AcquireWriterLock(Timeout.Infinite);
            try{
                for (int i = 0; i < up_vec.Count; i++)
                {
                    e = (Event)up_vec[i];
                    passUp(e);
                }
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Queue.deliverUpQueuedEvts()",   "delivered up queued msg count = " + up_vec.Count);
                up_vec.Clear();
                queueing_up = false;
            }
            finally { queingLock.ReleaseWriterLock(); }
		}
		
		
		
		public override void  down(Event evt)
		{
			
			switch (evt.Type)
			{
				case Event.VIEW_CHANGE_OK:
					if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Queue.down()",  "VIEW_CHANGE : lets stop queuing");
					deliverUpQueuedEvts(evt);
					break;
				}
			
			if (queueing_dn)
			{
				if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("queued down event: " + Util.Util.printEvent(evt));
				dn_vec.Add(evt);
				return;
			}

			passDown(evt); // Pass up to the layer below us
		}
	}
}