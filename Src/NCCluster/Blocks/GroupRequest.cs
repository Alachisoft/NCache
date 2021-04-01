// $Id: GroupRequest.java,v 1.8 2004/09/05 04:54:22 ovidiuf Exp $
using System;
using System.Collections;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Stack;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Monitoring;
using System.Text;
using System.Configuration;

using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NGroups.Util;


namespace Alachisoft.NGroups.Blocks
{
    /// <summary> Sends a message to all members of the group and waits for all responses (or timeout). Returns a
    /// boolean value (success or failure). Results (if any) can be retrieved when _done.<p>
    /// The supported transport to send requests is currently either a RequestCorrelator or a generic
    /// Transport. One of them has to be given in the constructor. It will then be used to send a
    /// request. When a message is received by either one, the receiveResponse() of this class has to
    /// be called (this class does not actively receive requests/responses itself). Also, when a view change
    /// or suspicion is received, the methods viewChange() or suspect() of this class have to be called.<p>
    /// When started, an array of responses, correlating to the membership, is created. Each response
    /// is added to the corresponding field in the array. When all fields have been set, the algorithm
    /// terminates.
    /// This algorithm can optionally use a suspicion service (failure detector) to detect (and
    /// exclude from the membership) fauly members. If no suspicion service is available, timeouts
    /// can be used instead (see <code>execute()</code>). When _done, a list of suspected members
    /// can be retrieved.<p>
    /// Because a channel might deliver requests, and responses to <em>different</em> requests, the
    /// <code>GroupRequest</code> class cannot itself receive and process requests/responses from the
    /// channel. A mechanism outside this class has to do this; it has to determine what the responses
    /// are for the message sent by the <code>execute()</code> method and call <code>receiveResponse()</code>
    /// to do so.<p>
    /// <b>Requirements</b>: lossless delivery, e.g. acknowledgment-based message confirmation.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    /// <version>  $Revision: 1.8 $
    /// </version>
    public class GroupRequest : RspCollector, Command
    {
        /// <summary>Returns the results as a RspList </summary>

        virtual public RspList Results
        {
            get
            {
                RspList retval = new RspList();
                Address sender;
                lock (rsp_mutex)
                {
                    for (int i = 0; i < membership.Length; i++)
                    {
                        sender = membership[i];
                        switch (received[i])
                        {

                            case SUSPECTED:
                                retval.addSuspect(sender);
                                break;

                            case RECEIVED:
                                retval.addRsp(sender, responses[i]);
                                break;

                            case NOT_RECEIVED:
                                retval.addNotReceived(sender);
                                break;
                        }
                    }
                   
                    return retval;
                }
            }

        }

        virtual public int NumSuspects
        {
            get { return suspects.Count; }
        }
        virtual public ArrayList Suspects
        {
            get { return suspects; }
        }
        virtual public bool Done
        {
            get { return _done; }
        }

        /// <summary>Generates a new unique request ID </summary>
        private static long RequestId
        {
            get
            {
                lock (req_mutex)
                {
                    //Request id ranges from 0 to long.Max. If it reaches the max we
                    //re-initialize it to -1;
                    if (last_req_id == long.MaxValue) last_req_id = -1;
                    long result = ++last_req_id;
                    return result;
                }
            }
        }

        public void AddNHop(Address sender)
        {

            lock (_nhopMutex)
            {
                expectedNHopResponses++;

                if (!nHops.Contains(sender))
                    nHops.Add(sender);
            }

        }

        public void AddNHopDefaultStatus(Address sender)
        {

            if (!receivedFromNHops.ContainsKey(sender))
                receivedFromNHops.Add(sender, NOT_RECEIVED);

        }
        
        virtual protected internal bool Responses
        {
            get
            {
                int num_not_received = getNum(NOT_RECEIVED);
                int num_received = getNum(RECEIVED);
                int num_suspected = getNum(SUSPECTED);
                int num_total = membership.Length;
                
                int num_receivedFromNHops = getNumFromNHops(RECEIVED);
                int num_suspectedNHops = getNumFromNHops(SUSPECTED);
                int num_okResponsesFromNHops = num_receivedFromNHops + num_suspectedNHops;

                switch (rsp_mode)
                {
                    case GET_FIRST:
                        if (num_received > 0)
                            return true;
                        if (num_suspected >= num_total)
                            // e.g. 2 members, and both suspected
                            return true;
                        break;

                    case GET_FIRST_NHOP:
                        if (num_received > 0 && num_okResponsesFromNHops == expectedNHopResponses)
                            return true;
                        if (num_suspected >= num_total)
                            return true;
                        break;

                    case GET_ALL:
                        if (num_not_received > 0)
                            return false;
                        return true;

                    case GET_ALL_NHOP:
                        if (num_not_received > 0)
                            return false;
                        if (num_okResponsesFromNHops < expectedNHopResponses)
                            return false;
                        
                        return true;
                        
                    case GET_N:
                        if (expected_mbrs >= num_total)
                        {
                            rsp_mode = GET_ALL;
                            return Responses;
                        }
                        if (num_received >= expected_mbrs)
                        {
                            return true;
                        }
                        if (num_received + num_not_received < expected_mbrs)
                        {
                            if (num_received + num_suspected >= expected_mbrs)
                            {
                                return true;
                            }
                            return false;
                        }
                        return false;

                    case GET_NONE:
                        return true;

                    default:
                        NCacheLog.Error("rsp_mode " + rsp_mode + " unknown !");
                        break;

                }
                return false;
            }

        }

