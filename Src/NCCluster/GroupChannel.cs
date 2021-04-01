// $Id: GroupChannel.java,v 1.25 2004/08/29 19:35:03 belaban Exp $
using System;

using ProtocolStack = Alachisoft.NGroups.Stack.ProtocolStack;
using Queue = Alachisoft.NCache.Common.DataStructures.Queue;
using QueueClosedException = Alachisoft.NGroups.Util.QueueClosedException;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NGroups.Stack;

namespace Alachisoft.NGroups
{
    /// <summary> GroupChannel is a pure Java implementation of Channel
    /// When a GroupChannel object is instantiated it automatically sets up the
    /// protocol stack
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    /// <author>  Filip Hanik
    /// </author>
    /// <version>  $Revision: 1.25 $
    /// </version>
    public class GroupChannel : Channel
	{
		internal string FORCE_PROPS = "force.properties";
		
		/* the protocol stack configuration string */
		private string props;
		
		/*the address of this GroupChannel instance*/
		private Address local_addr = null;
		
		/*the channel (also know as group) name*/
		private string channel_name = null; // group id

		private string subGroup_name = null; // subgroup id

		/*the latest view of the group membership*/
		private View my_view = null;
		/*the queue that is used to receive messages (events) from the protocol stack*/
		private Queue mq = new Queue();
		/*the protocol stack, used to send and receive messages from the protocol stack*/
		private Stack.ProtocolStack prot_stack = null;
		
		/// <summary>Thread responsible for closing a channel and potentially reconnecting to it (e.g. when shunned) </summary>
		internal CloserThread closer = null;
		
		/*lock objects*/
		private object local_addr_mutex = new object();
		private object connect_mutex = new object();
		private bool connect_ok_event_received = false;
        private object connect_mutex_phase2 = new object();
        private bool connect_ok_event_received_phase2 = false;
		private object disconnect_mutex = new object();
		private bool disconnect_ok_event_received = false;
		private object flow_control_mutex = new object();
        private object maintenance_mutex = new object();
        private bool maintenance_marked_event_received = false;
        private object is_in_transfer_mutex = new object();
        private bool is_in_transfer_event_received = false;

        /// <summary>wait until we have a non-null local_addr </summary>
        private long LOCAL_ADDR_TIMEOUT = 30000; //=Long.parseLong(System.getProperty("local_addr.timeout", "30000"));
		/*flag to indicate whether to receive views from the protocol stack*/
		private bool receive_views = true;
		/*flag to indicate whether to receive suspect messages*/
		private bool receive_suspects = true;
		/*flag to indicate whether to receive blocks, if this is set to true, receive_views is set to true*/
		private bool receive_blocks = false;
		/*flag to indicate whether to receive local messages
		*if this is set to false, the GroupChannel will not receive messages sent by itself*/
		private bool receive_local_msgs = true;
		/*flag to indicate whether the channel will reconnect (reopen) when the exit message is received*/
		private bool auto_reconnect = false;
		/*channel connected flag*/
		private bool connected = false;
		private bool block_sending = false; // block send()/down() if true (unlocked by UNBLOCK_SEND event)
		/*channel closed flag*/
		private bool closed = false; // close() has been called, channel is unusable
        private bool _isStartedAsMirror = false;

        private bool isClusterInStateTransfer = false;
        
      
        private ILogger _ncacheLog;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; 
            
