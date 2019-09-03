// $Id: ProtocolStack.java,v 1.12 2004/07/05 14:17:33 belaban Exp $
using System;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NGroups.Stack
{
    /// <summary> A ProtocolStack manages a number of protocols layered above each other. It creates all
	/// protocol classes, initializes them and, when ready, starts all of them, beginning with the
	/// bottom most protocol. It also dispatches messages received from the stack to registered
	/// objects (e.g. channel, GMP) and sends messages sent by those objects down the stack.<p>
	/// The ProtocolStack makes use of the Configurator to setup and initialize stacks, and to
	/// destroy them again when not needed anymore
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	internal class ProtocolStack : Protocol, Transport
	{
		/// <summary>Returns all protocols in a list, from top to bottom. <em>These are not copies of protocols,
		/// so modifications will affect the actual instances !</em> 
		/// </summary>
		virtual public System.Collections.ArrayList Protocols
		{
			get
			{
				Protocol p;
				System.Collections.ArrayList v = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
				
				p = top_prot;
				while (p != null)
				{
					v.Add(p);
					p = p.DownProtocol;
				}
				return v;
			}
			
		}

		override public string Name { get{ return "ProtocolStack";} }
		private Protocol top_prot = null;
		private Protocol bottom_prot = null;
		private Configurator conf = new Configurator();
		private GroupChannel channel = null;
		public TimeScheduler timer = new TimeScheduler(30 * 1000);

		private string setup_string;

		private bool operational = false;
		private bool stopped = true;
		
		internal Promise ack_promise = new Promise();
		/// <summary>Used to sync on START/START_OK events for start()</summary>
		internal Promise start_promise;
		/// <summary>used to sync on STOP/STOP_OK events for stop() </summary>
		internal Promise stop_promise;
		
		public const int ABOVE = 1; // used by insertProtocol()
		public const int BELOW = 2; // used by insertProtocol()
        private ProtocolStackType stackType;

        public PerfStatsCollector perfStatsColl = new PerfStatsCollector("EMpty");
        private ILogger _ncacheLog;
        public ILogger NCacheLog
        {
            get
            {
                return _ncacheLog;
            }
            set
            {
                _ncacheLog = value;
            }
        }
		public ProtocolStack(GroupChannel channel, string setup_string)
		{
			this.setup_string = setup_string;
			this.channel = channel;
		}

        public void InitializePerfCounters(string instance)
        {
            perfStatsColl.InstanceName = instance;
            bool enableDebuggingCounters = false;

            enableDebuggingCounters = ServiceConfiguration.EnableDebuggingCounters;
            perfStatsColl.NCacheLog = _ncacheLog;
            perfStatsColl.InitializePerfCounters(enableDebuggingCounters);
        }

        public ProtocolStackType StackType
        {
            get { return stackType; }
            set { stackType = value; }
        }
       
		public bool IsOperational
		{
			get { return operational; }
			set { operational = value; }
		}

        public bool DisableOperationOnMerge { get; internal set; }

        /// <summary> Prints the names of the protocols, from the bottom to top. If include_properties is true,
        /// the properties for each protocol will also be printed.
        /// </summary>
        public virtual string printProtocolSpec(bool include_properties)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			Protocol prot = top_prot;
			string name;
			
			while (prot != null)
			{
				name = prot.Name;
				if (name != null)
				{
					if ("ProtocolStack".Equals(name))
						break;
					sb.Append(name);
					if (include_properties)
					{
						System.Collections.Hashtable props = prot.getProperties();
						System.Collections.DictionaryEntry entry;
						if (props != null)
						{
							sb.Append('\n');
							for (System.Collections.IEnumerator it = props.GetEnumerator(); it.MoveNext(); )
							{
								entry = (System.Collections.DictionaryEntry) it.Current;
								sb.Append(entry + "\n");
							}
						}
					}
					sb.Append('\n');
					
					prot = prot.DownProtocol;
				}
			}
			
			return sb.ToString();
		}
		
		
		public virtual void  setup()
		{
			if (top_prot == null)
			{
				top_prot = conf.setupProtocolStack(setup_string, this);
				if (top_prot == null)
					throw new System.Exception("ProtocolStack.setup(): couldn't create protocol stack");
				top_prot.UpProtocol = this;
				bottom_prot = conf.getBottommostProtocol(top_prot);
				conf.startProtocolStack(bottom_prot); // sets up queues and threads
			}
		}
		
		
		
		
		/// <summary> Creates a new protocol given the protocol specification.</summary>
		/// <param name="prot_spec">The specification of the protocol. Same convention as for specifying a protocol stack.
		/// An exception will be thrown if the class cannot be created. Example:
		/// <pre>"VERIFY_SUSPECT(timeout=1500)"</pre> Note that no colons (:) have to be
		/// specified
		/// </param>
		/// <returns> Protocol The newly created protocol
		/// </returns>
		/// <exception cref=""> Exception Will be thrown when the new protocol cannot be created
		/// </exception>
		public virtual Protocol createProtocol(string prot_spec)
		{
			return conf.createProtocol(prot_spec, this);
		}
		
		/// <summary> Inserts an already created (and initialized) protocol into the protocol list. Sets the links
		/// to the protocols above and below correctly and adjusts the linked list of protocols accordingly.
		/// Note that this method may change the value of top_prot or bottom_prot.
		/// </summary>
		/// <param name="prot">The protocol to be inserted. Before insertion, a sanity check will ensure that none
		/// of the existing protocols have the same name as the new protocol.
		/// </param>
		/// <param name="position">Where to place the protocol with respect to the neighbor_prot (ABOVE, BELOW)
		/// </param>
		/// <param name="neighbor_prot">The name of the neighbor protocol. An exception will be thrown if this name
		/// is not found
		/// </param>
		/// <exception cref=""> Exception Will be thrown when the new protocol cannot be created, or inserted.
		/// </exception>
		public virtual void  insertProtocol(Protocol prot, int position, string neighbor_prot)
		{
			conf.insertProtocol(prot, position, neighbor_prot, this);
		}
		
		/// <summary> Removes a protocol from the stack. Stops the protocol and readjusts the linked lists of
		/// protocols.
		/// </summary>
		/// <param name="prot_name">The name of the protocol. Since all protocol names in a stack have to be unique
		/// (otherwise the stack won't be created), the name refers to just 1 protocol.
		/// </param>
		/// <exception cref=""> Exception Thrown if the protocol cannot be stopped correctly.
		/// </exception>
		public virtual void  removeProtocol(string prot_name)
		{
			conf.removeProtocol(prot_name);
		}
		
		
		/// <summary>Returns a given protocol or null if not found </summary>
		public virtual Protocol findProtocol(string name)
		{
			Protocol tmp = top_prot;
			string prot_name;
			while (tmp != null)
			{
				prot_name = tmp.Name;
				if (prot_name != null && prot_name.Equals(name))
					return tmp;
				tmp = tmp.DownProtocol;
			}
			return null; 
		}
		
		public override void  destroy()
		{
			if (top_prot != null)
			{
				conf.stopProtocolStack(top_prot); // destroys msg queues and threads

                if (perfStatsColl != null) perfStatsColl.Dispose();

				top_prot = null;
			}
		}
		
		/// <summary> Start all layers. The {@link Protocol#start()} method is called in each protocol,
		/// <em>from top to bottom</em>.
		/// Each layer can perform some initialization, e.g. create a multicast socket
		/// </summary>
		public virtual void  startStack()
		{
			object start_result = null;
			if (stopped == false)
				return ;
			
			timer.Start();
			
			if (start_promise == null)
				start_promise = new Promise();
			else
				start_promise.Reset();
			
			down(new Event(Event.START));
			start_result = start_promise.WaitResult(0);
			if (start_result != null && start_result is System.Exception)
			{
				if (start_result is System.Exception)
					throw (System.Exception) start_result;
				else
				{
					throw new System.Exception("ProtocolStack.start(): exception is " + start_result);
				}
			}
			stopped = false;
		}
		
		public override void  startUpHandler()
		{
			// DON'T REMOVE !!!!  Avoids a superfluous thread
		}
		
		public override void  startDownHandler()
		{
			// DON'T REMOVE !!!!  Avoids a superfluous thread
		}
		
		
		/// <summary> Iterates through all the protocols <em>from top to bottom</em> and does the following:
		/// <ol>
		/// <li>Waits until all messages in the down queue have been flushed (ie., size is 0)
		/// <li>Calls stop() on the protocol
		/// </ol>
		/// </summary>
		public virtual void  stopStack()
		{
			if (timer != null)
			{
				try
				{
					timer.Dispose();
				}
				catch (System.Exception ex)
				{
					NCacheLog.Error("ProtocolStack.stopStack",  "exception=" + ex);
				}
			}
			
			if (stopped)
				return ;
			
			if (stop_promise == null)
				stop_promise = new Promise();
			else
				stop_promise.Reset();
			
			down(new Event(Event.STOP));
			stop_promise.WaitResult(5000);

			operational = false;
			stopped = true;
		}
		
		public override void  stopInternal()
		{
			// do nothing, DON'T REMOVE !!!!
		}
		
		
		/// <summary> Flushes all events currently in the <em>down</em> queues and returns when done. This guarantees
		/// that all events sent <em>before</em> this call will have been handled.
		/// </summary>
		public virtual void  flushEvents()
		{
			long start, stop;
			ack_promise.Reset();
			start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			down(new Event(Event.ACK));
			ack_promise.WaitResult(0);
			stop = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
		}
		
		
		
		/*--------------------------- Transport interface ------------------------------*/
		
		public virtual void  send(Message msg)
		{
			down(new Event(Event.MSG, msg));
		}
		
		public virtual Object receive(long timeout)
		{
			throw new System.Exception("ProtocolStack.receive(): not implemented !");
		}
		/*------------------------- End of  Transport interface ---------------------------*/
		
		
		
		
		public override void  up(Event evt)
		{
			switch (evt.Type)
			{
				
				case Event.ACK_OK: 
					ack_promise.SetResult((object) true);
					return ;
				
				case Event.START_OK: 
					if (start_promise != null)
						start_promise.SetResult(evt.Arg);
					return ;
				
				case Event.STOP_OK: 
					if (stop_promise != null)
						stop_promise.SetResult(evt.Arg);
					return ;
				}
			
			if (channel != null)
				channel.up(evt);
		}
		
		
		
		
		public override void  down(Event evt)
		{
			if (top_prot != null)
				top_prot.receiveDownEvent(evt);
			else
				NCacheLog.Error("ProtocolStack",  "no down protocol available !");
		}
		
		
		
		public override void  receiveUpEvent(Event evt)
		{
			up(evt);
		}
		
		
		
		/// <summary>Override with null functionality: we don't need any threads to be started ! </summary>
		public virtual void  startWork()
		{
		}
		
		/// <summary>Override with null functionality: we don't need any threads to be started ! </summary>
		public virtual void  stopWork()
		{
		}
		
		
		/*----------------------- End of Protocol functionality ---------------------------*/
	}
}