        /// <summary>return only first response </summary>
        public const byte GET_FIRST = 1;
        /// <summary>return all responses </summary>
        public const byte GET_ALL = 2;
        /// <summary>return n responses (may block) </summary>
        public const byte GET_N = 3;
        /// <summary>return no response (async call) </summary>
        public const byte GET_NONE = 4;
        
        /// <summary>
        /// This type is used when two nodes dont communicate directly. Consider three nodes 1,2 and 3.
        /// 1 sends request to 2; 2 forwards request to 3; 3 executes the request and send the response 
        /// directly to 1 instead of 2. 
        /// </summary>
        public const byte GET_FIRST_NHOP = 5;

        public const byte GET_ALL_NHOP = 6;

        private const byte NOT_RECEIVED = 0;
        private const byte RECEIVED = 1;
        private const byte SUSPECTED = 2;

        private Address[] membership = null; // current membership
        private object[] responses = null; // responses corresponding to membership
        private byte[] received = null; // status of response for each mbr (see above)
        private long[] timeStats = null; // responses corresponding to membership
	
        /// <summary>
        /// replica nodes in the cluster from where we are expecting responses. Following is the detail of how
        /// it works.
        /// 1. In case of synchronous POR, when an operation is transferred to main node through clustering 
        /// layer, main node does the following: -
        ///     a) it executes the operation on itself.
        ///     b) it transfers the operation to its replica (the next hop).
        ///     c) it sends the response of this operation back and as part of this
        ///        response, it informs the node that another response is expected from replica node (the next hop).
        /// 2. this dictionary is filled with the replica addresses (next hop addresses) received as part of the response 
        ///    from main node along with the status (RECEIVED/NOT_RECEIVED...). 
        /// </summary>
        
		private Dictionary<Address, byte> receivedFromNHops = new Dictionary<Address, byte>();

        /// <summary>
        /// list of next hop members. 
        /// </summary>
        private List<Address> nHops = new List<Address>();
		
        /// <summary>
        /// number of responses expected from next hops. When one node send requests to other node (NHop Request),
        /// the node may or may not send the same request to next hop depending on the success/failure of the request
        /// on this node. this counter tells how many requests were sent to next hops and their responses are now
        /// expected.
        /// </summary>
        private int expectedNHopResponses = 0;

        private object _nhopMutex = new object();

        /// <summary>bounded queue of suspected members </summary>
        private ArrayList suspects = ArrayList.Synchronized(new ArrayList(10));

        /// <summary>list of members, changed by viewChange() </summary>
        private ArrayList members = ArrayList.Synchronized(new ArrayList(10));

        /// <summary>
        /// the list of all the current members in the cluster.
        /// this list is different from the members list of the Group Request which 
        /// only contains the addresses of members to which this group request must
        /// send the message.
        /// list of total membership is used to determine which member has been 
        /// suspected after the new list of members is received through view change
        /// event.
        /// </summary>
        private ArrayList clusterMembership = ArrayList.Synchronized(new ArrayList(10));

        /// <summary>keep suspects vector bounded </summary>
        private int max_suspects = 40;
        protected internal Message request_msg = null;
        protected internal RequestCorrelator corr = null; // either use RequestCorrelator or ...
        protected internal Transport transport = null; // Transport (one of them has to be non-null)

        protected internal byte rsp_mode = GET_ALL;
        private bool _done = false;
        protected internal object rsp_mutex = new object();
        protected internal long timeout = 0;
        protected internal int expected_mbrs = 0;

        /// <summary>to generate unique request IDs (see getRequestId()) </summary>
        private static long last_req_id = -1;

        protected internal long req_id = -1; // request ID for this request
        private static object req_mutex = new object();