                if (prot_stack != null)
                    prot_stack.NCacheLog = value;
            }
        }

		/// <summary>Used to maintain additional data across channel disconnects/reconnects. This is a kludge and will be remove
		/// as soon as JGroups supports logical addresses
		/// </summary>
		private byte[] additional_data = null;

		static GroupChannel()
		{
			Global.RegisterCompactTypes();
		}

        internal Stack.ProtocolStack Stack
        {
            get { return prot_stack; }
        }
		/// <summary> Constructs a <code>GroupChannel</code> instance with the protocol stack
		/// configuration based upon the specified properties parameter.
		/// 
		/// </summary>
		/// <param name="properties">an old style property string, a string representing a
		/// system resource containing a JGroups XML configuration,
		/// a string representing a URL pointing to a JGroups XML
		/// XML configuration, or a string representing a file name
		/// that contains a JGroups XML configuration.
		/// 
		/// </param>
		/// <throws>  ChannelException if problems occur during the configuration and </throws>
		/// <summary>                          initialization of the protocol stack.
		/// </summary>
        public GroupChannel(string properties, ILogger NCacheLog)
		{
			props = properties;
            this._ncacheLog = NCacheLog;

            /*create the new protocol stack*/
			prot_stack = new Stack.ProtocolStack(this, props);
            prot_stack.NCacheLog = NCacheLog;


			/* Setup protocol stack (create layers, queues between them */
			try
			{
				prot_stack.setup();
			}
			catch (System.Exception e)
			{
				NCacheLog.Error("GroupChannel.GroupChannel()",  e.ToString());
				throw new ChannelException("GroupChannel(): " + e);
			}
		}

        public void InitializePerformanceCounter(string instanceName)
        {
            if (prot_stack != null)
            {
                prot_stack.InitializePerfCounters(instanceName);
            }
        }

		/// <summary> Returns the protocol stack.
		/// Currently used by Debugger.
		/// Specific to GroupChannel, therefore
		/// not visible in Channel
		/// </summary>
		internal Stack.ProtocolStack ProtocolStack
		{
			get { return prot_stack; }
		}

        public override PerfStatsCollector ClusterStatCollector
        {
            get { return this.prot_stack.perfStatsColl; }
        }


		/// <summary> returns the protocol stack configuration in string format.
		/// an example of this property is<BR>
		/// "UDP:PING:FD:STABLE:NAKACK:UNICAST:FRAG:FLUSH:GMS:VIEW_ENFORCER:STATE_TRANSFER:QUEUE"
		/// </summary>
		public string Properties
		{
			get { return props; }			
		}

		/// <summary> returns true if the Open operation has been called successfully</summary>
		override public bool IsOpen
		{
			get { return !closed; }			
		}

		/// <summary> returns true if the Connect operation has been called successfully</summary>
		override public bool IsConnected
		{
			get { return connected; }			
		}

		override public int NumMessages
		{
			get { return mq != null?mq.Count:- 1; }			
		}

		/// <summary> returns the current view.<BR>
		/// if the channel is not connected or if it is closed it will return null<BR>
		/// </summary>
		/// <returns> returns the current group view, or null if the channel is closed or disconnected
		/// </returns>
		override public View View
		{
			get { return closed || !connected?null:my_view; }			
		}

		/// <summary> returns the local address of the channel
		/// returns null if the channel is closed
		/// </summary>
		override public Address LocalAddress
		{
			get { return closed?null:local_addr; }			
		}

		/// <summary> returns the name of the channel
		/// if the channel is not connected or if it is closed it will return null
		/// </summary>
		override public string ChannelName
		{
			get { return closed?null:(!connected?null:channel_name); }			
		}
		
		
		/// <summary> Returns a pretty-printed form of all the protocols. If include_properties is set,
		/// the properties for each protocol will also be printed.
		/// </summary>
		public virtual string printProtocolSpec(bool include_properties)
		{
			return prot_stack != null?prot_stack.printProtocolSpec(include_properties):null;
		}

        public override void connectPhase2()
        {
            lock (this)
            {
                /*make sure the channel is not closed*/
                checkClosed();

                // only connect if we are not a unicast channel
                if (channel_name != null)
                {

                    /* Wait for notification that the channel has been connected to the group */
                    lock (connect_mutex_phase2)
                    {
                        // wait for CONNECT_OK event
                        Event connect_event = new Event(Event.CONNECT_PHASE_2, _isStartedAsMirror);
                        connect_ok_event_received_phase2 = false; // added patch by Roland Kurman (see history.txt)
                        down(connect_event);

                        try
                        {
                            while (!connect_ok_event_received_phase2)
                                System.Threading.Monitor.Wait(connect_mutex_phase2);
                        }
                        catch (System.Exception e)
                        {
                            NCacheLog.Error("GroupChannel.connect():2",   "exception=" + e);
                        }
                    }
                }

            }
        }   
		
		/// <summary> Connects the channel to a group.<BR>
		/// If the channel is already connected, an error message will be printed to the error log<BR>
		/// If the channel is closed a ChannelClosed exception will be thrown<BR>
		/// This method starts the protocol stack by calling ProtocolStack.start<BR>
		/// then it sends an Event.CONNECT event down the stack and waits to receive a CONNECT_OK event<BR>
		/// Once the CONNECT_OK event arrives from the protocol stack, any channel listeners are notified<BR>
		/// and the channel is considered connected<BR>
		/// 
		/// </summary>
		/// <param name="channel_name">A <code>String</code> denoting the group name. Cannot be null.
		/// </param>
		/// <exception cref=""> ChannelException The protocol stack cannot be started
		/// </exception>
		/// <exception cref=""> ChannelClosedException The channel is closed and therefore cannot be used any longer.
		/// A new channel has to be created first.
		/// </exception>
		public override void connect(string channel_name, string subGroup_name, bool isStartedAsMirror,bool twoPhaseInitialization)
		{
			lock (this)
			{
				/*make sure the channel is not closed*/
				checkClosed();

                _isStartedAsMirror = isStartedAsMirror;
				/*if we already are connected, then ignore this*/
				if (connected)
				{
					NCacheLog.Error("GroupChannel",   "already connected to " + channel_name);
					return ;
				}
				
				/*make sure we have a valid channel name*/
				if (channel_name == null)
				{
					NCacheLog.Error("GroupChannel",   "channel_name is null, assuming unicast channel");
				}
				else
					this.channel_name = channel_name;

				//=============================================
				if (subGroup_name != null)
					this.subGroup_name = subGroup_name;
				//=============================================
				
				try
				{
					prot_stack.startStack(); // calls start() in all protocols, from top to bottom
				}
				catch (System.Exception e)
				{
					NCacheLog.Error("GroupChannel.connect()",   "exception: " + e);
					
					throw new ChannelException(e.Message,e);
				}
				
				/* try to get LOCAL_ADDR_TIMEOUT. Catch SecurityException thrown if called
				* in an untrusted environment (e.g. using JNLP) */
				LOCAL_ADDR_TIMEOUT = 30000;
				
				/* Wait LOCAL_ADDR_TIMEOUT milliseconds for local_addr to have a non-null value (set by SET_LOCAL_ADDRESS) */
				lock (local_addr_mutex)
				{
					long wait_time = LOCAL_ADDR_TIMEOUT, start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					while (local_addr == null && wait_time > 0)
					{
						try
						{
							System.Threading.Monitor.Wait(local_addr_mutex, TimeSpan.FromMilliseconds(wait_time));
						}
						catch (System.Threading.ThreadInterruptedException e)
						{
							NCacheLog.Error("GroupChannel.connect():2",   "exception=" + e);
						}
						wait_time -= ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start);
					}

					// SAL:
					if (wait_time < 0)
					{
						NCacheLog.Fatal( "[Timeout]GroupChannel.connect:" + wait_time);
					}
				}
				
				// must have given us a valid local address; if not we won't be able to continue
				if (local_addr == null)
				{
					NCacheLog.Error("GroupChannel",   "local_addr == null; cannot connect");
					throw new ChannelException("local_addr is null");
				}
				
				
				/*create a temporary view, assume this channel is the only member and
				*is the coordinator*/
				System.Collections.ArrayList t = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(1));
				t.Add(local_addr);
				my_view = new View(local_addr, 0, t); // create a dummy view
				
				// only connect if we are not a unicast channel
				if (channel_name != null)
				{
					
					/* Wait for notification that the channel has been connected to the group */
					lock (connect_mutex)
					{
						// wait for CONNECT_OK event
						Event connect_event = new Event(Event.CONNECT, new object[] { channel_name, subGroup_name, isStartedAsMirror,twoPhaseInitialization });
						connect_ok_event_received = false; // added patch by Roland Kurman (see history.txt)
						down(connect_event);
						
						try
						{
							while (!connect_ok_event_received)
								System.Threading.Monitor.Wait(connect_mutex);
						}
						catch (System.Exception e)
						{
							NCacheLog.Error("GroupChannel.connect():2",   "exception=" + e);
						}
					}
				}
				
				/*notify any channel listeners*/
				connected = true;
				if (channel_listener != null)
					channel_listener.channelConnected(this);
			}
        }
        
        public override bool IsClusterInStateTransfer()
        {
            lock (this)
            {
                if (closed)
                    return false;

                if (connected)
                {

                    if (channel_name != null)
                    {
                        Event isClusterInStateTransferEvent = new Event(Event.IS_CLUSTER_IN_STATE_TRANSFER);

                        lock (maintenance_mutex)
                        {
                            try
                            {
                                maintenance_marked_event_received = false;
                                down(isClusterInStateTransferEvent); // DISCONNECT is handled by each layer
                                while (!is_in_transfer_event_received)
                                    System.Threading.Monitor.Wait(is_in_transfer_mutex);
                                return isClusterInStateTransfer;
                            }
                            catch (System.Exception e)
                            {
                                NCacheLog.Error("GroupChannel.disconnect", e.ToString());
                            }
                        }
                    }
                }
                return false;
            }
        }

        public override void ExitMaintenance()
        {
            lock (this)
            {
                if (closed)
                    return;

                if (connected)
                {

                    if (channel_name != null)
                    {
                        Event unmarkForMaintenanceEvent = new Event(Event.UNMARK_FOR_MAINTENANCE);

                        lock (maintenance_mutex)
                        {
                            try
                            {
                                maintenance_marked_event_received = false;
                                down(unmarkForMaintenanceEvent); // DISCONNECT is handled by each layer
                                while (!maintenance_marked_event_received)
                                    System.Threading.Monitor.Wait(maintenance_mutex); // wait for DISCONNECT_OK event
                            }
                            catch (System.Exception e)
                            {
                                NCacheLog.Error("GroupChannel.disconnect", e.ToString());
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Disconnects the channel if it is connected. If the channel is closed, this operation is ignored<BR>
        /// Otherwise the following actions happen in the listed order<BR>
        /// <ol>
        /// <li> The GroupChannel sends a DISCONNECT event down the protocol stack<BR>
        /// <li> Blocks until the channel to receives a DISCONNECT_OK event<BR>
        /// <li> Sends a STOP_QUEING event down the stack<BR>
        /// <li> Stops the protocol stack by calling ProtocolStack.stop()<BR>
        /// <li> Notifies the listener, if the listener is available<BR>
        /// </ol>
        /// </summary>
        public override void  disconnect()
		{
			lock (this)
			{
				if (closed)
					return ;
				
				if (connected)
				{
					
					if (channel_name != null)
					{
						
						/* Send down a DISCONNECT event. The DISCONNECT event travels down to the GMS, where a
						*  DISCONNECT_OK response is generated and sent up the stack. GroupChannel blocks until a
						*  DISCONNECT_OK has been received, or until timeout has elapsed.
						*/
						Event disconnect_event = new Event(Event.DISCONNECT, local_addr);
						
						lock (disconnect_mutex)
						{
							try
							{
								disconnect_ok_event_received = false;
								down(disconnect_event); // DISCONNECT is handled by each layer
								while (!disconnect_ok_event_received)
									System.Threading.Monitor.Wait(disconnect_mutex); // wait for DISCONNECT_OK event
							}
							catch (System.Exception e)
							{
								NCacheLog.Error("GroupChannel.disconnect",  e.ToString());								
							}
						}
					}
					
					// Just in case we use the QUEUE protocol and it is still blocked...
					down(new Event(Event.STOP_QUEUEING));
					
					connected = false;
					try
					{
						prot_stack.stopStack(); // calls stop() in all protocols, from top to bottom
						prot_stack.destroy();
					}
					catch (System.Exception e)
					{
						NCacheLog.Error("GroupChannel.disconnect()",  e.ToString());						
					}
					
					if (channel_listener != null)
						channel_listener.channelDisconnected(this);
					
					init(); // sets local_addr=null; changed March 18 2003 (bela) -- prevented successful rejoining
				}
			}
		}
		
		
		/// <summary> Destroys the channel.<BR>
		/// After this method has been called, the channel us unusable.<BR>
		/// This operation will disconnect the channel and close the channel receive queue immediately<BR>
		/// </summary>
		public override void  close()
		{
			lock (this)
			{
				_close(true, true); // by default disconnect before closing channel and close mq
			}
		}
		
		
		/// <summary> Opens the channel.<BR>
		/// this does the following actions<BR>
		/// 1. Resets the receiver queue by calling Queue.reset<BR>
		/// 2. Sets up the protocol stack by calling ProtocolStack.setup<BR>
		/// 3. Sets the closed flag to false.<BR>
		/// </summary>
		public override void  open()
		{
			lock (this)
			{
				if (!closed)
					throw new ChannelException("GroupChannel.open(): channel is already open.");
				
				try
				{
					mq.reset();
					
					// new stack is created on open() - bela June 12 2003
					prot_stack = new Stack.ProtocolStack(this, props);
					prot_stack.setup();
					closed = false;
				}
				catch (System.Exception e)
				{
					throw new ChannelException("GroupChannel().open(): " + e.Message);
				}
			}
		}
		
		
		/// <summary> implementation of the Transport interface.<BR>
		/// Sends a message through the protocol stack<BR>
		/// </summary>
		/// <param name="msg">the message to be sent through the protocol stack,
		/// the destination of the message is specified inside the message itself
		/// </param>
		/// <exception cref=""> ChannelNotConnectedException
		/// </exception>
		/// <exception cref=""> ChannelClosedException
		/// </exception>
		public override void  send(Message msg)
		{
			checkClosed();
			checkNotConnected();

            //Rent an event
            Event evt = new Event();
            evt.Type = Event.MSG;
			msg.IsUserMsg = true;
            evt.Arg = msg;
            evt.Priority = msg.Priority;

            down(evt);
		}
		
		
		/// <summary> creates a new message with the destination address, and the source address
		/// and the object as the message value
		/// </summary>
		/// <param name="dst">- the destination address of the message, null for all members
		/// </param>
		/// <param name="src">- the source address of the message
		/// </param>
		/// <param name="obj">- the value of the message
		/// </param>
		/// <exception cref=""> ChannelNotConnectedException
		/// </exception>
		/// <exception cref=""> ChannelClosedException
		/// </exception>
		/// <seealso cref="GroupChannel#send">
		/// </seealso>
		public override void  send(Address dst, Address src, object obj)
		{
			send(new Message(dst, src, obj));
		}
		
		
		/// <summary> Blocking receive method.
		/// This method returns the object that was first received by this JChannel and that has not been
		/// received before. After the object is received, it is removed from the receive queue.<BR>
		/// If you only want to inspect the object received without removing it from the queue call
		/// JChannel.peek<BR>
		/// If no messages are in the receive queue, this method blocks until a message is added or the operation times out<BR>
		/// By specifying a timeout of 0, the operation blocks forever, or until a message has been received.
		/// </summary>
		/// <param name="timeout">the number of milliseconds to wait if the receive queue is empty. 0 means wait forever
		/// </param>
		/// <exception cref=""> TimeoutException if a timeout occurred prior to a new message was received
		/// </exception>
		/// <exception cref=""> ChannelNotConnectedException
		/// </exception>
		/// <exception cref=""> ChannelClosedException
		/// </exception>
		/// <seealso cref="JChannel#peek">
		/// </seealso>
		public override object receive(long timeout)
		{
			object retval = null;
			Event evt;
			
			checkClosed();
			checkNotConnected();
			
			try
			{
				evt = (timeout <= 0)?(Event) mq.remove():(Event) mq.remove(timeout);
				retval = getEvent(evt);
				evt = null;
				return retval;
			}
			catch (Util.QueueClosedException e)
			{
				NCacheLog.Error("GroupChannel.receive()",   e.ToString());
				throw new ChannelClosedException();
			}

            catch (NCache.Runtime.Exceptions.TimeoutException t)
            {
				NCacheLog.Error("GroupChannel.receive()",   t.ToString());
				
				throw t;
			}
			catch (System.Exception e)
			{
				NCacheLog.Error("GroupChannel.receive()",   e.ToString());
				return null;
			}
		}
		
		
		/// <summary> Just peeks at the next message, view or block. Does <em>not</em> install
		/// new view if view is received<BR>
		/// Does the same thing as GroupChannel.receive but doesn't remove the object from the
		/// receiver queue
		/// </summary>
		public override Event peek(long timeout)
		{
			Event evt;
			checkClosed();
			checkNotConnected();
			
			try
			{
                bool success = true;
				evt = (timeout <= 0)?(Event) mq.peek():(Event) mq.peek(timeout,out success);
                if (!success) // timeout eception
                {
                    NCacheLog.Fatal( "[Timeout]GroupChannel.peek: Timeout exception " + timeout);
                    return null;
                }
				return evt;
			}
			catch (Util.QueueClosedException queue_closed)
			{
				NCacheLog.Error("GroupChannel.peek()",   queue_closed.ToString());
				return null;
			}
			catch (System.Exception e)
			{
				NCacheLog.Error("GroupChannel.peek",   "exception: " + e.ToString());
				return null;
			}
		}
		
		
		/// <summary> sets a channel option
		/// the options can be either
		/// <PRE>
		/// Channel.BLOCK
		/// Channel.VIEW
		/// Channel.SUSPECT
		/// Channel.LOCAL
		/// Channel.GET_STATE_EVENTS
		/// Channel.AUTO_RECONNECT
		/// Channel.AUTO_GETSTATE
		/// </PRE>
		/// There are certain dependencies between the options that you can set, I will try to describe them here<BR>
		/// Option: Channel.VIEW option<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true the GroupChannel will receive VIEW change events<BR>
		/// <BR>
		/// Option: Channel.SUSPECT<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true the GroupChannel will receive SUSPECT events<BR>
		/// <BR>
		/// Option: Channel.BLOCK<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true will set setOpt(VIEW, true) and the GroupChannel will receive BLOCKS and VIEW events<BR>
		/// <BR>
		/// Option: GET_STATE_EVENTS<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true the GroupChannel will receive state events<BR>
		/// <BR>
		/// Option: LOCAL<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true the GroupChannel will receive messages that it self sent out.<BR>
		/// <BR>
		/// Option: AUTO_RECONNECT<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true and the GroupChannel will try to reconnect when it is being closed<BR>
		/// <BR>
		/// Option: AUTO_GETSTATE<BR>
		/// Value:  java.lang.Boolean<BR>
		/// Result: set to true, the AUTO_RECONNECT will be set to true and the GroupChannel will try to get the state after a close and reconnect happens<BR>
		/// <BR>
		/// 
		/// </summary>
		/// <param name="option">the parameter option Channel.VIEW, Channel.SUSPECT, etc
		/// </param>
		/// <param name="value">the value to set for this option
		/// 
		/// </param>
		public override void  setOpt(int option, object value_Renamed)
		{
			if (closed)
			{
				NCacheLog.Warn("GroupChannel.setOpt",   "channel is closed; option not set!");
				return ;
			}
			
			switch (option)
			{
				case SUSPECT: 
					if (value_Renamed is System.Boolean)
						receive_suspects = ((System.Boolean) value_Renamed);
					else
					{
						NCacheLog.Error("GroupChannel.setOpt",   "option " + Channel.option2String(option) + " (" + value_Renamed + "): value has to be Boolean.");
					}
					break;
				
				case BLOCK: 
					if (value_Renamed is System.Boolean)
						receive_blocks = ((System.Boolean) value_Renamed);
					else
					{
						NCacheLog.Error("GroupChannel.setOpt",   "option " + Channel.option2String(option) + " (" + value_Renamed + "): value has to be Boolean.");
					}
					if (receive_blocks)
						receive_views = true;
					break;
				
				
				case LOCAL: 
					if (value_Renamed is System.Boolean)
						receive_local_msgs = ((System.Boolean) value_Renamed);
					else 
					{
						NCacheLog.Error("GroupChannel.setOpt",   "option " + Channel.option2String(option) + " (" + value_Renamed + "): value has to be Boolean.");
					}
					break;
				
				
				case AUTO_RECONNECT: 
					if (value_Renamed is System.Boolean)
						auto_reconnect = ((System.Boolean) value_Renamed);
					else
					{
						NCacheLog.Error("GroupChannel.setOpt",   "option " + Channel.option2String(option) + " (" + value_Renamed + "): value has to be Boolean.");
					}
					break;
				
				
				default: 
					NCacheLog.Error("GroupChannel.setOpt",   "option " + Channel.option2String(option) + " not known.");
					break;
				
			}
		}
		
		
		/// <summary> returns the value of an option.</summary>
		/// <param name="option">the option you want to see the value for
		/// </param>
		/// <returns> the object value, in most cases java.lang.Boolean
		/// </returns>
		/// <seealso cref="GroupChannel#setOpt">
		/// </seealso>
		public override object getOpt(int option)
		{
			switch (option)
			{
				case BLOCK: 
					return receive_blocks?true:false;
				
				case SUSPECT: 
					return receive_suspects?true:false;
				
				case LOCAL: 
					return receive_local_msgs?true:false;
				
				default: 
					NCacheLog.Error("GroupChannel.get",   "option " + Channel.option2String(option) + " not known.");
					return null;
				
			}
		}
		
		
		/// <summary> Called to acknowledge a block() (callback in <code>MembershipListener</code> or
		/// <code>BlockEvent</code> received from call to <code>receive()</code>).
		/// After sending blockOk(), no messages should be sent until a new view has been received.
		/// Calling this method on a closed channel has no effect.
		/// </summary>
		public override void  blockOk()
		{
			down(new Event(Event.BLOCK_OK));
			down(new Event(Event.START_QUEUEING));
		}
		
		/// <summary> Callback method <BR>
		/// Called by the ProtocolStack when a message is received.
		/// It will be added to the message queue from which subsequent
		/// <code>Receive</code>s will dequeue it.
		/// </summary>
		/// <param name="evt">the event carrying the message from the protocol stack
		/// </param>
		public virtual void  up(Event evt)
		{
			int type = evt.Type;
			Message msg;
			
			/*if the queue is not available, there is no point in
			*processing the message at all*/
			if (mq == null)
			{
				NCacheLog.Error("GroupChannel.up",   "message queue is null.");
				return ;
			}
			switch (type)
			{
				
				
				case Event.MSG: 
					msg = (Message) evt.Arg;
					if (!receive_local_msgs)
					{
						// discard local messages (sent by myself to me)
						if (local_addr != null && msg.Src != null)
							if (local_addr.Equals(msg.Src))
								return ;
					}
					break;
				
				
				case Event.VIEW_CHANGE: 
					my_view = (View) evt.Arg;
					
					// we simply set the state to connected
					if (connected == false)
					{
						connected = true;
						lock (connect_mutex)
						{
							connect_ok_event_received = true;
							System.Threading.Monitor.Pulse(connect_mutex);
						}
					}
					
					// unblock queueing of messages due to previous BLOCK event:
					down(new Event(Event.STOP_QUEUEING));
					if (!receive_views)
					// discard if client has not set receving views to on
						return ;
					break;
				
				
				case Event.SUSPECT: 
					if (!receive_suspects)
						return ;
					break;
				
				
				case Event.CONFIG: 
					System.Collections.Hashtable config = (System.Collections.Hashtable) evt.Arg;
					break;
				
				
				case Event.BLOCK: 
					// If BLOCK is received by application, then we trust the application to not send
					// any more messages until a VIEW_CHANGE is received. Otherwise (BLOCKs are disabled),
					// we queue any messages sent until the next VIEW_CHANGE (they will be sent in the
					// next view)
					
					if (!receive_blocks)
					{
						// discard if client has not set 'receiving blocks' to 'on'
						down(new Event(Event.BLOCK_OK));
						down(new Event(Event.START_QUEUEING));
						return ;
					}
					break;
				
				
				case Event.CONNECT_OK: 
					lock (connect_mutex)
					{
						connect_ok_event_received = true;
						System.Threading.Monitor.Pulse(connect_mutex);
					}
					break;

                case Event.CONNECT_OK_PHASE_2:
                    lock (connect_mutex_phase2)
                    {
                        connect_ok_event_received_phase2 = true;
                        System.Threading.Monitor.Pulse(connect_mutex_phase2);
                    }
                    break;
				
				case Event.DISCONNECT_OK: 
					lock (disconnect_mutex)
					{
						disconnect_ok_event_received = true;
						System.Threading.Monitor.PulseAll(disconnect_mutex);
					}
					break;

                case Event.MARKED_FOR_MAINTENANCE:
                    lock (maintenance_mutex)
                    {
                        maintenance_marked_event_received = true;
                        System.Threading.Monitor.PulseAll(maintenance_mutex);
                    }
                    break;

                case Event.IS_CLUSTER_IN_STATE_TRANSFER_RSP:
                    lock (is_in_transfer_mutex)
                    {
                        isClusterInStateTransfer = (bool)evt.Arg;
                        is_in_transfer_event_received = true;
                        System.Threading.Monitor.PulseAll(is_in_transfer_mutex);
                    }
                    break;

                case Event.SET_LOCAL_ADDRESS: 
					lock (local_addr_mutex)
					{
						local_addr = (Address) evt.Arg;
						System.Threading.Monitor.PulseAll(local_addr_mutex);
					}
					break;
				
				
				case Event.EXIT: 
					handleExit(evt);
					return ; // no need to pass event up; already done in handleExit()
				
				
				case Event.BLOCK_SEND: 
					lock (flow_control_mutex)
					{
						NCacheLog.Error("GroupChannel.up",   "received BLOCK_SEND.");
						block_sending = true;
						System.Threading.Monitor.PulseAll(flow_control_mutex);
					}
					break;
				
				
				case Event.UNBLOCK_SEND: 
					lock (flow_control_mutex)
					{
						NCacheLog.Error("GroupChannel.up",   "received UNBLOCK_SEND.");
						block_sending = false;
						System.Threading.Monitor.PulseAll(flow_control_mutex);
					}
					break;
				
				
				default: 
					break;
				
			}
			
			
			// If UpHandler is installed, pass all events to it and return (UpHandler is e.g. a building block)
			if (up_handler != null)
			{
				up_handler.up(evt);
				return ;
			}
			
			if (type == Event.MSG || type == Event.VIEW_CHANGE || type == Event.SUSPECT /*|| type == Event.GET_APPLSTATE*/ || type == Event.BLOCK)
			{
				try
				{
					mq.add(evt);
				}
				catch (System.Exception e)
				{
					NCacheLog.Error("GroupChannel.up()",  e.ToString());					
				}
			}
		}
		
		
		/// <summary> Sends a message through the protocol stack if the stack is available</summary>
		/// <param name="evt">the message to send down, encapsulated in an event
		/// </param>
		public override void  down(Event evt)
		{
			if (evt == null)
				return ;
			
			// only block for messages; all other events are passed through
			if (block_sending && evt.Type == Event.MSG)
			{
				lock (flow_control_mutex)
				{
					while (block_sending)
						try
						{
							NCacheLog.Error("GroupChannel.down",   "down() blocks because block_sending == true");
							System.Threading.Monitor.Wait(flow_control_mutex);
						}
						catch (System.Exception e)
						{
							NCacheLog.Error("GroupChannel.down()",   "exception=" + e);
						}
				}
			}
			
			// handle setting of additional data (kludge, will be removed soon)
			if (evt.Type == Event.CONFIG)
			{
				try
				{
					System.Collections.IDictionary m = (System.Collections.IDictionary) evt.Arg;
					if (m != null && m.Contains("additional_data"))
					{
						additional_data = (byte[]) m["additional_data"];
					}
				}
				catch (System.Exception t)
				{
					NCacheLog.Error("GroupChannel.down()",   "CONFIG event did not contain a hashmap: " + t);					
				}
			}

			if (prot_stack != null)
				prot_stack.down(evt);
			else
				NCacheLog.Error("GroupChannel.down",   "no protocol stack available.");
		}
		
		
		public string ToString(bool details)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("local_addr=").Append(local_addr).Append('\n');
			sb.Append("channel_name=").Append(channel_name).Append('\n');
			sb.Append("my_view=").Append(my_view).Append('\n');
			sb.Append("connected=").Append(connected).Append('\n');
			sb.Append("closed=").Append(closed).Append('\n');
			if (mq != null)
				sb.Append("incoming queue size=").Append(mq.Count).Append('\n');
			if (details)
			{
				sb.Append("block_sending=").Append(block_sending).Append('\n');
				sb.Append("receive_views=").Append(receive_views).Append('\n');
				sb.Append("receive_suspects=").Append(receive_suspects).Append('\n');
				sb.Append("receive_blocks=").Append(receive_blocks).Append('\n');
				sb.Append("receive_local_msgs=").Append(receive_local_msgs).Append('\n');
				sb.Append("auto_reconnect=").Append(auto_reconnect).Append('\n');
				sb.Append("props=").Append(props).Append('\n');
			}
			
			return sb.ToString();
		}
		
		
		/* ----------------------------------- Private Methods ------------------------------------- */
		
		
		/// <summary> Initializes all variables. Used after <tt>close()</tt> or <tt>disconnect()</tt>,
		/// to be ready for new <tt>connect()</tt>
		/// </summary>
		private void  init()
		{
			local_addr = null;
			channel_name = null;
			my_view = null;
			connect_ok_event_received = false;
			disconnect_ok_event_received = false;
			connected = false;
			block_sending = false; // block send()/down() if true (unlocked by UNBLOCK_SEND event)
		}
		
		
		/// <summary> health check.<BR>
		/// throws a ChannelNotConnected exception if the channel is not connected
		/// </summary>
		void  checkNotConnected()
		{
			if (!connected)
				throw new ChannelNotConnectedException();
		}
		
		/// <summary> health check<BR>
		/// throws a ChannelClosed exception if the channel is closed
		/// </summary>
		void  checkClosed()
		{
			if (closed)
				throw new ChannelClosedException();
		}
		
		/// <summary> returns the value of the event<BR>
		/// These objects will be returned<BR>
		/// <PRE>
		/// <B>Event Type    - Return Type</B>
		/// Event.MSG           - returns a Message object
		/// Event.VIEW_CHANGE   - returns a View object
		/// Event.SUSPECT       - returns a SuspectEvent object
		/// Event.BLOCK         - returns a new BlockEvent object
		/// Event.GET_APPLSTATE - returns a GetStateEvent object
		/// Event.STATE_RECEIVED- returns a SetStateEvent object
		/// Event.Exit          - returns an ExitEvent object
		/// All other           - return the actual Event object
		/// </PRE>
		/// </summary>
		/// <param name="evt">- the event of which you want to extract the value
		/// </param>
		/// <returns> the event value if it matches the select list,
		/// returns null if the event is null
		/// returns the event itself if a match (See above) can not be made of the event type
		/// </returns>
		internal static object getEvent(Event evt)
		{
			if (evt == null)
				return null; // correct ?
			
			switch (evt.Type)
			{
				
				case Event.MSG: 
					return evt.Arg;
				
				case Event.VIEW_CHANGE: 
					return evt.Arg;
				
				case Event.SUSPECT: 
					return new SuspectEvent(evt.Arg);
				
				case Event.BLOCK: 
					return new BlockEvent();
				
				case Event.EXIT: 
					return new ExitEvent();
				
				default: 
					return evt;
				
			}
		}
		
		/// <summary> Disconnects and closes the channel.
		/// This method does the folloing things
		/// 1. Calls <code>this.disconnect</code> if the disconnect parameter is true
		/// 2. Calls <code>Queue.close</code> on mq if the close_mq parameter is true
		/// 3. Calls <code>ProtocolStack.stop</code> on the protocol stack
		/// 4. Calls <code>ProtocolStack.destroy</code> on the protocol stack
		/// 5. Sets the channel closed and channel connected flags to true and false
		/// 6. Notifies any channel listener of the channel close operation
		/// </summary>
		void  _close(bool disconect, bool close_mq)
		{
			if (closed)
				return ;
			
			if (disconect)
				disconnect(); // leave group if connected
			
			if (close_mq)
			{
				try
				{
					if (mq != null)
						mq.close(false); // closes and removes all messages
				}
				catch (System.Exception e)
				{
					NCacheLog.Error("GroupChannel._close()",   "exception: " + e.ToString());					
				}
			}
			
			if (prot_stack != null)
			{
				try
				{
					prot_stack.stopStack();
					prot_stack.destroy();
				}
				catch (System.Exception e)
				{
					NCacheLog.Error("GroupChannel._close():2",   "exception: " + e);
				}
			}
			closed = true;
			connected = false;
			if (channel_listener != null)
				channel_listener.channelClosed(this);
			init(); // sets local_addr=null; changed March 18 2003 (bela) -- prevented successful rejoining
		}
		
		
		/// <summary> Creates a separate thread to close the protocol stack.
		/// This is needed because the thread that called GroupChannel.up() with the EXIT event would
		/// hang waiting for up() to return, while up() actually tries to kill that very thread.
		/// This way, we return immediately and allow the thread to terminate.
		/// </summary>
		void  handleExit(Event evt)
		{
			if (channel_listener != null)
				channel_listener.channelShunned();
			
			if (closer != null && !closer.IsAlive)
				closer = null;
			if (closer == null)
			{
				NCacheLog.Error("GroupChannel.handleExit",   "received an EXIT event, will leave the channel");
				closer = new CloserThread(this, evt);
				closer.Start();
			}
		}

        public override void SetOperationModeOnMerge(OperationMode mode)
        {
            Stack.DisableOperationOnMerge = mode == OperationMode.OFFLINE;
        }

        /* ------------------------------- End of Private Methods ---------------------------------- */


        internal class CloserThread: ThreadClass
		{
			private void  InitBlock(GroupChannel enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private GroupChannel enclosingInstance;
			public GroupChannel Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal Event evt;
			internal ThreadClass t = null;
			
			
			internal CloserThread(GroupChannel enclosingInstance, Event evt)
			{
				InitBlock(enclosingInstance);
				this.evt = evt;
				Name = "CloserThread";
				IsBackground = true;
			}
			
			
			override public void  Run()
			{
				try
				{
					string old_channel_name = Enclosing_Instance.channel_name; // remember because close() will null it
					string old_subGroup_name = Enclosing_Instance.subGroup_name; // remember because for reconnect, it is required
                    this.enclosingInstance.NCacheLog.Error("CloserThread.Run", "GroupChannel: " + "closing the channel");
					Enclosing_Instance._close(false, false); // do not disconnect before closing channel, do not close mq (yet !)
					
					if (Enclosing_Instance.up_handler != null)
						Enclosing_Instance.up_handler.up(this.evt);
					else
					{
						try
						{
							Enclosing_Instance.mq.add(this.evt);
						}
						catch (System.Exception ex)
						{
                            this.enclosingInstance.NCacheLog.Error("CloserThread.Run()",  "exception: " + ex.ToString());							
						}
					}
					
					if (Enclosing_Instance.mq != null)
					{
						Util.Util.sleep(500); // give the mq thread a bit of time to deliver EXIT to the application
						try
						{
							Enclosing_Instance.mq.close(false);
						}
						catch (System.Exception e)
						{
                            this.enclosingInstance.NCacheLog.Error("CloserThread.Run()", "exception=" + e);
						}
					}
					
					if (Enclosing_Instance.auto_reconnect)
					{
						try
						{
                            this.enclosingInstance.NCacheLog.Error("GroupChannel",  "reconnecting to group " + old_channel_name);
							Enclosing_Instance.open();
						}
						catch (System.Exception ex)
						{
                            this.enclosingInstance.NCacheLog.Error("CloserThread.Run():2",  "failure reopening channel: " + ex.ToString());
							return ;
						}
						try
						{
							if (Enclosing_Instance.additional_data != null)
							{
								// set previously set additional data
								System.Collections.IDictionary m = new System.Collections.Hashtable(11);
								m["additional_data"] = Enclosing_Instance.additional_data;
								Enclosing_Instance.down(new Event(Event.CONFIG, m));
							}
							Enclosing_Instance.connect(old_channel_name, old_subGroup_name, Enclosing_Instance._isStartedAsMirror,false);
							if (Enclosing_Instance.channel_listener != null)
								Enclosing_Instance.channel_listener.channelReconnected(Enclosing_Instance.local_addr);
						}
						catch (System.Exception ex)
						{
                            this.enclosingInstance.NCacheLog.Error("CloserThread.Run():3",  "failure reconnecting to channel: " + ex.Message);
							return ;
						}
					}
				}
				catch (System.Exception ex)
				{
                    this.enclosingInstance.NCacheLog.Error("CloserThread.Run()",  ex.ToString());					
				}
				finally
				{
					Enclosing_Instance.closer = null;
				}
			}
		}
	}
}