// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $
using System;
using Alachisoft.NGroups;
using Alachisoft.NGroups.Protocols;
using Alachisoft.NGroups.Util;
using Alachisoft.NGroups.Protocols.pbcast;
using Alachisoft.NCache.Common.Net;



using Alachisoft.NCache.Common;
using System.Text;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NGroups.Stack;

namespace Alachisoft.NGroups.Protocols
{


    /// <summary> The TCPPING protocol layer retrieves the initial membership (used by the GMS when started
    /// by sending event FIND_INITIAL_MBRS down the stack). We do this by mcasting TCPPING
    /// requests to an IP MCAST address (or, if gossiping is enabled, by contacting the router).
    /// The responses should allow us to determine the coordinator whom we have to contact,
    /// e.g. in case we want to join the group.  When we are a server (after having received the
    /// BECOME_SERVER event), we'll respond to TCPPING requests with a TCPPING response.<p> The
    /// FIND_INITIAL_MBRS event will eventually be answered with a FIND_INITIAL_MBRS_OK event up
    /// the stack.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    class TCPPING : Protocol
    {
        override public System.String Name
        {
            get
            {
                return "TCPPING";
            }

        }
        internal System.Collections.ArrayList members = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10)), initial_members = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
        //		internal SetSupport members_set = new HashSetSupport(); //copy of the members vector for fast random access    
        internal Address local_addr = null;
        internal string group_addr = null;
        internal string subGroup_addr = null;
        internal System.String groupname = null;
        internal long timeout = 5000;
        internal long num_initial_members = 20;
        internal bool twoPhaseConnect;
        //muds: 24-06-08
        //as per iqbal sb. decision changing the default port-range to '1'
        internal int port_range = 1; // number of ports to be probed for initial membership

        internal ThreadClass mcast_receiver = null;

        internal String discovery_addr = "228.8.8.8";
        internal int discovery_port = 7700;

        internal int tcpServerPort = 7500;

        internal MPingBroadcast broadcaster = null;
        internal MPingReceiver receiver = null;
        internal bool hasStarted = false;

        internal bool isStatic = false;

        internal bool mbrDiscoveryInProcess = false;
        

        /// <summary>
        /// These two values are used to authorize a user so that he can not join 
        /// to other nodes where he is not allowed to.
        /// </summary>
        /// 

        private const string DEFAULT_USERID = "Ncache-Default-UserId";
        private const string DEFAULT_PASSWORD = "Ncache-Default-Password";
        internal string userId = DEFAULT_USERID;
        internal string password = DEFAULT_PASSWORD;
        internal byte[] secureUid = null;
        internal byte[] securePwd = null;

        /// <summary>List<Address> </summary>
        internal System.Collections.ArrayList initial_hosts = null; // hosts to be contacted for the initial membership
        internal bool is_server = false;

        public override bool setProperties(System.Collections.Hashtable props)
        {
            base.setProperties(props);

            if (stack.StackType == ProtocolStackType.TCP)
            {
                this.up_thread = false;
                this.down_thread = false;
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(Name + ".setProperties", "part of TCP stack");

            }
            if (props.Contains("timeout")) // max time to wait for initial members
            {
                timeout = Convert.ToInt64(props["timeout"]);
                props.Remove("timeout");
            }

            if (props.Contains("port_range")) // if member cannot be contacted on base port,
            {
                // how many times can we increment the port
                port_range = Convert.ToInt32(props["port_range"]);
                if (port_range < 1)
                {
                    port_range = 1;
                }
                props.Remove("port_range");
            }
            if (props.Contains("static"))
            {
                isStatic = Convert.ToBoolean(props["static"]);
                props.Remove("static");
            }
            if (props.Contains("num_initial_members")) // wait for at most n members
            {
                num_initial_members = Convert.ToInt32(props["num_initial_members"]);
                props.Remove("num_initial_members");
            }

            if (props.Contains("initial_hosts"))
            {
                String tmp = (String)props["initial_hosts"];
                if (tmp != null && tmp.Length > 0)
                {
                    initial_hosts = createInitialHosts(Convert.ToString(tmp));
                }
                if (initial_hosts != null)
                {
                    if (num_initial_members != initial_hosts.Count)
                        num_initial_members = initial_hosts.Count;
                }
                if (num_initial_members > 5)
                {
                    //We estimate the time for finding initital members
                    //for every member we add 1 sec timeout.
                    long temp = num_initial_members - 5;
                    timeout += (temp * 1000);
                }
                props.Remove("initial_hosts");
            }
            if (props.Contains("discovery_addr"))
            {
                discovery_addr = Convert.ToString(props["discovery_addr"]);

                if (discovery_addr != null && discovery_addr.Length > 0)
                    isStatic = false;
                else
                    isStatic = true;

                props.Remove("discovery_addr");
            }

            if (props.Contains("discovery_port"))
            {
                discovery_port = Convert.ToInt32(props["discovery_port"]);
                props.Remove("discovery_port");
            }

            if (props.Contains("user-id"))
            {
                userId = Convert.ToString(props["user-id"]);
                secureUid = EncryptionUtil.Encrypt(userId);
                props.Remove("user-id");
            }
            if (props.Contains("password"))
            {
                password = Convert.ToString(props["password"]);
                securePwd = EncryptionUtil.Encrypt(password);
                props.Remove("password");
            }
            if (props.Count > 0)
            {
                return true;
            }
            return true;
        }

        public bool IsStatic
        {
            get { return isStatic; }
        }


        public override void up(Event evt)
        {
            Message msg, rsp_msg;
            System.Object obj;
            PingHeader hdr, rsp_hdr;
            PingRsp rsp;
            Address coord;
            switch (evt.Type)
            {

                case Event.MSG:
                    msg = (Message)evt.Arg;

                    obj = msg.getHeader(HeaderType.TCPPING);
                    if (obj == null || !(obj is PingHeader))
                    {
                        passUp(evt);
                        return;
                    }

                    hdr = (PingHeader)msg.removeHeader(HeaderType.TCPPING);

                    switch (hdr.type)
                    {

                        case PingHeader.GET_MBRS_REQ:  // return Rsp(local_addr, coord)

                            if (!hdr.group_addr.Equals(group_addr))
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.up()", "GET_MBRS_REQ from different group , so discarded");
                                return;
                            }
                            Address src = (Address)hdr.arg;
                            msg.Src = src;

                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "GET_MBRS_REQ from " + msg.Src.ToString());

                            
                            //determine whether security is required at this level.
                            //we require security only in case of inproc initialization of cache.
                            bool authorized = true;
                            byte[] secureUid = hdr.userId;
                            byte[] securePwd = hdr.password;
                            string uid = EncryptionUtil.Decrypt(secureUid);
                            string pwd = EncryptionUtil.Decrypt(securePwd);
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", " before authorizing. I have received these credentials user-id = " + userId + ", password = " + password);

                            if (!authorized || Stack.DisableOperationOnMerge)
                            {
                                rsp_msg = new Message(msg.Src, null, null);
                                rsp_hdr = new PingHeader(PingHeader.GET_MBRS_RSP, new PingRsp(null, null, false, true));
                                rsp_msg.putHeader(HeaderType.TCPPING, rsp_hdr);
							    if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()",   "responding to GET_MBRS_REQ back to " + msg.Src.ToString() + " with empty response");
							    if(Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()",   "some un-authorized user has tried to connect to the cluster");
                                passDown(new Event(Event.MSG, rsp_msg, Priority.High));
                            }
                            else
                            {
                                lock (members.SyncRoot)
                                {
                                    coord = members.Count > 0 ? (Address)members[0] : local_addr;
                                }
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "my coordinator is " + coord.ToString());

                                rsp_msg = new Message(msg.Src, null, null);
                                rsp_hdr = new PingHeader(PingHeader.GET_MBRS_RSP, new PingRsp(local_addr, coord, Stack.IsOperational, Stack.IsOperational));
                                rsp_msg.putHeader(HeaderType.TCPPING, rsp_hdr);
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "responding to GET_MBRS_REQ back to " + msg.Src.ToString());

                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(local_addr + " - [FIND_INITIAL_MBRS] replying PING request to " + rsp_msg.Dest);

                                passDown(new Event(Event.MSG, rsp_msg, Priority.High));

                            }
                            return;

                        case PingHeader.GET_MBRS_RSP:  // add response to vector and notify waiting thread
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "GET_MBRS_RSP from " + msg.Src.ToString());
                            rsp = (PingRsp)hdr.arg;

                            
                            //check if the received response is valid i.e. successful security authorization
                            //at other end.
                            if (rsp.OwnAddress == null && rsp.CoordAddress == null && rsp.HasJoined == false)
                            {
                                lock (initial_members.SyncRoot)
                                {
                                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "I am not authorized to join to " + msg.Src.ToString());
                                    System.Threading.Monitor.PulseAll(initial_members.SyncRoot);
                                }
                            }
                            else
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "Before Adding initial members response");
                                lock (initial_members.SyncRoot)
                                {
                                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "Adding initial members response");
                                    if (!initial_members.Contains(rsp))
                                    {
                                        initial_members.Add(rsp);
                                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCPPING.up()", "Adding initial members response for " + rsp.OwnAddress);
                                    }
                                    else
                                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.up()", "response already received");
                                    System.Threading.Monitor.PulseAll(initial_members.SyncRoot);
                                }
                            }
                            return;
                        default:
                            Stack.NCacheLog.Warn("got TCPPING header with unknown type (" + hdr.type + ')');
                            return;

                    }
                case Event.SET_LOCAL_ADDRESS:
                    passUp(evt);
                    local_addr = (Address)evt.Arg;
                    // Add own address to initial_hosts if not present: we must always be able to ping ourself !
                    if (initial_hosts != null && local_addr != null)
                    {
                        if (!initial_hosts.Contains(local_addr))
                        {
                            Stack.NCacheLog.Debug("[SET_LOCAL_ADDRESS]: adding my own address (" + local_addr + ") to initial_hosts; initial_hosts=" + Global.CollectionToString(initial_hosts));
                            initial_hosts.Add(local_addr);
                        }
                    }
                    break;
                case Event.CONNECT_OK:  
                    obj = evt.Arg;

                    if (obj != null && obj is Address)
                    {
                        tcpServerPort = ((Address)obj).Port;
                    }
                    passUp(evt);
                    break;
                case Event.CONNECTION_NOT_OPENED:
                    if (mbrDiscoveryInProcess)
                    {
                        Address node = evt.Arg as Address;
                        PingRsp response = new PingRsp(node, node, true, false);
                        lock (initial_members.SyncRoot)
                        {
                            initial_members.Add(response);
                            System.Threading.Monitor.PulseAll(initial_members.SyncRoot);
                        }
                        if (Stack != null && Stack.NCacheLog != null && Stack.NCacheLog.IsDebugEnabled)
                            Stack.NCacheLog.Debug(Name + ".up",  "connection failure with " + node);
                    }

                    break;
                // end services
                default:
                    passUp(evt); // Pass up to the layer above us
                    break;

            }
        }
      
        public override void down(Event evt)
        {
            Message msg;
            long time_to_wait, start_time;

            switch (evt.Type)
            {


                case Event.FIND_INITIAL_MBRS:  // sent by GMS layer, pass up a GET_MBRS_OK event

                    //We pass this event down to tcp so that it can take some measures.
                    passDown(evt);

                    initial_members.Clear();
                    msg = new Message(null, null, null);

                    msg.putHeader(HeaderType.TCPPING, new PingHeader(PingHeader.GET_MBRS_REQ, (System.Object)local_addr, group_addr, secureUid, securePwd));

                    // if intitial nodes have been specified and static is true, then only those
                    // members will form the cluster, otherwise, nodes having the same IP Multicast and port
                    // will form the cluster dyanamically.

                    mbrDiscoveryInProcess = true;
                    lock (members.SyncRoot)
                    {
                        if (initial_hosts != null)
                        {
                            for (System.Collections.IEnumerator it = initial_hosts.GetEnumerator(); it.MoveNext(); )
                            {
                                Address addr = (Address)it.Current;
                                msg.Dest = addr;
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("[FIND_INITIAL_MBRS] sending PING request to " + msg.Dest);
                                passDown(new Event(Event.MSG_URGENT, msg.copy(), Priority.High));
                            }
                        }

                    }
                    // 2. Wait 'timeout' ms or until 'num_initial_members' have been retrieved

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "[FIND_INITIAL_MBRS] waiting for results...............");
                    lock (initial_members.SyncRoot)
                    {
                        start_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                        time_to_wait = timeout;

                        while (initial_members.Count < num_initial_members && time_to_wait > 0)
                        {
                            try
                            {
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "initial_members Count: " + initial_members.Count + "initialHosts Count: " + num_initial_members);
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "Time to wait for next response: " + time_to_wait);
                                ///initial members will be pulsed in case connection is not available.
                                ///so here we dont have to wait till each member is timed out.
                                ///this significantly improves time for initial member discovery. 
                                bool timeExpire = System.Threading.Monitor.Wait(initial_members.SyncRoot, TimeSpan.FromMilliseconds(time_to_wait));
                            }
                            catch (System.Exception e)
                            {
                                Stack.NCacheLog.Error("TCPPing.down(FIND_INITIAL_MBRS)", e.ToString());
                            }
                            time_to_wait = timeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time);
                        }
                        mbrDiscoveryInProcess = false;
                    }
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "[FIND_INITIAL_MBRS] initial members are " + Global.CollectionToString(initial_members));
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "[FIND_INITIAL_MBRS] initial members count " + initial_members.Count);

                    //remove those which are not functional due to twoPhaseConnect
                    for (int i = initial_members.Count - 1; i >= 0; i--)
                    {
                        PingRsp rsp = initial_members[i] as PingRsp;
                        if (!rsp.IsStarted) initial_members.RemoveAt(i);
                    }

                    // 3. Send response
                    passUp(new Event(Event.FIND_INITIAL_MBRS_OK, initial_members));
                    break;


                case Event.TMP_VIEW:
                case Event.VIEW_CHANGE:
                    System.Collections.ArrayList tmp;
                    if ((tmp = ((View)evt.Arg).Members) != null)
                    {
                        lock (members.SyncRoot)
                        {
                            members.Clear();
                            members.AddRange(tmp);
                        }
                    }
                    passDown(evt);
                    break;
                /****************************After removal of NackAck *********************************/
                //TCPPING emulates a GET_DIGEST call, which is required by GMS. This is needed
                //since we have now removed NAKACK from the stack!
                case Event.GET_DIGEST:
                    pbcast.Digest digest = new pbcast.Digest(members.Count);
                    for (int i = 0; i < members.Count; i++)
                    {
                        Address sender = (Address)members[i];
                        digest.add(sender, 0, 0);
                    }
                    passUp(new Event(Event.GET_DIGEST_OK, digest));
                    return;

                case Event.SET_DIGEST:
                    // Not needed! Just here to let you know that it is needed by GMS!
                    return;
                /********************************************************************************/

                case Event.BECOME_SERVER:  // called after client has joined and is fully working group member
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TcpPing.down()", "received BECOME_SERVER event");
                    passDown(evt);
                    is_server = true;
                    break;


                case Event.CONNECT:
                    object[] addrs = ((object[])evt.Arg);
                    group_addr = (string)addrs[0];
                    subGroup_addr = (string)addrs[1];
                    twoPhaseConnect = (bool)addrs[3];
                    if (twoPhaseConnect) timeout = 1000;
                    passDown(evt);
                    break;


                case Event.DISCONNECT:
                    passDown(evt);
                    break;

                case Event.HAS_STARTED:
                    hasStarted = true;
                    passDown(evt);
                    break;

                default:
                    passDown(evt); // Pass on to the layer below us
                    break;

            }
        }

        public override System.Collections.ArrayList providedUpServices()
        {
            System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(6));
            retval.Add((System.Int32)Event.FIND_INITIAL_MBRS);
            retval.Add((System.Int32)Event.GET_DIGEST);
            retval.Add((System.Int32)Event.SET_DIGEST);
            return retval;
        }


        /* -------------------------- Private methods ---------------------------- */



        /// <summary> Input is "daddy[8880],sindhu[8880],camille[5555]. Return List of IpAddresses</summary>
        private System.Collections.ArrayList createInitialHosts(System.String l)
        {
            Tokenizer tok = new Tokenizer(l, ",");
            System.String t;
            Address addr;
            int port;
            System.Collections.ArrayList retval = new System.Collections.ArrayList();
            System.Collections.Hashtable hosts = new System.Collections.Hashtable();

            //to be removed later on
            while (tok.HasMoreTokens())
            {
                try
                {
                    t = tok.NextToken();

                    System.String host = t.Substring(0, (t.IndexOf((System.Char)'[')) - (0));
                    host = host.Trim();
                    port = System.Int32.Parse(t.Substring(t.IndexOf((System.Char)'[') + 1, (t.IndexOf((System.Char)']')) - (t.IndexOf((System.Char)'[') + 1)));
                    hosts.Add(host, port);


                }
                catch (System.FormatException e)
                {
                    Stack.NCacheLog.Error("exeption is " + e);
                }
                catch (Exception e)
                {
                    Stack.NCacheLog.Error("TcpPing.createInitialHosts", "Error: " + e.ToString());

                    throw new Exception("Invalid initial members list");
                }
            }
            try
            {
                System.Collections.IDictionaryEnumerator ide;
                for (int i = 0; i < port_range; i++)
                {
                    ide = hosts.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        port = Convert.ToInt32(ide.Value);
                        addr = new Address((String)ide.Key, port+i);
                        retval.Add(addr);
                    }

                }
            }
            catch (Exception ex)
            {
                Stack.NCacheLog.Error("TcpPing.CreateInitialHosts()", "Error :" + ex);
                throw new Exception("Invalid initial memebers list");
            }
            return retval;
        }

    }
}