        private ILogger _ncacheLog;

        private ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        private bool _seqReset;
        private int _retriesAfteSeqReset;

        static GroupRequest()
        {

        }

        /// <param name="m">The message to be sent
        /// </param>
        /// <param name="corr">The request correlator to be used. A request correlator sends requests tagged with
        /// a unique ID and notifies the sender when matching responses are received. The
        /// reason <code>GroupRequest</code> uses it instead of a <code>Transport</code> is
        /// that multiple requests/responses might be sent/received concurrently.
        /// </param>
        /// <param name="members">The initial membership. This value reflects the membership to which the request
        /// is sent (and from which potential responses are expected). Is reset by reset().
        /// </param>
        /// <param name="rsp_mode">How many responses are expected. Can be
        /// <ol>
        /// <li><code>GET_ALL</code>: wait for all responses from non-suspected members.
        /// A suspicion service might warn
        /// us when a member from which a response is outstanding has crashed, so it can
        /// be excluded from the responses. If no suspision service is available, a
        /// timeout can be used (a value of 0 means wait forever). <em>If a timeout of
        /// 0 is used, no suspicion service is available and a member from which we
        /// expect a response has crashed, this methods blocks forever !</em>.
        /// <li><code>GET_FIRST</code>: wait for the first available response.
        /// <li><code>GET_MAJORITY</code>: wait for the majority of all responses. The
        /// majority is re-computed when a member is suspected.
        /// <li><code>GET_ABS_MAJORITY</code>: wait for the majority of
        /// <em>all</em> members.
        /// This includes failed members, so it may block if no timeout is specified.
        /// <li><code>GET_N</CODE>: wait for N members.
        /// Return if n is >= membership+suspects.
        /// <li><code>GET_NONE</code>: don't wait for any response. Essentially send an
        /// asynchronous message to the group members.
        /// </ol>
        /// </param>
        public GroupRequest(Message m, RequestCorrelator corr, ArrayList members, ArrayList clusterCompleteMembership, byte rsp_mode, ILogger NCacheLog)
        {
            request_msg = m;
            this.corr = corr;
            this.rsp_mode = rsp_mode;
            this._ncacheLog = NCacheLog;
            this.clusterMembership = clusterCompleteMembership;
            reset(members);
        }

        /// <param name="timeout">Time to wait for responses (ms). A value of <= 0 means wait indefinitely
        /// (e.g. if a suspicion service is available; timeouts are not needed).
        /// </param>
        public GroupRequest(Message m, RequestCorrelator corr, ArrayList members, ArrayList clusterCompleteMembership, byte rsp_mode, long timeout, int expected_mbrs, ILogger NCacheLog)
            : this(m, corr, members, clusterCompleteMembership, rsp_mode, NCacheLog)
        {
            if (timeout > 0)
                this.timeout = timeout;
            this.expected_mbrs = expected_mbrs;
        }

        public GroupRequest(Message m, Transport transport, ArrayList members, ArrayList clusterCompleteMembership, byte rsp_mode, ILogger NCacheLog)
        {
            request_msg = m;
            this.transport = transport;
            this.rsp_mode = rsp_mode;

            this._ncacheLog = NCacheLog;

            this.clusterMembership = clusterCompleteMembership;
            reset(members);
        }

        /// <param name="timeout">Time to wait for responses (ms). A value of <= 0 means wait indefinitely
        /// (e.g. if a suspicion service is available; timeouts are not needed).
        /// </param>
        public GroupRequest(Message m, Transport transport, ArrayList members, ArrayList clusterCompleteMembership, byte rsp_mode, long timeout, int expected_mbrs, ILogger NCacheLog)
            : this(m, transport, members, clusterCompleteMembership, rsp_mode, NCacheLog)
        {
            if (timeout > 0)
                this.timeout = timeout;
            this.expected_mbrs = expected_mbrs;
        }

