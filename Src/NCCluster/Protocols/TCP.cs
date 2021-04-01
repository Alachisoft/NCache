// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $

using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections;
using System.Net;
using System.Threading;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Protocols.pbcast;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> TCP based protocol. Creates a server socket, which gives us the local address of this group member. For
    /// each accept() on the server socket, a new thread is created that listens on the socket.
    /// For each outgoing message m, if m.dest is in the ougoing hashtable, the associated socket will be reused
    /// to send message, otherwise a new socket is created and put in the hashtable.
    /// When a socket connection breaks or a member is removed from the group, the corresponding items in the
    /// incoming and outgoing hashtables will be removed as well.<br>
    /// This functionality is in ConnectionTable, which is used by TCP. TCP sends messages using ct.send() and
    /// registers with the connection table to receive all incoming messages.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    class TCP : Protocol, ConnectionTable.Receiver, ConnectionTable.ConnectionListener
    {
       

        override public System.String Name
        {
            get
            {
                return "TCP";
            }

        }
        /// <summary>If the sender is null, set our own address. We cannot just go ahead and set the address
        /// anyway, as we might be sending a message on behalf of someone else ! E.g. in case of
        /// retransmission, when the original sender has crashed, or in a FLUSH protocol when we
        /// have to return all unstable messages with the FLUSH_OK response.
        /// </summary>
        private Message SourceAddress
        {
            set
            {
                if (value.Src == null)
                    value.Src = local_addr;
            }

        }
        private ConnectionTable ct = null;
        private Address local_addr = null;
        private string group_addr = null;
        private string subGroup_addr = null;
        //These should be fetch from the current worker role instance @UH incase of AZURE
        private System.Net.IPAddress bind_addr1 = null; // local IP address to bind srv sock to (m-homed systems)
        private System.Net.IPAddress bind_addr2 = null; // local IP address to bind srv sock to (m-homed systems)

        private int start_port = 7800; // find first available port starting at this port

        // decision changing the default port-range to '1'
        private System.Collections.ArrayList members = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(11));

        private int port_range = 1;


        private long reaper_interval = 0; // time in msecs between connection reaps
        private long conn_expire_time = 0; // max time a conn can be idle before being reaped

        private int _retries;
        private int _retryInterval;
        internal bool isStarting = true;

        internal bool loopback = true; // loops back msgs to self if true
        internal bool isClosing = false;

        /// <summary>If set it will be added to <tt>local_addr</tt>. Used to implement
        /// for example transport independent addresses 
        /// </summary>
        internal byte[] additional_data = null;

        /// <summary>List the maintains the currently suspected members. This is used so we don't send too many SUSPECT
        /// events up the stack (one per message !)
        /// </summary>
        internal BoundedList suspected_mbrs = new BoundedList(20);

        /// <summary>Should we drop unicast messages to suspected members or not </summary>
        internal bool skip_suspected_members = false;

        internal int recv_buf_size = 20000000;
        internal int send_buf_size = 640000;
        internal AsyncProcessor _asyncProcessor;
        /// <summary> Used to shortcircuit transactional messages from management messages. </summary>
        private Protocol upper;
        internal TimeStats stats = new TimeStats(1);
        Hashtable syncTable = new Hashtable();
        private ReaderWriterLock lock_members = new ReaderWriterLock();

        ConnectionKeepAlive _keepAlive;

        ArrayList asyncThreads = new ArrayList();
        bool asyncPassup = false;
        object async_mutex = new object();

        int _heartBeatInterval = 32000;
        bool _useKeepAlive = true;

        bool synchronizeConnections = true;

        internal DownHandler _unicastDownHandler;
        internal DownHandler _multicastDownHandler;
        internal DownHandler _tokenSeekingMsgDownHandler;
        internal Alachisoft.NCache.Common.DataStructures.Queue _unicastDownQueue;
        internal Alachisoft.NCache.Common.DataStructures.Queue _multicastDownQueue;
        internal Alachisoft.NCache.Common.DataStructures.Queue _tokenSeekingMsgDownQueue;

        internal UpHandler _tokenSeekingUpHandler;
        internal UpHandler _sequencedMsgUpHandler;
        internal UpHandler _sequencelessMsgUpHandler;
        internal Alachisoft.NCache.Common.DataStructures.Queue _sequencelessMsgUpQueue;
        internal Alachisoft.NCache.Common.DataStructures.Queue _sequenecedMsgUpQueue;
        internal Alachisoft.NCache.Common.DataStructures.Queue _tokenMsgUpQueue;
        ArrayList _nodeRejoiningList;

        HPTimeStats time;
        HPTimeStats loopTime;

        HPTimeStats _unicastSendTimeStats;
        HPTimeStats _multicastSendTimeStats;
        HPTimeStats _totalToTcpDownStats;
        private bool _leaving;
        private string environmentName="";
        private string versionType;
        private bool isInproc;

        public TCP()
        {
            time = new HPTimeStats();
            loopTime = new HPTimeStats();
            _totalToTcpDownStats = new HPTimeStats();
        }

        public override System.String ToString()
        {
            return "Protocol TCP(local address: " + local_addr + ')';
        }

      
        public override void receiveDownEvent(Event evt)
        {
            int type = evt.Type;

            if (evt.Type == Event.I_AM_LEAVING)
            {
                //Set leaving flag to true
                _leaving = true;
                return;
            }

            if (_unicastDownHandler == null)
            {
                if (type == Event.ACK || type == Event.START || type == Event.STOP)
                {
                    if (handleSpecialDownEvent(evt) == false)
                        return;
                }

                if (evt.Type == Event.HAS_STARTED)
                {
                    HasStarted();
                    return;
                }


                if (_printMsgHdrs && type == Event.MSG)
                    printMsgHeaders(evt, "down()");
                down(evt);
                return;
            }
            try
            {
                if (type == Event.STOP || type == Event.VIEW_BCAST_MSG)
                {
                    if (handleSpecialDownEvent(evt) == false)
                        return;
                    if (down_prot != null)
                    {
                        down_prot.receiveDownEvent(evt);
                    }
                    return;
                }

                if (evt.Type == Event.HAS_STARTED)
                {
                    HasStarted();
                    return;
                }

                if (evt.Type == Event.MSG || evt.Type == Event.MSG_URGENT)
                {
                    Message msg = evt.Arg as Message;
                    if (msg != null)
                    {
                        if ((msg.Type & MsgType.TOKEN_SEEKING) == MsgType.TOKEN_SEEKING)
                        {
                            _tokenSeekingMsgDownQueue.add(evt, evt.Priority);
                            return;
                        }

                        if (msg.Dests != null || msg.Dest == null)
                        {
                            _multicastDownQueue.add(evt, evt.Priority);
                            return;
                        }
                        else
                        {
                            _unicastDownQueue.add(evt, evt.Priority);
                            return;
                        }
                    }
                }
                _unicastDownQueue.add(evt, evt.Priority);
            }
            catch (System.Exception e)
            {
                Stack.NCacheLog.Info("Protocol.receiveDownEvent():2", e.ToString());
            }
        }

        private void HasStarted()
        {
            this.isStarting = false;
        }

        public override void receiveUpEvent(Event evt)
        {
            int type = evt.Type;
            if (_printMsgHdrs && type == Event.MSG)
                printMsgHeaders(evt, "up()");

            if (!up_thread)
            {
                up(evt);
                return;
            }
            try
            {

                if (evt.Type == Event.MSG || evt.Type == Event.MSG_URGENT)
                {
                    Message msg = evt.Arg as Message;


                    //We don't queue the critical priority events
                    if (evt.Priority == Priority.High)
                    {
                        GMS.HDR hdr = msg.getHeader(HeaderType.GMS) as GMS.HDR;
                        bool allowAsyncPassup = false;
                        if (hdr != null)
                        {
                            switch (hdr.type)
                            {
                                case GMS.HDR.VIEW:
                                    if (msg.Src != null && msg.Src.Equals(local_addr))
                                        allowAsyncPassup = true;
                                    break;

                                case GMS.HDR.IS_NODE_IN_STATE_TRANSFER:
                                case GMS.HDR.IS_NODE_IN_STATE_TRANSFER_RSP:
                                case GMS.HDR.VIEW_RESPONSE:
                                    allowAsyncPassup = true;
                                    break;
                            }

                        }
                        //passing up GET_MBRS_REQ for faster delivery
                        PingHeader pingHdr = msg.getHeader(HeaderType.TCPPING) as PingHeader;
                        if (pingHdr != null && pingHdr.type == PingHeader.GET_MBRS_REQ)
                            allowAsyncPassup = true;
                        if (allowAsyncPassup)
                        {
                            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolPassup), evt);
                            return;
                        }
                    }
                    //To fix issue of cyclic dependency during double clustered operations
                    else if(evt.Priority==Priority.Critical)
                    {
                        if (Stack != null && Stack.NCacheLog != null && Stack.NCacheLog.IsDebugEnabled)
                        {
                            Stack.NCacheLog.Debug("Protocol.receiveUpEvent()", "Event with critical priority received...");
                        }

                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolPassup), evt);

                        return;
                    }

                    switch (msg.Type)
                    {
                        case MsgType.TOKEN_SEEKING:
                            _tokenMsgUpQueue.add(evt, evt.Priority);
                            break;

                        case MsgType.SEQUENCED:
                            _sequenecedMsgUpQueue.add(evt, evt.Priority);
                            break;

                        case MsgType.SEQUENCE_LESS:
                            _sequencelessMsgUpQueue.add(evt, evt.Priority);
                            break;

                        default:
                            _sequencelessMsgUpQueue.add(evt, evt.Priority);
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Stack.NCacheLog.Info("Protocol.receiveUpEvent()", e.ToString());
            }
        }
        public override void startDownHandler()
        {
            if (down_thread)
            {
                if (_unicastDownHandler == null)
                {
                    _unicastDownQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _unicastDownHandler = new DownHandler(_unicastDownQueue, this, Name + ".unicast.DownHandler", 1);
                    _unicastDownHandler.Start();
                }
                if (_multicastDownHandler == null)
                {
                    _multicastDownQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _multicastDownHandler = new DownHandler(_multicastDownQueue, this, Name + ".muticast.DownHandler", 2);
                    _multicastDownHandler.Start();
                }

                if (_tokenSeekingMsgDownHandler == null)
                {
                    _tokenSeekingMsgDownQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _tokenSeekingMsgDownHandler = new DownHandler(_tokenSeekingMsgDownQueue, this, Name + ".token.DownHandler", 3);
                    _tokenSeekingMsgDownHandler.Start();
                }
            }
        }
        public override void startUpHandler()
        {
            if (up_thread)
            {

                if (_tokenSeekingUpHandler == null)
                {
                    _tokenMsgUpQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _tokenSeekingUpHandler = new UpHandler(_tokenMsgUpQueue, this, Name + ".token.UpHandler", 3);
                    _tokenSeekingUpHandler.Start();
                }

                if (_sequencedMsgUpHandler == null)
                {
                    _sequenecedMsgUpQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _sequencedMsgUpHandler = new UpHandler(_sequenecedMsgUpQueue, this, Name + ".seq.UpHandler", 2);
                    _sequencedMsgUpHandler.Start();
                }

                if (_sequencelessMsgUpHandler == null)
                {
                    _sequencelessMsgUpQueue = new Alachisoft.NCache.Common.DataStructures.Queue();
                    _sequencelessMsgUpHandler = new UpHandler(_sequencelessMsgUpQueue, this, Name + ".seqless.UpHandler", 1);
                    
                    _sequencelessMsgUpHandler.Start();
                }
            }
        }

        public override void stopInternal()
        {

            //stop up handlers
            if (_sequencelessMsgUpQueue != null)
                _sequencelessMsgUpQueue.close(false); // this should terminate up_handler thread

            if (_sequencelessMsgUpHandler != null && _sequencelessMsgUpHandler.IsAlive)
            {
                try
                {
                    _sequencelessMsgUpHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "up_handler.Join " + e.Message);
                }
                if (_sequencelessMsgUpHandler != null && _sequencelessMsgUpHandler.IsAlive)
                {
                    _sequencelessMsgUpHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _sequencelessMsgUpHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "up_handler.Join " + e.Message);
                    }
                    if (_sequencelessMsgUpHandler != null && _sequencelessMsgUpHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "up_handler thread for " + Name + " was interrupted (in order to be terminated), but is still alive");
                }
            }
            _sequencelessMsgUpHandler = null;

            if (_sequenecedMsgUpQueue != null)
                _sequenecedMsgUpQueue.close(false); // this should terminate down_handler thread
            if (_sequencedMsgUpHandler != null && _sequencedMsgUpHandler.IsAlive)
            {
                try
                {
                    _sequencedMsgUpHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                }
                if (_sequencedMsgUpHandler != null && _sequencedMsgUpHandler.IsAlive)
                {
                    _sequencedMsgUpHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _sequencedMsgUpHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                    }
                    if (_sequencedMsgUpHandler != null && _sequencedMsgUpHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
                }
            }
            _sequencedMsgUpHandler = null;

            if (_tokenMsgUpQueue != null)
                _tokenMsgUpQueue.close(false); // this should terminate down_handler thread
            if (_tokenSeekingUpHandler != null && _tokenSeekingUpHandler.IsAlive)
            {
                try
                {
                    _tokenSeekingUpHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                }
                if (_tokenSeekingUpHandler != null && _tokenSeekingUpHandler.IsAlive)
                {
                    _tokenSeekingUpHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _tokenSeekingUpHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                    }
                    if (_tokenSeekingUpHandler != null && _tokenSeekingUpHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
                }
            }
            _tokenSeekingUpHandler = null;

            ///stop down handler now.
            if (_unicastDownQueue != null)
                _unicastDownQueue.close(false); // this should terminate down_handler thread
            if (_unicastDownHandler != null && _unicastDownHandler.IsAlive)
            {
                try
                {
                    _unicastDownHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                }
                if (_unicastDownHandler != null && _unicastDownHandler.IsAlive)
                {
                    _unicastDownHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _unicastDownHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                    }
                    if (_unicastDownHandler != null && _unicastDownHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
                }
            }
            _unicastDownHandler = null;

            if (_multicastDownQueue != null)
                _multicastDownQueue.close(false); // this should terminate down_handler thread
            if (_multicastDownHandler != null && _multicastDownHandler.IsAlive)
            {
                try
                {
                    _multicastDownHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                }
                if (_multicastDownHandler != null && _multicastDownHandler.IsAlive)
                {
                    _multicastDownHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _multicastDownHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                    }
                    if (_multicastDownHandler != null && _multicastDownHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
                }
            }
            _multicastDownHandler = null;

            if (_tokenSeekingMsgDownQueue != null)
                _tokenSeekingMsgDownQueue.close(false); // this should terminate down_handler thread
            if (_tokenSeekingMsgDownHandler != null && _tokenSeekingMsgDownHandler.IsAlive)
            {
                try
                {
                    _tokenSeekingMsgDownHandler.Join(THREAD_JOIN_TIMEOUT);
                }
                catch (System.Exception e)
                {
                    Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                }
                if (_tokenSeekingMsgDownHandler != null && _tokenSeekingMsgDownHandler.IsAlive)
                {
                    _tokenSeekingMsgDownHandler.Interrupt(); // still alive ? let's just kill it without mercy...
                    try
                    {
                        _tokenSeekingMsgDownHandler.Join(THREAD_JOIN_TIMEOUT);
                    }
                    catch (System.Exception e)
                    {
                        Stack.NCacheLog.Error("Protocol.stopInternal()", "down_handler.Join " + e.Message);
                    }
                    if (_tokenSeekingMsgDownHandler != null && _tokenSeekingMsgDownHandler.IsAlive)
                        Stack.NCacheLog.Error("Protocol", "down_handler thread for " + Name + " was interrupted (in order to be terminated), but is is still alive");
                }
            }
            _tokenSeekingMsgDownHandler = null;

        }
        public override void start()
        {
            // Incase of TCP stack we'll get a reference to TOTAL, which is the top
            // protocol in our case.
            upper = Stack.findProtocol("TOTAL");

            ct = getConnectionTable(reaper_interval, conn_expire_time, bind_addr1, bind_addr2, start_port, _retries, _retryInterval, isInproc,environmentName);

            ct.addConnectionListener(this);
            ct.ReceiveBufferSize = recv_buf_size;
            ct.SendBufferSize = send_buf_size;
         
            local_addr = ct.LocalAddress;
            if (additional_data != null /*&& local_addr is Address*/)
                ((Address)local_addr).AdditionalData = additional_data;
            passUp(new Event(Event.SET_LOCAL_ADDRESS, local_addr, Priority.High));
            _asyncProcessor = new AsyncProcessor(stack.NCacheLog);
            _asyncProcessor.Start();

            _keepAlive = new ConnectionKeepAlive(this, ct, 32000/*default value*/);
            if (_useKeepAlive)
                _keepAlive.Start();


            Stack.NCacheLog.CriticalInfo("TCP.start", "operating parameters -> [ heart_beat:" + _useKeepAlive + " ;heart_beat_interval: " + _heartBeatInterval + " ;async_up_deliver: " + asyncPassup + " ;connection_retries: " + _retries + " ;connection_retry_interval: " + _retryInterval + " ]");
        }

        /// <param name="">
        /// </param>
        /// <param name="">cet
        /// </param>
        /// <param name="">b_addr
        /// </param>
        /// <param name="">s_port
        /// </param>
        /// <throws>  Exception </throws>
        /// <returns> ConnectionTable
        /// Sub classes overrides this method to initialize a different version of
        /// ConnectionTable.
        /// </returns>
        /// 

        protected internal virtual ConnectionTable getConnectionTable(long ri, long cet, System.Net.IPAddress b_addr1, System.Net.IPAddress b_addr2, int s_port, int retries, int retryInterval, bool isInproc,string environmentName)

        {
            ConnectionTable cTable = null;

            if (ri == 0 && cet == 0)
            {
                cTable = new ConnectionTable(this, b_addr1, bind_addr2, start_port, port_range, stack.NCacheLog, retries, retryInterval, isInproc, environmentName);
            }
            else
            {
                if (ri == 0)
                {
                    ri = 5000;
                    Stack.NCacheLog.Info("reaper_interval was 0, set it to " + ri);
                }
                if (cet == 0)
                {
                    cet = 1000 * 60 * 5;
                    Stack.NCacheLog.Info("conn_expire_time was 0, set it to " + cet);
                }
                cTable = new ConnectionTable(this, b_addr1, s_port, ri, cet, this.Stack.NCacheLog);
            }

            return cTable;
        }

        public override void stop()
        {
            isClosing = true;
            local_addr = null;
            if (_asyncProcessor != null)
                _asyncProcessor.Stop();
            _asyncProcessor = null;

            if (_keepAlive != null) _keepAlive.Stop();
       
            ct.stop();
            upper = null;
        }

        protected override bool handleSpecialDownEvent(Event evt)
        {
            //We handle the view message differently to handle the situation
            //where coordinator itself is leaving 
            if (evt.Type == Event.VIEW_BCAST_MSG)
            {
                Stack.NCacheLog.Error("TCP.handleSpecialDownEvent", evt.ToString());
                down(new Event(Event.MSG, evt.Arg, evt.Priority));
                Stack.NCacheLog.Error("TCP.handleSpecialDownEvent", "view broadcast is complete");
                return false;
            }

            return base.handleSpecialDownEvent(evt);
        }

        /// <summary>Sent to destination(s) using the ConnectionTable class.</summary>
        public override void down(Event evt)
        {
            Message msg;
            System.Object dest_addr;
            bool reEstablishCon = false;
            stats = new TimeStats(1);

            if (evt.Type != Event.MSG && evt.Type != Event.MSG_URGENT)
            {
                handleDownEvent(evt);
                return;
            }

            reEstablishCon = evt.Type == Event.MSG_URGENT ? true : false;
            msg = (Message) evt.Arg;
            msg.Priority = evt.Priority;
            if (Stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info("Tcp.down()", " message headers = " + Global.CollectionToString(msg.Headers));

            if (group_addr != null)
            {
                msg.putHeader(HeaderType.TCP, new TcpHeader(group_addr));
            }

            dest_addr = msg.Dest;
            if (dest_addr == null)
            {
                // broadcast (to all members)
                if (group_addr == null)
                {
                    Stack.NCacheLog.Info("dest address of message is null, and " + "sending to default address fails as group_addr is null, too !" + " Discarding message.");
                    return;
                }
                if (reEstablishCon && _asyncProcessor != null)
                {
                    lock (async_mutex)
                    {
                        if (asyncThreads != null)
                        {
                            TCP.AsnycMulticast asyncMcast = new TCP.AsnycMulticast(this, msg, reEstablishCon);

                            Thread asyncThread = new Thread(new ThreadStart(asyncMcast.Process));
                            asyncThreads.Add(asyncThread);
                            asyncThread.Start();
                        }
                    }

                    if (Stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info("Tcp.down", "broadcasting message asynchronously ");
                }
                else
                {
                    sendMulticastMessage(msg, reEstablishCon, evt.Priority); // send to current membership
                }
            }
            else
            {
                if (Stack.NCacheLog.IsInfoEnabled) stack.NCacheLog.Info("Tcp.down()", " destination address " + msg.Dest.ToString());
                if (reEstablishCon && _asyncProcessor != null)
                {
                    lock (async_mutex)
                    {
                        if (asyncThreads != null)
                        {
                            TCP.AsnycUnicast asyncUcast = new TCP.AsnycUnicast(this, msg, reEstablishCon);

                            Thread asyncThread = new Thread(new ThreadStart(asyncUcast.Process));
                            asyncThreads.Add(asyncThread);
                            asyncThread.Start();
                        }
                    }
                }
                else
                {
                    sendUnicastMessage(msg, reEstablishCon, msg.Payload, evt.Priority); // send to a single member
                }
            }
        }

        public override void PublishDownQueueStats(long count, int queueId)
        {
            switch (queueId)
            {
                case 1:
                    stack.perfStatsColl.IncrementTcpDownQueueCountStats(count);
                    break;
            }
        }


        public override void PublishUpQueueStats(long count, int queueId)
        {

            stack.perfStatsColl.IncrementTcpUpQueueCountStats(count);

        }

        /// <summary>ConnectionTable.Receiver interface </summary>
        public virtual void receive(Message msg)
        {
            TcpHeader hdr = null;
            msg.Dest = local_addr;
            Event evt = new Event();
            evt.Arg = msg;
            evt.Priority = msg.Priority;
            evt.Type = Event.MSG;

            HearBeat hrtBeat = msg.removeHeader(HeaderType.KEEP_ALIVE) as HearBeat;
            if (hrtBeat != null && _keepAlive != null)
            {
                _keepAlive.ReceivedHeartBeat(msg.Src, hrtBeat);
                return;
            }
            if (!asyncPassup)
                this.receiveUpEvent(evt);
            else
                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolPassup), evt);
        }

        public void ThreadPoolLocalPassUp(Event evt)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolPassup), evt);
        }
        public void ThreadPoolPassup(object evt)
        {
            try
            {
                this.up((Event)evt);
            }
            catch (Exception e)
            {
                Stack.NCacheLog.Error("ThreadPoolPassUp", e.ToString());
            }
        }
        public virtual void connectionOpened(Address peer_addr)
        {
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("opened connection to " + peer_addr);
        }

        public virtual void connectionClosed(Address peer_addr)
        {
            if (peer_addr != null && local_addr != null)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("closed connection to " + peer_addr + " added to the suspected list");
                suspected_mbrs.add(peer_addr);
                Event evt = new Event(Event.SUSPECT, peer_addr, Priority.High);
                evt.Reason = "Connection closed called for suspect event";
                passUp(evt);
            }
        }

        public void couldnotConnectTo(Address peer_addr)
        {
            passUp(new Event(Event.CONNECTION_NOT_OPENED,peer_addr,Priority.High));
        }


        public override void up(Event evt)
        {
            TcpHeader hdr = null;
            Message msg = null;

            switch (evt.Type)
            {
                case Event.MSG:
                    msg = (Message)evt.Arg;

                    if (msg.IsProfilable)
                    {
                        stack.NCacheLog.Error("--------------------------------------", " ---------------Request.Add-->" + msg.TraceMsg + "----------");
                        stack.NCacheLog.Error("TCP", msg.TraceMsg + " received from ---> " + msg.Src.ToString());
                    }
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCP.up()", "src: " + msg.Src + " ,priority: " + evt.Priority + ", hdrs: " + Global.CollectionToString(msg.Headers));
                   
                    hdr = (TcpHeader)msg.removeHeader(HeaderType.TCP);

                    if (hdr != null)
                    {
                        /* Discard all messages destined for a channel with a different name */
                        System.String ch_name = null;

                        if (hdr.group_addr != null)
                            ch_name = hdr.group_addr;

                        // Discard if message's group name is not the same as our group name unless the
                        // message is a diagnosis message (special group name DIAG_GROUP)
                        if (ch_name != null && !group_addr.Equals(ch_name))
                        {
                            Stack.NCacheLog.Info("discarded message from different group (" + ch_name + "). Sender was " + msg.Src);
                            return;
                        }

                    }
                    passUp(evt);
                    break;
            }
        }

        /// <summary>Setup the Protocol instance acording to the configuration string </summary>
        public override bool setProperties(System.Collections.Hashtable props)
        {
            System.String str;

            base.setProperties(props);

            asyncPassup = ServiceConfiguration.AsyncTcpUpQueue;
         
            down_thread = false;
            if (props.Contains("start_port"))
            {
                start_port = Convert.ToInt32(props["start_port"]);
                props.Remove("start_port");
            }
            if (props.Contains("is_inproc"))
            {
                isInproc = Convert.ToBoolean(props["is_inproc"]);
                props.Remove("is_inproc");
            }

            if (props.Contains("port_range"))
            {
                port_range = Convert.ToInt32(props["port_range"]);
                if (port_range <= 0) port_range = 1;
                props.Remove("port_range");
            }
            Version.Initialize();
      
           
            if (props.Contains("heart_beat_interval"))
            {
                props.Remove("heart_beat_interval");
            }

            if (props.Contains("connection_retries"))
            {
                _retries = Convert.ToInt32(props["connection_retries"]);
                props.Remove("connection_retries");
            }

            if (props.Contains("connection_retry_interval"))
            {
                _retryInterval = Convert.ToInt32(props["connection_retry_interval"]);
                props.Remove("connection_retry_interval");
            }

            _heartBeatInterval = 32000;

            // It is supposed that bind_addr will be provided only through props.
            bind_addr1 =ServiceConfiguration.BindToIP;
            
            if(bind_addr1 == null)
            {
                try
                {
                    str = Dns.GetHostName();
                    IPHostEntry ipEntry = Dns.GetHostByName(str);
                    bind_addr1 = ipEntry.AddressList[0];
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCP.SetProperties()", "Bind address = " + bind_addr1.ToString());
                }
                catch (Exception ex)
                {
                    stack.NCacheLog.Error("TCP.SetProperties()", "bind address failure :" + ex.ToString());
                }
            }

            if (props.Contains("bind_addr"))
            {
                props.Remove("bind_addr");
            }

            if (props.Contains("reaper_interval"))
            {
                reaper_interval = System.Int64.Parse((String)props["reaper_interval"]);
                props.Remove("reaper_interval");
            }

            if (props.Contains("conn_expire_time"))
            {
                conn_expire_time = System.Int64.Parse((String)props["conn_expire_time"]);
                props.Remove("conn_expire_time");
            }


            if (props.Contains("recv_buf_size"))
            {
                recv_buf_size = System.Int32.Parse((String)props["recv_buf_size"]);
                props.Remove("recv_buf_size");
            }

            if (props.Contains("send_buf_size"))
            {
                send_buf_size = System.Int32.Parse((String)props["send_buf_size"]);
                props.Remove("send_buf_size");
            }

            if (props.Contains("loopback"))
            {
                loopback = System.Boolean.Parse((String)props["loopback"]);
                props.Remove("loopback");
            }
            if (props.Contains("use_heart_beat"))
            {
                _useKeepAlive = System.Boolean.Parse((String)props["use_heart_beat"]);
                props.Remove("use_heart_beat");
            }

            if (props.Contains("skip_suspected_members"))
            {
                skip_suspected_members = System.Boolean.Parse((String)props["skip_suspected_members"]);
                props.Remove("skip_suspected_members");
            }

            if (props.Count > 0)
            {
                stack.NCacheLog.Error("TCP.setProperties()", "the following properties are not recognized:");
                return true;
            }

            return true;
        }

        /// <summary>
        /// Determines if the local node is junior than the other then
        /// the other node.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsJuniorThan(Address address)
        {
            bool isJunior = true;
            if (members != null)
            {
                int myIndex = members.IndexOf(local_addr);
                int otherIndex = members.IndexOf(address);

                isJunior = myIndex > otherIndex ? true : false;
            }
            return isJunior;
        }
        /// <summary>Send a message to the address specified in msg.dest </summary>
        private void sendLocalMessage(Message msg)
        {
            Message copy;
            System.Object hdr;
            Event evt;

            SourceAddress = msg;

            /* Don't send if destination is local address. Instead, switch dst and src and put in up_queue  */
            if (loopback && local_addr != null)
            {
                copy = msg.copy();
                copy.Type = msg.Type;

                hdr = copy.getHeader(HeaderType.TCP);
                if (hdr != null && hdr is TcpHeader)
                {
                    copy.removeHeader(HeaderType.TCP);
                }

                copy.Src = local_addr;
                copy.Dest = local_addr;

                if (msg.IsProfilable)
                {
                    stack.NCacheLog.Error("TCP", msg.TraceMsg + " sending to ---> " + msg.Dest.ToString());
                }
                evt = new Event(Event.MSG, copy, copy.Priority);
                if (!asyncPassup)
                    this.receiveUpEvent(evt);
                else
                    System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadPoolPassup), evt);

                if (msg.IsProfilable)
                {
                    stack.NCacheLog.Error("TCP", msg.TraceMsg + " sent to ---> " + msg.Dest.ToString());
                }
                return;
            }

        }

        private void sendUnicastMessage(Message msg, bool reEstablishConnection, Array UserPayLoad, Priority priority)
        {
            Address dest = msg.Dest;

            try
            {
                if (dest == null) return;

                if (dest.Equals(local_addr))
                    sendLocalMessage(msg);
                else
                {
                    IList binaryMessage = Util.Util.serializeMessage(msg);
                    sendUnicastMessage(dest, binaryMessage, reEstablishConnection, msg.Payload, priority);
                }
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>Send a message to the address specified in msg.dest </summary>
        private void sendUnicastMessage(Address dest, IList msg, bool reEstablishCon, Array UserPayload, Priority priority)
        {
            try
            {
                if (skip_suspected_members)
                {
                    if (suspected_mbrs.contains(dest))
                    {
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("will not send unicast message to " + dest + " as it is currently suspected");
                        return;
                    }
                }

                long bytesSent = ct.send(dest, msg, reEstablishCon, UserPayload, priority);
            }
            catch (ExtSocketException)
            {
                if (members.Contains(dest))
                {
                    if (!suspected_mbrs.contains(dest) && local_addr != null)
                    {
                        suspected_mbrs.add(dest);
                        Event evt = new Event(Event.SUSPECT, dest, Priority.High);
                        evt.Reason = "Tcp.sendUnicastMesssage caused suspect event";
                        passUp(evt);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
            catch (System.Exception e)
            {
                stack.NCacheLog.Error("TCP.sendUnicastMessage()", e.ToString());
            }
        }


        private void sendMulticastMessage(Message msg, bool reEstablishCon, Priority priority)
        {
            if (msg.IsProfilable)
                stack.NCacheLog.Error("Tcp.sendMulticastMessage", msg.TraceMsg + " :started");
            Address dest;
            ArrayList dest_addrs = msg.Dests;
            System.Collections.ArrayList mbrs = null;

            bool deliverLocal = false;

            //if not intended for a list of destinations
            if (dest_addrs == null || dest_addrs.Count == 0)
            {
                lock_members.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    mbrs = (System.Collections.ArrayList)members.Clone();
                }
                finally
                {
                    lock_members.ReleaseReaderLock();
                }
                mbrs.Remove(local_addr);
                deliverLocal = true;

                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.sendmulticastmessage()", "members count " + mbrs.Count);
            }
            else
            {
                dest_addrs = dest_addrs.Clone() as ArrayList;
                if (dest_addrs.Contains(local_addr))
                {
                    dest_addrs.Remove(local_addr);
                    //mbrs.Add(local_addr);
                    deliverLocal = true;
                }
            }

            IList binaryMsg = Util.Util.serializeMessage(msg);

            if (mbrs != null)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.sendmulticastmessage()", "Sending to all members -- broadcasting");
                for (int i = 0; i < mbrs.Count; i++)
                {
                    dest = (Address)mbrs[i];
                    msg.Dest = dest;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.sendmulticastmessage()", "Sending to " + dest.ToString());
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCP.sendUnicastMessage()", "dest=" + dest + ", hdrs:" + Global.CollectionToString(msg.Headers));

                    sendUnicastMessage(dest, binaryMsg, reEstablishCon, msg.Payload, priority);
                }
            }
            else
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.sendmulticastmessage()", "Sending to selective members -- multicasting");
                for (int i = 0; i < dest_addrs.Count; i++)
                {
                    dest = (Address)dest_addrs[i];
                    msg.Dest = dest;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.sendmulticastmessage()", "Sending to " + dest.ToString());
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCP.sendUnicastMessage()", "dest=" + dest + ", hdrs:" + Global.CollectionToString(msg.Headers));
                    sendUnicastMessage(dest, binaryMsg, reEstablishCon, msg.Payload, priority);
                }
            }

            if (deliverLocal) sendLocalMessage(msg);

            if (msg.IsProfilable)
                stack.NCacheLog.Error("Tcp.sendMulticastMessage", msg.TraceMsg + " :end");
        }


 	    public void HandleFailedNodes(object failedNodes)
        {
            if (failedNodes is ArrayList)
            {
                ArrayList nodes = (ArrayList)failedNodes;
                if (nodes.Count > 0)
                {
                    NCacheLog.CriticalInfo("TCP.HandleFailedNodes()", " can not establish connection with all the nodes ");

                    passUp(new Event(Event.CONNECTION_FAILURE, nodes, Priority.High));
                }
            }

        }

        private void handleDownEvent(Event evt)
        {
            switch (evt.Type)
            {
                case Event.GET_NODE_STATUS:
                    Address suspect = evt.Arg as Address;

                    if (_keepAlive != null)
                        _keepAlive.CheckStatus(suspect);

                    break;

                case Event.FIND_INITIAL_MBRS:
                    lock (async_mutex)
                    {
                        Stack.NCacheLog.Flush();
                        for (int i = 0; i < asyncThreads.Count; i++)
                        {
                            Thread t = asyncThreads[i] as Thread;
                            if (t != null && t.IsAlive)
#if !NETCORE
                                t.Abort();
#elif NETCORE
                                t.Interrupt();
#endif
                        }
                        asyncThreads.Clear();
                    }
                    break;

                case Event.GET_STATE:
                    passUp(new Event(Event.GET_STATE_OK));
                    break;

                case Event.TMP_VIEW:
                case Event.VIEW_CHANGE:
                    ArrayList temp_mbrs = new ArrayList();
                    ArrayList nodeJoiningList = new ArrayList();
                    lock_members.AcquireWriterLock(Timeout.Infinite);
                    try
                    {
                        members.Clear();
                        ArrayList tmpvec = ((View)evt.Arg).Members;
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.down()", ((evt.Type == Event.TMP_VIEW) ? "TMP_VIEW" : "VIEW_CHANGE"));
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Tcp.down()", " View change members count" + tmpvec.Count);

                        Address address = null;

                        for (int i = 0; i < tmpvec.Count; i++)
                        {


                            address = tmpvec[i] as Address;
                            temp_mbrs.Add(tmpvec[i]);

                            members.Add(tmpvec[i]);
                            nodeJoiningList.Add(tmpvec[i]);
                        }
                    }
                    finally
                    {
                        lock_members.ReleaseWriterLock();
                    }

                    if (_asyncProcessor != null)
                        _asyncProcessor.Stop();

                    lock (async_mutex)
                    {
                        if (Stack.NCacheLog != null) Stack.NCacheLog.Flush();
                        for (int i = 0; i < asyncThreads.Count; i++)
                        {
                            Thread t = asyncThreads[i] as Thread;
                            if (t != null && t.IsAlive)
#if !NETCORE
                                t.Abort();
#elif NETCORE
                                t.Interrupt();
#endif
                        }
                        asyncThreads.Clear();
                    }

                    ct.ConfigureNodeRejoining(nodeJoiningList);
                    ArrayList failedNodes = ct.synchronzeMembership(temp_mbrs, false);
                    passUp(evt);

                    break;

                case Event.GET_LOCAL_ADDRESS:  
                    passUp(new Event(Event.SET_LOCAL_ADDRESS, local_addr));
                    break;
                case Event.CONNECT:
                    object[] addrs = ((object[])evt.Arg);
                    group_addr = (string)addrs[0];
                    subGroup_addr = (string)addrs[1];
                    bool twoPhaseConnect = (bool)addrs[2];
                    synchronizeConnections = !twoPhaseConnect;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TCP.HandleDownEvent()", " group_address is : " + group_addr);
                    // removed March 18 2003 (bela), not needed (handled by GMS)
                    // Can't remove it; otherwise TCPGOSSIP breaks (bela May 8 2003) !
                    Address addr = new Address(ct.srv_port);
                    passUp(new Event(Event.CONNECT_OK, (object)addr));
                    break;

                case Event.DISCONNECT:
                    passUp(new Event(Event.DISCONNECT_OK));
                    break;

                case Event.CONFIG:
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("received CONFIG event: " + evt.Arg);
                    handleConfigEvent((System.Collections.Hashtable)evt.Arg);
                    break;

                case Event.ACK:
                    passUp(new Event(Event.ACK_OK));
                    break;
            }
        }


        internal virtual void handleConfigEvent(System.Collections.Hashtable map)
        {
            if (map == null)
                return;
            if (map.ContainsKey("additional_data"))
            {
                additional_data = (byte[])map["additional_data"];
            }
        }

        internal bool IsMember(Address node)
        {
            if (node != null)
            {
                lock_members.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    if (members != null && members.Contains(node))
                        return true;

                }
                finally
                {
                    lock_members.ReleaseReaderLock();
                }
            }
            return false;
        }
        #region /                       --- AsnycUnicast  ---                   /

        /// <summary>
        /// Asynchronously sends the unicast message.
        /// </summary>
        internal class AsnycUnicast : AsyncProcessor.IAsyncTask
        {
            private TCP _parent;
            private Message _message;
            private bool _reEstablishConnection;

            public AsnycUnicast(TCP parent, Message m, bool reEstablish)
            {
                _parent = parent;
                _message = m;
                _reEstablishConnection = reEstablish;
            }
            #region IAsyncTask Members

            /// <summary>
            /// Sends the message.
            /// </summary>
            public void Process()
            {
                if (_parent != null)
                {
                    if (_parent.Stack.NCacheLog.IsInfoEnabled) _parent.Stack.NCacheLog.Info("TCP.AsnycUnicast.Process", "sending message to " + _message.Dest.ToString());
                    _parent.sendUnicastMessage(_message, _reEstablishConnection, _message.Payload, Priority.High);
                }
            }

            #endregion
        }

        #endregion

        #region /                       --- AsnycMulticast  ---                   /
        /// <summary>
        /// Asynchronously sends the multicast message.
        /// </summary>
        internal class AsnycMulticast : AsyncProcessor.IAsyncTask
        {
            private TCP _parent;
            private Message _message;
            private bool _reEstablishConnection;

            public AsnycMulticast(TCP parent, Message m, bool reEstablish)
            {
                _parent = parent;
                _message = m;
                _reEstablishConnection = reEstablish;
            }
            #region IAsyncTask Members

            /// <summary>
            /// broadcasts the message.
            /// </summary>
            public void Process()
            {
                if (_parent != null)
                {
                    if (_parent.Stack.NCacheLog.IsInfoEnabled) _parent.Stack.NCacheLog.Info("TCP.AsnycMulticast.Process", "broadcasting message");
                    _parent.sendMulticastMessage(_message, _reEstablishConnection, Priority.High);
                }
            }

            #endregion
        }

        #endregion

        #region /               --- Connection Keep Alive ---           /

        internal class ConnectionKeepAlive
        {
            TCP _enclosingInstance;
            ConnectionTable _ct;
            int _interval = 8000;
            Hashtable _idleConnections = new Hashtable();
            Thread _thread;
            Thread _statusCheckingThread;
            int _maxAttempts = 5;
            ArrayList _checkStatusList = new ArrayList();
            object _status_mutex = new object();
            object _statusReceived;
            Address _currentSuspect;
            int _statusTimeout = 45000;

            public ConnectionKeepAlive(TCP enclosingInsatnce, ConnectionTable connectionTable, int interval)
            {
                _enclosingInstance = enclosingInsatnce;
                _ct = connectionTable;
                _interval = interval;
            }

            public void Start()
            {
                if (_thread == null)
                {
                    _thread = new Thread(new ThreadStart(Run));
                    _thread.Name = "TCP.ConnectionKeepAlive";
                    _thread.Start();
                }
            }

            public void Stop()
            {
                if (_enclosingInstance.Stack.NCacheLog != null) _enclosingInstance.Stack.NCacheLog.Flush();
                if (_thread != null)
                {
#if !NETCORE
                    _thread.Abort();
#elif NETCORE
                    _thread.Interrupt();
#endif
                    _thread = null;
                }
                if (_statusCheckingThread != null)
                {
#if !NETCORE
                    _statusCheckingThread.Abort();
#elif NETCORE
                    _statusCheckingThread.Interrupt();
#endif
                    _statusCheckingThread = null;
                }
            }

            public void Run()
            {
                try
                {
                    ArrayList idleMembers;
                    ArrayList suspectedList = new ArrayList();
                    _ct.SetConnectionsStatus(true);
                    Thread.Sleep(_interval);

                    while (_thread != null)
                    {
                        idleMembers = _ct.GetIdleMembers();

                        if (idleMembers.Count > 0)
                        {
                            lock (_idleConnections.SyncRoot)
                            {
                                for (int i = 0; i < idleMembers.Count; i++)
                                {
                                    Address member = idleMembers[i] as Address;

                                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.Run", "pining idle member ->:" + member.ToString());

                                    if (!_idleConnections.Contains(member))
                                        _idleConnections.Add(idleMembers[i], (int)1);
                                    else
                                    {
                                        int attemptCount = (int)_idleConnections[member];
                                        attemptCount++;

                                        if (attemptCount > _maxAttempts)
                                        {
                                            _idleConnections.Remove(member);
                                            suspectedList.Add(member);

                                        }
                                        else
                                        {
                                            if (_enclosingInstance.Stack.NCacheLog.IsErrorEnabled) _enclosingInstance.Stack.NCacheLog.Error("ConnectionKeepAlive.Run", attemptCount + " did not received any heart beat ->:" + member.ToString());

                                            _idleConnections[member] = attemptCount;
                                        }

                                    }

                                }
                            }
                            AskHeartBeats(idleMembers);
                        }

                        if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.Run", "setting connections status to idle ->:");

                        _ct.SetConnectionsStatus(true);

                        foreach (Address suspected in suspectedList)
                        {
                            if (_enclosingInstance.Stack.NCacheLog.IsErrorEnabled) _enclosingInstance.Stack.NCacheLog.Error("ConnectionKeepAlive.Run", "member being suspected ->:" + suspected.ToString());

                            _ct.remove(suspected, true);
                            _enclosingInstance.connectionClosed(suspected);
                        }
                        suspectedList.Clear();

                        Thread.Sleep(_interval);
                    }

                }
                catch (ThreadAbortException) { }
                catch (ThreadInterruptedException) { }
                catch (Exception e)
                {
                    _enclosingInstance.Stack.NCacheLog.Error("ConnectionKeepAlive.Run", e.ToString());
                }
                if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.Run", "exiting keep alive thread");
            }

            /// <summary>
            /// Cheks the status of a node in a separate thread.
            /// </summary>
            /// <param name="node"></param>
            public void CheckStatus(Address node)
            {
                lock (_checkStatusList.SyncRoot)
                {
                    _checkStatusList.Add(node);
                    if (_statusCheckingThread == null)
                    {
                        _statusCheckingThread = new Thread(new ThreadStart(CheckStatus));
                        _statusCheckingThread.Name = "ConnectioKeepAlive.CheckStatus";
                        _statusCheckingThread.IsBackground = true;
                        _statusCheckingThread.Start();
                    }
                }
            }

            /// <summary>
            /// Checks the status of a node whether he is running or not. We send a status request
            /// message and wait for the response for a particular timeout. If the node is alive
            /// it sends backs its status otherwise timeout occurs and we consider hime DEAD.
            /// </summary>
            private void CheckStatus()
            {
                while (_statusCheckingThread != null)
                {
                    lock (_checkStatusList.SyncRoot)
                    {
                        if (_checkStatusList.Count > 0)
                        {
                            _currentSuspect = _checkStatusList[0] as Address;
                            _checkStatusList.Remove(_currentSuspect);
                        }
                        else
                            _currentSuspect = null;

                        if (_currentSuspect == null)
                        {
                            _statusCheckingThread = null;
                            continue;
                        }
                    }

                    lock (_status_mutex)
                    {
                        try
                        {
                            NodeStatus nodeStatus = null;
                            if (_enclosingInstance.ct.ConnectionExist(_currentSuspect))
                            {
                                Message msg = new Message(_currentSuspect, null, new byte[0]);
                                msg.putHeader(HeaderType.KEEP_ALIVE, new HearBeat(HearBeat.ARE_YOU_ALIVE));

                                if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "sending status request to " + _currentSuspect);


                                _enclosingInstance.sendUnicastMessage(msg, false, msg.Payload, Priority.High);
                                _statusReceived = null;

                                //wait for the result or timeout occurs first;
                                Monitor.Wait(_status_mutex, _statusTimeout);

                                if (_statusReceived != null)
                                {
                                    HearBeat status = _statusReceived as HearBeat;

                                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "received status " + status + " from " + _currentSuspect);

                                    if (status.Type == HearBeat.I_AM_NOT_DEAD)
                                        nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_ALIVE);
                                    else if (status.Type == HearBeat.I_AM_LEAVING)
                                        nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_LEAVING);
                                    else if (status.Type == HearBeat.I_AM_STARTING)
                                        nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_DEAD);
                                }
                                else
                                {
                                    nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_DEAD);
                                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "did not receive status from " + _currentSuspect + "; consider him DEAD");
                                }
                            }
                            else
                            {
                                if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "no connection exists for " + _currentSuspect);
                                nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_DEAD);
                            }

                            Event statusEvent = new Event(Event.GET_NODE_STATUS_OK, nodeStatus);
                            _enclosingInstance.passUp(statusEvent);
                        }
                        catch (Exception e)
                        {
                            _enclosingInstance.Stack.NCacheLog.Error("ConnectionKeepAlive.CheckStatus", e.ToString());
                        }
                        finally
                        {
                            _currentSuspect = null;
                            _statusReceived = null;
                        }
                    }
                }
            }
            private void AskHeartBeats(ArrayList idleMembers)
            {
                Message msg = new Message(null, null, new byte[0]);
                msg.putHeader(HeaderType.KEEP_ALIVE, new HearBeat(HearBeat.SEND_HEART_BEAT));
                msg.Dests = idleMembers.Clone() as ArrayList;
                _enclosingInstance.sendMulticastMessage(msg, false, Priority.High);
            }

            private bool CheckConnected(Address member)
            {
                bool isConnected = false;
                ConnectionTable ct = _enclosingInstance.ct;
                ConnectionTable.Connection con;
                con = ct.GetPrimaryConnection(member, false);
                if (con != null)
                {
                    isConnected = con.IsConnected;
                }
                return isConnected;
            }

            public void ReceivedHeartBeat(Address sender, HearBeat hrtBeat)
            {
                Message rspMsg;
                switch (hrtBeat.Type)
                {

                    case HearBeat.HEART_BEAT:
                        if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "received heartbeat from ->:" + sender.ToString());

                        lock (_idleConnections.SyncRoot)
                        {
                            _idleConnections.Remove(sender);
                        }
                        break;

                    case HearBeat.SEND_HEART_BEAT:

                        rspMsg = new Message(sender, null, new byte[0]);
                        rspMsg.putHeader(HeaderType.KEEP_ALIVE, new HearBeat(HearBeat.HEART_BEAT));
                        if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "seding heartbeat to ->:" + sender.ToString());

                        _enclosingInstance.sendUnicastMessage(rspMsg, false, rspMsg.Payload, Priority.High);
                        break;

                    case HearBeat.ARE_YOU_ALIVE:

                        rspMsg = new Message(sender, null, new byte[0]);
                        HearBeat rsphrtBeat = (_enclosingInstance.isClosing || _enclosingInstance._leaving) ? new HearBeat(HearBeat.I_AM_LEAVING) : new HearBeat(HearBeat.I_AM_NOT_DEAD);
                        rsphrtBeat = _enclosingInstance.isStarting ? new HearBeat(HearBeat.I_AM_STARTING) : rsphrtBeat;
                        rspMsg.putHeader(HeaderType.KEEP_ALIVE, rsphrtBeat);
                        if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "seding status" + rsphrtBeat + " to ->:" + sender.ToString());

                        _enclosingInstance.sendUnicastMessage(rspMsg, false, rspMsg.Payload, Priority.High);
                        break;

                    case HearBeat.I_AM_STARTING:
                    case HearBeat.I_AM_LEAVING:
                    case HearBeat.I_AM_NOT_DEAD:

                        lock (_status_mutex)
                        {
                            if (_currentSuspect != null && _currentSuspect.Equals(sender))
                            {
                                _statusReceived = hrtBeat;
                                Monitor.Pulse(_status_mutex);
                            }
                        }
                        break;
                }
            }
        }

        public class HearBeat : Header, ICompactSerializable
        {
            byte _type;

            public const byte SEND_HEART_BEAT = 1;
            public const byte HEART_BEAT = 2;
            public const byte ARE_YOU_ALIVE = 3;
            public const byte I_AM_NOT_DEAD = 4;
            public const byte I_AM_LEAVING = 5;
            public const byte I_AM_STARTING = 6;

            public HearBeat() { }
            public HearBeat(byte type)
            {
                _type = type;
            }

            public byte Type
            {
                get { return _type; }
                set { _type = value; }
            }

            #region ICompactSerializable Members

            public void Deserialize(CompactReader reader)
            {
                _type = reader.ReadByte();
            }

            public void Serialize(CompactWriter writer)
            {
                writer.Write(_type);
            }

            #endregion

            public override string ToString()
            {
                string toString = "NA";
                switch (_type)
                {
                    case SEND_HEART_BEAT:
                        toString = "SEND_HEART_BEAT";
                        break;

                    case HEART_BEAT:
                        toString = "HEART_BEAT";
                        break;

                    case ARE_YOU_ALIVE:
                        toString = "ARE_YOU_ALIVE";
                        break;

                    case I_AM_NOT_DEAD:
                        toString = "I_AM_NOT_DEAD";
                        break;

                    case I_AM_LEAVING:
                        toString = "I_AM_LEAVING";
                        break;

                    case I_AM_STARTING:
                        toString = "I_AM_STARTING";
                        break;

                }
                return toString;
            }
        }


        #endregion

    }

}
