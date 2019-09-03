// $Id: Protocol.java,v 1.18 2004/07/05 14:17:33 belaban Exp $
using System;
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NGroups.Util;

namespace Alachisoft.NGroups.Stack
{
    /// <summary> The Protocol class provides a set of common services for protocol layers. Each layer has to
    /// be a subclass of Protocol and override a number of methods (typically just <code>up()</code>,
    /// <code>Down</code> and <code>getName</code>. Layers are stacked in a certain order to form
    /// a protocol stack. <a href=org.jgroups.Event.html>Events</a> are passed from lower
    /// layers to upper ones and vice versa. E.g. a Message received by the UDP layer at the bottom
    /// will be passed to its higher layer as an Event. That layer will in turn pass the Event to
    /// its layer and so on, until a layer handles the Message and sends a response or discards it,
    /// the former resulting in another Event being passed down the stack.<p>
    /// Each layer has 2 FIFO queues, one for up Events and one for down Events. When an Event is
    /// received by a layer (calling the internal upcall <code>ReceiveUpEvent</code>), it is placed
    /// in the up-queue where it will be retrieved by the up-handler thread which will invoke method
    /// <code>Up</code> of the layer. The same applies for Events traveling down the stack. Handling
    /// of the up-handler and down-handler threads and the 2 FIFO queues is donw by the Protocol
    /// class, subclasses will almost never have to override this behavior.<p>
    /// The important thing to bear in mind is that Events have to passed on between layers in FIFO
    /// order which is guaranteed by the Protocol implementation and must be guranteed by subclasses
    /// implementing their on Event queuing.<p>
    /// <b>Note that each class implementing interface Protocol MUST provide an empty, public
    /// constructor !</b>
    /// </summary>
    internal abstract class Protocol
	{
		internal class UpHandler:ThreadClass
		{
            private Alachisoft.NCache.Common.DataStructures.Queue mq;
			private Protocol			handler;
            int id;
            DateTime time;
            TimeSpan worsTime = new TimeSpan(0,0,0);


            public UpHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler)
			{
				this.mq = mq;
				this.handler = handler;
				if (handler != null)
				{
					Name = "UpHandler (" + handler.Name + ')';
				}
				else
				{
					Name = "UpHandler";
				}
				IsBackground = true;
			}

