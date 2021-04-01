// $Id: RequestCorrelator.java,v 1.12 2004/09/05 04:54:21 ovidiuf Exp $
using System;
using System.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;

namespace Alachisoft.NGroups.Blocks
{
    /// <summary> Framework to send requests and receive matching responses (matching on
	/// request ID).
	/// Multiple requests can be sent at a time. Whenever a response is received,
	/// the correct <code>RspCollector</code> is looked up (key = id) and its
	/// method <code>receiveResponse()</code> invoked. A caller may use
	/// <code>done()</code> to signal that no more responses are expected, and that
	/// the corresponding entry may be removed.
	/// <p>
	/// <code>RequestCorrelator</code> can be installed at both client and server
	/// sides, it can also switch roles dynamically, i.e. send a request and at
	/// the same time process an incoming request (when local delivery is enabled,
	/// this is actually the default).
	/// <p>
	/// 
	/// </summary>
	/// <author>  Bela Ban
	/// </author>
	public class RequestCorrelator
	{
		/// <summary> Switch the deadlock detection mechanism on/off</summary>
		/// <param name="flag">the deadlock detection flag
		/// </param>
		virtual public bool DeadlockDetection
		{
			set
			{
				if (deadlock_detection != value)
				{
					// only set it if different
					deadlock_detection = value;
					if (started)
					{
						if (deadlock_detection)
						{
							startScheduler();
						}
						else
						{
							stopScheduler();
						}
					}
				}
			}
			
		}
		virtual public RequestHandler RequestHandler
		{
			set 
			{
				request_handler = value;
				start();
			}
		}

		virtual public bool ConcurrentProcessing
		{
			set { this.concurrent_processing = value; }
		}

		virtual public Address LocalAddress
		{
			get { return local_addr; }
			set { this.local_addr = value; }			
		}
		
		/// <summary>The protocol layer to use to pass up/down messages. Can be either a Protocol or a Transport </summary>
		protected internal object transport = null;
		
		/// <summary>The table of pending requests (keys=Long (request IDs), values=<tt>RequestEntry</tt>) </summary>
		protected internal System.Collections.Hashtable requests = new System.Collections.Hashtable();
		
		/// <summary>The handler for the incoming requests. It is called from inside the
		/// dispatcher thread 
		/// </summary>
		protected internal RequestHandler request_handler = null;
		
		/// <summary>makes the instance unique (together with IDs) </summary>
		protected internal string name = null;
		
		
		
		/// <summary>The address of this group member </summary>
		protected internal Address local_addr = null;
		
		/// <summary> This field is used only if deadlock detection is enabled.
		/// In case of nested synchronous requests, it holds a list of the
		/// addreses of the senders with the address at the bottom being the
		/// address of the first caller
		/// </summary>
		protected internal System.Collections.ArrayList call_stack = null;
		
		/// <summary>Whether or not to perform deadlock detection for synchronous (potentially recursive) group method invocations.
		/// If on, we use a scheduler (handling a priority queue), otherwise we don't and call handleRequest() directly.
		/// </summary>
		protected internal bool deadlock_detection = false;
		
		/// <summary>Process items on the queue concurrently (Scheduler). The default is to wait until the processing of an item
		/// has completed before fetching the next item from the queue. Note that setting this to true
		/// may destroy the properties of a protocol stack, e.g total or causal order may not be
		/// guaranteed. Set this to true only if you know what you're doing ! 
		/// </summary>
		protected internal bool concurrent_processing = false;

        private bool stopReplying;

		protected internal bool started = false;

        private ILogger _ncacheLog = null;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        private ArrayList members = new ArrayList(); //current members list;

        private long last_req_id = -1;

        private object req_mutex = new object();
        private Hashtable _reqStatusTable = new Hashtable();
        private Thread _statusCleanerThread = null;


        protected System.Threading.ReaderWriterLock req_lock = new System.Threading.ReaderWriterLock();
		/// <summary> Constructor. Uses transport to send messages. If <code>handler</code>
		/// is not null, all incoming requests will be dispatched to it (via
		/// <code>handle(Message)</code>).
		/// 
		/// </summary>
		/// <param name="name">Used to differentiate between different RequestCorrelators
		/// (e.g. in different protocol layers). Has to be unique if multiple
		/// request correlators are used.
		/// 
		/// </param>
		/// <param name="transport">Used to send/pass up requests. Can be either a Transport (only send() will be
		/// used then), or a Protocol (passUp()/passDown() will be used)
		/// 
		/// </param>
		/// <param name="handler">Request handler. Method <code>handle(Message)</code>
		/// will be called when a request is received.
		/// </param>
        public RequestCorrelator(string name, object transport, RequestHandler handler, ILogger NCacheLog)
		{
			this.name = name;
			this.transport = transport;
			request_handler = handler;
            this._ncacheLog = NCacheLog;
			start();
		}
		
		
		public RequestCorrelator(string name, object transport, RequestHandler handler, Address local_addr, ILogger NCacheLog)
		{
			this.name = name;
			this.transport = transport;
			this.local_addr = local_addr;
			request_handler = handler;
            this._ncacheLog = NCacheLog;
			start();
		}

        internal void StopReplying()
        {
            stopReplying = true;
        }

        internal void StartReplying()
        {
            stopReplying = false;
        }
		/// <summary> Constructor. Uses transport to send messages. If <code>handler</code>
		/// is not null, all incoming requests will be dispatched to it (via
		/// <code>handle(Message)</code>).
		/// 
		/// </summary>
		/// <param name="name">Used to differentiate between different RequestCorrelators
		/// (e.g. in different protocol layers). Has to be unique if multiple
		/// request correlators are used.
		/// 
		/// </param>
		/// <param name="transport">Used to send/pass up requests. Can be either a Transport (only send() will be
		/// used then), or a Protocol (passUp()/passDown() will be used)
		/// 
		/// </param>
		/// <param name="handler">Request handler. Method <code>handle(Message)</code>
		/// will be called when a request is received.
		/// 
		/// </param>
		/// <param name="deadlock_detection">When enabled (true) recursive synchronous
		/// message calls will be detected and processed with higher priority in
		/// order to solve deadlocks. Slows down processing a little bit when
		/// enabled due to runtime checks involved.
		/// </param>
		public RequestCorrelator(string name, object transport, RequestHandler handler, bool deadlock_detection, ILogger NCacheLog)
		{
			this.deadlock_detection = deadlock_detection;
			this.name = name;
			this.transport = transport;
			request_handler = handler;
            this._ncacheLog = NCacheLog;
			start();
		}
		
		
		public RequestCorrelator(string name, object transport, RequestHandler handler, bool deadlock_detection, bool concurrent_processing, ILogger NCacheLog)
		{
			this.deadlock_detection = deadlock_detection;
			this.name = name;
			this.transport = transport;
			request_handler = handler;
			this.concurrent_processing = concurrent_processing;

            this._ncacheLog = NCacheLog;

			start();
		}
		
