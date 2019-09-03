using System;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NGroups
{
	//public enum Priority {Critical, Normal, Low};

	/// <summary>
	/// Used for intra-stack communication.
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	public class Event : IRentableObject
	{		
		public const int MSG = 1;
		public const int CONNECT = 2; // arg = group address (string)
		public const int CONNECT_OK = 3; // arg = group multicast address (Address)
		public const int DISCONNECT = 4; // arg = member address (Address)
		public const int DISCONNECT_OK = 5;
		public const int VIEW_CHANGE = 6; // arg = View (or MergeView in case of merge)
		public const int GET_LOCAL_ADDRESS = 7;
		public const int SET_LOCAL_ADDRESS = 8;
		public const int SUSPECT = 9; // arg = Address of suspected member
		public const int BLOCK = 10;
		public const int BLOCK_OK = 11;
		public const int FIND_INITIAL_MBRS = 12;
		public const int FIND_INITIAL_MBRS_OK = 13; // arg = Vector of PingRsps
		public const int MERGE = 14; // arg = Vector of Objects
		public const int TMP_VIEW = 15; // arg = View
		public const int BECOME_SERVER = 16; // sent when client has joined group
        public const int GET_STATE = 19; // arg = StateTransferInfo
        public const int GET_STATE_OK = 20; // arg = Object or Vector (state(s))
		public const int START_QUEUEING = 22;
		public const int STOP_QUEUEING = 23; // arg = Vector (event-list)
		public const int SWITCH_NAK = 24;
		public const int SWITCH_NAK_ACK = 25;
		public const int SWITCH_OUT_OF_BAND = 26;
		public const int FLUSH = 27; // arg = Vector (destinatinon for FLUSH)
		public const int FLUSH_OK = 28; // arg = FlushRsp
		public const int DROP_NEXT_MSG = 29;
		public const int STABLE = 30; // arg = long[] (stable seqnos for mbrs)
		public const int GET_MSG_DIGEST = 31; // arg = long[] (highest seqnos from mbrs)
		public const int GET_MSG_DIGEST_OK = 32; // arg = Digest
		public const int REBROADCAST_MSGS = 33; // arg = Vector (msgs with NakAckHeader)
		public const int REBROADCAST_MSGS_OK = 34;
		public const int GET_MSGS_RECEIVED = 35;
		public const int GET_MSGS_RECEIVED_OK = 36; // arg = long[] (highest deliverable seqnos)
		public const int GET_MSGS = 37; // arg = long[][] (range of seqnos for each m.)
		public const int GET_MSGS_OK = 38; // arg = List
		public const int GET_DIGEST = 39; //
		public const int GET_DIGEST_OK = 40; // arg = Digest (response to GET_DIGEST)
		public const int SET_DIGEST = 41; // arg = Digest
        public const int GET_DIGEST_STATE = 42; // see ./JavaStack/Protocols/pbcast/DESIGN for explanantion
		public const int GET_DIGEST_STATE_OK = 43; // see ./JavaStack/Protocols/pbcast/DESIGN for explanantion
		public const int SET_PARTITIONS = 44; // arg = Hashtable of addresses and numbers
		public const int MERGE_DENIED = 45; // Passed down from gms when a merge attempt fails
		public const int EXIT = 46; // received when member was forced out of the group
		public const int PERF_START = 47; // for performance measurements
		public const int SUBVIEW_MERGE = 48; // arg = vector of addresses; see JGroups/EVS/Readme.txt
		public const int SUBVIEWSET_MERGE = 49; // arg = vector of addresses; see JGroups/EVS/Readme.txt
		public const int HEARD_FROM = 50; // arg = Vector (list of Addresses)
		public const int UNSUSPECT = 51; // arg = Address (of unsuspected member)
//		public const int SET_PID = 52; // arg = Integer (process id)
		public const int MERGE_DIGEST = 53; // arg = Digest
		public const int BLOCK_SEND = 54; // arg = null
		public const int UNBLOCK_SEND = 55; // arg = null
		public const int CONFIG = 56; // arg = HashMap (config properties)
		public const int GET_DIGEST_STABLE = 57;
		public const int GET_DIGEST_STABLE_OK = 58; // response to GET_DIGEST_STABLE
		public const int ACK = 59; // used to flush down events
		public const int ACK_OK = 60; // response to ACK
		public const int START = 61; // triggers start() - internal event, handled by Protocol
		public const int START_OK = 62; // arg = exception of null - internal event, handled by Protocol
		public const int STOP = 63; // triggers stop() - internal event, handled by Protocol
		public const int STOP_OK = 64; // arg = exception or null - internal event, handled by Protocol
		public const int SUSPEND_STABLE = 65; // arg = null
		public const int RESUME_STABLE = 66; // arg = null
		public const int VIEW_CHANGE_OK = 67; // arg = null
        public const int TCPPING = 68; // 
        public const int PERF_STOP = 69; // for performance measurements
        public const int PERF_STOP_OK = 70; // for performance measurements
        public const int USER_DEFINED = 1000; // arg = <user def., e.g. evt type + data>
        public const int MSG_URGENT= 71; // for urgent messages
        public const int GET_NODE_STATUS = 72; // to get the status of a node
        public const int GET_NODE_STATUS_OK = 73; // returns the status of the
        public const int CHECK_NODE_CONNECTED = 74; // to checks whether all this node is connected with all nodes or not;
        public const int CHECK_NODE_CONNECTED_OK = 75; // returns the list of nodes to which this node can not make connection
        public const int CONNECTION_FAILURE = 76; // tells about the nodes with which we can not establish connection
        public const int VIEW_BCAST_MSG = 78; // view message to be broadcasted; it is handled differently;
        public const int HASHMAP_REQ = 79; // Request for both HashMaps for Data Distribution and Mirror Mapping for dynamic mirroring.
        public const int HASHMAP_RESP = 80; // Response of the above Hashmap and Mirror Mapping request.
        public const int CONNECT_PHASE_2 = 81;
        public const int CONNECT_OK_PHASE_2 = 82;
        public const int CONFIGURE_NODE_REJOINING = 83;
        public const int NODE_REJOINING = 84;
        public const int RESET_SEQUENCE = 85;
        public const int CONNECTION_BREAKAGE = 86;
        public const int CONNECTION_RE_ESTABLISHED = 87;
        public const int CONFIRM_CLUSTER_STARTUP = 88;
        public const int HAS_STARTED = 89;
        public const int ASK_JOIN = 90;
        public const int ASK_JOIN_RESPONSE = 91;
        public const int MARK_CLUSTER_IN_STATETRANSFER = 92;
        public const int MARK_CLUSTER_STATETRANSFER_COMPLETED = 93;
        public const int I_AM_LEAVING = 94;
        public const int CONNECTION_NOT_OPENED = 95;
        public const int NOTIFY_LEAVING = 96;
        public const int MARK_FOR_MAINTENANCE = 97;
        public const int MARKED_FOR_MAINTENANCE = 98;
        public const int UNMARK_FOR_MAINTENANCE = 99;
        public const int IS_CLUSTER_IN_STATE_TRANSFER = 100;
        public const int IS_CLUSTER_IN_STATE_TRANSFER_RSP = 101;


        /// <summary> Current type of event </summary>
        private int     type=0;  

		/// <summary> Object associated with the type </summary>
		private Object  arg=null;     // must be serializable if used for inter-stack communication

		/// <summary> Priority of this event. </summary>
		private Priority priority = Priority.Normal;
        private string reason = "";

        private int rentId;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of Event</param>
		public Event(int type) 
		{
			this.type=type;
		}

        public Event() { }
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of Event</param>
		/// <param name="arg">Object associated with type</param>
		public Event(int type, Object arg) 
		{
			this.type=type;
			this.arg=arg;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of Event</param>
		/// <param name="arg">Object associated with type</param>
		public Event(int type, Object arg, Priority priority) 
		{
			this.type=type;
			this.arg=arg;
			this.priority = priority;
		}

		/// <summary>
		/// Gets and sets the type of the Event
		/// </summary>
		public int Type
		{
			get {return type;}
			set {type = value;}
		}
		
		/// <summary>
		/// Gets and sets the object associated with the Event
		/// </summary>
		public Object Arg
		{
			get {return arg;}
			set {arg = value;}
		}	

		/// <summary>
		/// Gets and sets the type of the Event
		/// </summary>
		public Priority Priority
		{
			get {return priority;}
			set {priority = value;}
		}

        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }

		/// <summary>
		/// Converts an Event type to a string representation
		/// </summary>
		/// <param name="t">Type of event</param>
		/// <returns>A string representatio nof the Event type</returns>
		public static string type2String(int t)
		{
			switch (t)
			{
				case MSG:  return "MSG";
				case CONNECT:  return "CONNECT";
				case CONNECT_OK:  return "CONNECT_OK";
				case DISCONNECT:  return "DISCONNECT";
				case DISCONNECT_OK:  return "DISCONNECT_OK";
				case VIEW_CHANGE:  return "VIEW_CHANGE";
				case GET_LOCAL_ADDRESS:  return "GET_LOCAL_ADDRESS";
				case SET_LOCAL_ADDRESS:  return "SET_LOCAL_ADDRESS";
				case SUSPECT:  return "SUSPECT";
				case BLOCK:  return "BLOCK";
				case BLOCK_OK:  return "BLOCK_OK";
				case FIND_INITIAL_MBRS:  return "FIND_INITIAL_MBRS";
				case FIND_INITIAL_MBRS_OK:  return "FIND_INITIAL_MBRS_OK";
				case TMP_VIEW:  return "TMP_VIEW";
				case BECOME_SERVER:  return "BECOME_SERVER";
                case GET_STATE: return "GET_STATE";
                case GET_STATE_OK: return "GET_STATE_OK";
				case START_QUEUEING:  return "START_QUEUEING";
				case STOP_QUEUEING:  return "STOP_QUEUEING";
				case SWITCH_NAK:  return "SWITCH_NAK";
				case SWITCH_NAK_ACK:  return "SWITCH_NAK_ACK";
				case SWITCH_OUT_OF_BAND:  return "SWITCH_OUT_OF_BAND";
				case FLUSH:  return "FLUSH";
				case FLUSH_OK:  return "FLUSH_OK";
				case DROP_NEXT_MSG:  return "DROP_NEXT_MSG";
				case STABLE:  return "STABLE";
				case GET_MSG_DIGEST:  return "GET_MSG_DIGEST";
				case GET_MSG_DIGEST_OK:  return "GET_MSG_DIGEST_OK";
				case REBROADCAST_MSGS:  return "REBROADCAST_MSGS";
				case REBROADCAST_MSGS_OK:  return "REBROADCAST_MSGS_OK";
				case GET_MSGS_RECEIVED:  return "GET_MSGS_RECEIVED";
				case GET_MSGS_RECEIVED_OK:  return "GET_MSGS_RECEIVED_OK";
				case GET_MSGS:  return "GET_MSGS";
				case GET_MSGS_OK:  return "GET_MSGS_OK";
				case GET_DIGEST:  return "GET_DIGEST";
				case GET_DIGEST_OK:  return "GET_DIGEST_OK";
				case SET_DIGEST:  return "SET_DIGEST";
				case GET_DIGEST_STATE:  return "GET_DIGEST_STATE";
				case GET_DIGEST_STATE_OK:  return "GET_DIGEST_STATE_OK";
				case SET_PARTITIONS:  return "SET_PARTITIONS"; // Added by gianlucac@tin.it to support PARTITIONER
				case MERGE:  return "MERGE"; // Added by gianlucac@tin.it to support partitions merging in GMS
				case MERGE_DENIED:  return "MERGE_DENIED"; // as above
				case EXIT:  return "EXIT";
				case PERF_START:  return "PERF_START";
                case PERF_STOP: return "PERF_STOP";
				case SUBVIEW_MERGE:  return "SUBVIEW_MERGE";
				case SUBVIEWSET_MERGE:  return "SUBVIEWSET_MERGE";
				case HEARD_FROM:  return "HEARD_FROM";
				case UNSUSPECT:  return "UNSUSPECT";
//				case SET_PID:  return "SET_PID";
				case MERGE_DIGEST:  return "MERGE_DIGEST";
				case BLOCK_SEND:  return "BLOCK_SEND";
				case UNBLOCK_SEND:  return "UNBLOCK_SEND";
				case CONFIG:  return "CONFIG";
				case GET_DIGEST_STABLE:  return "GET_DIGEST_STABLE";
				case GET_DIGEST_STABLE_OK:  return "GET_DIGEST_STABLE_OK";
				case ACK:  return "ACK";
				case ACK_OK:  return "ACK_OK";
				case START:  return "START";
				case START_OK:  return "START_OK";
				case STOP:  return "STOP";
				case STOP_OK:  return "STOP_OK";
				case SUSPEND_STABLE:  return "SUSPEND_STABLE";
				case RESUME_STABLE:  return "RESUME_STABLE";
				case VIEW_CHANGE_OK : return "VIEW_CHANGE_OK";
                case MSG_URGENT: return "MSG_URGENT";
                case CHECK_NODE_CONNECTED: return "CHECK_NODE_CONNECTED";
                case CHECK_NODE_CONNECTED_OK: return "CHECK_NODE_CONNECTED_OK";
                case GET_NODE_STATUS: return "GET_NODE_STATUS";
                case GET_NODE_STATUS_OK: return "GET_NODE_STATUS_OK";
                case VIEW_BCAST_MSG: return "VIEW_BCAST_MSG";
				
				case USER_DEFINED:  return "USER_DEFINED";
                case CONNECT_PHASE_2: return "CONNECT_PHASE_2";
                case CONNECT_OK_PHASE_2: return "CONNECT_OK_PHASE_2";
                case CONFIGURE_NODE_REJOINING: return "CONFIGURE_NODE_REJOINING";
                case NODE_REJOINING: return "NODE_REJOINING";
                case RESET_SEQUENCE : return "RESET_SEQUENCE";
                case CONNECTION_BREAKAGE: return "CONNECTION_BREAKAGE";
                case CONNECTION_RE_ESTABLISHED: return "CONNECTION_RE_ESTABLISHED";
                case CONFIRM_CLUSTER_STARTUP: return "CONFIRM_CLUSTER_STARTUP";
                case HAS_STARTED: return "HAS_STARTED";
                case ASK_JOIN: return "ASK_JOIN";
                case ASK_JOIN_RESPONSE: return "ASK_JOIN_RESPONSE";
                case I_AM_LEAVING: return "I_AM_LEAVING";
				default:  return "UNDEFINED";
			}
		}
        public void Reset()
        {
            type = 0;
            arg = null;
            priority = Priority.Normal;
        }

		/// <summary>
		/// Returns a string representation of the Event 
		/// </summary>
		/// <returns>A string representation of the Event</returns>
		public override string ToString() 
		{
			return "Event[type=" + type2String(type) + ", arg=" + arg + "]";
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
    }
}
