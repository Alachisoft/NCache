// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $
using System;
using System.IO;
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NGroups.Stack;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> Implements the total ordering layer using a message sequencer
    /// <p>
    /// 
    /// The protocol guarantees that all bcast sent messages will be delivered in
    /// the same order to all members. For that it uses a sequencer which assignes
    /// monotonically increasing sequence ID to broadcasts. Then all group members
    /// deliver the bcasts in ascending sequence ID order.
    /// <p>
    /// <ul>
    /// <li>
    /// When a bcast message comes down to this layer, it is placed in the pending
    /// down queue. A bcast request is sent to the sequencer.</li>
    /// <li>
    /// When the sequencer receives a bcast request, it creates a bcast reply
    /// message and assigns to it a monotonically increasing seqID and sends it back
    /// to the source of the bcast request.</li>
    /// <li>
    /// When a broadcast reply is received, the corresponding bcast message is
    /// assigned the received seqID. Then it is broadcasted.</li>
    /// <li>
    /// Received bcasts are placed in the up queue. The queue is sorted according
    /// to the seqID of the bcast. Any message at the head of the up queue with a
    /// seqID equal to the next expected seqID is delivered to the layer above.</li>
    /// <li>
    /// Unicast messages coming from the layer below are forwarded above.</li>
    /// <li>
    /// Unicast messages coming from the layer above are forwarded below.</li>
    /// </ul>
    /// <p>
    /// <i>Please note that once a <code>BLOCK_OK</code> is acknowledged messages
    /// coming from above are discarded!</i> Either the application must stop
    /// sending messages when a <code>BLOCK</code> event is received from the
    /// channel or a QUEUE layer should be placed above this one. Received messages
    /// are still delivered above though.
    /// <p>
    /// bcast requests are retransmitted periodically until a bcast reply is
    /// received. In case a BCAST_REP is on its way during a BCAST_REQ
    /// retransmission, then the next BCAST_REP will be to a non-existing
    /// BCAST_REQ. So, a nulll BCAST message is sent to fill the created gap in
    /// the seqID of all members.
    /// 
    /// </summary>
    /// <author>  i.georgiadis@doc.ic.ac.uk
    /// </author>
    internal class TOTAL : Protocol
    {
        /// <summary> The header processed by the TOTAL layer and intended for TOTAL
        /// inter-stack communication
        /// </summary>
        [Serializable]
        internal class HDR : Header, ICompactSerializable, IRentableObject
        {
            // HDR types
            /// <summary>Null value for the tag </summary>
            public const byte NULL_TYPE = 0;
            /// <summary>Request to broadcast by the source </summary>
            public const byte REQ = 1;
            /// <summary>Reply to broadcast request. </summary>
            public const byte REP = 2;
            /// <summary>Unicast message </summary>
            public const byte UCAST = 3;
            /// <summary>Broadcast Message </summary>
            public const byte BCAST = 4;
            /// <summary>Multicast Message</summary>
            public const byte MCAST = 5;
            /// <summary>Request to multicast by the source.</summary>
            public const byte REQMCAST = 6;
            /// <summary>Reply to a multicast request.</summary>
            public const byte REPMCAST = 7;

            public int rentId;
            /// <summary>The header's type tag </summary>
            public byte type;
            /// <summary> The ID used by the message source to match replies from the
            /// sequencer
            /// </summary>
            public long localSeqID;

            /// <summary>The ID imposing the total order of messages </summary>
            public long seqID;

            public int viewId;

            /// <summary> used for externalization</summary>
            public HDR()
            {
            }
            /// <summary> Create a header for the TOTAL layer</summary>
            /// <param name="type">the header's type
            /// </param>
            /// <param name="localSeqID">the ID used by the sender of broadcasts to match
            /// requests with replies from the sequencer
            /// </param>
            /// <param name="seqID">the ID imposing the total order of messages
            /// 
            /// </param>
            /// <throws>  IllegalArgumentException if the provided header type is unknown</throws>
            /// <summary>
            /// </summary>
            public HDR(byte type, long localSeqID, long seqID, int viewId)
                : base()
            {
                switch (type)
                {
                    case REQ:
                    case REP:
                    case UCAST:
                    case BCAST:
                    case MCAST:
                    case REQMCAST:
                    case REPMCAST: this.type = type; break;

                    default:
                        this.type = NULL_TYPE;
                        throw new System.ArgumentException("Invalid header type.");
                }
                this.localSeqID = localSeqID;
                this.seqID = seqID;
                this.viewId = viewId;
            }
            public override object Clone(ObjectProvider provider)
            {
                HDR hdr = null;
                if (provider != null)
                    hdr = (HDR)provider.RentAnObject();
                else
                    hdr = new HDR();
                hdr.type = this.type;
                hdr.seqID = seqID;
                hdr.localSeqID = localSeqID;
                hdr.viewId = viewId;
                return hdr;
            }
            /// <summary> For debugging purposes</summary>
            public override string ToString()
            {
                System.Text.StringBuilder buffer = new System.Text.StringBuilder();
                string typeName;
                buffer.Append("[TOTAL.HDR");
                switch (type)
                {
                    case REQ: typeName = "REQ"; break;
                    case REQMCAST: typeName = "REQMCAST"; break;
                    case REP: typeName = "REP"; break;
                    case REPMCAST: typeName = "REPMCAST"; break;
                    case UCAST: typeName = "UCAST"; break;
                    case BCAST: typeName = "BCAST"; break;
                    case MCAST: typeName = "MCAST"; break;
                    case NULL_TYPE: typeName = "NULL_TYPE"; break;
                    default: typeName = ""; break;
                }
                buffer.Append(", type=" + typeName);
                buffer.Append(", " + "localID=" + localSeqID);
                buffer.Append(", " + "seqID=" + seqID);
                buffer.Append(", " + "viewId=" + viewId);
                buffer.Append(']');

                return (buffer.ToString());
            }

            public void Reset()
            {
                seqID = localSeqID = NULL_ID;
                type = NULL_TYPE;
            }
            #region ICompactSerializable Members

            public void Deserialize(CompactReader reader)
            {
                type = reader.ReadByte();
                localSeqID = reader.ReadInt64();
                seqID = reader.ReadInt64();
                viewId = reader.ReadInt32();
            }

            public void Serialize(CompactWriter writer)
            {
                writer.Write(type);
                writer.Write(localSeqID);
                writer.Write(seqID);
                writer.Write(viewId);
            }
            #endregion

            public static HDR ReadTotalHeader(CompactReader reader)
            {
                byte isNull = reader.ReadByte();
                if (isNull == 1)
                    return null;
                HDR newHdr = new HDR();
                newHdr.Deserialize(reader);
                return newHdr;
            }

            public static void WriteTotalHeader(CompactWriter writer, HDR hdr)
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

            #region IRentableObject Members

            public int RentId
            {
                get
                {
                    return rentId;
                }
                set
                {
                    rentId = value;
                }
            }

            #endregion

            #region ICustomSerializable Members

            public void DeserializeLocal(BinaryReader reader)
            {
                type = reader.ReadByte();
                localSeqID = reader.ReadInt64();
                seqID = reader.ReadInt64();
                viewId = reader.ReadInt32();
            }

            public void SerializeLocal(BinaryWriter writer)
            {
                writer.Write(type);
                writer.Write(localSeqID);
                writer.Write(seqID);
                writer.Write(viewId);
            }

            #endregion
        }


        /// <summary> The retransmission listener - It is called by the
        /// <code>AckSenderWindow</code> when a retransmission should occur
        /// </summary>
        private class Command : AckSenderWindow.RetransmitCommand
        {
            private void InitBlock(TOTAL enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private TOTAL enclosingInstance;
            public TOTAL Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            public Command(TOTAL enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            public virtual void retransmit(long seqNo, Message msg)
            {
                Enclosing_Instance._retransmitBcastRequest(seqNo);
            }
        }

        /// <summary> The retransmission listener - It is called by the
        /// <code>AckSenderWindow</code> when a retransmission should occur
        /// </summary>
        private class MCastCommand : AckSenderWindow.RetransmitCommand
        {
            private void InitBlock(TOTAL enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private TOTAL enclosingInstance;
            public TOTAL Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            public MCastCommand(TOTAL enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            public virtual void retransmit(long seqNo, Message msg)
            {
                ArrayList mbrs = msg.Dests.Clone() as ArrayList;

                string subGroupID = Enclosing_Instance._mbrsSubgroupMap[mbrs[0]] as string;

                ArrayList groupMbrs = (ArrayList)Enclosing_Instance._sequencerTbl[subGroupID] as ArrayList;
                Address groupSequencerAddr = groupMbrs[0] as Address;
                if (groupSequencerAddr != null)
                    Enclosing_Instance._retransmitMcastRequest(seqNo, groupSequencerAddr);
            }
        }


        /// <summary>Protocol name </summary>
        private const string PROT_NAME = "TOTAL";
        /// <summary>Property names </summary>
        private const string TRACE_PROP = "trace";

        /// <summary>Average time between broadcast request retransmissions </summary>
        /// private long[] AVG_RETRANSMIT_INTERVAL = new long[]{1000, 2000, 3000, 4000};
        private long[] AVG_RETRANSMIT_INTERVAL = new long[] { 55000, 65000, 70000, 75000 };

        /// <summary>Average time between broadcast request retransmissions </summary>
        private long[] AVG_MCAST_RETRANSMIT_INTERVAL = new long[] { 60000, 65000, 70000, 75000 };

        /// <summary>Null value for the IDs </summary>
        private const long NULL_ID = -1;
        // Layer sending states
        /// <summary>No group has been joined yet </summary>
        private const int NULL_STATE = -1;
        /// <summary>When set, all messages are sent/received </summary>
        private const int RUN = 0;
        /// <summary> When set, only session-specific messages are sent/received, i.e. only
        /// messages essential to the session's integrity
        /// </summary>
        private const int FLUSH = 1;
        /// <summary>No message is sent to the layer below </summary>
        private const int BLOCK = 2;


        /// <summary>The state lock allowing multiple reads or a single write </summary>
        private ReaderWriterLock stateLock = new ReaderWriterLock();
        /// <summary>Protocol layer message-sending state </summary>
        private int state = NULL_STATE;
        /// <summary>The address of this stack </summary>
        private Address addr = null;
        /// <summary>The address of the sequencer </summary>
        private Address sequencerAddr = null;

        /// <summary> The sequencer's seq ID. The ID of the most recently broadcast reply
        /// message
        /// </summary>
        private long sequencerSeqID = NULL_ID;

        /// <summary> Mutex to make sequenceID generation atomic </summary>
        private readonly object seqIDMutex = new object();
        /// <summary> Mutex to make sequenceID generation atomic </summary>
        private readonly object localSeqIDMutex = new object();

        /// <summary> Mutex to make sequenceID generation atomic </summary>
        private readonly object localMcastSeqIDMutex = new object();
        /// <summary> The local sequence ID, i.e. the ID sent with the last broadcast request
        /// message. This is increased with every broadcast request sent to the
        /// sequencer and it's used to match the requests with the sequencer's
        /// replies
        /// </summary>
        private long localSeqID = NULL_ID;

        /// <summary> The total order sequence ID. This is the ID of the most recently
        /// delivered broadcast message. As the sequence IDs are increasing without
        /// gaps, this is used to detect missing broadcast messages
        /// </summary>
        private long seqID = NULL_ID;

        //==========================================
        private long _mcastSequencerSeqID = NULL_ID;
        private long _mcastLocalSeqID = NULL_ID;
        private long _mcastSeqID = NULL_ID;
        private Hashtable _sequencerTbl = new Hashtable();
        private Hashtable _mbrsSubgroupMap;
        private Hashtable _mcastUpTbl;
        private Hashtable _mcastReqTbl;
        private string subGroup_addr = null;
        private readonly object _mcastSeqIDMutex = new object();
        private Address _groupSequencerAddr = null;
        private int _groupMbrsCount = 0; //indicator of group mbrs change.
        private AckSenderWindow _mcastRetransmitter;
        private ArrayList _undeliveredMessages = ArrayList.Synchronized(new ArrayList());
        //=============================================

        /// <summary> The list of unanswered broadcast requests to the sequencer. The entries
        /// are stored in increasing local sequence ID, i.e. in the order they were
        /// 
        /// sent localSeqID -> Broadcast msg to be sent.
        /// </summary>
        private Hashtable reqTbl;
        /// <summary>
        /// it allows the sequencer itself to get next sequence directly.
        /// </summary>
        //private bool shortcut = true;
        /// <summary> The list of received broadcast messages that haven't yet been delivered
        /// to the layer above. The entries are stored in increasing sequence ID,
        /// i.e. in the order they must be delivered above
        /// 
        /// seqID -> Received broadcast msg
        /// </summary>
        private Hashtable upTbl;

        /// <summary>Retranmitter for pending broadcast requests </summary>
        private AckSenderWindow retransmitter;

        /// <summary> Used to shortcircuit transactional messages from management messages. </summary>
        private Protocol transport;

        private ReaderWriterLock request_lock = new ReaderWriterLock();

        long start_time = 0;

        long start_time_bcast = 0;

        /// <summary>used for monitoring</summary>
        HPTimeStats _timeToTakeBCastSeq = new HPTimeStats();
        HPTimeStats _timeToTakeMCastSeq = new HPTimeStats();
        HPTimeStats _totalWaitTime = new HPTimeStats();

        /// <summary>operation timeout as specified by the user. we wait for te missing message for this timeout</summary>
        private long opTimeout = 60000;

        private ViewId currentViewId;

        /// <summary> Print addresses in host_ip:port form to bypass DNS</summary>
        private string _addrToString(object addr)
        {
            return (addr == null ? "<null>" : ((addr is Address) ? (((Address)addr).IpAddress.ToString() + ':' + ((Address)addr).Port) : addr.ToString()));
        }

        override public string Name { get { return PROT_NAME; } }

        /// <summary>
        /// Returns the next squence number.
        /// </summary>
        private long NextSequenceID
        {
            get
            {
                lock (seqIDMutex) { return ++sequencerSeqID; }
            }
        }

        /// <summary>
        /// Returns the next mcast sequence number.
        /// </summary>
        private long NextMCastSeqID
        {
            get
            {
                lock (_mcastSeqIDMutex) { return ++_mcastSequencerSeqID; }
            }
        }

        /// <summary> Configure the protocol based on the given list of properties
        /// 
        /// </summary>
        /// <param name="properties">the list of properties to use to setup this layer
        /// </param>
        /// <returns> false if there was any unrecognized property or a property with
        /// an invalid value
        /// </returns>
        private bool _setProperties(Hashtable properties)
        {
            base.setProperties(properties);

            if (stack.StackType == ProtocolStackType.TCP)
            {
                this.up_thread = false;
                this.down_thread = false;
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(Name + ".setProperties", "part of TCP stack");
            }

            if (properties.Contains("timeout"))
            {
                long[] tmp = Util.Util.parseCommaDelimitedLongs(properties["timeout"] as string);
                properties.Remove("timeout");
                if (tmp != null && tmp.Length > 0)
                {
                    AVG_RETRANSMIT_INTERVAL = tmp;
                }
            }

            if (properties.Contains("op_timeout"))
            {
                long val = Convert.ToInt64(properties["op_timeout"]);
                opTimeout = val;
                properties.Remove("op_timeout");
            }

            if (properties.Count > 0)
            {
                Stack.NCacheLog.Error("The following properties are not " + "recognized: " + Global.CollectionToString(properties.Keys));
                return (true);
            }
            return (true);
        }

        /// <summary> Events that some layer below must handle
        /// 
        /// </summary>
        /// <returns> the set of <code>Event</code>s that must be handled by some layer
        /// below
        /// </returns>
        internal virtual ArrayList _requiredDownServices()
        {
            ArrayList services = ArrayList.Synchronized(new ArrayList(10));

            return (services);
        }
        /// <summary> Events that some layer above must handle
        /// 
        /// </summary>
        /// <returns> the set of <code>Event</code>s that must be handled by some
        /// layer above
        /// </returns>
        internal virtual ArrayList _requiredUpServices()
        {
            ArrayList services = ArrayList.Synchronized(new ArrayList(10));

            return (services);
        }


        /// <summary> Extract as many messages as possible from the pending up queue and send
        /// them to the layer above
        /// </summary>
        private void _deliverBcast()
        {
            long time_left = opTimeout;
            do
            {
                Message msg = null;
                lock (upTbl.SyncRoot)
                {
                    _msgArrived++;

                    msg = (Message)upTbl[(long)(seqID + 1)];
                    if (upTbl.Count > 0)
                    {
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverBCast()", "UP table [" + upTbl.Count + "]");
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverBCast()", "seq: " + (seqID + 1));
                    }
                    if (msg == null)
                    {
                        if (upTbl.Count > 0)
                        {
                            if (start_time_bcast == 0)
                                start_time_bcast = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;

                            time_left = opTimeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time_bcast);
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverBCast()", "timeout[" + (seqID + 1) + "] ->" + time_left);
                        }

                        if (time_left <= 0)
                        {
                            if (Stack.NCacheLog.IsErrorEnabled) Stack.NCacheLog.Error("Total._deliverBCast()", "bypassed a missing message " + (seqID + 1) + " timeout :" + time_left);
                            if (Stack.NCacheLog.IsErrorEnabled) Stack.NCacheLog.Error("Total._deliverBCast()", "arrived msgs " + _msgArrived + " passed :" + _msgAfterReset);
                            ++seqID;
                            start_time_bcast = 0;
                            time_left = opTimeout;
                            continue;
                        }
                        break;
                    }
                    time_left = opTimeout;
                    start_time_bcast = 0;
                    upTbl.Remove((long)(seqID + 1));
                    _msgAfterReset++;
                    ++seqID;
                }
                HDR header = (HDR)msg.removeHeader(HeaderType.TOTAL);

                if (header.localSeqID != NULL_ID)
                {

                    if (enableMonitoring)
                    {
                        stack.perfStatsColl.IncrementBcastQueueCountStats(upTbl.Count);
                    }

                    passUp(new Event(Event.MSG, msg));
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._deliverBcast()", seqID.ToString() + " hdr = " + Global.CollectionToString(msg.Headers));
                }

            } while (true);
        }

        /// <summary> Extract as many messages as possible from the pending up queue and send
        /// them to the layer above
        /// </summary>
        private void _deliverMcast()
        {
            long time_left = opTimeout;
            do
            {
                Message msg = null;
                lock (_mcastUpTbl.SyncRoot)
                {
                    msg = (Message)_mcastUpTbl[(long)(_mcastSeqID + 1)];
                    if (_mcastUpTbl.Count > 0)
                    {
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverMcast()", "UP table [" + _mcastUpTbl.Count + "]");
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverMcast()", "mcast_seq: " + (_mcastSeqID + 1));
                    }

                    if (msg == null)
                    {
                        if (_mcastUpTbl.Count > 0)
                        {

                            if (start_time == 0)
                                start_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;

                            time_left = opTimeout - ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start_time);
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._deliverMcast()", "timeout[" + (_mcastSeqID + 1) + "] ->" + time_left);
                        }

                        if (time_left <= 0)
                        {
                            if (Stack.NCacheLog.IsErrorEnabled) Stack.NCacheLog.Error("Total._deliverMcast()", "bypassed a missing message " + (_mcastSeqID + 1));
                            ++_mcastSeqID;
                            start_time = 0;
                            time_left = opTimeout;

                            continue;
                        }
                        break;
                    }
                    start_time = 0;
                    time_left = opTimeout;

                    _mcastUpTbl.Remove((long)(_mcastSeqID + 1));
                    ++_mcastSeqID;
                }
                HDR header = (HDR)msg.removeHeader(HeaderType.TOTAL);

                if (header.localSeqID != NULL_ID)
                {

                    if (enableMonitoring)
                    {
                        stack.perfStatsColl.IncrementMcastQueueCountStats(_mcastUpTbl.Count);
                    }

                    passUp(new Event(Event.MSG, msg));
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._deliverMcast()", _mcastSeqID.ToString() + " hdr = " + Global.CollectionToString(msg.Headers));
                }

            } while (true);
        }


        /// <summary> Add all undelivered bcasts sent by this member in the req queue and then
        /// replay this queue
        /// </summary>
        private void _replayBcast()
        {
            Message msg;
            HDR header;

            // i. Remove all undelivered bcasts sent by this member and place them
            // again in the pending bcast req queue

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._replayBcast()", "upTabl size = " + upTbl.Count.ToString());
            lock (upTbl.SyncRoot)
            {
                if (upTbl.Count > 0)
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Replaying undelivered bcasts");

                IDictionaryEnumerator it = upTbl.GetEnumerator();
                while (it.MoveNext())
                {
                    msg = (Message)it.Value;
                    if (!msg.Src.Equals(addr))
                    {
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("During replay: " + "discarding BCAST[" + ((TOTAL.HDR)msg.getHeader(HeaderType.TOTAL)).seqID + "] from " + _addrToString(msg.Src));
                        continue;
                    }

                    header = (HDR)msg.removeHeader(HeaderType.TOTAL);
                    if (header.localSeqID == NULL_ID)
                        continue;
                    _sendBcastRequest(msg, header.localSeqID);
                }
                start_time_bcast = 0;
                upTbl.Clear();
            } 
        }

        /// <summary> Add all undelivered mcasts sent by this member in the req queue and then
        /// replay this queue
        /// </summary>
        private void _replayMcast()
        {
            Message msg;
            HDR header;

            // i. Remove all undelivered bcasts sent by this member and place them
            // again in the pending bcast req queue

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._replayBcast()", "upTabl size = " + _mcastUpTbl.Count.ToString());
            lock (_mcastUpTbl.SyncRoot)
            {
                start_time = 0;
                _mcastUpTbl.Clear();
            } 
        }


        /// <summary> Send a unicast message: Add a <code>UCAST</code> header
        /// 
        /// </summary>
        /// <param name="msg">the message to unicast
        /// </param>
        /// <returns> the message to send
        /// </returns>
        private Message _sendUcast(Message msg)
        {
            msg.putHeader(HeaderType.TOTAL, new HDR(HDR.UCAST, NULL_ID, NULL_ID, -1));
            return (msg);
        }

        /// <summary> Receive a unicast message: Remove the <code>UCAST</code> header
        /// 
        /// </summary>
        /// <param name="msg">the received unicast message
        /// </param>
        private void _recvUcast(Message msg)
        {
            msg.removeHeader(HeaderType.TOTAL);
        }



        /// <summary> Replace the original message with a broadcast request sent to the
        /// sequencer. The original bcast message is stored locally until a reply to
        /// bcast is received from the sequencer. This function has the side-effect
        /// of increasing the <code>localSeqID</code>
        /// 
        /// </summary>
        /// <param name="msg">the message to broadcast
        /// </param>
        private void _sendBcastRequest(Message msg)
        {
            long seqId = -1;
            lock (localSeqIDMutex) { seqId = ++localSeqID; }

            _sendBcastRequest(msg, seqId);
        }

        private void _sendMcastRequest(Message msg)
        {
            long seqId = -1;
            lock (localMcastSeqIDMutex) { seqId = ++_mcastLocalSeqID; }

            _sendMcastRequest(msg, seqId);
        }

        private Address getGroupSequencer(ArrayList dests)
        {
            Address groupSequencerAddr = null;
            stateLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (dests != null)
                {
                    for (int i = 0; i < dests.Count; i++)
                    {
                        string subGroupID = this._mbrsSubgroupMap[dests[i]] as string;

                        if (subGroupID == null) continue; //probably this member has left and view has been changed.

                        ArrayList groupMbrs = (ArrayList)this._sequencerTbl[subGroupID] as ArrayList;
                        return groupSequencerAddr = groupMbrs[0] as Address;
                    }
                }
            }
            finally
            {
                stateLock.ReleaseReaderLock();
            }
            return groupSequencerAddr;
        }
        private void _sendMcastRequest(Message msg, long id)
        {
            // i. Store away the message while waiting for the sequencer's reply
            // ii. Send a mcast request immediatelly and also schedule a
            // retransmission
            Address groupSequencerAddr = addr;
            ArrayList dests = msg.Dests;


            groupSequencerAddr = getGroupSequencer(dests);

            if (groupSequencerAddr == null) return;

            if (addr.CompareTo(groupSequencerAddr) == 0)
            {
                long seqid = NextMCastSeqID;

                int viewId = -1;
                try
                {
                    stateLock.AcquireReaderLock(Timeout.Infinite);
                    viewId = (int)(currentViewId != null ? currentViewId.Id : -1);
                }
                finally
                {
                    stateLock.ReleaseReaderLock();
                }
                //Rent the event
                Event evt = null;
                evt = new Event();
                evt.Type = Event.MSG;
                evt.Priority = msg.Priority;
                evt.Arg = msg;

                //Rent the header
                HDR hdr = new HDR();
                hdr.type = HDR.MCAST;
                hdr.localSeqID = id;
                hdr.seqID = seqid;
                hdr.viewId = viewId;
                msg.Type = MsgType.SEQUENCED;

                msg.putHeader(HeaderType.TOTAL, hdr);

                //===================================================
                //now the message will contain a list of addrs in case of multicast.
                //=======================================================

                passDown(evt);
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._sendMcastRequest()", "shortcut mcast seq# " + seqid);
                return;
            }
            //lock (reqTbl.SyncRoot)
            request_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _mcastReqTbl[(long)id] = msg;
            }
            finally
            {
                request_lock.ReleaseWriterLock();
            }
            _transmitMcastRequest(id, groupSequencerAddr);
            _mcastRetransmitter.add(id, msg);
        }


        /// <summary> Replace the original message with a broadcast request sent to the
        /// sequencer. The original bcast message is stored locally until a reply
        /// to bcast is received from the sequencer
        /// 
        /// </summary>
        /// <param name="msg">the message to broadcast
        /// </param>
        /// <param name="id">the local sequence ID to use
        /// </param>
        private void _sendBcastRequest(Message msg, long id)
        {

            // i. Store away the message while waiting for the sequencer's reply
            // ii. Send a bcast request immediatelly and also schedule a
            // retransmission
            msg.Dest = null;  
            if (addr.CompareTo(this.sequencerAddr) == 0)
            {
                long seqid = NextSequenceID;
                int viewId = -1;
                try
                {
                    stateLock.AcquireReaderLock(Timeout.Infinite);
                    viewId = (int)(currentViewId != null ? currentViewId.Id : -1);
                }
                finally
                {
                    stateLock.ReleaseReaderLock();
                }
                //Rent the event
                Event evt = null;
                evt = new Event();
                evt.Type = Event.MSG;
                evt.Priority = msg.Priority;
                evt.Arg = msg;

                //Rent the header
                HDR hdr = new HDR();
                hdr.type = HDR.BCAST;
                hdr.localSeqID = id;
                hdr.seqID = seqid;
                hdr.viewId = viewId;
                msg.putHeader(HeaderType.TOTAL, hdr);

                msg.Dest = null;
                msg.Type = MsgType.SEQUENCED;
                passDown(evt);
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._sendBcastRequest()", "shortcut bcast seq# " + seqid);
                return;
            }
            request_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                reqTbl[(long)id] = msg;
            }
            finally
            {
                request_lock.ReleaseWriterLock();
            }
            _transmitBcastRequest(id);
            retransmitter.add(id, msg);
        }


        /// <summary> Send the bcast request with the given localSeqID
        /// 
        /// </summary>
        /// <param name="seqID">the local sequence id of the
        /// </param>
        private void _transmitBcastRequest(long seqID)
        {
            Message reqMsg;

            // i. If NULL_STATE, then ignore, just transient state before
            // shutting down the retransmission thread
            // ii. If blocked, be patient - reschedule
            // iii. If the request is not pending any more, acknowledge it
            // iv. Create a broadcast request and send it to the sequencer

            if (state == NULL_STATE)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Transmit BCAST_REQ[" + seqID + "] in NULL_STATE");
                return;
            }
            if (state == BLOCK)
                return;

            request_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!reqTbl.Contains((long)seqID))
                {
                    retransmitter.ack(seqID);
                    return;
                }
            }
            finally
            {
                request_lock.ReleaseReaderLock();
            }

            //Rent the message
            reqMsg = new Message();
            reqMsg.Dest = sequencerAddr;
            reqMsg.Src = addr;
            reqMsg.setBuffer(new byte[0]);

            //Rent the header
            HDR hdr = new HDR();
            hdr.type = HDR.REQ;
            hdr.localSeqID = seqID;
            hdr.seqID = NULL_ID;

            reqMsg.putHeader(HeaderType.TOTAL, hdr);
            reqMsg.IsUserMsg = true;
            reqMsg.Type = MsgType.TOKEN_SEEKING;
            //rent the event
            Event evt = new Event();
            evt.Type = Event.MSG;
            evt.Arg = reqMsg;

            passDown(evt);
        }

        /// <summary> Send the mcast request with the given localSeqID
        /// 
        /// </summary>
        /// <param name="seqID">the local sequence id of the
        /// </param>
        private void _transmitMcastRequest(long seqID, Address groupSequencerAddr)
        {
            Message reqMsg;

            // i. If NULL_STATE, then ignore, just transient state before
            // shutting down the retransmission thread
            // ii. If blocked, be patient - reschedule
            // iii. If the request is not pending any more, acknowledge it
            // iv. Create a broadcast request and send it to the sequencer

            if (state == NULL_STATE)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Transmit MCAST_REQ[" + seqID + "] in NULL_STATE");
                return;
            }
            if (state == BLOCK)
                return;

            request_lock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!_mcastReqTbl.Contains((long)seqID))
                {
                    _mcastRetransmitter.ack(seqID);
                    return;
                }
            }
            finally
            {
                request_lock.ReleaseReaderLock();
            }

            reqMsg = new Message();
            reqMsg.Dest = groupSequencerAddr;
            reqMsg.Src = addr;
            reqMsg.setBuffer(new byte[0]);

            HDR hdr = new HDR();
            hdr.type = HDR.REQMCAST;
            hdr.localSeqID = seqID;
            hdr.seqID = NULL_ID;



            reqMsg.putHeader(HeaderType.TOTAL, hdr);
            reqMsg.IsUserMsg = true;
            reqMsg.Type = MsgType.TOKEN_SEEKING;

            Event evt = new Event();
            evt.Type = Event.MSG;
            evt.Arg = reqMsg;

            passDown(evt);
        }

        /// <summary> Receive a broadcast message: Put it in the pending up queue and then
        /// try to deliver above as many messages as possible
        /// 
        /// </summary>
        /// <param name="msg">the received broadcast message
        /// </param>
        private void _recvBcast(Message msg)
        {
            HDR header = (HDR)msg.getHeader(HeaderType.TOTAL);

            // i. Put the message in the up pending queue only if it's not
            // already there, as it seems that the event may be received
            // multiple times before a view change when all members are
            // negotiating a common set of stable msgs
            //
            // ii. Deliver as many messages as possible
            int existingViewId = -1;
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite);
                existingViewId = (int)(currentViewId != null ? currentViewId.Id : -1);
            }
            finally
            {
                stateLock.ReleaseReaderLock();
            }
            lock (upTbl.SyncRoot)
            {
                if (header.seqID <= seqID)
                {
                    if (header.viewId > existingViewId)
                    {
                        //this messages is of latest view therefore we put it into the table.
                        lock (_undeliveredMessages.SyncRoot)
                        {
                            _undeliveredMessages.Add(new Event(Event.MSG, msg, msg.Priority));
                        }
                        return;
                    }
                    Stack.NCacheLog.CriticalInfo("TOTAL._recvBcast", header.seqID + " is already consumed");
                    return;
                }
                else
                {
                    if (header.viewId < existingViewId)
                    {
                        //this messages is of an old view therefore we discard it
                        return;
                    }
                }
                upTbl[(long)header.seqID] = msg;
            }
            _deliverBcast();
        }

        /// <summary> Receive a multicast message: Put it in the pending up queue and then
        /// try to deliver above as many messages as possible
        /// 
        /// </summary>
        /// <param name="msg">the received broadcast message
        /// </param>
        private void _recvMcast(Message msg)
        {
            HDR header = (HDR)msg.getHeader(HeaderType.TOTAL);

            // i. Put the message in the up pending queue only if it's not
            // already there, as it seems that the event may be received
            // multiple times before a view change when all members are
            // negotiating a common set of stable msgs
            //
            // ii. Deliver as many messages as possible
            int existingViewId = -1;
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite);
                existingViewId = (int)(currentViewId != null ? currentViewId.Id : -1);
            }
            finally
            {
                stateLock.ReleaseReaderLock();
            }
            lock (_mcastUpTbl.SyncRoot)
            {
                if (header.seqID <= _mcastSeqID)
                {
                    if (header.viewId > existingViewId)
                    {
                        //this messages is of latest view therefore we put it into the table.
                        lock (_undeliveredMessages.SyncRoot)
                        {
                            _undeliveredMessages.Add(new Event(Event.MSG, msg, msg.Priority));
                        }
                        return;
                    }
                    if (Stack.NCacheLog.IsErrorEnabled) Stack.NCacheLog.Error("TOTAL._recvMcast", header.seqID + " is already consumed");
                    return;
                }
                else
                {
                    if (header.viewId < existingViewId)
                    {
                        //this messages is of an old view therefore we discard it
                        return;
                    }
                }
                _mcastUpTbl[(long)header.seqID] = msg;
            }

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("muds: delivering mcast a message with seq : " + header.seqID);
            _deliverMcast();
        }


        /// <summary> Received a bcast request - Ignore if not the sequencer, else send a
        /// bcast reply
        /// 
        /// </summary>
        /// <param name="msg">the broadcast request message
        /// </param>
        private void _recvBcastRequest(Message msg)
        {
            HDR header;
            Message repMsg;

            // i. If blocked, discard the bcast request
            // ii. Assign a seqID to the message and send it back to the requestor

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._recvBcastRequest()", "hdr = " + Global.CollectionToString(msg.Headers));

            if (!addr.Equals(sequencerAddr))
            {
                Stack.NCacheLog.Error("Received bcast request " + "but not a sequencer");
                return;
            }
            if (state == BLOCK)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Blocked, discard bcast req");
                return;
            }

            header = (HDR)msg.getHeader(HeaderType.TOTAL);
            repMsg = new Message(msg.Src, addr, new byte[0]);
            repMsg.Priority = msg.Priority;
            int viewId = -1;
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite);
                viewId = (int)(currentViewId != null ? currentViewId.Id : -1);
            }
            finally
            {
                stateLock.ReleaseReaderLock();
            }

            HDR rspHdr = new HDR(HDR.REP, header.localSeqID, NextSequenceID, viewId);
            repMsg.putHeader(HeaderType.TOTAL, rspHdr);
            repMsg.IsUserMsg = true;
            repMsg.Type = MsgType.TOKEN_SEEKING;

            passDown(new Event(Event.MSG, repMsg));
        }

        /// <summary> Received an mcast request - Ignore if not the sequencer, else send an
        /// mcast reply
        /// 
        /// </summary>
        /// <param name="msg">the multicast request message
        /// </param>
        private void _recvMcastRequest(Message msg)
        {
            HDR header;
            Message repMsg;

            // i. If blocked, discard the mcast request
            // ii. Assign a seqID to the message and send it back to the requestor

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._recvMcastRequest()", "hdr = " + Global.CollectionToString(msg.Headers));

            if (!addr.Equals(_groupSequencerAddr))
            {
                Stack.NCacheLog.Error("Received mcast request from " + msg.Src.ToString() + " but not a group sequencer");
                return;
            }
            if (state == BLOCK)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Blocked, discard mcast req");
                return;
            }

            header = (HDR)msg.getHeader(HeaderType.TOTAL);
            repMsg = new Message(msg.Src, addr, new byte[0]);
            repMsg.Priority = msg.Priority;
            int viewId = -1;
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite);
                viewId = (int)(currentViewId != null ? currentViewId.Id : -1);
            }
            finally
            {
                stateLock.ReleaseReaderLock();
            }

            HDR reqHdr = new HDR(HDR.REPMCAST, header.localSeqID, NextMCastSeqID, viewId);
            repMsg.putHeader(HeaderType.TOTAL, reqHdr);
            repMsg.IsUserMsg = true;
            repMsg.Type = MsgType.TOKEN_SEEKING;

            passDown(new Event(Event.MSG, repMsg));
        }

        /// <summary> Received a bcast reply - Match with the pending bcast request and move
        /// the message in the list of messages to be delivered above
        /// 
        /// </summary>
        /// <param name="header">the header of the bcast reply
        /// </param>
        private void _recvBcastReply(HDR header, Message rspMsg)
        {
            Message msg;
            long id;

            // i. If blocked, discard the bcast reply
            //
            // ii. Assign the received seqID to the message and broadcast it
            //
            // iii.
            // - Acknowledge the message to the retransmitter
            // - If non-existent BCAST_REQ, send a fake bcast to avoid seqID gaps
            // - If localID == NULL_ID, it's a null BCAST, else normal BCAST
            // - Set the seq ID of the message to the one sent by the sequencer

            if (state == BLOCK)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Blocked, discard bcast rep");
                return;
            }
            request_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                object tempObject = reqTbl[(long)header.localSeqID];
                reqTbl.Remove((long)header.localSeqID);
                msg = (Message)tempObject;
            }
            finally
            {
                request_lock.ReleaseWriterLock();
            }
            if (msg != null)
            {
                retransmitter.ack(header.localSeqID);
                id = header.localSeqID;
            }
            else
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Bcast reply to " + "non-existent BCAST_REQ[" + header.localSeqID + "], Sending NULL bcast");
                id = NULL_ID;
                msg = new Message(null, addr, new byte[0]);
                msg.IsUserMsg = true;
            }


            //Rent the header
            HDR hdr = new HDR();
            hdr.type = HDR.BCAST;
            hdr.localSeqID = id;
            hdr.seqID = header.seqID;
            hdr.viewId = header.viewId;

            msg.putHeader(HeaderType.TOTAL, hdr);
            msg.IsUserMsg = true;
            msg.Type = MsgType.SEQUENCED;
            //rent the event
            Event evt = new Event();
            evt.Type = Event.MSG;
            evt.Arg = msg;
            evt.Priority = msg.Priority;
            msg.Dest = null;
            msg.Dests = null;

            passDown(evt);
        }

        /// <summary> Received an mcast reply - Match with the pending mcast request and move
        /// the message in the list of messages to be delivered above
        /// 
        /// </summary>
        /// <param name="header">the header of the bcast reply
        /// </param>
        private void _recvMcastReply(HDR header, Address subgroupCoordinator)
        {
            Message msg;
            long id;

            // i. If blocked, discard the mcast reply
            //
            // ii. Assign the received seqID to the message and multicast it
            //
            // iii.
            // - Acknowledge the message to the retransmitter
            // - If non-existent MCAST_REQ, send a fake mcast to avoid seqID gaps
            // - If localID == NULL_ID, it's a null MCAST, else normal MCAST
            // - Set the seq ID of the message to the one sent by the group sequencer

            if (state == BLOCK)
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Blocked, discard mcast rep");
                return;
            }
            request_lock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                object tempObject = _mcastReqTbl[(long)header.localSeqID];
                _mcastReqTbl.Remove((long)header.localSeqID);
                msg = (Message)tempObject;

            }
            finally
            {
                request_lock.ReleaseWriterLock();
            }

            if (msg != null)
            {
                _mcastRetransmitter.ack(header.localSeqID);
                id = header.localSeqID;
            }
            else
            {
                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Mcast reply to " + "non-existent MCAST_REQ[" + header.localSeqID + "], Sending NULL mcast");
                id = NULL_ID;
                msg = new Message(null, addr, new byte[0]);
                msg.IsUserMsg = true;

                string subGroupID = this._mbrsSubgroupMap[subgroupCoordinator] as string;
                if (subGroupID != null)
                {
                    ArrayList groupMbrs = (ArrayList)this._sequencerTbl[subGroupID] as ArrayList;
                    if (groupMbrs != null && groupMbrs.Count > 0)
                        msg.Dests = groupMbrs.Clone() as ArrayList;

                    if (msg.Dests == null)
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info('[' + subGroupID + "]destination list is empty");

                }
                else
                    return;
            }


            //Rent the header
            HDR hdr = new HDR();
            hdr.type = HDR.MCAST;
            hdr.localSeqID = id;
            hdr.seqID = header.seqID;
            hdr.viewId = header.viewId;

            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._recvMcastReply()", id + " hdr = " + Global.CollectionToString(msg.Headers));
            msg.putHeader(HeaderType.TOTAL, hdr);
            msg.IsUserMsg = true;
            msg.Type = MsgType.SEQUENCED;
            Event evt = new Event();
            evt.Type = Event.MSG;
            evt.Arg = msg;
            evt.Priority = msg.Priority;

            passDown(evt);
        }


        /// <summary> Resend the bcast request with the given localSeqID
        /// 
        /// </summary>
        /// <param name="seqID">the local sequence id of the
        /// </param>
        private void _retransmitBcastRequest(long seqID)
        {
            // *** Get a shared lock
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite); try
                {

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._retransmitBcastRequest()", "Retransmit BCAST_REQ[" + seqID + ']');
                    _transmitBcastRequest(seqID);

                    // ** Revoke the shared lock
                }
                finally
                {
                    stateLock.ReleaseReaderLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._retransmitBcasrRequest", ex.ToString());
            }
        }

        /// <summary> Resend the mcast request with the given localSeqID
        /// 
        /// </summary>
        /// <param name="seqID">the local sequence id of the
        /// </param>
        private void _retransmitMcastRequest(long seqID, Address groupSequencerAddr)
        {
            // *** Get a shared lock
            try
            {
                stateLock.AcquireReaderLock(Timeout.Infinite); try
                {

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("TOTAL._retransmitMcastRequest()", "Retransmit MCAST_REQ[" + seqID + "] to : " + groupSequencerAddr.ToString());
                    _transmitMcastRequest(seqID, groupSequencerAddr);

                    // ** Revoke the shared lock
                }
                finally
                {
                    stateLock.ReleaseReaderLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._retransmitMCastRequest", ex.ToString());
            }
        }


        /* Up event handlers
        * If the return value is true the event travels further up the stack
        * else it won't be forwarded
        */

        /// <summary> Prepare for a VIEW_CHANGE: switch to flushing state
        /// 
        /// </summary>
        /// <returns> true if the event is to be forwarded further up
        /// </returns>
        private bool _upBlock()
        {
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    state = FLUSH;
                    // *** Revoke the exclusive lock
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._upBlock", ex.ToString());
            }

            return (true);
        }


        /// <summary> Handle an up MSG event
        /// 
        /// </summary>
        /// <param name="event">the MSG event
        /// </param>
        /// <returns> true if the event is to be forwarded further up
        /// </returns>
        private bool _upMsg(Event evt)
        {
            Message msg;
            object obj;
            HDR header;

            // *** Get a shared lock
            try
            {

                try
                {
                    msg = (Message)evt.Arg;

                    // If NULL_STATE, shouldn't receive any msg on the up queue!
                    if (state == NULL_STATE)
                    {
                        stateLock.AcquireReaderLock(Timeout.Infinite);
                        try
                        {
                            string hdrToSting = msg != null ? Global.CollectionToString(msg.Headers) : " Null header";
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info(" Up msg in NULL_STATE " + hdrToSting);
                            if (state == NULL_STATE)
                            {
                                lock (_undeliveredMessages.SyncRoot)
                                {
                                    _undeliveredMessages.Add(evt);
                                    return (false);
                                }
                            }
                        }
                        finally
                        {
                            stateLock.ReleaseReaderLock();
                        }
                    }

                    // Peek the header:
                    //
                    // (UCAST) A unicast message - Send up the stack
                    // (BCAST) A broadcast message - Handle specially
                    // (REQ) A broadcast request - Handle specially
                    // (REP) A broadcast reply from the sequencer - Handle specially
                    if (!((obj = msg.getHeader(HeaderType.TOTAL)) is TOTAL.HDR))
                    {
                       
                    }
                    else
                    {
                        header = (HDR)obj;

                        switch (header.type)
                        {

                            case HDR.UCAST:
                                _recvUcast(msg);
                                return (true);

                            case HDR.BCAST:
                                _recvBcast(msg);
                                return (false);

                            case HDR.MCAST:
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("muds: a command for mcast from " + msg.Src + " to me[" + addr + "],  local-seq : " + header.localSeqID + " seq : " + header.seqID);
                                _recvMcast(msg);
                                return (false);

                            case HDR.REQ:
                                _recvBcastRequest(msg);
                                return (false);

                            case HDR.REP:
                                _recvBcastReply(header, msg);
                                return (false);

                            case HDR.REQMCAST:
                                if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("muds: recieved mcast request " + " local-seq : " + header.localSeqID + " seq : " + header.seqID);
                                _recvMcastRequest(msg);
                                return (false);

                            case HDR.REPMCAST:
                                _recvMcastReply(header, msg.Src);
                                return (false);

                            default:
                                Stack.NCacheLog.Error("Unknown header type");
                                return (false);

                        }
                    }
                    // ** Revoke the shared lock
                }
                finally
                {
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._upMsg", ex.ToString());
            }

            return (true);
        }

        /// <summary>
        /// Delivers the pending messages which were queued when stat was NULL_STATE.
        /// </summary>
        private void deliverPendingMessages()
        {
            try
            {
                lock (_undeliveredMessages.SyncRoot)
                {
                    if (_undeliveredMessages.Count > 0)
                    {
                        ArrayList clone = _undeliveredMessages.Clone() as ArrayList;
                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(deliverPendingMessagesAsync), clone);
                        _undeliveredMessages.Clear();
                    }
                }
            }
            catch (Exception) { }
        }

        private void deliverPendingMessagesAsync(object msgs)
        {
            ArrayList pendingMessages = msgs as ArrayList;
            if (pendingMessages != null)
            {
                for (int i = 0; i < pendingMessages.Count; i++)
                {
                    try
                    {
                        up(pendingMessages[i] as Event);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        /// <summary> Set the address of this group member
        /// 
        /// </summary>
        /// <param name="event">the SET_LOCAL_ADDRESS event
        /// </param>
        /// <returns> true if event should be forwarded further up
        /// </returns>
        private bool _upSetLocalAddress(Event evt)
        {
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite); try
                {

                    addr = (Address)evt.Arg;

                    // *** Revoke the exclusive lock
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._upSetLocalAddress", ex.ToString());
            }
            return (true);
        }


        /// <summary> Handle view changes
        /// 
        /// param event the VIEW_CHANGE event
        /// </summary>
        /// <returns> true if the event should be forwarded to the layer above
        /// </returns>
        private bool _upViewChange(Event evt)
        {
            object oldSequencerAddr;
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "received VIEW_CHANGE");
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite);
                try
                {

                    state = RUN;

                    // i. See if this member is the sequencer
                    // ii. If this is the sequencer, reset the sequencer's sequence ID
                    // iii. Reset the last received sequence ID
                    //
                    // iv. Replay undelivered bcasts: Put all the undelivered bcasts
                    // sent by us back to the req queue and discard the rest
                    oldSequencerAddr = sequencerAddr;
                    sequencerAddr = (Address)((View)evt.Arg).Members[0];

                    currentViewId = ((View)evt.Arg).Vid.Copy();
                    //============================================
                    //copy the sequencer table from the new view.
                    this._sequencerTbl = ((View)evt.Arg).SequencerTbl.Clone() as Hashtable;
                    this._mbrsSubgroupMap = ((View)evt.Arg).MbrsSubgroupMap.Clone() as Hashtable;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "this._sequencerTbl.count = " + this._sequencerTbl.Count);
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "this._mbrsSubgroupMap.count = " + this._mbrsSubgroupMap.Count);

                    ArrayList groupMbrs = (ArrayList)_sequencerTbl[subGroup_addr];
                    if (groupMbrs != null)
                    {
                        if (groupMbrs.Count != 0)
                        {
                            this._groupSequencerAddr = groupMbrs[0] as Address;
                        }
                    }

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", subGroup_addr + " old mbrs count = " + _groupMbrsCount);
                    int newCount = groupMbrs.Count;
                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "new mbrs count = " + newCount);
                    if (newCount != _groupMbrsCount)
                    {
                        // i.	the group this member belongs to, has changed 
                        // ii.	therefore reset the  _mcastSeqID
                        // iii.	if this node is new group sequencer, reset the 
                        //		_mcastSequencerSeqID.
                        // iii. Reset the last received mcast sequence ID
                        // iv.	Replay undelivered mcasts 

                        if (addr.Equals(_groupSequencerAddr))
                        {
                            _mcastSequencerSeqID = NULL_ID;
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "resetting _mcastSequencerSeqID");
                        }

                        _mcastSeqID = NULL_ID;
                        if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange()", "resetting _mcastSeqID");
                        _groupMbrsCount = newCount;

                        _replayMcast();
                    }

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upViewChange", "my group sequencer is " + this._groupSequencerAddr.ToString());
                    //============================================

                    if (addr.Equals(sequencerAddr))
                    {
                        sequencerSeqID = NULL_ID;
                        if ((oldSequencerAddr == null) || (!addr.Equals(oldSequencerAddr)))
                            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("I'm the new sequencer");

                    }
                    lock (upTbl.SyncRoot)
                    {
                        seqID = NULL_ID;
                    }
                    _replayBcast();

                    if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total.upViewChange()", "VIEW_CHANGE_OK");
                    Event viewEvt = new Event(Event.VIEW_CHANGE_OK, null, Priority.High);
                    passDown(viewEvt);

                    // *** Revoke the exclusive lock
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._upViewChange", ex.ToString());
            }
            return (true);
        }

        int _seqResetRequestCount = 0;
        int _msgAfterReset = 0;
        int _msgArrived = 0;
        private bool _upResetSequence(Event evt)
        {
            _seqResetRequestCount++;
            ViewId vid = evt.Arg as ViewId;
            if (Stack.NCacheLog.IsInfoEnabled) Stack.NCacheLog.Info("Total._upResetSequence()", "Sequence reset request received :" + _seqResetRequestCount);
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    currentViewId = vid.Copy();
                    _msgAfterReset = 0;
                    _msgArrived = 0;
                    state = RUN;
                    lock (_mcastSeqIDMutex)
                    {
                        _mcastSequencerSeqID = NULL_ID;
                    }
                    lock (_mcastUpTbl.SyncRoot)
                    {
                        _mcastSeqID = NULL_ID;
                    }
                    _replayMcast();

                    lock (seqIDMutex)
                    {
                        sequencerSeqID = NULL_ID;
                    }
                    lock (upTbl.SyncRoot)
                    {
                        seqID = NULL_ID;
                    }
                    _replayBcast();
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL._upResetSequence", ex.ToString());
            }
            return (true);
        }


        /*
        * Down event handlers
        * If the return value is true the event travels further down the stack
        * else it won't be forwarded
        */


        /// <summary> Blocking confirmed - No messages should come from above until a
        /// VIEW_CHANGE event is received. Switch to blocking state.
        /// 
        /// </summary>
        /// <returns> true if event should travel further down
        /// </returns>
        private bool _downBlockOk()
        {
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite); try
                {

                    state = BLOCK;

                    // *** Revoke the exclusive lock
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error(ex.Message);
            }

            return (true);
        }


        /// <summary> A MSG event travelling down the stack. Forward unicast messages, treat
        /// specially the broadcast messages.<br>
        /// 
        /// If in <code>BLOCK</code> state, i.e. it has replied to a
        /// <code>BLOCk_OK</code> and hasn't yet received a
        /// <code>VIEW_CHANGE</code> event, messages are discarded<br>
        /// 
        /// If in <code>FLUSH</code> state, forward unicast but queue broadcasts
        /// 
        /// </summary>
        /// <param name="event">the MSG event
        /// </param>
        /// <returns> true if event should travel further down
        /// </returns>
        private bool _downMsg(Event evt)
        {
            Message msg;

            // *** Get a shared lock
            try
            {
                try
                {

                    // i. Discard all msgs, if in NULL_STATE
                    // ii. Discard all msgs, if blocked
                    if (state == NULL_STATE)
                    {
                        Stack.NCacheLog.Error("TOTAL._downMsg()", "Discard msg in NULL_STATE");
                        return (false);
                    }
                    if (state == BLOCK)
                    {
                        Stack.NCacheLog.Error("TOTAL._downMsg()", "Blocked, discard msg");
                        return (false);
                    }

                    msg = (Message)evt.Arg;
                    msg.Priority = evt.Priority;

                    if (msg.IsSeqRequired)
                    {
                        if (msg.Dest != null || msg.Dests != null)
                        {
                            //if it is a unicast msg with a single destination.
                            if (msg.Dests == null)
                            {
                                //msg = _sendUcast(msg);
                                evt.Arg = msg;
                            }
                            // if it is a multicast msg with multiple destinations.
                            else
                            {
                                _sendMcastRequest(msg);
                                return (false);
                            }
                        }
                        else //its a broadcast msg.
                        {
                            _sendBcastRequest(msg);
                            return (false);
                        }
                    }
                    else
                    {
                        return true;
                    }

                    // ** Revoke the shared lock
                }
                finally
                {
                    //stateLock.ReleaseReaderLock();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("TOTAL._downMsg()", ex.ToString());
            }
            return (true);
        }




        /// <summary> Prepare this layer to receive messages from above</summary>
        public override void start()
        {
            TimeScheduler timer;

            // Incase of TCP stack we'll get a reference to TCP, which is the transport
            // protocol in our case. For udp stack we'll fail.
            transport = Stack.findProtocol("TCP");
            reqTbl = Hashtable.Synchronized(new Hashtable());
            upTbl = Hashtable.Synchronized(new Hashtable());

            //======================================================
            _mcastReqTbl = Hashtable.Synchronized(new Hashtable());
            _mcastUpTbl = Hashtable.Synchronized(new Hashtable());
            //======================================================

            //NewTrace nTrace = stack.nTrace;
            retransmitter = new AckSenderWindow(new Command(this), AVG_RETRANSMIT_INTERVAL, stack.NCacheLog);
            _mcastRetransmitter = new AckSenderWindow(new MCastCommand(this), AVG_MCAST_RETRANSMIT_INTERVAL, stack.NCacheLog);
        }




        /// <summary> Handle the stop() method travelling down the stack.
        /// <p>
        /// The local addr is set to null, since after a Start->Stop->Start
        /// sequence this member's addr is not guaranteed to be the same
        /// 
        /// </summary>
        public override void stop()
        {
            // *** Get an exclusive lock
            try
            {
                stateLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    state = NULL_STATE;
                    retransmitter.reset();
                    _mcastRetransmitter.reset();
                    reqTbl.Clear();
                    upTbl.Clear();
                    addr = null;


                    // *** Revoke the exclusive lock
                }
                finally
                {
                    stateLock.ReleaseWriterLock();
                    transport = null;
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Stack.NCacheLog.Error("Protocols.TOTAL.stop", ex.ToString());
            }
        }


        /// <summary> Process an event coming from the layer below
        /// 
        /// </summary>
        /// <param name="event">the event to process
        /// </param>
        private void _up(Event evt)
        {
            switch (evt.Type)
            {

                case Event.BLOCK:
                    if (!_upBlock())
                        return;
                    break;

                case Event.MSG:
                    if (!_upMsg(evt))
                    {
                        return;
                    }
                    break;

                case Event.SET_LOCAL_ADDRESS:
                    if (!_upSetLocalAddress(evt))
                        return;
                    break;

                case Event.VIEW_CHANGE:
                    if (!_upViewChange(evt))
                        return;
                    deliverPendingMessages();
                    break;

                case Event.RESET_SEQUENCE:
                    _upResetSequence(evt);
                    deliverPendingMessages();
                    break;

                default: break;

            }

            passUp(evt);

        }


        /// <summary> Process an event coming from the layer above
        /// 
        /// </summary>
        /// <param name="event">the event to process
        /// </param>
        private void _down(Event evt)
        {
            switch (evt.Type)
            {

                case Event.BLOCK_OK:
                    if (!_downBlockOk())
                        return;
                    break;

                case Event.MSG:
                    if (!_downMsg(evt))
                    {
                        return;
                    }
                    break;

                case Event.CONNECT:
                    object[] addrs = ((object[])evt.Arg);
                    subGroup_addr = (string)addrs[1];
                    passDown(evt);
                    break;

                default: break;

            }

            passDown(evt);
        }

      
        /// <summary> Create the TOTAL layer</summary>
        public TOTAL()
        {
        }
        public override bool setProperties(Hashtable properties)
        {
            return (_setProperties(properties));
        }
        public override ArrayList requiredDownServices()
        {
            return (_requiredDownServices());
        }
        public override ArrayList requiredUpServices()
        {
            return (_requiredUpServices());
        }
        public override void up(Event evt)
        {
            _up(evt);
        }
        public override void down(Event evt)
        {
            _down(evt);
        }
    }
}