		public RequestCorrelator(string name, object transport, RequestHandler handler, bool deadlock_detection, Address local_addr, ILogger NCacheLog)
		{
			this.deadlock_detection = deadlock_detection;
			this.name = name;
			this.transport = transport;
			this.local_addr = local_addr;
			request_handler = handler;
            this._ncacheLog = NCacheLog;
			start();
		}
		
		public RequestCorrelator(string name, object transport, RequestHandler handler, bool deadlock_detection, Address local_addr, bool concurrent_processing, ILogger NCacheLog)
		{
			this.deadlock_detection = deadlock_detection;
			this.name = name;
			this.transport = transport;
			this.local_addr = local_addr;
			request_handler = handler;
			this.concurrent_processing = concurrent_processing;

            this._ncacheLog = NCacheLog;

            start();
		}
		
		
		/// <summary> Helper method for {@link #sendRequest(long,List,Message,RspCollector)}.</summary>
		public virtual void  sendRequest(long id, Message msg, RspCollector coll)
		{
			sendRequest(id, null, msg, coll);
		}

        public virtual void sendRequest(long id, System.Collections.ArrayList dest_mbrs, Message msg, RspCollector coll)
        {
            sendRequest(id, dest_mbrs, msg, coll, HDR.REQ);
        }

		/// <summary> Send a request to a group. If no response collector is given, no
		/// responses are expected (making the call asynchronous).
		/// 
		/// </summary>
		/// <param name="id">The request ID. Must be unique for this JVM (e.g. current
		/// time in millisecs)
		/// </param>
		/// <param name="dest_mbrs">The list of members who should receive the call. Usually a group RPC
		/// is sent via multicast, but a receiver drops the request if its own address
		/// is not in this list. Will not be used if it is null.
		/// </param>
		/// <param name="msg">The request to be sent. The body of the message carries
		/// the request data
		/// 
		/// </param>
		/// <param name="coll">A response collector (usually the object that invokes
		/// this method). Its methods <code>ReceiveResponse</code> and
		/// <code>Suspect</code> will be invoked when a message has been received
		/// or a member is suspected, respectively.
		/// </param>
		public virtual void  sendRequest(long id, System.Collections.ArrayList dest_mbrs, Message msg, RspCollector coll, byte hdrType)
		{
			HDR hdr = null;
			
			if (transport == null)
			{
				NCacheLog.Warn("RequestCorrelator.sendRequest()", "transport is not available !");
				return ;
			}
			
			// i. Create the request correlator header and add it to the
			// msg
			// ii. If a reply is expected (sync call / 'coll != null'), add a
			// coresponding entry in the pending requests table
			// iii. If deadlock detection is enabled, set/update the call stack
			// iv. Pass the msg down to the protocol layer below

            hdr = msg.getHeader(HeaderType.REQUEST_COORELATOR) as RequestCorrelator.HDR;
            if (hdr == null)
            {
                hdr = new HDR();
                hdr.type = hdrType;
                hdr.id = id;
                hdr.rsp_expected = coll != null ? true : false;
                hdr.dest_mbrs = dest_mbrs;
            }

			if (coll != null)
			{
				if (deadlock_detection)
				{
					if (local_addr == null)
					{
						NCacheLog.Error("RequestCorrelator.sendRequest()", "local address is null !");
						return ;
					}
					System.Collections.ArrayList new_call_stack = (call_stack != null?(System.Collections.ArrayList) call_stack.Clone():new System.Collections.ArrayList());
					new_call_stack.Add(local_addr);
					hdr.call_stack = new_call_stack;
				}
                addEntry(hdr.id, new RequestEntry(coll), dest_mbrs);
			}
			msg.putHeader(HeaderType.REQUEST_COORELATOR, hdr);
			
			try
			{
                if (transport is Protocol)
                {
                    Event evt = new Event();
                    evt.Type = Event.MSG;
                    evt.Arg = msg;
                    ((Protocol)transport).passDown(evt);
                }
                else if (transport is Transport)
                    ((Transport)transport).send(msg);
                else
                    NCacheLog.Error("RequestCorrelator.sendRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
			}
			catch (System.Exception e)
			{
				NCacheLog.Error("RequestCorrelator.sendRequest()",e.ToString());				
			}
		}

        public virtual void sendNHopRequest(long id, System.Collections.ArrayList dest_mbrs, Message msg, RspCollector coll)
        {
            sendRequest(id, dest_mbrs, msg, coll, HDR.NHOP_REQ);
        }

		/// <summary> Used to signal that a certain request may be garbage collected as
		/// all responses have been received.
		/// </summary>
		public virtual void  done(long id)
		{
			removeEntry(id);
		}

        /// <summary>
        /// Checks wheter the given adress is memeber or not.
        /// </summary>
        /// <param name="member">address of the member</param>
        /// <returns>True if given address is member or not.</returns>
        public bool CheckForMembership(Address member)
        {
            req_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (members != null && members.Contains(member)) return true;
            }
            finally
            {
                req_lock.ReleaseReaderLock();
            }
            return false;
        }
		
		/// <summary> <b>Callback</b>.
		/// <p>
		/// Called by the protocol below when a message has been received. The
		/// algorithm should test whether the message is destined for us and,
		/// if not, pass it up to the next layer. Otherwise, it should remove
		/// the header and check whether the message is a request or response.
		/// In the first case, the message will be delivered to the request
		/// handler registered (calling its <code>handle()</code> method), in the
		/// second case, the corresponding response collector is looked up and
		/// the message delivered.
		/// </summary>
		public virtual bool  receive(Event evt)
		{
			switch (evt.Type)
			{
				case Event.SUSPECT:  // don't wait for responses from faulty members
					receiveSuspect((Address) evt.Arg);
					break;
				
				case Event.VIEW_CHANGE:  // adjust number of responses to wait for
					receiveView((View) evt.Arg);
					break;
				
				case Event.SET_LOCAL_ADDRESS: 
					LocalAddress = (Address) evt.Arg;
					break;
				
				case Event.MSG: 
					if (!receiveMessage((Message) evt.Arg))
						return true;
					break;

                case Event.RESET_SEQUENCE:
                    
                    receiveSequenceReset();
                    return true;
			}
			return false;
		}
		
		
		