        /// <summary> Sends the message. Returns when n responses have been received, or a
        /// timeout  has occurred. <em>n</em> can be the first response, all
        /// responses, or a majority  of the responses.
        /// </summary>
        public virtual bool execute()
        {
            if(ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.Exec", "mode :" + rsp_mode);
            
            bool retval;
            if (corr == null && transport == null)
            {
                NCacheLog.Error("GroupRequest.execute()", "both corr and transport are null, cannot send group request");
                return false;
            }
            lock (rsp_mutex)
            {
                _done = false;
                retval = doExecute(timeout);
                if (retval == false)
                {
                    if(NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.execute()", "call did not execute correctly, request is " + ToString());
                }
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.Exec", "exited; result:" + retval);

                _done = true;
                return retval;
            }
        }

        /// <summary> Resets the group request, so it can be reused for another execution.</summary>
        public virtual void reset(Message m, byte mode, long timeout)
        {
            lock (rsp_mutex)
            {
                _done = false;
                request_msg = m;
                rsp_mode = mode;
                this.timeout = timeout;
                System.Threading.Monitor.PulseAll(rsp_mutex);
            }
        }

        public virtual void reset(Message m, ArrayList members, byte rsp_mode, long timeout, int expected_rsps)
        {
            lock (rsp_mutex)
            {
                reset(m, rsp_mode, timeout);
                reset(members);
                this.expected_mbrs = expected_rsps;
                System.Threading.Monitor.PulseAll(rsp_mutex);
            }
        }

        /// <summary> This method sets the <code>membership</code> variable to the value of
        /// <code>members</code>. It requires that the caller already hold the
        /// <code>rsp_mutex</code> lock.
        /// </summary>
        /// <param name="mbrs">The new list of members
        /// </param>
        public virtual void reset(ArrayList mbrs)
        {
            if (mbrs != null)
            {
                int size = mbrs.Count;
                membership = new Address[size];
                responses = new object[size];
                received = new byte[size];
                timeStats = new long[size];
                for (int i = 0; i < size; i++)
                {
                    membership[i] = (Address)mbrs[i];
                    responses[i] = null;
                    received[i] = NOT_RECEIVED;
                    timeStats[i] = 0;
                }
                // maintain local membership
                this.members.Clear();
                this.members.AddRange(mbrs);
            }
            else
            {
                if (membership != null)
                {
                    for (int i = 0; i < membership.Length; i++)
                    {
                        responses[i] = null;
                        received[i] = NOT_RECEIVED;
                    }
                }
            }
        }

        public void SequenceReset()
        {
            lock (rsp_mutex)
            {
                _seqReset = true;
                _retriesAfteSeqReset = 0;
                System.Threading.Monitor.PulseAll(rsp_mutex);
            }
        }
        /* ---------------------- Interface RspCollector -------------------------- */
        /// <summary> <b>Callback</b> (called by RequestCorrelator or Transport).
        /// Adds a response to the response table. When all responses have been received,
        /// <code>execute()</code> returns.
        /// </summary>

        public virtual void receiveResponse(Message m)
        {
            Address sender = m.Src, mbr;
            object val = null;
            if (_done)
            {
                NCacheLog.Warn("GroupRequest.receiveResponse()", "command is done; cannot add response !");
                return;
            }
            if (suspects != null && suspects.Count > 0 && suspects.Contains(sender))
            {
                NCacheLog.Warn("GroupRequest.receiveResponse()", "received response from suspected member " + sender + "; discarding");
                return;
            }
            if (m.Length > 0 || m.BufferLength >0)
            {
                try
                {
                    if (m.responseExpected)
                    {
                        OperationResponse opRes = new OperationResponse();
                        
                        if (m.Buffers != null)
                            opRes.SerializablePayload = m.Buffers;
                        else
                            opRes.SerializablePayload = m.getFlatObject();
                        opRes.UserPayload = m.Payload;
                        val = opRes;
                    }
                    else
                    {
                        if (m.Buffers != null)
                            val = m.Buffers;
                        else
                            val = m.getFlatObject();
                    }
                }
                catch (System.Exception e)
                {
                    NCacheLog.Error("GroupRequest.receiveResponse()", "exception=" + e.Message);
                }
            }

            lock (rsp_mutex)
            {
                bool isMainMember = false;
                for (int i = 0; i < membership.Length; i++)
                {
                    mbr = membership[i];
                    if (mbr.Equals(sender))
                    {
                        isMainMember = true;

                        if (received[i] == NOT_RECEIVED)
                        {
                            responses[i] = val;
                            received[i] = RECEIVED;
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.receiveResponse()", "received response for request " + req_id + ", sender=" + sender + ", val=" + val);
                            System.Threading.Monitor.PulseAll(rsp_mutex); // wakes up execute()
                            break;
                        }
                    }
                }

                if (!isMainMember)
                {

                    receivedFromNHops[sender] = RECEIVED;
                    //nTrace.criticalInfo("GroupRequest.receiveResponse", sender.ToString() + " has sent nhop response");
                    System.Threading.Monitor.PulseAll(rsp_mutex);

                }
            }
        }

        /// <summary> <b>Callback</b> (called by RequestCorrelator or Transport).
        /// Report to <code>GroupRequest</code> that a member is reported as faulty (suspected).
        /// This method would probably be called when getting a suspect message from a failure detector
        /// (where available). It is used to exclude faulty members from the response list.
        /// </summary>
        public virtual void suspect(Address suspected_member)
        {
            Address mbr;
            bool isMainMember = false;

            lock (rsp_mutex)
            {
                // modify 'suspects' and 'responses' array
                for (int i = 0; i < membership.Length; i++)
                {
                    mbr = membership[i];
                    if (mbr.Equals(suspected_member))
                    {
                        isMainMember = true;
                        addSuspect(suspected_member);
                        responses[i] = null;
                        received[i] = SUSPECTED;
                        System.Threading.Monitor.PulseAll(rsp_mutex);
                        break;
                    }
                }

                if (!isMainMember)
                {

                    if (clusterMembership != null && clusterMembership.Contains(suspected_member))
                        receivedFromNHops[suspected_member] = SUSPECTED;
                    System.Threading.Monitor.PulseAll(rsp_mutex);

                }
            }
        }

        /// <summary> Any member of 'membership' that is not in the new view is flagged as
        /// SUSPECTED. Any member in the new view that is <em>not</em> in the
        /// membership (ie, the set of responses expected for the current RPC) will
        /// <em>not</em> be added to it. If we did this we might run into the
        /// following problem:
        /// <ul>
        /// <li>Membership is {A,B}
        /// <li>A sends a synchronous group RPC (which sleeps for 60 secs in the
        /// invocation handler)
        /// <li>C joins while A waits for responses from A and B
        /// <li>If this would generate a new view {A,B,C} and if this expanded the
        /// response set to {A,B,C}, A would wait forever on C's response because C
        /// never received the request in the first place, therefore won't send a
        /// response.
        /// </ul>
        /// </summary>
        public virtual void viewChange(View new_view)
        {
            Address mbr;
            ArrayList mbrs = new_view != null ? new_view.Members : null;
            if (membership == null || membership.Length == 0 || mbrs == null)
                return;            

            lock (rsp_mutex)
            {
                ArrayList oldMembership = clusterMembership != null ? clusterMembership.Clone() as ArrayList : null;
                clusterMembership.Clear();
                clusterMembership.AddRange(mbrs);

                this.members.Clear();
                this.members.AddRange(mbrs);
                for (int i = 0; i < membership.Length; i++)
                {
                    mbr = membership[i];
                    if (!mbrs.Contains(mbr))
                    {
                        addSuspect(mbr);
                        responses[i] = null;
                        received[i] = SUSPECTED;
                    }

                    if (oldMembership != null)
                        oldMembership.Remove(mbr);
                }

                //by this time, membershipClone cotains all those members that are not part of  
                //group request normal membership and are no longer part of the cluster membership
                //according to the new view.
                //this way we are suspecting replica members.
                if (oldMembership != null)
                {

                    foreach (Address member in oldMembership)
                    {
                        if (!mbrs.Contains(member))
                            receivedFromNHops[member] = SUSPECTED;
                    }

                }

                System.Threading.Monitor.PulseAll(rsp_mutex);
            }
        }


        /* -------------------- End of Interface RspCollector ----------------------------------- */


        public override string ToString()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder();
            ret.Append("[GroupRequest:\n");
            ret.Append("req_id=").Append(req_id).Append('\n');
            ret.Append("members: ");
            for (int i = 0; i < membership.Length; i++)
            {
                ret.Append(membership[i] + " ");
            }
            ret.Append("\nresponses: ");
            for (int i = 0; i < responses.Length; i++)
            {
                ret.Append(responses[i] + " ");
            }
            if (suspects.Count > 0)
                ret.Append("\nsuspects: " + Global.CollectionToString(suspects));
            ret.Append("\nrequest_msg: " + request_msg);
            ret.Append("\nrsp_mode: " + rsp_mode);
            ret.Append("\ndone: " + _done);
            ret.Append("\ntimeout: " + timeout);
            ret.Append("\nexpected_mbrs: " + expected_mbrs);
            ret.Append("\n]");
            return ret.ToString();
        }

        /* --------------------------------- Private Methods -------------------------------------*/
        /// <summary>This method runs with rsp_mutex locked (called by <code>execute()</code>). </summary>
        protected internal virtual bool doExecute_old(long timeout)
        {
            long start_time = 0;
            Address mbr, suspect;
            if (rsp_mode != GET_NONE)
            {
                req_id = corr.NextRequestId;
            }
            reset(null); // clear 'responses' array
            if (suspects != null)
            {
                // mark all suspects in 'received' array
                for (int i = 0; i < suspects.Count; i++)
                {
                    suspect = (Address)suspects[i];
                    for (int j = 0; j < membership.Length; j++)
                    {
                        mbr = membership[j];
                        if (mbr.Equals(suspect))
                        {
                            received[j] = SUSPECTED;
                            break; // we can break here because we ensure there are no duplicate members
                        }
                    }
                }
            }

            try
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "sending request (id=" + req_id + ')');
                if (corr != null)
                {
                    ArrayList tmp = members != null ? members : null;
                    corr.sendRequest(req_id, tmp, request_msg, rsp_mode == GET_NONE ? null : this);
                }
                else
                {
                    transport.send(request_msg);
                }
            }
            catch (System.Exception e)
            {
                NCacheLog.Error("GroupRequest.doExecute()", "exception=" + e.Message);
                if (corr != null)
                {
                    corr.done(req_id);
                }
                return false;
            }