            public UpHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler, string name, int id)
            {
                this.mq = mq;
                this.handler = handler;
                if(name != null)
                    Name = name;
                IsBackground = true;
                this.id = id;
            }
		
		
			/// <summary>Removes events from mq and calls handler.up(evt) </summary>
			override public void  Run()
			{
                if (handler.Stack.NCacheLog.IsInfoEnabled) handler.Stack.NCacheLog.Info(Name, "---> Started!");
				try
				{
					while (!mq.Closed)
					{
						try
						{
							Event evt = (Event) mq.remove();
							if (evt == null)
							{
                                handler.Stack.NCacheLog.Warn("Protocol", "removed null event");
								continue;
							}

                            if (handler.enableMonitoring)
                            {
                                handler.PublishUpQueueStats(mq.Count,id);
                            }

                            time = DateTime.Now;
							handler.up(evt);
                            DateTime now = DateTime.Now;
                            TimeSpan ts = now - time;

                            if (ts.TotalMilliseconds > worsTime.TotalMilliseconds)
                                worsTime = ts;
						}
						catch (QueueClosedException e)
						{
                            handler.Stack.NCacheLog.Error(Name, e.ToString());
							break;
						}
						catch (ThreadInterruptedException ex)
						{
                            handler.Stack.NCacheLog.Error(Name, ex.ToString());
							break;
						}
						catch (System.Exception e)
						{
                            handler.Stack.NCacheLog.Error(Name, " exception: " + e.ToString());					
						}
					}
				}
				catch (ThreadInterruptedException ex)
				{
                    handler.Stack.NCacheLog.Error(Name, ex.ToString());
				}
                if (handler.Stack.NCacheLog.IsInfoEnabled) handler.Stack.NCacheLog.Info(Name + "    ---> Stopped!");
			}
		}
	
	
		internal class DownHandler:ThreadClass
		{
            private Alachisoft.NCache.Common.DataStructures.Queue mq;
			private Protocol handler;
            int id;

            public DownHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler)
			{
				this.mq = mq;
				this.handler = handler;
                string name = null;
				if (handler != null)
				{
                    Name = "DownHandler (" + handler.Name + ')';
				}
				else
				{
                    Name = "DownHandler";
				}

				IsBackground = true;
			}
            public DownHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler, string name, int id)
            {
                this.mq = mq;
                this.handler = handler;
                Name = name;
                IsBackground = true;
                this.id = id;
            }
		
		
			/// <summary>Removes events from mq and calls handler.down(evt) </summary>
			override public void  Run()
			{
				try
				{
					while (!mq.Closed)
					{
						try
						{
							Event evt = (Event) mq.remove();
							if (evt == null)
							{
                                handler.Stack.NCacheLog.Warn("Protocol", "removed null event");
								continue;
							}
			
							int type = evt.Type;
							if (type == Event.ACK || type == Event.START || type == Event.STOP)
							{
								if (handler.handleSpecialDownEvent(evt) == false)
									continue;
							}

                            if (handler.enableMonitoring)
                            {
                                handler.PublishDownQueueStats(mq.Count,id);
                            }

							handler.down(evt);
						}
						catch (QueueClosedException e)
						{
                            handler.Stack.NCacheLog.Error(Name, e.ToString());
							break;
						}
						catch (ThreadInterruptedException e)
						{
                            handler.Stack.NCacheLog.Error(Name, e.ToString());
							break;
						}
						catch (System.Exception e)
						{
                            handler.Stack.NCacheLog.Warn(Name, " exception is " + e.ToString());
						}
					}
				}
				catch (ThreadInterruptedException e)
				{
                    handler.Stack.NCacheLog.Error("DownHandler.Run():3", "exception=" + e.ToString());
				}
			}
		}
	

		public abstract string Name{get;}

        public Alachisoft.NCache.Common.DataStructures.Queue UpQueue { get { return up_queue; } }
        public Alachisoft.NCache.Common.DataStructures.Queue DownQueue { get { return down_queue; } }


		public ProtocolStack Stack
		{
			get { return this.stack; }			
			set { this.stack = value; }			
		}

		public Protocol UpProtocol
		{
			get { return up_prot; }
			set { this.up_prot = value; }			
		}

		public Protocol DownProtocol
		{
			get { return down_prot; }			
			set { this.down_prot = value; }			
		}

		protected long THREAD_JOIN_TIMEOUT = 1000;

		protected Hashtable props = new Hashtable();
		protected Protocol up_prot, down_prot;
		protected ProtocolStack stack;
        protected Alachisoft.NCache.Common.DataStructures.Queue up_queue, down_queue;
		
		protected int up_thread_prio = - 1;
		protected int down_thread_prio = - 1;
		protected bool down_thread = false; // determines whether the down_handler thread should be started
		protected bool up_thread = true; // determines whether the up_handler thread should be started

		protected UpHandler up_handler;
		protected DownHandler down_handler;
        protected bool _printMsgHdrs = false;
        internal bool enableMonitoring;
        protected bool useAvgStats = false;
		
		/// <summary> Configures the protocol initially. A configuration string consists of name=value
		/// items, separated by a ';' (semicolon), e.g.:<pre>
		/// "loopback=false;unicast_inport=4444"
		/// </pre>
		/// </summary>
		public virtual bool setProperties(Hashtable props)
		{
			if (props != null)
				this.props = (Hashtable)props.Clone();
			return true;
		}
		
		
		/// <summary>Called by Configurator. Removes 2 properties which are used by the Protocol directly and then
		/// calls setProperties(), which might invoke the setProperties() method of the actual protocol instance.
		/// </summary>
		public virtual bool setPropertiesInternal(Hashtable props)
		{
			this.props = (Hashtable)props.Clone();
			
			if (props.Contains("down_thread"))
			{
				down_thread = Convert.ToBoolean(props["down_thread"]);
				props.Remove("down_thread");
			}
			if (props.Contains("down_thread_prio"))
			{
				down_thread_prio = Convert.ToInt32(props["down_thread_prio"]);
				props.Remove("down_thread_prio");
			}
			if (props.Contains("up_thread"))
			{
				up_thread = Convert.ToBoolean(props["up_thread"]);
				props.Remove("up_thread");
			}
			if (props.Contains("up_thread_prio"))
			{
				up_thread_prio = Convert.ToInt32(props["up_thread_prio"]);
				props.Remove("up_thread_prio");
			}

            enableMonitoring = ServiceConfiguration.EnableDebuggingCounters;

            useAvgStats = ServiceConfiguration.UseAvgStats;

			return setProperties(props);
		}
		
		public virtual Hashtable getProperties()
		{
			return props;
		}

        public virtual void PublishUpQueueStats(long count,int queueId)
        {
        }

        public virtual void PublishDownQueueStats(long count,int queueId)
        {

        }


		/// <summary> Called after instance has been created (null constructor) and before protocol is started.
		/// Properties are already set. Other protocols are not yet connected and events cannot yet be sent.
		/// </summary>
		/// <exception cref=""> Exception Thrown if protocol cannot be initialized successfully. This will cause the
		/// ProtocolStack to fail, so the channel constructor will throw an exception
		/// </exception>
		public virtual void  init()
		{
		}
		
		/// <summary> This method is called on a {@link org.jgroups.Channel#connect(String)}. Starts work.
		/// Protocols are connected and queues are ready to receive events.
		/// Will be called <em>from bottom to top</em>. This call will replace
		/// the <b>START</b> and <b>START_OK</b> events.
		/// </summary>
		/// <exception cref=""> Exception Thrown if protocol cannot be started successfully. This will cause the ProtocolStack
		/// to fail, so {@link org.jgroups.Channel#connect(String)} will throw an exception
		/// </exception>
		public virtual void  start()
		{
		}
		
		/// <summary> This method is called on a {@link org.jgroups.Channel#disconnect()}. Stops work (e.g. by closing multicast socket).
		/// Will be called <em>from top to bottom</em>. This means that at the time of the method invocation the
		/// neighbor protocol below is still working. This method will replace the
		/// <b>STOP</b>, <b>STOP_OK</b>, <b>CLEANUP</b> and <b>CLEANUP_OK</b> events. The ProtocolStack guarantees that
		/// when this method is called all messages in the down queue will have been flushed
		/// </summary>
		public virtual void  stop()
		{
		}
		
		
		/// <summary> This method is called on a {@link org.jgroups.Channel#close()}.
		/// Does some cleanup; after the call the VM will terminate
		/// </summary>
		public virtual void  destroy()
		{
		}
		
		
		/// <summary>List of events that are required to be answered by some layer above.</summary>
		/// <returns> Vector (of Integers) 
		/// </returns>
		public virtual ArrayList requiredUpServices()
		{
			return null;
		}
		
		/// <summary>List of events that are required to be answered by some layer below.</summary>
		/// <returns> Vector (of Integers) 
		/// </returns>
		public virtual ArrayList requiredDownServices()
		{
			return null;
		}
		
		/// <summary>List of events that are provided to layers above (they will be handled when sent down from
		/// above).
		/// </summary>
		/// <returns> Vector (of Integers) 
		/// </returns>
		public virtual ArrayList providedUpServices()
		{
			return null;
		}
		
		/// <summary>List of events that are provided to layers below (they will be handled when sent down from
		/// below).
		/// </summary>
		/// <returns> Vector (of Integers) 
		/// </returns>
		public virtual ArrayList providedDownServices()
		{
			return null;
		}
		
		
		/// <summary>Used internally. If overridden, call this method first. Only creates the up_handler thread
		/// if down_thread is true 
		/// </summary>
		public virtual void  startUpHandler()
		{
			if (up_thread)
			{
				if (up_handler == null)
				{
                    up_queue = new Alachisoft.NCache.Common.DataStructures.Queue();
					up_handler = new UpHandler(up_queue, this);
					
					up_handler.Start();
				}
			}
		}
		
		
		/// <summary>Used internally. If overridden, call this method first. Only creates the down_handler thread
		/// if down_thread is true 
		/// </summary>
		public virtual void  startDownHandler()
		{
			if (down_thread)
			{
				if (down_handler == null)
				{
                    down_queue = new Alachisoft.NCache.Common.DataStructures.Queue();
					down_handler = new DownHandler(down_queue, this);
					
					down_handler.Start();
				}
			}
		}
		
		
		/// <summary>Used internally. If overridden, call parent's method first </summary>
		public virtual void  stopInternal()
		{
			if(up_queue != null)
                up_queue.close(false); // this should terminate up_handler thread
			
			if (up_handler != null && up_handler.IsAlive)
			{
				try
				{
					up_handler.Join(THREAD_JOIN_TIMEOUT);
				}
				catch (System.Exception e)
				{
                    stack.NCacheLog.Error("Protocol.stopInternal()", "up_handler.Join " + e.Message);
				}
				if (up_handler != null && up_handler.IsAlive)
				{
					up_handler.Interrupt(); // still alive ? let's just kill it without mercy...
					try
					{
						up_handler.Join(THREAD_JOIN_TIMEOUT);
					}
					catch (System.Exception e)
					{
                        stack.NCacheLog.Error("Protocol.stopInternal()", "up_handler.Join " + e.Message);
					}
					if (up_handler != null && up_handler.IsAlive)
                        stack.NCacheLog.Error("Protocol", "up_handler thread for " + Name + " was interrupted (in order to be terminated), but is still alive");
				}
			}
			up_handler = null;

			if(down_queue != null)
                down_queue.close(false); // this should terminate down_handler thread
			if (down_handler != null && down_handler.IsAlive)
			{
				try
				{
					down_handler.Join(THREAD_JOIN_TIMEOUT);
				}
				catch (System.Exception e)
				{
                    stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
				}
				if (down_handler != null && down_handler.IsAlive)
				{
					down_handler.Interrupt(); // still alive ? let's just kill it without mercy...
					try
					{
						down_handler.Join(THREAD_JOIN_TIMEOUT);
					}
					catch (System.Exception e)
					{
                        stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
					}
					if (down_handler != null && down_handler.IsAlive)
                        stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
				}
			}
			down_handler = null;
		}
		
		
		/// <summary> Internal method, should not be called by clients. Used by ProtocolStack. I would have
		/// used the 'friends' modifier, but this is available only in C++ ... If the up_handler thread
		/// is not available (down_thread == false), then directly call the up() method: we will run on the
		/// caller's thread (e.g. the protocol layer below us).
		/// </summary>
		public virtual void  receiveUpEvent(Event evt)
		{
            int type = evt.Type;
            if (_printMsgHdrs && type == Event.MSG)
                printMsgHeaders(evt,"up()");

            if (up_handler == null)
			{
				up(evt);
				return ;
			}
			try
			{
                if (stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info(Name + ".receiveUpEvent()", "RentId :" + evt.RentId + "up queue count : " + up_queue.Count);
				up_queue.add(evt, evt.Priority);
			}
			catch (System.Exception e)
			{
                stack.NCacheLog.Warn("Protocol.receiveUpEvent()", e.ToString());
			}
		}
        /// <summary>
        /// Prints the header of a message. Used for debugging purpose.
        /// </summary>
        /// <param name="evt"></param>
        protected void printMsgHeaders(Event evt,string extra)
        {
            Message m = (Message)evt.Arg;
            try
            {
            if(m != null)
            {
                if (stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info(this.Name + "." + extra + ".printMsgHeaders()", Global.CollectionToString(m.Headers));
            }
            }
            catch(Exception e)
            {
                stack.NCacheLog.Error(this.Name + ".printMsgHeaders()", e.ToString());
            }
        }
		/// <summary> Internal method, should not be called by clients. Used by ProtocolStack. I would have
		/// used the 'friends' modifier, but this is available only in C++ ... If the down_handler thread
		/// is not available (down_thread == false), then directly call the down() method: we will run on the
		/// caller's thread (e.g. the protocol layer above us).
		/// </summary>
		public virtual void  receiveDownEvent(Event evt)
		{
			int type = evt.Type;

            if (down_handler == null)
			{
				if (type == Event.ACK || type == Event.START || type == Event.STOP)
				{
					if (handleSpecialDownEvent(evt) == false)
						return ;
				}
                if (_printMsgHdrs && type == Event.MSG)
                    printMsgHeaders(evt,"down()");
				down(evt);
				return ;
			}
			try
			{
				if (type == Event.STOP || type == Event.VIEW_BCAST_MSG)
				{
					if (handleSpecialDownEvent(evt) == false)
						return ;
					if (down_prot != null)
					{
						down_prot.receiveDownEvent(evt);
					}
					return;
				}
				down_queue.add(evt, evt.Priority);
			}
			catch (System.Exception e)
			{
                stack.NCacheLog.Warn("Protocol.receiveDownEvent():2",   e.ToString());
			}
		}
		
		/// <summary> Causes the event to be forwarded to the next layer up in the hierarchy. Typically called
		/// by the implementation of <code>Up</code> (when done).
		/// </summary>
		public virtual void  passUp(Event evt)
		{
			if (up_prot != null)
			{
#if DEBUG
				if(evt.Type == Event.MSG)
				{
					Message msg = (Message)evt.Arg;
                    if (stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info(Name + ".passUp()", "hdr: " + Global.CollectionToString(msg.Headers));
				}
#endif
				up_prot.receiveUpEvent(evt);
			}
			else
                stack.NCacheLog.Error("Protocol", "no upper layer available");
		}
		
		/// <summary> Causes the event to be forwarded to the next layer down in the hierarchy.Typically called
		/// by the implementation of <code>Down</code> (when done).
		/// </summary>
		public virtual void  passDown(Event evt)
		{
			if (down_prot != null)
			{
#if DEBUG
				if(evt.Type == Event.MSG)
				{
					Message msg = (Message)evt.Arg;
                    if (stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info(Name + ".passDown()", "hdr: " + Global.CollectionToString(msg.Headers));
				}
#endif
				down_prot.receiveDownEvent(evt);
			}
			else
                stack.NCacheLog.Error("Protocol", "no lower layer available");
		}
		
		
		/// <summary> An event was received from the layer below. Usually the current layer will want to examine
		/// the event type and - depending on its type - perform some computation
		/// (e.g. removing headers from a MSG event type, or updating the internal membership list
		/// when receiving a VIEW_CHANGE event).
		/// Finally the event is either a) discarded, or b) an event is sent down
		/// the stack using <code>passDown()</code> or c) the event (or another event) is sent up
		/// the stack using <code>passUp()</code>.
		/// </summary>
		public virtual void  up(Event evt)
		{
			passUp(evt);
		}
		
		/// <summary> An event is to be sent down the stack. The layer may want to examine its type and perform
		/// some action on it, depending on the event's type. If the event is a message MSG, then
		/// the layer may need to add a header to it (or do nothing at all) before sending it down
		/// the stack using <code>passDown()</code>. In case of a GET_ADDRESS event (which tries to
		/// retrieve the stack's address from one of the bottom layers), the layer may need to send
		/// a new response event back up the stack using <code>passUp()</code>.
		/// </summary>
		public virtual void  down(Event evt)
		{
			passDown(evt);
		}
		
		
		/// <summary>These are special internal events that should not be handled by protocols</summary>
		/// <returns> boolean True: the event should be passed further down the stack. False: the event should
		/// be discarded (not passed down the stack)
		/// </returns>
		protected  virtual bool handleSpecialDownEvent(Event evt)
		{
			switch (evt.Type)
			{
				case Event.ACK: 
					if (down_prot == null)
					{
						passUp(new Event(Event.ACK_OK));
						return false; // don't pass down the stack
					}
					goto case Event.START;
				
				case Event.START: 
					try
					{
						start();
						
						// if we're the transport protocol, reply with a START_OK up the stack
						if (down_prot == null)
						{
							passUp(new Event(Event.START_OK, (object) true));
							return false; // don't pass down the stack
						}
						return true; // pass down the stack
					}
					catch (System.Exception e)
					{
                        stack.NCacheLog.Error("Protocol.handleSpecialDownEvent", e.ToString());
						passUp(new Event(Event.START_OK, new System.Exception(e.Message,e)));
					}
					return false;
				
				case Event.STOP: 
					try
					{
						stop();
					}
					catch (System.Exception e)
					{
                        stack.NCacheLog.Error("Protocol.handleSpecialDownEvent()", e.ToString());
					}
					if (down_prot == null)
					{
						passUp(new Event(Event.STOP_OK, (object) true));
						return false; // don't pass down the stack
					}
					return true; // pass down the stack
				
				default: 
					return true; // pass down by default
				
			}
		}
	}
}