		public virtual void  start()
		{
			if (deadlock_detection)
			{
				startScheduler();
			}
			started = true;

            if (_statusCleanerThread == null)
            {
                _statusCleanerThread = new Thread(new ThreadStart(RequstStatusClean));
                _statusCleanerThread.IsBackground = true;
                _statusCleanerThread.Start();
            }
		}
		
		
		protected virtual void  startScheduler()
        {
		}
		
		public virtual void  stop()
		{
			stopScheduler();
			started = false;

            if (_statusCleanerThread != null)
            {
                try
                {
                    NCacheLog.Flush();
#if !NETCORE
                    _statusCleanerThread.Abort();
#elif NETCORE
                    _statusCleanerThread.Interrupt();
#endif
                }
                catch (Exception) { }
            }
            _reqStatusTable.Clear();
            _reqStatusTable = null;
		}
		
		protected virtual void  stopScheduler()
		{
		}
		
		
		// .......................................................................

        public void receiveSequenceReset()
        {
            RequestEntry entry;
            System.Collections.ArrayList copy;

            // copy so we don't run into bug #761804 - Bela June 27 2003
            //lock (requests.SyncRoot)
            req_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                copy = new System.Collections.ArrayList(requests.Values);
            }
            finally
            {
                req_lock.ReleaseReaderLock();
            }
            for (System.Collections.IEnumerator it = copy.GetEnumerator(); it.MoveNext(); )
            {
                entry = (RequestEntry)it.Current;
                if (entry.coll != null && entry.coll is GroupRequest)
                    ((GroupRequest) entry.coll).SequenceReset();
            }
        }
		