			long orig_timeout = timeout;
            if (timeout <= 0)
            {
                while (true)
                {
                    /* Wait for responses: */
                    adjustMembership(); // may not be necessary, just to make sure...
                    if (Responses)
                    {
                        if (corr != null)
                        {
                            corr.done(req_id);
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "received all responses: " + ToString());
                        return true;
                    }
                    try
                    {
                        System.Threading.Monitor.Wait(rsp_mutex);
                    }
                    catch (System.Exception e)
                    {
                        NCacheLog.Error("GroupRequest.doExecute():2", "exception=" + e.Message);
                    }
                }
            }
            else
            {
                start_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                while (timeout > 0)
                {
                    /* Wait for responses: */
                    if (Responses)
                    {
                        if (corr != null)
                            corr.done(req_id);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "received all responses: " + ToString());
                        return true;
                    }
                    timeout = orig_timeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time);
                    if (timeout > 0)
                    {
                        try
                        {
                            System.Threading.Monitor.Wait(rsp_mutex, TimeSpan.FromMilliseconds(timeout));
                        }
                        catch (System.Exception e)
                        {
                            NCacheLog.Error("GroupRequest.doExecute():3", "exception=" + e);
                            //e.printStacknTrace();
                        }
                    }
                }

                if (timeout <= 0)
                {
                    RspList rspList = Results;
                    string failedNodes = "";
                    if (rspList != null)
                    {
                        for (int i = 0; i < rspList.size(); i++)
                        {
                            Rsp rsp = rspList.elementAt(i) as Rsp;
                            if (rsp != null && !rsp.wasReceived())
                                failedNodes += rsp.Sender;
                        }
                    }

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute:", "[ " + req_id + " ] did not receive rsp from " + failedNodes + " [Timeout] " + timeout + " [timeout-val =" + orig_timeout + "]");
                }

                if (corr != null)
                {
                    corr.done(req_id);
                }
                return false;
            }
        }

        protected internal virtual bool doExecute(long timeout)
        {
            long start_time = 0;
            Address mbr, suspect;
            if (rsp_mode != GET_NONE)
            {
                req_id = corr.NextRequestId;
            }
            reset(null); // clear 'responses' array
            if (suspects != null)
            {
                // mark all suspects in 'received' array
                for (int i = 0; i < suspects.Count; i++)
                {
                    suspect = (Address)suspects[i];
                    for (int j = 0; j < membership.Length; j++)
                    {
                        mbr = membership[j];
                        if (mbr.Equals(suspect))
                        {
                            received[j] = SUSPECTED;
                            break; // we can break here because we ensure there are no duplicate members
                        }
                    }
                }
            }

            try
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.doExec", "sending req_id :" + req_id + "; timeout: " + timeout);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "sending request (id=" + req_id + ')');
                if (corr != null)
                {
                    ArrayList tmp = members != null ? members : null;
                    
                    if (rsp_mode == GET_FIRST_NHOP || rsp_mode == GET_ALL_NHOP)
                        corr.sendNHopRequest(req_id, tmp, request_msg, this);
                    else
                        corr.sendRequest(req_id, tmp, request_msg, rsp_mode == GET_NONE ? null : this);
                }
                else
                {
                    transport.send(request_msg);
                }
            }
            catch (System.Exception e)
            {
                NCacheLog.Error("GroupRequest.doExecute()", "exception=" + e.Message);
                if (corr != null)
                {
                    corr.done(req_id);
                }
                return false;
            }

            long orig_timeout = timeout;
            if (timeout <= 0)
            {
                while (true)
                {
                    /* Wait for responses: */
                    adjustMembership(); // may not be necessary, just to make sure...
                    if (Responses)
                    {
                        if (corr != null)
                        {
                            corr.done(req_id);
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "received all responses: " + ToString());
                        return true;
                    }
                    try
                    {
                        System.Threading.Monitor.Wait(rsp_mutex);
                    }
                    catch (System.Exception e)
                    {
                        NCacheLog.Error("GroupRequest.doExecute():2", "exception=" + e.Message);
                    }
                }
            }
            else
            {
                start_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                long wakeuptime = timeout;
                int retries = ServiceConfiguration.RequestEnquiryInterval;
                int enquiryFailure = 0;

                if (ServiceConfiguration.AllowRequestEnquiry)
                {
                    wakeuptime = ServiceConfiguration.RequestEnquiryInterval * 1000;
                }

                while (timeout > 0)
                {
                    /* Wait for responses: */
                    if (Responses)
                    {
                        if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.doExec", "req_id :" + req_id + " completed" );

                        if (corr != null)
                            corr.done(req_id);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "received all responses: " + ToString());
                        return true;
                    }
                    timeout = orig_timeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time);

                    if (ServiceConfiguration.AllowRequestEnquiry)
                    {
                        if (wakeuptime > timeout)
                            wakeuptime = timeout;
                    }
                    else
                    {
                        wakeuptime = timeout;
                    }

                    if (timeout > 0)
                    {
                        try
                        {
                          timeout = orig_timeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time);

                           bool reacquired = System.Threading.Monitor.Wait(rsp_mutex, TimeSpan.FromMilliseconds(wakeuptime));

                           if ((!reacquired || _seqReset) && ServiceConfiguration.AllowRequestEnquiry)
                           {
                               //_seqReset = false;
                              
                               if (Responses)
                               {
                                   if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.doExec", "req_id :" + req_id + " completed");

                                   if (corr != null)
                                       corr.done(req_id);
                                   if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute()", "received all responses: " + ToString());
                                   return true;
                               }
                               else
                               {
                                   if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.doExec", "req_id :" + req_id + " completed");

                                   if ((timeout > 0 && wakeuptime < timeout) && retries > 0)
                                   {
                                       if (_seqReset)
                                       {
                                           _retriesAfteSeqReset++;
                                       }

                                       retries--;                                       

                                       bool enquireAgain = GetRequestStatus();
                                       
                                       if (!enquireAgain) 
                                           enquiryFailure++;


                                       if (enquiryFailure >=3 || _retriesAfteSeqReset > 3)
                                       {
                                           if (corr != null)
                                               corr.done(req_id);
                                           return false;
                                       }
                                   }
                               }
                           }
                        }
                        catch (System.Exception e)
                        {
                            NCacheLog.Error("GroupRequest.doExecute():3", "exception=" + e);
                        }
                    }
                }

                if (timeout <= 0)
                {
                    RspList rspList = Results;
                    string failedNodes = "";
                    if (rspList != null)
                    {
                        for (int i = 0; i < rspList.size(); i++)
                        {
                            Rsp rsp = rspList.elementAt(i) as Rsp;
                            if (rsp != null && !rsp.wasReceived())
                                failedNodes += rsp.Sender;
                        }
                    }

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.doExecute:", "[ " + req_id + " ] did not receive rsp from " + failedNodes + " [Timeout] " + timeout + " [timeout-val =" + orig_timeout + "]");
                }

                if (corr != null)
                {
                    corr.done(req_id);
                }
                return false;
            }
        }

        private bool GetRequestStatus()
        {
            Hashtable statusResult = null;
            ArrayList failedNodes = new ArrayList();
            RspList rspList = Results;
            bool enquireStatusAgain = true;
            int suspectCount = 0;
            string notRecvNodes = "";
            
            if (rspList != null)
            {
                for (int i = 0; i < rspList.size(); i++)
                {
                    Rsp rsp = rspList.elementAt(i) as Rsp;
                    
                    if (rsp != null && !rsp.wasReceived())
                    {
                        notRecvNodes += rsp.sender + ",";
                        failedNodes.Add(rsp.Sender);
                    }
                    if (rsp != null && rsp.wasSuspected())
                        suspectCount++;
                }
            }
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.GetReqStatus", req_id + " rsp not received from " + failedNodes.Count + " nodes");

            bool resendReq = true;
            ArrayList resendList = new ArrayList();
            int notRespondingCount = 0;

            if(failedNodes.Count >0)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.GetReqStatus", " did not recv rsps from " + notRecvNodes + " nodes");
                
                statusResult = corr.FetchRequestStatus(failedNodes, this.clusterMembership, req_id);
                StringBuilder sb = null;
                if (ServerMonitor.MonitorActivity) sb = new StringBuilder();
                if (statusResult != null)
                {
                    foreach (Address node in failedNodes)
                    {
                        RequestStatus status = statusResult[node] as RequestStatus;
                        if (status.Status == RequestStatus.REQ_NOT_RECEIVED)
                        {
                            if(sb != null) sb.Append("(" + node + ":" + "REQ_NOT_RECEIVED)");
                            resendList.Add(node);
                        }
                        if (status.Status == RequestStatus.NONE)
                        {
                            if (sb != null) sb.Append("(" + node + ":" + "NONE)");
                            notRespondingCount++;
                        }
                        if (status.Status == RequestStatus.REQ_PROCESSED)
                        {
                            if (sb != null) sb.Append("(" + node + ":" + "REQ_PROCESSED)");
                            if (!request_msg.IsSeqRequired)
                            {
                                resendList.Add(node);
                            }
                        }
                    }
                    if (sb != null && ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GrpReq.GetReqStatus", "status of failed nodes " + sb.ToString());

                    if (request_msg.IsSeqRequired)
                    {
                        if (resendList.Count != rspList.size())
                        {
                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.GetReqStatus", req_id + "sequence message; no need to resend; resend_count " + resendList.Count);
                            if (notRespondingCount > 0)
                            {
                                resendReq = false;
                            }
                            else
                            {
                                enquireStatusAgain = false;
                                resendReq = false;
                            }
                        }
                    }
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.GetReqStatus", req_id + "received REQ_NOT_RECEIVED status from " + resendList.Count + " nodes");

                }
                else
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.GetReqStatus", req_id + " status result is NULL");



                if (resendReq && resendList.Count > 0)
                {
                    if (corr != null)
                    {
                        if (resendList.Count == 1)
                        {
                            request_msg.Dest = resendList[0] as Address;
                        }
                        else
                        {
                            request_msg.Dests = resendList;
                        }

                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("GroupRequest.GetReqStatus", req_id + " resending messages to " + resendList.Count);
                        corr.sendRequest(req_id, resendList, request_msg, rsp_mode == GET_NONE ? null : this);
                    }
                }

            }
            return enquireStatusAgain;
        }
        /// <summary>Return number of elements of a certain type in array 'received'. Type can be RECEIVED,
        /// NOT_RECEIVED or SUSPECTED 
        /// </summary>
        public virtual int getNum(int type)
        {
            int retval = 0;
            for (int i = 0; i < received.Length; i++)
                if (received[i] == type)
                    retval++;
            return retval;
        }

        public virtual int getNumFromNHops(int type)
        {
            int retval = 0;

            lock (_nhopMutex)
            {

                foreach (Address replica in nHops)
                {
                    byte status = 0;
                    if (receivedFromNHops.TryGetValue(replica, out status))
                    {
                        if (status == type)
                            retval++;
                    }
                }

            }

            return retval;
        }

        public virtual void printReceived()
        {
            for (int i = 0; i < received.Length; i++)
            {
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info(membership[i] + ": " + (received[i] == NOT_RECEIVED ? "NOT_RECEIVED" : (received[i] == RECEIVED ? "RECEIVED" : "SUSPECTED")));
            }
        }

        /// <summary> Adjusts the 'received' array in the following way:
        /// <ul>
        /// <li>if a member P in 'membership' is not in 'members', P's entry in the 'received' array
        /// will be marked as SUSPECTED
        /// <li>if P is 'suspected_mbr', then P's entry in the 'received' array will be marked
        /// as SUSPECTED
        /// </ul>
        /// This call requires exclusive access to rsp_mutex (called by getResponses() which has
        /// a the rsp_mutex locked, so this should not be a problem).
        /// </summary>
        public virtual void adjustMembership()
        {
            Address mbr;
            if (membership == null || membership.Length == 0)
            {
                return;
            }
            for (int i = 0; i < membership.Length; i++)
            {
                mbr = membership[i];
                if ((this.members != null && !this.members.Contains(mbr)) || suspects.Contains(mbr))
                {
                    addSuspect(mbr);
                    responses[i] = null;
                    received[i] = SUSPECTED;
                }
            }
        }

        /// <summary> Adds a member to the 'suspects' list. Removes oldest elements from 'suspects' list
        /// to keep the list bounded ('max_suspects' number of elements)
        /// </summary>
        public virtual void addSuspect(Address suspected_mbr)
        {
            if (!suspects.Contains(suspected_mbr))
            {
                suspects.Add(suspected_mbr);
                while (suspects.Count >= max_suspects && suspects.Count > 0)
                    suspects.RemoveAt(0); // keeps queue bounded
            }
        }
    }
}