		/// <summary> <tt>Event.SUSPECT</tt> event received from a layer below
		/// <p>
		/// All response collectors currently registered will
		/// be notified that <code>mbr</code> may have crashed, so they won't
		/// wait for its response.
		/// </summary>
		public virtual void  receiveSuspect(Address mbr)
		{
			RequestEntry entry;
			System.Collections.ArrayList copy;
			
			if (mbr == null)
				return ;
			
			NCacheLog.Debug("suspect=" + mbr);
					
			// copy so we don't run into bug #761804 - Bela June 27 2003
            req_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                copy = new System.Collections.ArrayList(requests.Values);
            }
            finally
            {
                req_lock.ReleaseReaderLock();
            }
			for (System.Collections.IEnumerator it = copy.GetEnumerator(); it.MoveNext(); )
			{
				entry = (RequestEntry) it.Current;
				if (entry.coll != null)
					entry.coll.suspect(mbr);
			}
		}

        private void MarkRequestArrived(long requestId, Address node)
        {
            Hashtable nodeStatusTable = null;
            RequestStatus status = new RequestStatus(requestId);
            status.MarkReceived();

            lock (_reqStatusTable.SyncRoot)
            {
                nodeStatusTable = _reqStatusTable[node] as Hashtable;
                if (nodeStatusTable == null)
                {
                    nodeStatusTable = new Hashtable();
                    _reqStatusTable.Add(node, nodeStatusTable);
                }
                nodeStatusTable[requestId] = status;
            }
        }
        private void MarkRequestProcessed(long requestId, Address node)
        {
            Hashtable nodeStatusTable = null;
            RequestStatus status = null;
            
            lock (_reqStatusTable.SyncRoot)
            {
                nodeStatusTable = _reqStatusTable[node] as Hashtable;
                if (nodeStatusTable != null && nodeStatusTable[requestId] != null)
                {
                    status = nodeStatusTable[requestId] as RequestStatus;
                    status.MarkProcessed();
                }
            }
        }

        private RequestStatus GetRequestStatus(long requestId, Address node)
        {
            Hashtable nodeStatusTable = null;

            lock (_reqStatusTable.SyncRoot)
            {
                nodeStatusTable = _reqStatusTable[node] as Hashtable;
                if (nodeStatusTable != null && nodeStatusTable[requestId] != null)
                {
                    return nodeStatusTable[requestId] as RequestStatus;
                }
            }
            return new RequestStatus(requestId);
        }

        private void RequstStatusClean()
        {
            Hashtable nodeReqTable = null;
            ArrayList expiredReqStatus = new ArrayList();
            RequestStatus reqStatus = null;
            

            while (_statusCleanerThread != null)
            {
                try
                {

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.RequestCleaner", "request cleaning is happening " + Thread.CurrentThread.ManagedThreadId);

                    expiredReqStatus.Clear();
                    ArrayList nodes = new ArrayList();

                    if (_reqStatusTable.Count > 0)
                    {
                        lock (_reqStatusTable.SyncRoot)
                        {
                            nodes.AddRange(_reqStatusTable.Keys);
                        }
                    }
                    foreach (Address node in nodes)
                    {
                        lock (_reqStatusTable.SyncRoot)
                        {

                            nodeReqTable = _reqStatusTable[node] as Hashtable;
                            if (nodeReqTable != null)
                            {
                                IDictionaryEnumerator ide = nodeReqTable.GetEnumerator();
                                while (ide.MoveNext())
                                {
                                    reqStatus = ide.Value as RequestStatus;
                                    if (reqStatus != null && reqStatus.HasExpired())
                                    {
                                        expiredReqStatus.Add(reqStatus.ReqId);
                                    }
                                }
                            }
                        }
                        if (nodeReqTable != null)
                        {
                            foreach (long reqId in expiredReqStatus)
                            {
                                lock (_reqStatusTable.SyncRoot)
                                {
                                    nodeReqTable.Remove(reqId);
                                }
                            }
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.RequestCleaner", "total requst_status :" + nodeReqTable.Count + " ;expired :" + expiredReqStatus.Count);
                        }
                    }
                    Thread.Sleep(15000);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if(NCacheLog.IsErrorEnabled) NCacheLog.Error("RequestCorrelator.RequestCleaner", "An error occurred while cleaning request_status. " + e.ToString());
                }
            }
        }
        /// <summary>
        /// Fetches the request status from the nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public Hashtable FetchRequestStatus(ArrayList nodes, ArrayList clusterMembership, long reqId)
        {
            Hashtable result = new Hashtable();
            if (nodes != null && nodes.Count > 0)
            {
                HDR hdr = new HDR(HDR.GET_REQ_STATUS, NextRequestId,true, null);
                hdr.status_reqId = reqId;
                Message msg = new Message();
                msg.putHeader(HeaderType.REQUEST_COORELATOR, hdr);
                msg.Dests = nodes;
                msg.IsSeqRequired = false;
                msg.IsUserMsg = true;
                msg.RequestId = reqId;
                msg.setBuffer(new byte[0]);

                GroupRequest req = new GroupRequest(msg, this, nodes, clusterMembership, GroupRequest.GET_ALL, 2000, 0, this._ncacheLog);
                req.execute();

                RspList rspList = req.Results;
                RequestStatus reqStatus = null;
                if (rspList != null)
                {
                    for (int i = 0; i < rspList.size(); i++)
                    {
                        Rsp rsp = rspList.elementAt(i) as Rsp;
                        if (rsp != null)
                        {
                            if (rsp.received)
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReqCorrelator.FetchReqStatus", reqId + " status response received from " + rsp.sender);
                                object rspValue = rsp.Value;
                                if (rspValue is byte[])
                                    reqStatus = CompactBinaryFormatter.Deserialize(new MemoryStream((byte[])rspValue), null) as RequestStatus;
                                else
                                    reqStatus = rsp.Value as RequestStatus;

                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReqCorrelator.FetchReqStatus", reqId + " status response: " + reqStatus);

                                result[rsp.Sender] = reqStatus;
                            }
                            else
                            {
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReqCorrelator.FetchReqStatus", reqId + " status response NOT received from " + rsp.sender);
                                result[rsp.Sender] = new RequestStatus(reqId,RequestStatus.NONE);
                            }
                        }
                    }
                }

            }
            return result;
        }
		/// <summary> <tt>Event.VIEW_CHANGE</tt> event received from a layer below
		/// <p>
		/// Mark all responses from members that are not in new_view as
		/// NOT_RECEIVED.
		/// 
		/// </summary>
		public virtual void  receiveView(View new_view)
		{
			RequestEntry entry;
			System.Collections.ArrayList copy;
			ArrayList oldMembers = new ArrayList();
			// copy so we don't run into bug #761804 - Bela June 27 2003
            req_lock.AcquireReaderLock(Timeout.Infinite);
            try
			{
                if (new_view != null)
                {
                    if(members != null)
                    {
                        foreach(Address member in members)
                        {
                            if(!new_view.Members.Contains(member))
                                oldMembers.Add(member);
                        }
                    }
                    members = new_view.Members.Clone() as ArrayList;
                }
				copy = new System.Collections.ArrayList(requests.Values);
			}
            finally
            {
                req_lock.ReleaseReaderLock();
            }
			for (System.Collections.IEnumerator it = copy.GetEnumerator(); it.MoveNext(); )
			{
				entry = (RequestEntry) it.Current;
				if (entry.coll != null)
					entry.coll.viewChange(new_view);
			}
            //Remove request status information for all requests from the old members.
            lock (_reqStatusTable.SyncRoot)
            {
                if (members != null)
                {
                    foreach (Address oldmember in oldMembers)
                    {
                        _reqStatusTable.Remove(oldmember);
                    }
                }
            }
		}
		
		
		/// <summary> 
		/// Handles a message coming from a layer below
		/// </summary>
		/// <returns> true if the event should be forwarded further up, otherwise false 
		/// (message was consumed)
		/// </returns>
		public virtual bool receiveMessage(Message msg)
		{
			object tmpHdr;
			HDR hdr;
			RspCollector coll;
			System.Collections.IList dests;
			
			// i. If header is not an instance of request correlator header, ignore
			//
			// ii. Check whether the message was sent by a request correlator with
			// the same name (there may be multiple request correlators in the same
			// protocol stack...)
			tmpHdr = msg.getHeader(HeaderType.REQUEST_COORELATOR);
			if (!(tmpHdr is HDR))
				return (true);
			
			hdr = (HDR) tmpHdr;
			
			// If the header contains a destination list, and we are not part of it, then we discard the
			// request (was addressed to other members)
			dests = hdr.dest_mbrs;
                        
            if (dests != null && local_addr != null && !dests.Contains(local_addr))
            {
                NCacheLog.Debug("RequestCorrelator.receiveMessage()", "discarded request from " + msg.Src + " as we are not part of destination list (local_addr=" + local_addr + ", hdr=" + hdr + ')');
                return false;
            }
            if (!hdr.doProcess)
            {
                NCacheLog.Debug("RequestCorrelator.receiveMessage()", hdr.id + " I should not process");
                return false;
            }
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.receiveMessage()", "header is " + hdr);
						
			// [HDR.REQ]:
			// i. If there is no request handler, discard
			// ii. Check whether priority: if synchronous and call stack contains
			// address that equals local address -> add priority request. Else
			// add normal request.
			//
			// [HDR.RSP]:
			// Remove the msg request correlator header and notify the associated
			// <tt>RspCollector</tt> that a reply has been received
			switch (hdr.type)
			{
                case HDR.GET_REQ_STATUS:
				case HDR.REQ: 
					if (request_handler == null)
						return (false);

                    //In case of NHop requests, the response is not sent to the sender of the request. Instead, 
                    //response is sent back to a node whose address is informed by the sender.

					handleRequest(msg,hdr.whomToReply);
                    break;

                case HDR.NHOP_REQ:        
                    handleNHopRequest(msg);
                    break;

				case HDR.RSP:
                    msg.removeHeader(HeaderType.REQUEST_COORELATOR);
					coll = findEntry(hdr.id);
					if (coll != null)
					{

						coll.receiveResponse(msg);

					}
					break;

                case HDR.NHOP_RSP:
                    msg.removeHeader(HeaderType.REQUEST_COORELATOR);
                    coll = findEntry(hdr.id);

                    if (coll != null)
                    {
                        if (hdr.expectResponseFrom != null)
                        {
                            if (coll is GroupRequest)
                            {
                                GroupRequest groupRequest = coll as GroupRequest;
                                groupRequest.AddNHop(hdr.expectResponseFrom);
                                groupRequest.AddNHopDefaultStatus(hdr.expectResponseFrom);
                            }
                        }
                        coll.receiveResponse(msg);
                    }
                    break;
				
				default:
                    msg.removeHeader(HeaderType.REQUEST_COORELATOR);
					NCacheLog.Error("RequestCorrelator.receiveMessage()", "header's type is neither REQ nor RSP !");
					break;
				
			}
			
            return (false);
		}

        /// <summary>Generates a new unique request ID </summary>
        public long NextRequestId
        {
            get
            {
                lock (req_mutex)
                {
                    // Request id ranges from 0 to long.Max. If it reaches the max we
                    //re-initialize it to -1;
                    if (last_req_id == long.MaxValue) last_req_id = -1;
                    long result = ++last_req_id;
                    return result;
                }
            }
        }
		// .......................................................................
		
		/// <summary> Add an association of:<br>
		/// ID -> <tt>RspCollector</tt>
		/// </summary>
		private void  addEntry(long id, RequestEntry entry,ArrayList dests)
		{
			System.Int64 id_obj = (long) id;
            req_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                //we check whether all the destination are still alive or not
                //if view has changed and one or more destination members has left
                //then we should declare them suspect.
                if (dests != null)
                {
                    foreach (Address dest in dests)
                    {
                        if (!members.Contains(dest) && entry.coll != null)
                            entry.coll.suspect(dest);
                    }
                }
                if (!requests.ContainsKey(id_obj))
                    requests[id_obj] = entry;
                
            }
            finally
            {
                req_lock.ReleaseWriterLock();
            }
		}
		
		
		/// <summary> Remove the request entry associated with the given ID
		/// 
		/// </summary>
		/// <param name="id">the id of the <tt>RequestEntry</tt> to remove
		/// </param>
		private void  removeEntry(long id)
		{
			System.Int64 id_obj = (long) id;
			
			// changed by bela Feb 28 2003 (bug fix for 690606)
			// changed back to use synchronization by bela June 27 2003 (bug fix for #761804),
			// we can do this because we now copy for iteration (viewChange() and suspect())
			//lock (requests.SyncRoot)
            req_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                requests.Remove(id_obj);
            }
            finally
            {
                req_lock.ReleaseWriterLock();
            }
		}
		
		
		/// <param name="id">the ID of the corresponding <tt>RspCollector</tt>
		/// 
		/// </param>
		/// <returns> the <tt>RspCollector</tt> associated with the given ID
		/// </returns>
		private RspCollector findEntry(long id)
		{
			System.Int64 id_obj = (long) id;
			RequestEntry entry;
			
            req_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                entry = (RequestEntry)requests[id_obj];
            }
            finally
            {
                req_lock.ReleaseReaderLock();
            }
            return ((entry != null) ? entry.coll : null);
		}

        /// <summary> Handle a request msg for this correlator
        /// 
        /// </summary>
        /// <param name="req">the request msg
        /// </param>
        private void handleNHopRequest(Message req)
        {
            object retval = null;
            byte[] rsp_buf = null;
            IList rsp_buffers = null;
            HDR hdr, rsp_hdr, replicaMsg_hdr;
            Message rsp;

            Address destination = null;
            Message replicationMsg = null;

            // i. Remove the request correlator header from the msg and pass it to
            // the registered handler
            //
            // ii. If a reply is expected, pack the return value from the request
            // handler to a reply msg and send it back. The reply msg has the same
            // ID as the request and the name of the sender request correlator
            hdr = (HDR)req.removeHeader(HeaderType.REQUEST_COORELATOR);

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleNHopRequest()", "calling (" + (request_handler != null ? request_handler.GetType().FullName : "null") + ") with request " + hdr.id);

            try
            {
                if (hdr.rsp_expected)
                    req.RequestId = hdr.id;
                else
                    req.RequestId = -1;

                if (req.HandledAysnc)
                {
                    request_handler.handle(req);
                    return;
                }
                if (hdr.type == HDR.NHOP_REQ)
                {
                    MarkRequestArrived(hdr.id, req.Src);
                    retval = request_handler.handleNHopRequest(req, out destination, out replicationMsg);
                }
            }
            catch (System.Exception t)
            {
                NCacheLog.Error("RequestCorrelator.handleNHopRequest()", "error invoking method, exception=" + t.ToString());
                retval = t;
            }

            if (!hdr.rsp_expected || stopReplying)
                return;

            if (transport == null)
            {
                NCacheLog.Error("RequestCorrelator.handleNHopRequest()", "failure sending " + "response; no transport available");
                return;
            }

            //1. send request to other replica.
            //   this node will send the response to original node.
            if (replicationMsg != null)
            {
                replicaMsg_hdr = new HDR();
                replicaMsg_hdr.type = HDR.REQ;
                replicaMsg_hdr.id = hdr.id;
                replicaMsg_hdr.rsp_expected = true;
                replicaMsg_hdr.whomToReply = req.Src;

                replicationMsg.Dest = destination;
                replicationMsg.putHeader(HeaderType.REQUEST_COORELATOR, replicaMsg_hdr);

                try
                {
                    if (transport is Protocol)
                    {
                        Event evt = new Event();
                        evt.Type = Event.MSG;
                        evt.Arg = replicationMsg;
                        ((Protocol)transport).passDown(evt);
                    }
                    else if (transport is Transport)
                        ((Transport)transport).send(replicationMsg);
                    else
                        NCacheLog.Error("RequestCorrelator.handleRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
                }
                catch (System.Exception e)
                {
                    NCacheLog.Error("RequestCorrelator.handleRequest()", e.ToString());
                }
            }

            //2. send reply back to original node
            //   and inform the original node that it must expect another response 
            //   from the replica node. (the response of the request sent in part 1)
            rsp = req.makeReply();

            try
            {
                if (retval is OperationResponse)
                {
                    
                    if(((OperationResponse)retval).SerializablePayload is byte[])
                        rsp_buf = (byte[])((OperationResponse)retval).SerializablePayload;
                    else if (((OperationResponse)retval).SerializablePayload is IList)
                        rsp_buffers = (IList)((OperationResponse)retval).SerializablePayload;

                    rsp.Payload = ((OperationResponse)retval).UserPayload;
                    rsp.responseExpected = true;
                }

                else if (retval is Byte[])
                    rsp_buf = (byte[])retval;
                else if (retval is IList)
                    rsp_buffers = (IList)retval;

                else
                    rsp_buf = CompactBinaryFormatter.ToByteBuffer(retval, null); // retval could be an exception, or a real value
            }
            catch (System.Exception t)
            {
                NCacheLog.Error("RequestCorrelator.handleRequest()", t.ToString());
                try
                {
                    rsp_buf = CompactBinaryFormatter.ToByteBuffer(t, null); // this call shoudl succeed (all exceptions are serializable)
                }
                catch (System.Exception)
                {
                    NCacheLog.Error("RequestCorrelator.handleRequest()", "failed sending response: " + "return value (" + retval + ") is not serializable");
                    return;
                }
            }

            if (rsp_buf != null)
                rsp.setBuffer(rsp_buf);
            if (rsp_buffers != null)
                rsp.Buffers = rsp_buffers;

            rsp_hdr = new HDR();
            rsp_hdr.type = HDR.NHOP_RSP;
            rsp_hdr.id = hdr.id;
            rsp_hdr.rsp_expected = false;

            if (replicationMsg != null)
            {
                rsp_hdr.expectResponseFrom = destination;
            }

            rsp.putHeader(HeaderType.REQUEST_COORELATOR, rsp_hdr);

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleRequest()", "sending rsp for " + rsp_hdr.id + " to " + rsp.Dest);

            try
            {
                if (transport is Protocol)
                {
                    Event evt = new Event();
                    evt.Type = Event.MSG;
                    evt.Arg = rsp;
                    ((Protocol)transport).passDown(evt);
                }
                else if (transport is Transport)
                    ((Transport)transport).send(rsp);
                else
                    NCacheLog.Error("RequestCorrelator.handleRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
            }
            catch (System.Exception e)
            {
                NCacheLog.Error("RequestCorrelator.handleRequest()", e.ToString());
            }

            MarkRequestProcessed(hdr.id, req.Src);
        }
		
		/// <summary> Handle a request msg for this correlator
		/// 
		/// </summary>
		/// <param name="req">the request msg
		/// </param>
		private void  handleRequest(Message req,Address replyTo)
		{
			object retval;
			byte[] rsp_buf = null;
            IList rsp_buffers = null;
			HDR hdr, rsp_hdr;
			Message rsp;
			
			// i. Remove the request correlator header from the msg and pass it to
			// the registered handler
			//
			// ii. If a reply is expected, pack the return value from the request
			// handler to a reply msg and send it back. The reply msg has the same
			// ID as the request and the name of the sender request correlator
			hdr = (HDR) req.removeHeader(HeaderType.REQUEST_COORELATOR);
			
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleRequest()", "calling (" + (request_handler != null?request_handler.GetType().FullName:"null") + ") with request " + hdr.id);

            TimeStats appTimeStats = null;
            bool isProfilable = false;
			try
			{

                if (hdr.rsp_expected)
                    req.RequestId = hdr.id;
                else
                    req.RequestId = -1;

                if (req.HandledAysnc)
                {
                    request_handler.handle(req);
                    return;
                }
                if (hdr.type == HDR.GET_REQ_STATUS)
                {
                    if(NCacheLog.IsInfoEnabled) NCacheLog.Info("ReqCorrelator.handleRequet", hdr.status_reqId + " receive RequestStatus request from " + req.Src);
                    retval = GetRequestStatus(hdr.status_reqId, req.Src);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ReqCorrelator.handleRequet", hdr.status_reqId + " RequestStatus :" + retval);
                }
                else
                {
                    MarkRequestArrived(hdr.id, req.Src);
                    retval = request_handler.handle(req);
                }

                //request is being handled asynchronously, so response will be send by
                //the the user itself.
                

			}
			catch (System.Exception t)
			{
				NCacheLog.Error("RequestCorrelator.handleRequest()", "error invoking method, exception=" + t.ToString());
				retval = t;
			}

            if (!hdr.rsp_expected || stopReplying)
                // asynchronous call, we don't need to send a response; terminate call here
                return;
			
			if (transport == null)
			{
				NCacheLog.Error("RequestCorrelator.handleRequest()", "failure sending " + "response; no transport available");
				return ;
			}

            rsp = req.makeReply();
            if (replyTo != null) rsp.Dest = replyTo;
			// changed (bela Feb 20 2004): catch exception and return exception
			try
			{
                if (retval is OperationResponse)
                {
                    if (((OperationResponse)retval).SerilizationStream != null)
                    {
                        rsp.SerlizationStream = ((OperationResponse)retval).SerilizationStream;
                    }
                    else
                    {
                        if(((OperationResponse)retval).SerializablePayload is byte[])
                            rsp_buf = (byte[])((OperationResponse)retval).SerializablePayload;
                        else if(((OperationResponse)retval).SerializablePayload is IList)
                            rsp_buffers = (IList)((OperationResponse)retval).SerializablePayload;
                    }
                    
                    rsp.Payload = ((OperationResponse)retval).UserPayload;
                    rsp.responseExpected = true;
                }
                else if(retval is Byte[])
					rsp_buf = (byte[])retval;
                else if (retval is IList)
                    rsp_buffers = (IList)retval;
				else
					rsp_buf = CompactBinaryFormatter.ToByteBuffer(retval,null); // retval could be an exception, or a real value
			}
			catch (System.Exception t)
			{
				NCacheLog.Error("RequestCorrelator.handleRequest()", t.ToString());
				try
				{
					rsp_buf = CompactBinaryFormatter.ToByteBuffer(t,null); // this call shoudl succeed (all exceptions are serializable)
				}
				catch (System.Exception)
				{
					NCacheLog.Error("RequestCorrelator.handleRequest()", "failed sending response: " + "return value (" + retval + ") is not serializable");
					return ;
				}
			}

            if (rsp_buf != null)
				rsp.setBuffer(rsp_buf);

            if (rsp_buffers != null)
                rsp.Buffers = rsp_buffers;

            if (rsp.Dest.Equals(local_addr))
            {
                //we need not to put our response on the stack.
                rsp.Src = local_addr;
                ReceiveLocalResponse(rsp,hdr.id);
                return;
            }
            rsp_hdr = new HDR();
            rsp_hdr.type = HDR.RSP;
            rsp_hdr.id = hdr.id;
            rsp_hdr.rsp_expected = false;

			rsp.putHeader(HeaderType.REQUEST_COORELATOR, rsp_hdr);
			
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleRequest()", "sending rsp for " + rsp_hdr.id + " to " + rsp.Dest);
						
			try
			{
               
                if (transport is Protocol)
                {
                    Event evt = new Event();
                    evt.Type = Event.MSG;
                    evt.Arg = rsp;
                    ((Protocol)transport).passDown(evt);
                }
                else if (transport is Transport)
                    ((Transport)transport).send(rsp);
                else
                    NCacheLog.Error("RequestCorrelator.handleRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
			}
			catch (System.Exception e)
			{
				NCacheLog.Error("RequestCorrelator.handleRequest()", e.ToString());
			}
            MarkRequestProcessed(hdr.id, req.Src);
		}

        private void handleStatusRequest(Message req)
        {
            object retval;
            byte[] rsp_buf = null;
            HDR hdr, rsp_hdr;
            Message rsp;

            // i. Remove the request correlator header from the msg and pass it to
            // the registered handler
            //
            // ii. If a reply is expected, pack the return value from the request
            // handler to a reply msg and send it back. The reply msg has the same
            // ID as the request and the name of the sender request correlator
            hdr = (HDR)req.removeHeader(HeaderType.REQUEST_COORELATOR);

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleStatusRequest()", "calling (" + (request_handler != null ? request_handler.GetType().FullName : "null") + ") with request " + hdr.id);


            if (transport == null)
            {
                NCacheLog.Error("RequestCorrelator.handleStatusRequest()", "failure sending " + "response; no transport available");
                return;
            }
            RequestStatus status = GetRequestStatus(hdr.id, req.Src);

            rsp_hdr = new HDR();
            rsp_hdr.type = HDR.GET_REQ_STATUS_RSP;
            rsp_hdr.id = hdr.id;
            rsp_hdr.rsp_expected = false;
            rsp_hdr.reqStatus = status;

            rsp = req.makeReply();
            rsp.putHeader(HeaderType.REQUEST_COORELATOR, rsp_hdr);

            if (rsp.Dest.Equals(local_addr))
            {
                //we need not to put our response on the stack.
                rsp.Src = local_addr;
                ReceiveLocalResponse(rsp, hdr.id);
                return;
            }

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("RequestCorrelator.handleStatusRequest()", "sending rsp for " + rsp_hdr.id + " to " + rsp.Dest);

            try
            {

                if (transport is Protocol)
                {
                    Event evt = new Event();
                    evt.Type = Event.MSG;
                    evt.Arg = rsp;
                    ((Protocol)transport).passDown(evt);
                }
                else if (transport is Transport)
                    ((Transport)transport).send(rsp);
                else
                    NCacheLog.Error("RequestCorrelator.handleStatusRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
            }
            catch (System.Exception e)
            {
                NCacheLog.Error("RequestCorrelator.handleStatusRequest()", e.ToString());
            }
        }

        public void AsyncProcessRequest(object req)
        {
            request_handler.handle((Message)req);
        }
        public void SendResponse(long resp_id, Message response)
        {
            if (response.Dest.Equals(local_addr))
            {
                //we need not to put our response on the stack.
                response.Src = local_addr;
                ReceiveLocalResponse(response, resp_id);
                return;
            }

            HDR rsp_hdr = new HDR();
            rsp_hdr.type = HDR.RSP;
            rsp_hdr.id = resp_id;
            rsp_hdr.rsp_expected = false;

            response.putHeader(HeaderType.REQUEST_COORELATOR, rsp_hdr);

            try
            {

                if (transport is Protocol)
                {
                    Event evt = new Event();
                    evt.Type = Event.MSG;
                    evt.Arg = response;
                    ((Protocol)transport).passDown(evt);
                }
                else if (transport is Transport)
                    ((Transport)transport).send(response);
                else
                    NCacheLog.Error("RequestCorrelator.handleRequest()", "transport object has to be either a " + "Transport or a Protocol, however it is a " + transport.GetType());
            }
            catch (System.Exception e)
            {
                NCacheLog.Error("RequestCorrelator.handleRequest()", e.ToString());
            }
        }
		// .......................................................................
		
		/// <summary> Associates an ID with an <tt>RspCollector</tt></summary>
		private class RequestEntry
		{
			public RspCollector coll = null;
			
			public RequestEntry(RspCollector coll)
			{
				this.coll = coll;
			}
		}

        private void ReceiveLocalResponse(Message rsp, long req_id)
        {
            RspCollector coll = findEntry(req_id);
            if (coll != null)
            {

                coll.receiveResponse(rsp);

            }
        }
		
		
		/// <summary> The header for <tt>RequestCorrelator</tt> messages</summary>
		[Serializable]
		internal class HDR: Header, ICompactSerializable , IRentableObject
		{
			public const byte REQ = 0;
			public const byte RSP = 1;
            public const byte GET_REQ_STATUS = 3;
            public const byte GET_REQ_STATUS_RSP = 4;
            public const byte NHOP_REQ = 5;
            public const byte NHOP_RSP = 6;

            public int rentid;
			/// <summary>Type of header: request or reply </summary>
			public byte type = REQ;

			/// <summary> The id of this request to distinguish among other requests from
			/// the same <tt>RequestCorrelator</tt>
			/// </summary>
			public long id = 0;
			
			/// <summary>msg is synchronous if true </summary>
			public bool rsp_expected = true;
			
			/// <summary>The unique name of the associated <tt>RequestCorrelator</tt> </summary>
			//public string name = null;
			
			/// <summary>Contains senders (e.g. P --> Q --> R) </summary>
			public System.Collections.ArrayList call_stack = null;
			
			/// <summary>Contains a list of members who should receive the request (others will drop). Ignored if null </summary>
            public System.Collections.ArrayList dest_mbrs = null;

            public bool serializeFlag = true;
            public RequestStatus reqStatus;
            public long status_reqId;
            public Address whomToReply;
            public Address expectResponseFrom;

            public bool doProcess = true;
			/// <summary> Used for externalization</summary>
			public HDR()
			{
			}
			
			/// <param name="type">type of header (<tt>REQ</tt>/<tt>RSP</tt>)
			/// </param>
			/// <param name="id">id of this header relative to ids of other requests
			/// originating from the same correlator
			/// </param>
			/// <param name="rsp_expected">whether it's a sync or async request
			/// </param>
			/// <param name="name">the name of the <tt>RequestCorrelator</tt> from which
			/// this header originates
			/// </param>
			public HDR(byte type, long id, bool rsp_expected, string name)
			{
				this.type = type;
				this.id = id;
				this.rsp_expected = rsp_expected;
				//this.name = name;
			}

            /// <param name="type">type of header (<tt>REQ</tt>/<tt>RSP</tt>)
            /// </param>
            /// <param name="id">id of this header relative to ids of other requests
            /// originating from the same correlator
            /// </param>
            /// <param name="rsp_expected">whether it's a sync or async request
            /// </param>
            /// <param name="name">the name of the <tt>RequestCorrelator</tt> from which
            /// this header originates
            /// <param name="apptimeTaken">Time taken to complete an operation by the receiving application.</param>
            /// </param>
            public HDR(byte type, long id, bool rsp_expected, string name,long apptimeTaken)
            {
                this.type = type;
                this.id = id;
                this.rsp_expected = rsp_expected;
               // this.name = name;

            }
			public override string ToString()
			{
				System.Text.StringBuilder ret = new System.Text.StringBuilder();
				//ret.Append("[HDR: name=" + name + ", type=");
                string typeStr = "<unknown>";
                switch (type)
                {
                    case REQ:
                        typeStr = "REQ";
                        break;

                    case RSP:
                        typeStr = "RSP";
                        break;

                    case GET_REQ_STATUS:
                        typeStr = "GET_REQ_STATUS";
                        break;

                    case GET_REQ_STATUS_RSP:
                        typeStr = "GET_REQ_STATUS_RSP";
                        break;


                }
				ret.Append(typeStr);
				ret.Append(", id=" + id);
				ret.Append(", rsp_expected=" + rsp_expected + ']');
				if (dest_mbrs != null)
					ret.Append(", dest_mbrs=").Append(dest_mbrs);
				return ret.ToString();
			}
            public void DeserializeLocal(BinaryReader reader)
            {
                type = reader.ReadByte();
                id = reader.ReadInt64();
                rsp_expected = reader.ReadBoolean();
                doProcess = reader.ReadBoolean();

                bool getWhomToReply = reader.ReadBoolean();

                if (getWhomToReply)
                {
                    this.whomToReply = new Address();
                    this.whomToReply.DeserializeLocal(reader);
                }

                bool getExpectResponseFrom = reader.ReadBoolean();
                if (getExpectResponseFrom)
                {
                    this.expectResponseFrom = new Address();
                    this.expectResponseFrom.DeserializeLocal(reader);
                }
            }

            public void SerializeLocal(BinaryWriter writer)
            {
                writer.Write(type);
                writer.Write(id);
                writer.Write(rsp_expected);
                writer.Write(doProcess);

                if (whomToReply != null)
                {
                    writer.Write(true);
                    whomToReply.SerializeLocal(writer);
                }
                else
                    writer.Write(false);

                if (expectResponseFrom != null)
                {
                    writer.Write(true);
                    expectResponseFrom.SerializeLocal(writer);
                }
                else
                    writer.Write(false);
            }
			
			#region ICompactSerializable Members

            public void Deserialize(CompactReader reader)
			{
				type = reader.ReadByte();
				id = reader.ReadInt64();
				rsp_expected = reader.ReadBoolean();
                reqStatus = reader.ReadObject() as RequestStatus;
                status_reqId = reader.ReadInt64();
				//name = reader.ReadString();
				//call_stack = (System.Collections.ArrayList)reader.ReadObject();
                //byte[] arr = (byte[])reader.ReadObject();
                //dest_mbrs = arr != null ?(System.Collections.IList)CompactBinaryFormatter.FromByteBuffer(arr, null): null;
				//dest_mbrs = (System.Collections.IList)reader.ReadObject();
                dest_mbrs = (System.Collections.ArrayList)reader.ReadObject();
                doProcess = reader.ReadBoolean();
                whomToReply = (Address)reader.ReadObject();
                expectResponseFrom = (Address)reader.ReadObject();

			}

			public void Serialize(CompactWriter writer)
			{
				writer.Write(type);
				writer.Write(id);
				writer.Write(rsp_expected);
                writer.WriteObject(reqStatus);
                writer.Write(status_reqId);
                if (serializeFlag)
                    writer.WriteObject(dest_mbrs);
                else
                    writer.WriteObject(null);

                writer.Write(doProcess);
                writer.WriteObject(whomToReply);
                writer.WriteObject(expectResponseFrom);

			}

            public static HDR ReadCorHeader(CompactReader reader)
            {
                byte isNull = reader.ReadByte();
                if (isNull == 1)
                    return null;
                HDR newHdr = new HDR();
                newHdr.Deserialize(reader);
                return newHdr;
            }

            public static void WriteCorHeader(CompactWriter writer, HDR hdr)
            {
                byte isNull = 1;
                if (hdr == null)
                    writer.Write(isNull);
                else
                {
                    isNull = 0;
                    writer.Write(isNull);
                    hdr.Serialize(writer);
                }
                return;
            }  	


            public void Reset()
            {
                 dest_mbrs =  call_stack = null;
                 doProcess =  rsp_expected = true;

                 type = RequestCorrelator.HDR.REQ;
            }
			#endregion

            #region IRentableObject Members

            public int RentId
            {
                get
                {
                    return rentid;
                }
                set
                {
                    rentid = value;
                }
            }

            #endregion
        }
		
		
		
		
		
		/// <summary> The runnable for an incoming request which is submitted to the
		/// dispatcher
		/// </summary>
		private class Request : IThreadRunnable
		{
			private RequestCorrelator enclosingInstance;
			public RequestCorrelator Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public Message req;
			
			public Request(RequestCorrelator enclosingInstance, Message req)
			{
				this.enclosingInstance = enclosingInstance;
				this.req = req;
			}

			public virtual void  Run()
			{
				Enclosing_Instance.handleRequest(req,null);
			}
			
			public override string ToString()
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				if (req != null)
				{
					sb.Append("req=" + req + ", headers=" + Global.CollectionToString(req.Headers));
				}
				return sb.ToString();
			}
		}

	}
}
