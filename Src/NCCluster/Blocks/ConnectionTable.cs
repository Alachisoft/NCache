
// $Id: GroupRequest.java,v 1.8 2004/09/05 04:54:22 ovidiuf Exp $
using System;
using System.Collections;

using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Alachisoft.NCache.Common.Sockets;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NGroups.Protocols;
using System.Diagnostics;

#if NETCORE
using System.Runtime.InteropServices;
#endif

namespace Alachisoft.NGroups.Blocks
{
    /// <summary> Manages incoming and outgoing TCP connections. For each outgoing message to destination P, if there
    /// is not yet a connection for P, one will be created. Subsequent outgoing messages will use this
    /// connection.  For incoming messages, one server socket is created at startup. For each new incoming
    /// client connecting, a new thread from a thread pool is allocated and listens for incoming messages
    /// until the socket is closed by the peer.<br>Sockets/threads with no activity will be killed
    /// after some time.<br> Incoming messages from any of the sockets can be received by setting the
    /// message listener.
    /// </summary>
    /// <author>  Bela Ban
    /// </author>
    class ConnectionTable
    {
        virtual public Address LocalAddress
        {
            get
            {
                if (local_addr == null)
                    local_addr = bind_addr1 != null ? new Address(bind_addr1, srv_port) : null;
                return local_addr;
            }

        }

        virtual public int SendBufferSize
        {
            get
            {
                return send_buf_size;
            }

            set
            {
                this.send_buf_size = value;
            }

        }
        virtual public int ReceiveBufferSize
        {
            get
            {
                return recv_buf_size;
            }

            set
            {
                this.recv_buf_size = value;
            }

        }
        private System.Collections.Hashtable conns_NIC_1 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection

        private System.Collections.Hashtable secondayrConns_NIC_1 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private System.Collections.Hashtable conns_NIC_2 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private System.Collections.Hashtable secondayrConns_NIC_2 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection

        private System.Collections.Hashtable dedicatedSenders = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private ConnectionTable.Receiver receiver = null;
        // srv_sock1 & srv_sock2 was initially tcplistener
        private System.Net.Sockets.Socket srv_sock1 = null;

        private System.Net.Sockets.Socket srv_sock2 = null;


        private System.Net.IPAddress bind_addr1 = null;

        private System.Net.IPAddress bind_addr2 = null;


        private Address local_addr = null; // bind_addr + port of srv_sock
        private Address local_addr_s = null; // bind_addr + port of Secondary srv_sock.

        internal int srv_port = 7800;
        public string env_name = "";

        private bool stopped;
        private object newcon_sync_lock = new object();

        internal int port_range = 1;

        private Thread acceptor1 = null; // continuously calls srv_sock.accept()
        private Thread acceptor2 = null; // continuously calls srv_sock.accept()
        private int recv_buf_size = 20000000;
        private int send_buf_size = 640000;
        private System.Collections.ArrayList conn_listeners = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10)); // listeners to be notified when a conn is established/torn down
        private System.Object recv_mutex = new System.Object(); // to serialize simultaneous access to receive() from multiple Connections
        private Reaper reaper = null; // closes conns that have been idle for more than n secs
        private long reaper_interval = 60000; // reap unused conns once a minute
        private long conn_expire_time = 300000; // connections can be idle for 5 minutes before they are reaped
        private bool use_reaper = false; // by default we don't reap idle conns
        private MemoryManager memManager;
        private ReaderWriterLock conn_syn_lock = new ReaderWriterLock();


        private ILogger _ncacheLog;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        public Protocols.TCP enclosingInstance;
        bool useDualConnection = false;
        object con_selection_mutex = new object();
        bool _usePrimary = true;
        bool enableMonitoring;
        bool enableNaggling;
        int nagglingSize;
        bool useDedicatedSender = true;
        object con_reestablish_sync = new object();
        ArrayList _nodeRejoiningList;

        private int _retries;
        private int _retryInterval;

        private int _idGenerator = 0;


        /// <summary>Used for message reception </summary>
        public interface Receiver
        {
            void receive(Message msg);
        }

        /// <summary>Used to be notified about connection establishment and teardown </summary>
        public interface ConnectionListener
        {
            void connectionOpened(Address peer_addr);
            void connectionClosed(Address peer_addr);
            void couldnotConnectTo(Address peer_addr);
            void HandleFailedNodes(object failedNodes);
        }


        /// <summary> Regular ConnectionTable without expiration of idle connections</summary>
        /// <param name="srv_port">The port on which the server will listen. If this port is reserved, the next
        /// free port will be taken (incrementing srv_port).
        /// </param>
        public ConnectionTable(int srv_port, ILogger NCacheLog)
        {
            this.srv_port = srv_port;
            this._ncacheLog = NCacheLog;
            start();
        }


        /// <summary> ConnectionTable including a connection reaper. Connections that have been idle for more than conn_expire_time
        /// milliseconds will be closed and removed from the connection table. On next access they will be re-created.
        /// </summary>
        /// <param name="srv_port">The port on which the server will listen
        /// </param>
        /// <param name="reaper_interval">Number of milliseconds to wait for reaper between attepts to reap idle connections
        /// </param>
        /// <param name="conn_expire_time">Number of milliseconds a connection can be idle (no traffic sent or received until
        /// it will be reaped
        /// </param>
        /// 

        public ConnectionTable(int srv_port, long reaper_interval, long conn_expire_time, ILogger NCacheLog)
        {
            this.srv_port = srv_port;
            this.reaper_interval = reaper_interval;
            this.conn_expire_time = conn_expire_time;
            this._ncacheLog = NCacheLog;
            start();
        }



        /// <summary> Create a ConnectionTable</summary>
        /// <param name="r">A reference to a receiver of all messages received by this class. Method <code>receive()</code>
        /// will be called.
        /// </param>
        /// <param name="bind_addr">The host name or IP address of the interface to which the server socket will bind.
        /// This is interesting only in multi-homed systems. If bind_addr is null, the
        /// server socket will bind to the first available interface (e.g. /dev/hme0 on
        /// Solaris or /dev/eth0 on Linux systems).
        /// </param>
        /// <param name="srv_port">The port to which the server socket will bind to. If this port is reserved, the next
        /// free port will be taken (incrementing srv_port).
        /// </param>
        /// 

        public ConnectionTable(ConnectionTable.Receiver r, System.Net.IPAddress bind_addr1, System.Net.IPAddress bind_addr2, int srv_port, int port_range, ILogger NCacheLog, int retries, int retryInterval, bool isInproc,string environmentName)
        {
            setReceiver(r);
            enclosingInstance = (TCP)r;
            this.bind_addr1 = bind_addr1;

            this.bind_addr2 = bind_addr2;

            this.srv_port = srv_port;

            this.port_range = port_range;

            this._ncacheLog = NCacheLog;

            this._retries = retries;
            this._retryInterval = retryInterval;

            this._isInproc = isInproc;
            this.env_name = environmentName;
            start();
        }


        /// <summary> ConnectionTable including a connection reaper. Connections that have been idle for more than conn_expire_time
        /// milliseconds will be closed and removed from the connection table. On next access they will be re-created.
        /// 
        /// </summary>
        /// <param name="srv_port">The port on which the server will listen.If this port is reserved, the next
        /// free port will be taken (incrementing srv_port).
        /// </param>
        /// <param name="bind_addr">The host name or IP address of the interface to which the server socket will bind.
        /// This is interesting only in multi-homed systems. If bind_addr is null, the
        /// server socket will bind to the first available interface (e.g. /dev/hme0 on
        /// Solaris or /dev/eth0 on Linux systems).
        /// </param>
        /// <param name="srv_port">The port to which the server socket will bind to. If this port is reserved, the next
        /// free port will be taken (incrementing srv_port).
        /// </param>
        /// <param name="reaper_interval">Number of milliseconds to wait for reaper between attepts to reap idle connections
        /// </param>
        /// <param name="conn_expire_time">Number of milliseconds a connection can be idle (no traffic sent or received until
        /// it will be reaped
        /// </param>
        /// 

        public ConnectionTable(ConnectionTable.Receiver r, System.Net.IPAddress bind_addr, int srv_port, long reaper_interval, long conn_expire_time, ILogger NCacheLog)
        {
            setReceiver(r);
            this.bind_addr1 = bind_addr;
            this.srv_port = srv_port;
            this.reaper_interval = reaper_interval;
            this.conn_expire_time = conn_expire_time;
            this._ncacheLog = NCacheLog;
            start();
        }


        public virtual void setReceiver(ConnectionTable.Receiver r)
        {
            receiver = r;
        }


        public virtual void addConnectionListener(ConnectionTable.ConnectionListener l)
        {
            if (l != null && !conn_listeners.Contains(l))
                conn_listeners.Add(l);

        }


        public virtual void removeConnectionListener(ConnectionTable.ConnectionListener l)
        {
            if (l != null)
                conn_listeners.Remove(l);
        }

        public void publishBytesReceivedStats(long byteReceived)
        {

            enclosingInstance.Stack.perfStatsColl.IncrementBytesReceivedPerSecStats(byteReceived);

        }

        public int GetConnectionId()
        {
            lock (this)
            {
                return _idGenerator++;
            }
        }
        /// <summary>
        /// Creates connection with the members with which it has not connected before
        /// </summary>
        /// <param name="members"></param>
        /// 

        public ArrayList synchronzeMembership(ArrayList members, bool establishConnectionWithSecondaryNIC)
        {
            ArrayList failedNodes = new ArrayList();
            if (members != null)
            {
                int indexOfLocal = members.IndexOf(local_addr);

                ArrayList newConList = new ArrayList();
                foreach (Address memeber in members)
                {
                    try
                    {
                        Connection con = null;
                        if (memeber.Equals(local_addr)) continue;

                        int indexOfmember = members.IndexOf(memeber);

                        if (!conns_NIC_1.Contains(memeber))
                        {
                            NCacheLog.CriticalInfo("ConnectionTable.synchronzeMembership", "member :" + memeber + " is not part of the custer");

                            if (indexOfLocal > indexOfmember)
                            {
                                newConList.Add(memeber);
                            }
                        }
                        else
                        {
                            con = conns_NIC_1[memeber] as Connection;
                            if (con != null) con.IsPartOfCluster = true;
                        }
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("ConnectionTable.makeConnection", "member :" + memeber + " Exception:" + e.ToString());
                    }
                }
                if (newConList.Count > 0)
                {
                    lock (newcon_sync_lock)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(MakeConnectionAsync), newConList);
                        //we wait for two seconds for connection to be established.
                        Monitor.Wait(newcon_sync_lock, 2000);
                    }
                }

                members.Remove(local_addr);

                try
                {
                    conn_syn_lock.AcquireWriterLock(Timeout.Infinite);

                    ArrayList leavingMembers = new ArrayList();
                    foreach (Address oldMember in conns_NIC_1.Keys)
                    {
                        if (!members.Contains(oldMember)) leavingMembers.Add(oldMember);
                    }

                    Connection con = null;

                    foreach (Address leavingNode in leavingMembers)
                    {
                        con = conns_NIC_1[leavingNode] as Connection;

                        if (con != null && con.IsPartOfCluster && !leavingNode.IpAddress.Equals(local_addr.IpAddress))
                        {
                            NCacheLog.Error("ConnectionTable.synchronizeMembership", leavingNode.ToString() + " is no more part of the membership");
                            RemoveDedicatedMessageSender(leavingNode);
                            con.DestroySilent();
                            conns_NIC_1.Remove(leavingNode);
                        }
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ConnectionTable.makeConnection", "destroying connection with member : Exception:" + e.ToString());
                }
                finally
                {
                    conn_syn_lock.ReleaseWriterLock();

                }

                if (establishConnectionWithSecondaryNIC)
                {
                    try
                    {
                        Hashtable primaryConnections = null;
                        try
                        {
                            conn_syn_lock.AcquireWriterLock(Timeout.Infinite);
                            primaryConnections = conns_NIC_1.Clone() as Hashtable;
                        }
                        finally
                        {
                            conn_syn_lock.ReleaseWriterLock();
                        }

                        if (primaryConnections != null)
                        {
                            IDictionaryEnumerator ide = primaryConnections.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                Connection con = ide.Value as Connection;
                                if (!conns_NIC_2.Contains(con.peer_addr))
                                {
                                    ConnectToPeerOnSecondaryAddress(con);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("ConnectionTable.makeConnection", "an error occurred while establishing secondary connection. Exception:" + e.ToString());
                    }

                }



            }
            return failedNodes;
        }

        /// <summary>
        /// We establish connection asynchronously in a dedictated threadpool thread.
        /// </summary>
        /// <param name="state"></param>
        private void MakeConnectionAsync(object state)
        {
            ArrayList nodeList = state as ArrayList;
            ArrayList failedNodes = new ArrayList();
            try
            {
                foreach (Address member in nodeList)
                {
                    Connection con = null;
                    if (stopped) return;
                    try
                    {
                        con = GetConnection(member, null, true, useDualConnection, true);
                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("ConnectionTable.MakeConnectionAsync", " Exception:" + e.ToString());
                    }
                    if (con == null)
                    {
                        NCacheLog.Error("ConnectionTable.MakeConnectionAsync", "could not establish connection with " + member);
                        failedNodes.Add(member);

                    }
                    else
                    {
                        con.IsPartOfCluster = true;
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.MakeConnectionAsync", "established connection with " + member);
                    }
                }

                for (int i = 0; i < conn_listeners.Count; i++)
                    ((ConnectionTable.ConnectionListener)conn_listeners[i]).HandleFailedNodes(failedNodes);

            }
            catch (Exception e)
            {
                NCacheLog.Error("ConnectionTable.MakeConnectionAsync", " Exception:" + e.ToString());
            }
            finally
            {
                lock (newcon_sync_lock)
                {
                    Monitor.PulseAll(newcon_sync_lock);
                }
            }
        }
        /// <summary>Sends a message to a unicast destination. The destination has to be set</summary>
        /// <param name="msg">The message to send
        /// </param>
        /// <throws>  SocketException Thrown if connection cannot be established </throws>
        /// <param name="reEstablishCon">indicate that if connection is not found in
        /// connectin table then re-establish the connection or not.
        /// </param>
        public virtual long send(Address dest, IList msg, bool reEstablishCon, Array userPayload, Priority priority)
        {
            Connection conn = null;
            long bytesSent = 0;
            if (dest == null)
            {
                NCacheLog.Error("msg is null or message's destination is null");
                return bytesSent;
            }

            // 1. Try to obtain correct Connection (or create one if not yet existent)
            try
            {
                conn = GetConnection(dest, reEstablishCon);//getConnection(dest, reEstablishCon,useDualConnection);
                if (conn == null)
                {
                    if (useDedicatedSender)
                    {
                        DedicatedMessageSendManager dmSenderMgr = dedicatedSenders[dest] as DedicatedMessageSendManager;
                        if (dmSenderMgr != null)
                        {
                            int queueCount = dmSenderMgr.QueueMessage(msg, userPayload, priority);

                            enclosingInstance.Stack.perfStatsColl.IncrementTcpDownQueueCountStats(queueCount);

                            return bytesSent;
                        }
                    }
                    return bytesSent;
                }
            }
            catch (System.Net.Sockets.SocketException sock_ex)
            {
                if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.GetConnection", sock_ex.Message);
                for (int i = 0; i < conn_listeners.Count; i++)
                    ((ConnectionTable.ConnectionListener)conn_listeners[i]).couldnotConnectTo(dest);

                return bytesSent;
            }
            catch (ThreadAbortException) { return bytesSent; }
            catch (ThreadInterruptedException) { return bytesSent; }
            catch (System.Exception ex)
            {
                if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.GetConnection", "connection to " + dest + " could not be established: " + ex);
                throw new ExtSocketException(ex.ToString());
            }

            // 2. Send the message using that connection
            try
            {
                if (useDedicatedSender)
                {
                    DedicatedMessageSendManager dmSenderMgr = dedicatedSenders[dest] as DedicatedMessageSendManager;
                    if (dmSenderMgr != null)
                    {
                        int queueCount = dmSenderMgr.QueueMessage(msg, userPayload, priority);

                        enclosingInstance.Stack.perfStatsColl.IncrementTcpDownQueueCountStats(queueCount);

                        return bytesSent;
                    }
                }
                HPTimeStats socketSendTimeStats = null;
                if (enclosingInstance.enableMonitoring)
                {
                    socketSendTimeStats = new HPTimeStats();
                    socketSendTimeStats.BeginSample();
                }
                bytesSent = conn.send(msg, userPayload);

                if (socketSendTimeStats != null)
                {
                    socketSendTimeStats.EndSample();
                    long operationsperSec = (long)(1000 / socketSendTimeStats.Avg);
                }
            }
            catch (System.Exception ex)
            {
                if (conn.NeedReconnect)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send", local_addr + " re-establishing connection with " + dest);

                    conn = ReEstablishConnection(dest);
                    if (conn != null)
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send", local_addr + " re-established connection successfully with " + dest);

                        try
                        {
                            bytesSent = conn.send(msg, userPayload);
                            return bytesSent;
                        }
                        catch (Exception e)
                        {
                            NCacheLog.Error("ConnectionTable.send", "send failed after reconnect " + e.ToString());
                        }
                    }
                    else
                        NCacheLog.Error("ConnectionTable.send", local_addr + " failed to re-establish connection  with " + dest);
                }
                else
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send", local_addr + " need not to re-establish connection with " + dest);

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Ct.send", "sending message to " + dest + " failed (ex=" + ex.GetType().FullName + "); removing from connection table");
                throw new ExtSocketException(ex.ToString());
            }
            return bytesSent;
        }

        public ArrayList GetIdleMembers()
        {
            ArrayList idleMembers = new ArrayList();
            try
            {
                conn_syn_lock.AcquireReaderLock(Timeout.Infinite);

                IDictionaryEnumerator ide = conns_NIC_1.GetEnumerator();
                Connection secondary = null;
                while (ide.MoveNext())
                {

                    secondary = secondayrConns_NIC_1[ide.Key] as Connection;

                    if (((Connection)ide.Value).IsIdle)
                    {

                        if (secondary != null)
                        {
                            if (secondary.IsIdle)

                                idleMembers.Add(ide.Key);

                        }
                        else
                        {
                            idleMembers.Add(ide.Key);
                        }

                    }
                }

            }
            finally
            {
                conn_syn_lock.ReleaseReaderLock();
            }
            return idleMembers;
        }

        public void SetConnectionsStatus(bool idle)
        {
            try
            {
                conn_syn_lock.AcquireReaderLock(Timeout.Infinite);

                IDictionaryEnumerator ide = conns_NIC_1.GetEnumerator();
                while (ide.MoveNext())
                {
                    ((Connection)ide.Value).IsIdle = idle;
                }

                ide = secondayrConns_NIC_1.GetEnumerator();
                while (ide.MoveNext())
                {
                    ((Connection)ide.Value).IsIdle = idle;
                }

            }
            finally
            {
                conn_syn_lock.ReleaseReaderLock();
            }
        }
        /// <summary>
        /// Gets or sets the memory manager.
        /// </summary>
        public MemoryManager MemManager
        {
            get { return memManager; }
            set { memManager = value; }
        }

        public Socket Connect(Address node, bool withFirstNIC)
        {
            System.Net.Sockets.Socket sock;
            IPEndPoint ipRemoteEndPoint = new IPEndPoint(((Address)node).IpAddress, ((Address)node).Port);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, send_buf_size);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, recv_buf_size);

            IPAddress bindAddr = bind_addr1;

            if (!withFirstNIC)
            {
                if (bind_addr2 != null)
                    bindAddr = bind_addr2;
            }

            try
            {
                sock.Bind(new IPEndPoint(bindAddr, 0));

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Connect", "Opening socket connection with " + ipRemoteEndPoint.ToString());
                sock.Connect(ipRemoteEndPoint);

            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10049) //"Requested address is not valid in its context
                {
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, send_buf_size);
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, recv_buf_size);

                    //we do not bind an IP address.
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Connect", "Opening socket connection with " + ipRemoteEndPoint.ToString());
                    sock.Connect(ipRemoteEndPoint);
                }
                else
                    throw;
            }

            object size = sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            size = sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            int lport = 0;
            if (!sock.Connected)
                NCacheLog.Error("Connection.getConnection()", "can not be connected");
            else
            {
                lport = ((IPEndPoint)sock.LocalEndPoint).Port;
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CONNECTED at local port = " + lport);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", "client port local_port= " + ((IPEndPoint)sock.LocalEndPoint).Port + "client port remote_port= " + ((IPEndPoint)sock.RemoteEndPoint).Port);
            }
            return sock;
        }

        internal void ConnectToPeerOnSecondaryAddress(Connection con_with_NIC_1)
        {
            if (con_with_NIC_1 != null)
            {
                Address secondaryAddres = con_with_NIC_1.GetSecondaryAddressofPeer();
                if (secondaryAddres != null)
                {
                    bool establishConnection = true;
                    if (local_addr_s != null)
                    {
                        establishConnection = enclosingInstance.IsJuniorThan(con_with_NIC_1.peer_addr);
                    }

                    if (establishConnection)
                    {
                        Connection con = GetConnection(secondaryAddres, con_with_NIC_1.peer_addr, true, useDualConnection, false);
                        if (con == null)
                            NCacheLog.Error("ConnectionTable.ConnectToPeerOnSeconary", "failed to connect with " + con_with_NIC_1.peer_addr + " on second IP " + secondaryAddres);
                    }
                }

            }
        }

        internal Connection GetConnection(Address dest, bool reEstablish)
        {
            Connection con = null;
            lock (con_selection_mutex)
            {

                if (_usePrimary || !useDualConnection)
                    con = conns_NIC_1[dest] as Connection;

                else
                {
                    con = secondayrConns_NIC_1[dest] as Connection;
                    if (con == null && !reEstablish)
                        con = conns_NIC_1[dest] as Connection;
                }
                //toggle the selection to make sure that both connections are used in 
                //round robin fashion....
                _usePrimary = !_usePrimary;

            }
            if (con == null && reEstablish)
            {

                con = GetConnection(dest, null, reEstablish, useDualConnection, true);

            }
            return con;
        }

        /// <summary>
        /// Gets the primary connection.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="reEstablish"></param>
        /// <returns></returns>
        public Connection GetPrimaryConnection(Address dest, bool reEstablish)
        {
            Connection con;

            con = getConnection(dest, null, reEstablish, true, true, true,env_name);

            return con;
        }

        protected virtual Connection GetConnection(Address dest, Address primaryAddress, bool reEstablish, bool getDualConnection, bool withFirstNIC)
        {
            Connection con;
            con = getConnection(dest, primaryAddress, reEstablish, true, withFirstNIC, true,env_name);
            if (con != null && getDualConnection && reEstablish)
            {
                getConnection(dest, primaryAddress, reEstablish, false, withFirstNIC, true,env_name);
            }
            return con;
        }


        /// <summary>Try to obtain correct Connection (or create one if not yet existent) </summary>
        protected virtual Connection getConnection(Address dest, Address primaryAddress, bool reEstablishCon, bool isPrimary, bool withFirstNIC, bool connectingFirstTime,string environmentName)
        {
            //env_name

            Connection conn = null;
            System.Net.Sockets.Socket sock;
            Address peer_addr = null;
            try
            {
                if (primaryAddress == null) primaryAddress = dest;
                if (withFirstNIC)
                {
                    if (isPrimary)
                        conn = (Connection)conns_NIC_1[dest];
                    else
                        conn = (Connection)secondayrConns_NIC_1[dest];
                }
                else
                {
                    if (isPrimary)
                        conn = (Connection)conns_NIC_2[primaryAddress];
                    else
                        conn = (Connection)secondayrConns_NIC_2[primaryAddress];
                }

                if ((conn == null || !connectingFirstTime) && reEstablishCon)
                {

                    try
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", "No Connetion was found found with " + dest.ToString());
                        if (local_addr == null) return null; //cluster being stopped.

                        sock = Connect(dest, withFirstNIC);
                        conn = new Connection(this, sock, primaryAddress, this.NCacheLog, isPrimary, ServiceConfiguration.NaglingSize, _retries, _retryInterval,env_name);
                        conn.MemManager = MemManager;
                        conn.IamInitiater = true;
                        ConnectInfo conInfo = null;
                        try
                        {
                            byte connectStatus = connectingFirstTime ? ConnectInfo.CONNECT_FIRST_TIME : ConnectInfo.RECONNECTING;

                            conn.sendLocalAddress(local_addr, connectingFirstTime);
                            conn.readPeerAddress(sock, ref peer_addr);
                            if (((Address)local_addr).CompareTo((Address)dest) > 0)
                            {
                                conInfo = new ConnectInfo(connectStatus, GetConnectionId());
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection", dest + " I should send connect_info");
                                conn.SendConnectInfo(conInfo);
                            }
                            else
                            {
                                conInfo = conn.ReadConnectInfo(sock);
                            }
                            //log.Error("ConnectionTable.getConnection",   " conn_info :" + conInfo);
                            conn.ConInfo = conInfo;
                        }
                        catch (System.Exception e)
                        {
                            NCacheLog.Error("ConnectionTable.getConnection()", e.Message);
                            conn.DestroySilent();
                            return null;
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection", "b4 lock conns.SyncRoot");
                        try
                        {
                            conn_syn_lock.AcquireWriterLock(Timeout.Infinite);
                            if (isPrimary)
                            {
                                if (withFirstNIC)
                                {
                                    if (conns_NIC_1.ContainsKey(dest))
                                    {
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", "connection is already in the table");
                                        Connection tmpConn = (Connection)conns_NIC_1[dest];

                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection", "table_con id :" + tmpConn.ConInfo.Id + " new_con id :" + conn.ConInfo.Id);
                                        if (conn.ConInfo.Id < tmpConn.ConInfo.Id)
                                        {
                                            conn.Destroy();
                                            return tmpConn;
                                        }
                                        else
                                        {
                                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", dest + "--->connection present in the table is terminated");
                                            tmpConn.Destroy();
                                            conns_NIC_1.Remove(dest);
                                        }
                                    }

                                    notifyConnectionOpened(dest);
                                }
                                else
                                {
                                    if (conns_NIC_2.ContainsKey(primaryAddress))
                                    {
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", "connection is already in the table");
                                        Connection tmpConn = (Connection)conns_NIC_1[dest];
                                        if (conn.ConInfo.Id < tmpConn.ConInfo.Id)
                                        {
                                            conn.Destroy();
                                            return tmpConn;
                                        }
                                        else
                                        {
                                            NCacheLog.Warn("ConnectionTable.getConnection()", dest + "connection present in the table is terminated");
                                            tmpConn.Destroy();
                                            conns_NIC_2.Remove(primaryAddress);
                                        }
                                    }
                                }
                            }
                            addConnection(primaryAddress, conn, isPrimary, withFirstNIC);
                            if (useDedicatedSender) AddDedicatedMessageSender(primaryAddress, conn, withFirstNIC);
                            conn.init();
                        }
                        finally
                        {
                            if (conn_syn_lock.IsWriterLockHeld)
                                conn_syn_lock.ReleaseWriterLock();
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection", "after lock conns.SyncRoot");

                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()", "connection is now working");
                    }
                    finally
                    {

                    }

                }
            }
            catch (System.Threading.ThreadAbortException e)
            {
                if (conn != null) conn.Destroy();
                conn = null;
            }
            catch (System.Threading.ThreadInterruptedException ex)
            {
                if (conn != null) conn.Destroy();
                conn = null;
            }
            finally
            {
            }
            return conn;
        }

        /// <summary>
        /// Re-Establishes the connection to a node in case an already existing
        /// connection is broken disgracefully.
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>Connection</returns>
        public Connection ReEstablishConnection(Address addr)
        {
            Connection con = null;
            try
            {
                if (addr == null) return null;
                conn_syn_lock.AcquireWriterLock(Timeout.Infinite);
                con = conns_NIC_1[addr] as Connection;

                //Another thread might have been able to re-establish the connnection.
                if (con != null && con.IsConnected)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ReEstablishConnection", "already re-established connection with " + addr);
                    return con;
                }
                con = getConnection(addr, null, true, true, true, false,env_name);


            }
            catch (Exception ex)
            {
                con = null;
                NCacheLog.Error("ConnectionTable.ReEstablishConnection", "failed to re-establish connection with " + addr + " " + ex.ToString());
            }
            finally
            {
                conn_syn_lock.ReleaseWriterLock();
            }
            return con;
        }


        protected void AddDedicatedMessageSender(Address primaryAddress, Connection con, bool onPrimaryNIC)

        {
            if (con != null)
            {
                lock (dedicatedSenders.SyncRoot)
                {
                    DedicatedMessageSendManager dmSenderManager = dedicatedSenders[primaryAddress] as DedicatedMessageSendManager;
                    if (dmSenderManager == null)
                    {
                        dmSenderManager = new DedicatedMessageSendManager(_ncacheLog);
                        dedicatedSenders[primaryAddress] = dmSenderManager;

                        dmSenderManager.AddDedicatedSenderThread(con, onPrimaryNIC);

                    }
                    else
                    {
                        dmSenderManager.UpdateConnection(con);
                    }
                }
            }
        }

        protected void RemoveDedicatedMessageSender(Address node)
        {
            if (node != null)
            {
                lock (dedicatedSenders.SyncRoot)
                {
                    DedicatedMessageSendManager dmSenderManager = dedicatedSenders[node] as DedicatedMessageSendManager;
                    if (dmSenderManager != null)
                    {
                        dedicatedSenders.Remove(node);
                        dmSenderManager.Dispose();
                    }
                }
            }
        }

        private void StopDedicatedSenders()
        {
            lock (dedicatedSenders.SyncRoot)
            {
                if (dedicatedSenders != null)
                {
                    IDictionaryEnumerator ide = dedicatedSenders.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        DedicatedMessageSendManager dmSenderManager = ide.Value as DedicatedMessageSendManager;
                        if (dmSenderManager != null)
                        {
                            dmSenderManager.Dispose();
                        }
                    }
                    dedicatedSenders.Clear();
                }
            }
        }

        public virtual void start()
        {
            srv_sock1 = createServerSocket(bind_addr1, srv_port);


            if (bind_addr2 != null)
                srv_sock2 = createServerSocket(bind_addr2, 0);


            if (srv_sock1 == null)
            {
                throw new ExtSocketException("Cluster can not be started on the given server port. The port might be already in use.");
            }
            if (bind_addr1 != null)
                local_addr = new Address(bind_addr1, ((System.Net.IPEndPoint)srv_sock1.LocalEndPoint).Port);
            else
                local_addr = new Address(((IPEndPoint)srv_sock1.LocalEndPoint).Address, ((System.Net.IPEndPoint)srv_sock1.LocalEndPoint).Port);

            if (srv_sock2 != null)
            {
                local_addr_s = new Address(bind_addr2, ((System.Net.IPEndPoint)srv_sock2.LocalEndPoint).Port);
            }


            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("server socket created on " + local_addr);

            enableNaggling = ServiceConfiguration.EnableNagling;
            nagglingSize = ServiceConfiguration.NaglingSize * 1024;
            enableMonitoring = ServiceConfiguration.EnableDebuggingCounters;
            useDualConnection = ServiceConfiguration.EnableDualSocket;


            acceptor1 = new Thread(new ThreadStart(this.RunPrimary));
            acceptor1.Name = "ConnectionTable.AcceptorThread_p";
            acceptor1.IsBackground = true;
            acceptor1.Start();

            NCacheLog.CriticalInfo("ConnectionTable.Start", "operating parameters -> [bind_addr :" + local_addr + " ; dual_socket: " + useDualConnection + " ; nagling: " + ServiceConfiguration.EnableNagling + " ; nagling_size : " + ServiceConfiguration.NaglingSize + " ]");

            // start the connection reaper - will periodically remove unused connections
            if (use_reaper && reaper == null)
            {
                reaper = new Reaper(this);
                reaper.start();
            }

        }


        /// <summary>Closes all open sockets, the server socket and all threads waiting for incoming messages </summary>
        public virtual void stop()
        {
            stopped = true;
            System.Collections.IEnumerator it = null;
            Connection conn;
            System.Net.Sockets.Socket tmp;
            if (disconThread != null)
            {
                //Flush: Buffer Appender can clear all Logs as reported by this thread
                NCacheLog.Flush();
#if !NETCORE
                disconThread.Abort();
#elif NETCORE
                disconThread.Interrupt();
#endif
                disconThread = null;
            }
            // 1. close the server socket (this also stops the acceptor thread)
            if (srv_sock1 != null)
            {
                try
                {
                    tmp = srv_sock1;
                    srv_sock1 = null;
#if NETCORE
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        tmp.Shutdown(SocketShutdown.Both);
                    }    
#endif
                    tmp.Close();
                }
                catch (System.Exception)
                {
                }
            }

            if (srv_sock2 != null)
            {
                try
                {
                    tmp = srv_sock2;
                    srv_sock2 = null;
#if NETCORE
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        tmp.Shutdown(SocketShutdown.Both);
                    }
#endif
                    tmp.Close();
                }
                catch (System.Exception)
                {
                }
            }

            local_addr = null;

            //2. Stop dedicated senders
            StopDedicatedSenders();

            // 3. then close the connections

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.stop", "b4 lock conns.SyncRoot");
            try
            {
                conn_syn_lock.AcquireWriterLock(Timeout.Infinite);

                Hashtable connsCopy = conns_NIC_1.Clone() as Hashtable;
                it = connsCopy.Values.GetEnumerator();

                while (it.MoveNext())
                {
                    conn = (Connection)it.Current;
                    conn.SendLeaveNotification();
                    conn.Destroy();
                }
                conns_NIC_1.Clear();

                connsCopy = secondayrConns_NIC_1.Clone() as Hashtable;
                it = connsCopy.Values.GetEnumerator();

                while (it.MoveNext())
                {
                    conn = (Connection)it.Current;
                    conn.DestroySilent();
                }
                secondayrConns_NIC_1.Clear();

                connsCopy = conns_NIC_2.Clone() as Hashtable;
                it = connsCopy.Values.GetEnumerator();

                while (it.MoveNext())
                {
                    conn = (Connection)it.Current;
                    conn.SendLeaveNotification();
                    conn.Destroy();
                }
                conns_NIC_2.Clear();

                connsCopy = secondayrConns_NIC_2.Clone() as Hashtable;
                it = connsCopy.Values.GetEnumerator();

                while (it.MoveNext())
                {
                    conn = (Connection)it.Current;
                    conn.DestroySilent();
                }
                secondayrConns_NIC_2.Clear();

            }
            finally
            {
                if (conn_syn_lock.IsWriterLockHeld)
                    conn_syn_lock.ReleaseWriterLock();
            }
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.stop", "after lock conns.SyncRoot");
        }


        /// <summary>Remove <code>addr</code>from connection table. This is typically triggered when a member is suspected.</summary>
        /// 

        public virtual void remove(Address addr, bool isPrimary)
        {
            Connection conn;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.remove", "b4 lock conns.SyncRoot");
            try
            {
                conn_syn_lock.AcquireWriterLock(Timeout.Infinite);

                if (isPrimary)
                {

                    conn = (Connection)conns_NIC_1[addr];

                    if (conn != null)
                    {
                        try
                        {
                            conn.Destroy(); // won't do anything if already destroyed
                        }
                        catch (System.Exception)
                        {
                        }
                        conns_NIC_1.Remove(addr);
                    }

                }

                conn = (Connection)secondayrConns_NIC_1[addr];

                if (conn != null)
                {
                    try
                    {
                        conn.Destroy(); // won't do anything if already destroyed
                    }
                    catch (System.Exception)
                    {
                    }
                    secondayrConns_NIC_1.Remove(addr);
                }


                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("addr=" + addr + ",   connections are " + ToString());
            }
            finally
            {
                if (conn_syn_lock.IsWriterLockHeld)
                    conn_syn_lock.ReleaseWriterLock();
            }
            RemoveDedicatedMessageSender(addr);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.remove", "after lock conns.SyncRoot");
        }

        Thread disconThread;
        private bool _isInproc;

        public void ConfigureNodeRejoining(ArrayList list)
        {
            {
                _nodeRejoiningList = list;
            }
            bool simulate = false;

            simulate = ServiceConfiguration.SimulateSocketClose;

            if (simulate && disconThread == null)
            {
                disconThread = new Thread(new ThreadStart(Disconnect));
                disconThread.IsBackground = true;
                disconThread.Start();
            }

        }
        private void Disconnect()
        {
            int interval = 60;

            NCacheLog.CriticalInfo("ConnectionTable.Disconnect", "simulating sudden disconnect " + Thread.CurrentThread.ManagedThreadId);

            interval = ServiceConfiguration.SocketCloseInterval;

            NCacheLog.CriticalInfo("ConnectionTable.Disconnect", "socket close interval :" + interval + " seconds");
            while (true)
            {
                try
                {
                    Random random = new Random(10);
                    int randomInterval = random.Next(3, 12);
                    Thread.Sleep(new TimeSpan(0, 0, (interval + randomInterval)));
                    NCacheLog.CriticalInfo("ConnectionTable.Disconnect", "poling interval :" + (interval + randomInterval));
                    if (_nodeRejoiningList != null && _nodeRejoiningList.Count > 0)
                    {
                        int nextNode = -1;
                        if (_nodeRejoiningList.Count == 2)
                        {
                            if (_nodeRejoiningList[0].Equals(local_addr))
                            {
                                nextNode = 1;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < _nodeRejoiningList.Count; i++)
                            {
                                if (_nodeRejoiningList[i].Equals(local_addr))
                                {
                                    nextNode = i + 1;
                                    if (nextNode == _nodeRejoiningList.Count)
                                        nextNode = 0;
                                    break;
                                }
                            }
                        }
                        if (nextNode != -1)
                        {
                            Connection con = GetConnection(_nodeRejoiningList[nextNode] as Address, false);
                            if (con != null)
                            {
                                NCacheLog.CriticalInfo("ConnectionTable.Disconnect", "going to disconnect with " + con.peer_addr);
                                con.markedClose = true;
#if NETCORE
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                                {
                                    con.sock.Shutdown(SocketShutdown.Both);
                                }
#endif
                                con.sock.Close();
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    NCacheLog.CriticalInfo("ConnetionTable.Disconnect", e.ToString());
                }
            }
        }
        private void InformConnectionClose(Address node)
        {
            if (!stopped)
            {
                Event evt = new Event(Event.CONNECTION_BREAKAGE, node, Priority.High);
                enclosingInstance.passUp(evt);
            }
        }
        private void InformConnectionReestablishment(Address node)
        {
            if (!stopped)
            {
                Event evt = new Event(Event.CONNECTION_RE_ESTABLISHED, node, Priority.High);
                enclosingInstance.passUp(evt);
            }
        }
        public Connection Reconnect(Address node, out bool connectionCloseNotified)
        {
            Connection peerConnection = null;
            bool shouldConnect = false;
            bool initiateReconnection = false;
            connectionCloseNotified = false;

            if (node == null)
            {
                NCacheLog.Error("ConnectionTable.Reconnect", "node name is NULL");
                return null;
            }



            lock (con_reestablish_sync)
            {
                try
                {
                    if (_nodeRejoiningList != null)
                    {
                        lock (_nodeRejoiningList.SyncRoot)
                        {
                            int localNodeIndex = -1;
                            int nodeIndex = -1;
                            for (int i = 0; i < _nodeRejoiningList.Count; i++)
                            {
                                Address listNode = _nodeRejoiningList[i] as Address;
                                if (listNode.Equals(node))
                                {
                                    nodeIndex = i;
                                }
                                if (listNode.Equals(LocalAddress))
                                {
                                    localNodeIndex = i;
                                }
                            }
                            if (nodeIndex >= 0 && localNodeIndex >= 0)
                            {
                                shouldConnect = true;
                                if (nodeIndex > localNodeIndex)
                                {
                                    initiateReconnection = true;
                                }
                            }
                        }
                    }
                    if (shouldConnect)
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Reconnect", node.ToString() + " is part of node rejoining list");

                        int connectionRetries = _retries;



                        while (connectionRetries-- > 0)
                        {
                            peerConnection = ReEstablishConnection(node);
                            if (peerConnection == null)
                            {

                                Thread.Sleep(new TimeSpan(0, 0, _retryInterval));

                            }
                            else
                            {
                                break;
                            }
                        }
                        if (peerConnection == null)
                        {

                            if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.Reconnect", "Can not establish connection with " + node + " after " + _retries + " retries");

                            notifyConnectionClosed(node);
                            connectionCloseNotified = false;

                        }
                        else
                        {
                            NCacheLog.CriticalInfo("ConnectionTable.Reconnect", "Connection re-establised with " + node);
                            if (peerConnection.IamInitiater)
                            {
                                //inform above layers about re-connection.
                                System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(InformAboutReconnection), node);
                            }
                        }
                    }
                    else
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Reconnect", node.ToString() + " is not part of the node rejoining list");
                        notifyConnectionClosed(node);
                        connectionCloseNotified = true;

                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ConnectionTable.Reconnect", "An error occurred while reconnecting with " + node + " Error :" + e.ToString());
                    notifyConnectionClosed(node);
                    connectionCloseNotified = true;

                }
            }

            return peerConnection;
        }
        private void InformAboutReconnection(object state)
        {
            Address node = state as Address;

            try
            {
                enclosingInstance.passUp(new Event(Event.NODE_REJOINING, node, Alachisoft.NCache.Common.Enum.Priority.High));
            }
            catch (Exception e)
            {
                NCacheLog.Error("ConnectionTable.InformAboutReconnection", e.ToString());
            }
        }
        public void RunPrimary()
        {
            Run(new object[] { srv_sock1, true });
        }

        public void RunSecondary()
        {
            Run(new object[] { srv_sock2, false });
        }

        /// <summary> Acceptor thread. Continuously accept new connections. Create a new thread for each new
        /// connection and put it in conns. When the thread should stop, it is
        /// interrupted by the thread creator.
        /// </summary>
        public virtual void Run(Object arg)
        {
            Object[] objArr = arg as object[];
            Socket socketListener = objArr[0] as Socket;
            bool isPrimaryListener = (bool)objArr[1];

            System.Net.Sockets.Socket client_sock;
            Connection conn = null;
            Address peer_addr = null;

            while (socketListener != null)
            {
                try
                {
                    client_sock = socketListener.Accept();
                    int cport = ((IPEndPoint)client_sock.RemoteEndPoint).Port;

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "CONNECTION ACCPETED Remote port = " + cport);
                    client_sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);

                    client_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, send_buf_size);
                    client_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, recv_buf_size);

                    object size = client_sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    size = client_sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
                    // create new thread and add to conn table
                    conn = new Connection(this, client_sock, null, _ncacheLog, true, ServiceConfiguration.NaglingSize, _retries, _retryInterval,env_name); // will call receive(msg)

                    // get peer's address
                    bool connectingFirstTime = conn.readPeerAddress(client_sock, ref peer_addr);
                    conn.sendLocalAddress(local_addr, connectingFirstTime);
                    ConnectInfo conInfo = null;
                    if (((Address)local_addr).CompareTo((Address)peer_addr) > 0)
                    {
                        conInfo = new ConnectInfo(ConnectInfo.CONNECT_FIRST_TIME, GetConnectionId());
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run", peer_addr + " I should send connect_info");
                        conn.SendConnectInfo(conInfo);
                    }
                    else
                    {
                        conInfo = conn.ReadConnectInfo(client_sock);
                    }
                    conn.ConInfo = conInfo;
                    conn.ConInfo.ConnectStatus = connectingFirstTime ? ConnectInfo.CONNECT_FIRST_TIME : ConnectInfo.RECONNECTING;

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "Read peer address " + peer_addr.ToString() + "at port" + cport);

                    conn.PeerAddress = peer_addr;

                    NCacheLog.CriticalInfo("ConnectionTable.Run", peer_addr + " established connection");

                    if (conInfo.ConnectStatus == ConnectInfo.RECONNECTING)
                    {
                        //if other node is reconnecting then we should check for its member ship first.
                        bool ismember = enclosingInstance.IsMember(peer_addr);
                        if (!ismember)
                        {
                            NCacheLog.CriticalInfo("ConnectionTable.Run", "ConnectionTable.Run" + peer_addr + " has connected. but it is no more part of the membership");
                            conn.SendLeaveNotification();
                            Thread.Sleep(1000); //just to make sure that peer node receives the leave notification.
                            conn.Destroy();
                            continue;
                        }
                    }

                    bool isPrimary = true;
                    
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.run", "b4 lock conns.SyncRoot");
                    try
                    {
                        conn_syn_lock.AcquireWriterLock(Timeout.Infinite);

                        if (isPrimaryListener)
                        {
                            if (conns_NIC_1.ContainsKey(peer_addr))
                            {
                                if (!secondayrConns_NIC_1.Contains(peer_addr) && useDualConnection)
                                {
                                    secondayrConns_NIC_1[peer_addr] = conn;
                                    isPrimary = false;
                                }
                                else
                                {
                                    Connection tmpConn = (Connection)conns_NIC_1[peer_addr];
                                    if (conn.ConInfo.Id < tmpConn.ConInfo.Id && conn.ConInfo.ConnectStatus != ConnectInfo.CONNECT_FIRST_TIME)
                                    {
                                        NCacheLog.CriticalInfo("ConnectionTable.Run", "1. Destroying Connection (conn.ConInfo.Id < tmpConn.ConInfo.Id)" + conn.ConInfo.Id.ToString() + ":" + tmpConn.ConInfo.Id.ToString() + conn.ToString());
                                        conn.Destroy();
                                        continue;
                                    }
                                    else
                                    {
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "-->connection present in the talble is terminated");
                                        tmpConn.Destroy();
                                        conns_NIC_1.Remove(peer_addr);
                                    }

                                }

                            }
                        }

                        else
                        {
                            if (conns_NIC_2.ContainsKey(peer_addr))
                            {
                                if (!secondayrConns_NIC_2.Contains(peer_addr) && useDualConnection)
                                {
                                    secondayrConns_NIC_2[peer_addr] = conn;
                                    isPrimary = false;
                                }
                                else
                                {
                                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "connection alrady exists in the table");
                                    Connection tmpConn = (Connection)conns_NIC_2[peer_addr];
                                    if (conn.ConInfo.Id < tmpConn.ConInfo.Id)
                                    {
                                        conn.Destroy();
                                        continue;
                                    }
                                    else
                                    {
                                        NCacheLog.Error("ConnectionTable.Run()", "connection present in the talble is terminated");
                                        tmpConn.Destroy();
                                        conns_NIC_2.Remove(peer_addr);
                                    }
                                }
                            }
                        }
                        conn.IsPrimary = isPrimary;
                        addConnection(peer_addr, conn, isPrimary, isPrimaryListener);

                        conn.MemManager = memManager;

                        if (useDedicatedSender) AddDedicatedMessageSender(conn.peer_addr, conn, isPrimaryListener);

                        conn.init(); // starts handler thread on this socket
                    }
                    finally
                    {
                        if (conn_syn_lock.IsWriterLockHeld)
                            conn_syn_lock.ReleaseWriterLock();
                    }
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.run", "after lock conns.SyncRoot");

                    if (isPrimary && isPrimaryListener)

                        notifyConnectionOpened(peer_addr);



                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "connection working now");
                }
                catch (VersionMismatchException ex)
                {
                    NCacheLog.Error("ConnectionTable.Run", "exception is " + ex);
                    if (conn != null)
                        conn.DestroySilent();
                }
                catch (ExtSocketException sock_ex)
                {
                    NCacheLog.Error("ConnectionTable.Run", "exception is " + sock_ex);
                    if (conn != null)
                        conn.DestroySilent();
                    if (srv_sock1 == null)
                        break; // socket was closed, therefore stop
                }
                catch (System.Exception ex)
                {
                    NCacheLog.Error("ConnectionTable.Run", "exception is " + ex);
                    if (srv_sock1 == null)
                        break; // socket was closed, therefore stop

                }
            }
        }



        /// <summary> Calls the receiver callback. We serialize access to this method because it may be called concurrently
        /// by several Connection handler threads. Therefore the receiver doesn't need to synchronize.
        /// </summary>
        public virtual void receive(Message msg)
        {
            if (receiver != null)
            {
                receiver.receive(msg);
            }
            else
                NCacheLog.Error("receiver is null (not set) !");
        }


        public override System.String ToString()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder();
            Address key;
            Connection val;

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ToString", "b4 lock conns.SyncRoot");
            try
            {
                conn_syn_lock.AcquireReaderLock(Timeout.Infinite);

                ret.Append("connections (" + conns_NIC_1.Count + "):\n");
                for (System.Collections.IEnumerator e = conns_NIC_1.Keys.GetEnumerator(); e.MoveNext();)
                {
                    key = (Address)e.Current;
                    val = (Connection)conns_NIC_1[key];
                    ret.Append("key: " + key.ToString() + ": " + val.ToString() + '\n');
                }
            }
            finally
            {
                conn_syn_lock.ReleaseReaderLock();
            }
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ToString", "b4 lock conns.SyncRoot");
            ret.Append('\n');
            return ret.ToString();
        }

        public bool ConnectionExist(Address member)
        {
            return conns_NIC_1 != null ? conns_NIC_1.Contains(member) : false;
        }
        /// <summary>Finds first available port starting at start_port and returns server socket. Sets srv_port </summary>
        protected internal virtual System.Net.Sockets.Socket createServerSocket(IPAddress bind_addr, int start_port)
        {
            System.Net.Sockets.Socket ret = null;
            //We will try to start on a two  ports
            for (int i = 1; i <= port_range; i++)
            {

                try
                {
                    if (bind_addr == null)
                    {
                        System.Net.Sockets.Socket temp_tcpListener;
                        IPEndPoint temp_endpoint = new IPEndPoint(IPAddress.Any, start_port);
                        temp_tcpListener = new System.Net.Sockets.Socket(temp_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        temp_tcpListener.Bind(temp_endpoint);
                        temp_tcpListener.Listen(start_port);
                        ret = temp_tcpListener;
                    }
                    else
                    {
                        System.Net.Sockets.Socket temp_tcpListener2;
                        IPEndPoint temp_endpoint = new System.Net.IPEndPoint(bind_addr, start_port);
                        temp_tcpListener2 = new System.Net.Sockets.Socket(temp_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        temp_tcpListener2.Bind(temp_endpoint);
                        temp_tcpListener2.Listen(start_port);
                        ret = temp_tcpListener2;
                    }
                }
                catch (System.Net.Sockets.SocketException bind_ex)
                {
                    start_port++;
                    ret = null;
                    continue;

                }
                catch (System.IO.IOException io_ex)
                {
                    ret = null;
                }
                srv_port = start_port;

                break;
            }

            if (ret == null) NCacheLog.Error("ConnectionTable.createServerSocket", "binding failed " + bind_addr == null ? "null" : bind_addr.ToString() + " is not valid");
            return ret;
        }


        internal virtual void notifyConnectionOpened(Address peer)
        {
            if (peer == null)
                return;
            for (int i = 0; i < conn_listeners.Count; i++)
                ((ConnectionTable.ConnectionListener)conn_listeners[i]).connectionOpened(peer);
        }

        internal virtual void notifyConnectionClosed(Address peer)
        {
            NCacheLog.CriticalInfo("ConnectionTable.notifyConnectionClosed", peer.ToString() + " connection close notification");
            if (peer == null)
                return;
            for (int i = 0; i < conn_listeners.Count; i++)
                ((ConnectionTable.ConnectionListener)conn_listeners[i]).connectionClosed(peer);
        }


        internal virtual void addConnection(Address peer, Connection c, bool isPrimary, bool fromNIC1)
        {
            if (fromNIC1)
            {
                if (isPrimary)
                    conns_NIC_1[peer] = c;
                else
                    secondayrConns_NIC_1[peer] = c;
            }
            else
            {
                if (isPrimary)
                    conns_NIC_2[peer] = c;
                else
                    secondayrConns_NIC_2[peer] = c;
            }

            if (reaper != null && !reaper.Running)
                reaper.start();
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.addConnection", "Connection added to the table");
        }



        internal class Connection : IThreadRunnable
        {
            private void InitBlock(ConnectionTable enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private ConnectionTable enclosingInstance;
            private ProductVersion _prodVersion = ProductVersion.ProductInfo;


            virtual public Address PeerAddress
            {
                set
                {
                    this.peer_addr = value;
                }

            }
            public ConnectionTable Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            internal System.Net.Sockets.Socket sock = null; // socket to/from peer (result of srv_sock.accept() or new Socket())
            internal ThreadClass handler = null; // thread for receiving messages
            internal Address peer_addr = null; // address of the 'other end' of the connection
            internal System.Object send_mutex = new System.Object(); // serialize sends
            internal long last_access = (System.DateTime.Now.Ticks - 621355968000000000) / 10000; // last time a message was sent or received
            internal bool self_close = false;
            internal Stream inStream = new MemoryStream(8000);
            private MemoryManager memManager;
            private bool _isIdle = false;
            private bool leavingGracefully = false;
            private bool socket_error = false;
            private bool isConnected = true;

            const long sendBufferSize = 1024 * 1024;
            const long receiveBufferSize = 1024 * 1024;
            private byte[] sendBuffer = new byte[sendBufferSize];
            private byte[] receiveBuffer = null;

            private ILogger _ncacheLog;
            public ILogger NCacheLog
            {
                get { return _ncacheLog; }
            }
            const int LARGE_OBJECT_SIZE = 79 * 1024;
            internal Socket _secondarySock;

            internal bool _isPrimary;

            object get_addr_sync = new object();
            Address secondaryAddress;
            object initializationPhase_mutex = new object();
            bool inInitializationPhase = false;
            //muds:

            private int _retries;
            private int _retryInterval;

            bool isMember;
            public bool markedClose;
            private ConnectInfo conInfo;
            private bool iaminitiater;
            private TimeSpan _worsRecvTime = new TimeSpan(0, 0, 0);
            private TimeSpan _worsSendTime = new TimeSpan(0, 0, 0);
            private string env_name = "";

            internal Connection(ConnectionTable enclosingInstance, System.Net.Sockets.Socket s, Address peer_addr, ILogger NCacheLog, bool isPrimary, int naglingSize, int retries, int retryInterval,string environmentName)

            {
                InitBlock(enclosingInstance);
                sock = s;
                this.peer_addr = peer_addr;

                this._retries = retries;
                this._retryInterval = retryInterval;

                this._ncacheLog = NCacheLog;

                _isPrimary = isPrimary;

                if (naglingSize > receiveBufferSize)
                    receiveBuffer = new byte[naglingSize + 8];
                else
                    receiveBuffer = new byte[receiveBufferSize];

                _socSendArgs = new SocketAsyncEventArgs();
                _socSendArgs.UserToken = new SendContext();
                _socSendArgs.Completed += OnCompleteAsyncSend;
                env_name = environmentName;
            }

            public ConnectInfo ConInfo
            {
                get { return conInfo; }
                set { conInfo = value; }
            }
            /// <summary>
            /// Gets/Sets the flag which indicates that whether this node remained 
            /// part of the cluster at any time or not.
            /// </summary>
            public bool IsPartOfCluster
            {
                get { return isMember; }
                set { isMember = value; }
            }

            public bool IamInitiater
            {
                get { return iaminitiater; }
                set { iaminitiater = value; }
            }

            public bool IsPrimary
            {
                get { return _isPrimary; }
                set { _isPrimary = value; }
            }

            public bool IsIdle
            {
                get { return _isIdle; }
                set { lock (send_mutex) { _isIdle = value; } }
            }
            public bool IsConnected
            {
                get { return isConnected; }
                set { isConnected = value; }
            }

            internal virtual bool established()
            {
                return handler != null;
            }
            public MemoryManager MemManager
            {
                get { return memManager; }
                set { memManager = value; }
            }
            internal virtual void updateLastAccessed()
            {
                last_access = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
            }

            public bool NeedReconnect
            {
                get
                {
                    return (!leavingGracefully && !self_close);
                }
            }

            internal virtual void init()
            {

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("connection was created to " + peer_addr);
                if (handler == null)
                {
                    handler = new ThreadClass(new System.Threading.ThreadStart(this.Run), "ConnectionTable.Connection.HandlerThread");
                    handler.IsBackground = true;
                    handler.Start();
                }
            }

            #region Async mechanism

            private const int BufferHeader = sizeof(int);

            private readonly object _sendWaitObject = new object();

            private SocketAsyncEventArgs _socSendArgs;
            private SocketAsyncEventArgs _socRecArgs;

            private bool _isSending = false;

            private bool BeginAsyncSend(SocketAsyncEventArgs args, byte[] buffer, int offset, int count)
            {
                SendContext sendingStruct = (SendContext)args.UserToken;
                sendingStruct.DataToSend = count;
                sendingStruct.Buffer = new ArraySegment<byte>(buffer, offset, count);

                _socSendArgs.SetBuffer(buffer, offset, count);

                if (!sock.SendAsync(_socSendArgs))
                {
                    SendDataAsync(_socSendArgs);
                    return false;
                }
                return true;
            }

            private void BeginAsyncReceive(long requestSize, byte[] buffer, int offset, int count)
            {
                ReceiveContext receiveStruct = (ReceiveContext)_socRecArgs.UserToken;
                receiveStruct.RequestSize = requestSize;
                receiveStruct.ChunkToReceive = count;
                receiveStruct.Buffer = new ArraySegment<byte>(buffer, offset, count);

                _socRecArgs.SetBuffer(buffer, offset, count);
                if (!sock.Connected)
                    throw new ExtSocketException("socket closed");
                if (!sock.ReceiveAsync(_socRecArgs))
                {
                    ReceiveDataAsync(_socRecArgs);
                }
            }

            private void OnCompleteAsyncSend(object obj, SocketAsyncEventArgs args)
            {
                SendDataAsync(args);
            }

            private void OnCompleteAsyncReceive(object sender, SocketAsyncEventArgs e)
            {
                ReceiveDataAsync(e);
            }

            private void SendDataAsync(SocketAsyncEventArgs sockAsynArgs)
            {
                try
                {
                    SendContext sendStruct = (SendContext)sockAsynArgs.UserToken;
                    int bytesSent = sockAsynArgs.BytesTransferred;

                    if (bytesSent == 0)
                    {
                        PulseSend();
                        return;
                    }

                    if (sockAsynArgs.SocketError != SocketError.Success)
                    {
                        PulseSend();
                        return;
                    }

                    if (bytesSent < sendStruct.DataToSend)
                    {
                        int newDataToSend = sendStruct.DataToSend - bytesSent;
                        int newOffset = sendStruct.Buffer.Array.Length - newDataToSend;
                        BeginAsyncSend(sockAsynArgs, sendStruct.Buffer.Array, newOffset, newDataToSend);
                        return;
                    }

                    PulseSend();
                }
                catch (ThreadAbortException) { }
                catch (ThreadInterruptedException) { }
                catch (Exception e)
                {
                    lock (send_mutex) { isConnected = false; }
                    NCacheLog.Error("Connection.SendDataAsync()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + e);
                }
            }

            private void ReceiveDataAsync(SocketAsyncEventArgs sockAsynArgs)
            {
                try
                {
                    ReceiveContext receiveStruct = (ReceiveContext)sockAsynArgs.UserToken;
                    int bytesReceived = sockAsynArgs.BytesTransferred;

                    if (bytesReceived == 0 || sockAsynArgs.SocketError != SocketError.Success)
                    {
                        NCacheLog.Error("Connection.ReceiveDataAsnc()", "connection closed with " + peer_addr.ToString() + "");

                        lock (send_mutex) { isConnected = false; }
                        try { }
                        finally
                        {
                            receiveStruct.StreamBuffer.Dispose();
                        }

                        return;
                    }

                    receiveStruct.RequestSize -= bytesReceived;
                    receiveStruct.StreamBuffer.Write(receiveStruct.Buffer.Array, receiveStruct.Buffer.Offset, bytesReceived);

                    //For checking of the receival of current chunk...
                    if (bytesReceived < receiveStruct.ChunkToReceive)
                    {
                        int newDataToReceive = receiveStruct.ChunkToReceive - bytesReceived;
                        int newOffset = receiveStruct.Buffer.Array.Length - newDataToReceive;
                        BeginAsyncReceive(receiveStruct.RequestSize, receiveStruct.Buffer.Array, newOffset,
                            newDataToReceive);
                        return;
                    }

                    //Check if there is any more command/request left to receive...
                    if (receiveStruct.RequestSize > 0)
                    {
                        int dataToRecieve = GetSafeCollectionCount(receiveStruct.RequestSize);
                        BeginAsyncReceive(receiveStruct.RequestSize, new byte[dataToRecieve], 0, dataToRecieve);
                        return;
                    }

                    switch (receiveStruct.State)
                    {
                        case ReceivingState.ReceivingDataLength:

                            //Reading message's header...
                            byte[] reqSizeBytes = new byte[BufferHeader];
                            Array.Copy(sockAsynArgs.Buffer, reqSizeBytes, BufferHeader);

                            //Reading message's header...
                            int dataSize = BitConverter.ToInt32(reqSizeBytes, 0);
                            int dataToRecieve = GetSafeCollectionCount(dataSize);
                            receiveStruct.State = ReceivingState.ReceivingData;
                            receiveStruct.StreamBuffer = new ClusteredMemoryStream(dataSize);
                            receiveStruct.ReceiveTimeStats = null;

                            if (enclosingInstance.enableMonitoring)
                            {
                                receiveStruct.ReceiveTimeStats = new HPTimeStats();
                                receiveStruct.ReceiveTimeStats.BeginSample();
                            }

                            receiveStruct.ReceiveStartTime = DateTime.Now;
                            BeginAsyncReceive(dataSize, new byte[dataToRecieve], 0, dataToRecieve);
                            break;

                        case ReceivingState.ReceivingData:

                            using (Stream dataStream = receiveStruct.StreamBuffer)
                            {

                                HPTimeStats socketReceiveTimeStats = receiveStruct.ReceiveTimeStats;
                                DateTime startTime = receiveStruct.ReceiveStartTime;

                                if (sock == null)
                                {
                                    NCacheLog.Error("input stream is null !");
                                    return;
                                }

                                //Start receiving before processing the current data...
                                receiveStruct.State = ReceivingState.ReceivingDataLength;
                                receiveStruct.StreamBuffer = new ClusteredMemoryStream(BufferHeader);
                                BeginAsyncReceive(BufferHeader, new byte[BufferHeader], 0, BufferHeader);

                                DateTime now = DateTime.Now;
                                TimeSpan receiveTime = now - startTime;

                                if (receiveTime.TotalMilliseconds > _worsRecvTime.TotalMilliseconds)
                                {
                                    _worsRecvTime = receiveTime;
                                }

                                if (socketReceiveTimeStats != null)
                                {
                                    socketReceiveTimeStats.EndSample();
                                    enclosingInstance.enclosingInstance.Stack.perfStatsColl
                                        .IncrementSocketReceiveTimeStats((long)socketReceiveTimeStats.Current);
                                    enclosingInstance.enclosingInstance.Stack.perfStatsColl
                                        .IncrementSocketReceiveSizeStats(dataStream.Length);
                                }

                                enclosingInstance.publishBytesReceivedStats(dataStream.Length + BufferHeader);

                                dataStream.Position = 0;
                                int streamOffset;

                                byte[] lengthBytes = new byte[sizeof(int)];
                                dataStream.Read(lengthBytes, 0, sizeof(int));
                                int noOfMessages = BitConverter.ToInt32(lengthBytes, 0);
                                streamOffset = +sizeof(int);

                                for (int msgCount = 0; msgCount < noOfMessages; msgCount++)
                                {

                                    int totalMessagelength, messageLength;
                                    Message msg = ReadMessage(dataStream, out messageLength, out totalMessagelength);

                                    if (msg != null)
                                    {

                                        int payLoadLength = totalMessagelength - messageLength - sizeof(int);
                                        if (payLoadLength > 0)
                                        {
                                            msg.Payload = ReadPayload(dataStream, payLoadLength, messageLength, streamOffset);
                                        }

                                        streamOffset += (totalMessagelength + sizeof(int));

                                        ConnectionHeader hdr = msg.getHeader("ConnectionHeader") as ConnectionHeader;
                                        if (hdr != null)
                                        {
                                            switch (hdr.Type)
                                            {
                                                case ConnectionHeader.CLOSE_SILENT:
                                                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.Run", "connection being closed silently");
                                                    self_close = true;
                                                    handler = null;
                                                    continue;

                                                case ConnectionHeader.LEAVE:
                                                    //The node is leaving the cluster gracefully.
                                                    leavingGracefully = true;
                                                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.Run", peer_addr + " is leaving gracefully");
                                                    if (LeavingGracefully)
                                                    {
                                                        enclosingInstance.notifyConnectionClosed(peer_addr);
                                                        enclosingInstance.remove(peer_addr, IsPrimary);
                                                    }
                                                    return;

                                                case ConnectionHeader.GET_SECOND_ADDRESS_REQ:
                                                    SendSecondaryAddressofPeer();
                                                    continue;

                                                case ConnectionHeader.GET_SECOND_ADDRESS_RSP:
                                                    lock (get_addr_sync)
                                                    {
                                                        secondaryAddress = hdr.MySecondaryAddress;
                                                        Monitor.PulseAll(get_addr_sync);
                                                    }
                                                    continue;

                                                case ConnectionHeader.ARE_U_IN_INITIALIZATION_PHASE:
                                                    try
                                                    {
                                                        bool iMinInitializationPhase = !enclosingInstance.enclosingInstance.Stack.IsOperational;
                                                        SendInitializationPhaseRsp(iMinInitializationPhase);
                                                    }
                                                    catch
                                                    {
                                                    }
                                                    break;

                                                case ConnectionHeader.INITIALIZATION_PHASE_RSP:
                                                    lock (initializationPhase_mutex)
                                                    {
                                                        inInitializationPhase = hdr.InitializationPhase;
                                                        Monitor.PulseAll(inInitializationPhase);
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    msg.Src = peer_addr;
                                    msg.MarkArrived();
                                    Enclosing_Instance.receive(msg); // calls receiver.receiver(msg)
                                }
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    lock (send_mutex) { isConnected = false; }
                    NCacheLog.Error("Connection.RecaiveDataAsync()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + e);
                }
            }

            #region helpers

            //Pulse the waiting sync call...
            private void PulseSend()
            {
                lock (_sendWaitObject)
                {
                    _isSending = false;
                    Monitor.Pulse(_sendWaitObject);
                }
            }

            private void CompleteSend()
            {
                lock (_sendWaitObject)
                {
                    if (_isSending)
                    {
                        Monitor.Wait(_sendWaitObject);
                    }

                    if (_socSendArgs != null && (_socSendArgs.BytesTransferred == 0 || _socSendArgs.SocketError != SocketError.Success))
                        throw new SocketException((int)SocketError.ConnectionReset);

                }
            }

            private Message ReadMessage(Stream stream, out int msgLen, out int tMsgLen)
            {
                byte[] lengthBytes = new byte[sizeof(int)];

                stream.Read(lengthBytes, 0, lengthBytes.Length);
                tMsgLen = BitConverter.ToInt32(lengthBytes, 0);

                stream.Read(lengthBytes, 0, lengthBytes.Length);
                msgLen = BitConverter.ToInt32(lengthBytes, 0);

                BinaryReader msgReader = new BinaryReader(stream, new UTF8Encoding(true));
                FlagsByte flags = new FlagsByte();
                flags.DataByte = msgReader.ReadByte();

                if (flags.AnyOn(FlagsByte.Flag.TRANS))
                {
                    Message tmpMsg = new Message();
                    tmpMsg.DeserializeLocal(msgReader);
                    return tmpMsg;
                }
                return (Message)CompactBinaryFormatter.Deserialize(stream, null, false, null);
            }

            private Array ReadPayload(Stream stream, int payLoadLength, int messageLength, int streamOffset)
            {
                int noOfChunks = payLoadLength / LARGE_OBJECT_SIZE;
                noOfChunks += (payLoadLength - (noOfChunks * LARGE_OBJECT_SIZE)) != 0 ? 1 : 0;
                Array payload = new Array[noOfChunks];
                int nextChunk = 0;
                int startIndex = streamOffset + sizeof(int) * +messageLength;

                for (int i = 0; i < noOfChunks; i++)
                {

                    int nextChunkSize = payLoadLength - nextChunk;

                    if (nextChunkSize > LARGE_OBJECT_SIZE)
                    {
                        nextChunkSize = LARGE_OBJECT_SIZE;
                    }

                    byte[] binaryChunk = new byte[nextChunkSize];
                    stream.Read(binaryChunk, 0, nextChunkSize);
                    nextChunk += nextChunkSize;
                    startIndex += nextChunkSize;
                    payload.SetValue(binaryChunk, i);
                }
                return payload;
            }

            private int GetSafeCollectionCount(long length)
            {
                return ((length > 20000) ? 20000 : (int)length);
            }

            #endregion

            #endregion

            public void ConnectionDestructionSimulator()
            {
                System.Threading.Thread.Sleep(new TimeSpan(0, 2, 0));
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionDestructionSimulator", "BREAKING THE CONNECTION WITH " + peer_addr);
                Destroy();
            }

            internal virtual void Destroy()
            {

                closeSocket(); // should terminate handler as well
                if (handler != null && handler.IsAlive)
                {
                    try
                    {
                        NCacheLog.Flush();
#if !NETCORE
                        handler.Abort();
#elif NETCORE
                        handler.Interrupt();
#endif
                    }
                    catch (Exception) { }
                }
                handler = null;
                try
                {
                    if (inStream != null) inStream.Close();
                }
                catch (Exception) { }
            }
            internal virtual void DestroySilent()
            {
                DestroySilent(true);
            }
            internal virtual void DestroySilent(bool sendNotification)
            {
                lock (send_mutex) { this.self_close = true; }// we intentionally close the connection. no need to suspect for such close
                if (IsConnected) SendSilentCloseNotification();//Inform the peer about closing the socket.
                Destroy();
            }


            /// <summary>
            /// Sends the notification to the peer that connection is being closed silently.
            /// </summary>
            private void SendSilentCloseNotification()
            {
                self_close = true;
                ConnectionHeader header = new ConnectionHeader(ConnectionHeader.CLOSE_SILENT);
                Message closeMsg = new Message(peer_addr, null, new byte[0]);
                closeMsg.putHeader("ConnectionHeader", header);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.SendSilentCloseNotification", "sending silent close request");
                try
                {
                    IList binaryMsg = Util.Util.serializeMessage(closeMsg);

                    SendInternal(binaryMsg);
                }
                catch (Exception e)
                {
                    NCacheLog.Error("Connection.SendSilentCloseNotification", e.ToString());
                }
            }

            public bool AreUinInitializationPhase()
            {
                self_close = true;
                ConnectionHeader header = new ConnectionHeader(ConnectionHeader.ARE_U_IN_INITIALIZATION_PHASE);
                Message closeMsg = new Message(peer_addr, null, new byte[0]);
                closeMsg.putHeader("ConnectionHeader", header);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.SendSilentCloseNotification", "sending silent close request");
                try
                {
                    lock (initializationPhase_mutex)
                    {
                        IList binaryMsg = Util.Util.serializeMessage(closeMsg);
                        SendInternal(binaryMsg);
                        Monitor.Wait(initializationPhase_mutex, 1000);
                        return inInitializationPhase;
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("Connection.SendSilentCloseNotification", e.ToString());
                }
                return false;
            }

            public bool SendInitializationPhaseRsp(bool initializationPhase)
            {
                self_close = true;
                ConnectionHeader header = new ConnectionHeader(ConnectionHeader.INITIALIZATION_PHASE_RSP);
                header.InitializationPhase = initializationPhase;
                Message closeMsg = new Message(peer_addr, null, new byte[0]);
                closeMsg.putHeader("ConnectionHeader", header);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.SendSilentCloseNotification", "sending silent close request");
                try
                {
                    lock (initializationPhase_mutex)
                    {
                        IList binaryMsg = Util.Util.serializeMessage(closeMsg);
                        SendInternal(binaryMsg);
                        Monitor.Wait(initializationPhase_mutex);
                        return inInitializationPhase;
                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("Connection.SendSilentCloseNotification", e.ToString());
                }
                return false;
            }

            /// <summary>
            /// Used to send the internal messages of the connection.
            /// </summary>
            /// <param name="binaryMsg"></param>
            private void SendInternal(IList binaryMsg)
            {
                if (binaryMsg != null)
                {
                    ClusteredMemoryStream stream = new ClusteredMemoryStream();

                    stream.Seek(4, System.IO.SeekOrigin.Begin);
                    byte[] msgCount = Util.Util.WriteInt32(1);
                    stream.Write(msgCount, 0, msgCount.Length);
                    int length = 0;
                    foreach (byte[] buffer in binaryMsg)
                    {
                        length += buffer.Length;
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    byte[] lenBuf = Util.Util.WriteInt32(length + 4);
                    stream.Write(lenBuf, 0, lenBuf.Length);
                    send(stream.GetInternalBuffer(), null);
                }
            }
            /// <summary>
            /// Sends notification to other node about leaving.
            /// </summary>
            public void SendLeaveNotification()
            {
                leavingGracefully = true;
                ConnectionHeader header = new ConnectionHeader(ConnectionHeader.LEAVE);
                Message leaveMsg = new Message(peer_addr, null, new byte[0]);
                leaveMsg.putHeader("ConnectionHeader", header);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.SendSilentCloseNotification", "sending leave request");
                try
                {
                    IList binaryMsg = Util.Util.serializeMessage(leaveMsg);
                    SendInternal(binaryMsg);
                }
                catch (Exception e)
                {
                    NCacheLog.Error("Connection.SendLeaveNotification", e.ToString());
                }
            }

            internal virtual long send(IList msg, Array userPayload)
            {
                return send(msg, userPayload, 0);
            }

            internal virtual long send(IList msg, Array userPayload, int bytesToSent)
            {
                long bytesSent = 0;
                try
                {
                    HPTimeStats socketSendTimeStats = null;

                    if (enclosingInstance.enableMonitoring)
                    {
                        socketSendTimeStats = new HPTimeStats();
                        socketSendTimeStats.BeginSample();
                    }

                    bytesSent = doSend(msg, userPayload, bytesToSent);

                    if (socketSendTimeStats != null)
                    {
                        socketSendTimeStats.EndSample();
                        enclosingInstance.enclosingInstance.Stack.perfStatsColl.IncrementSocketSendTimeStats((long)socketSendTimeStats.Current);
                        enclosingInstance.enclosingInstance.Stack.perfStatsColl.IncrementSocketSendSizeStats((long)bytesSent);

                    }
                }
                catch (ObjectDisposedException)
                {
                    lock (send_mutex)
                    {
                        socket_error = true;
                        isConnected = false;
                    }
                    throw new ExtSocketException("Connection is closed");
                }
                catch (SocketException sock_exc)
                {
                    lock (send_mutex)
                    {
                        socket_error = true;
                        isConnected = false;
                    }
                    throw new ExtSocketException(sock_exc.Message);
                }
                catch (System.Exception ex)
                {
                    NCacheLog.Error("exception is " + ex);
                    throw;
                }
                return bytesSent;
            }


            internal virtual long doSend(IList msg, Array userPayload, int bytesToSent)
            {
                long bytesSent = 0;

                Address dst_addr = (Address)peer_addr;
                byte[] buffie = null;

                if (dst_addr == null || dst_addr.IpAddress == null)
                {
                    NCacheLog.Error("the destination address is null; aborting send");
                    return bytesSent;
                }

                try
                {

                    // we're using 'double-writes', sending the buffer to the destination in 2 pieces. this would
                    // ensure that, if the peer closed the connection while we were idle, we would get an exception.
                    // this won't happen if we use a single write (see Stevens, ch. 5.13).
                    //if(nTrace.isInfoEnabled) NCacheLog.Info("ConnectionTable.Connection.doSend()"," before writing to out put stream");
                    if (sock != null)
                    {


                        //sock.Send(Util.Util.WriteInt32(buffie.Length)); // write the length of the data buffer first
                        DateTime dt = DateTime.Now;
                        bytesSent = AssureSend(msg, userPayload, bytesToSent);
                        DateTime now = DateTime.Now;
                        TimeSpan ts = now - dt;
                        if (ts.TotalMilliseconds > _worsSendTime.TotalMilliseconds)
                            _worsSendTime = ts;

                        enclosingInstance.enclosingInstance.Stack.perfStatsColl.IncrementBytesSentPerSecStats(bytesSent);


                    }
                }
                catch (SocketException ex)
                {
                    lock (send_mutex)
                    {
                        socket_error = true;
                        isConnected = false;
                    }
                    NCacheLog.Error(Enclosing_Instance.local_addr + " to " + dst_addr + ",   exception is " + ex);
                    //if(!markedClose) Enclosing_Instance.remove(dst_addr, IsPrimary);
                    throw ex;
                }
                catch (System.Exception ex)
                {
                    lock (send_mutex)
                    {
                        socket_error = true;
                        isConnected = false;
                    }
                    NCacheLog.Error(Enclosing_Instance.local_addr + "to " + dst_addr + ",   exception is " + ex);
                    //if (!markedClose) Enclosing_Instance.remove(dst_addr, IsPrimary);
                    throw ex;
                }
                return bytesSent;
            }

            private long AssureSend(IList buffers, Array userPayLoad, int bytesToSent)
            {
                int totalDataLength = 0;

                lock (send_mutex)
                {
                    int count = 0;
                    int bytesCopied = 0;
                    int mainIndex = 0;

                    totalDataLength = 0;

                    if (userPayLoad == null)
                    {
                        foreach (byte[] buffer in buffers)
                        {
                            totalDataLength += buffer.Length;
                            AssureSend(buffer, buffer.Length);
                        }
                    }
                    else
                    {
                        foreach (byte[] buffer in buffers)
                        {
                            while (bytesCopied < buffer.Length)
                            {
                                count = buffer.Length - bytesCopied;
                                if (count > sendBuffer.Length - mainIndex)
                                    count = sendBuffer.Length - mainIndex;

                                Buffer.BlockCopy(buffer, bytesCopied, sendBuffer, mainIndex, count);
                                bytesCopied += count;
                                mainIndex += count;

                                if (mainIndex >= sendBuffer.Length)
                                {
                                    AssureSend(sendBuffer, sendBuffer.Length);
                                    mainIndex = 0;
                                }

                            }
                        }

                        //AssureSend(buffer);
                        if (userPayLoad != null && userPayLoad.Length > 0)
                        {
                            for (int i = 0; i < userPayLoad.Length; i++)
                            {
                                byte[] buffer = userPayLoad.GetValue(i) as byte[];
                                bytesCopied = 0;
                                totalDataLength += buffer.Length;

                                while (bytesCopied < buffer.Length)
                                {
                                    count = buffer.Length - bytesCopied;
                                    if (count > sendBuffer.Length - mainIndex)
                                        count = sendBuffer.Length - mainIndex;

                                    Buffer.BlockCopy(buffer, bytesCopied, sendBuffer, mainIndex, count);
                                    bytesCopied += count;
                                    mainIndex += count;

                                    if (mainIndex >= sendBuffer.Length)
                                    {
                                        AssureSend(sendBuffer, sendBuffer.Length);
                                        mainIndex = 0;
                                    }
                                }

                                if (mainIndex >= sendBuffer.Length)
                                {
                                    AssureSend(sendBuffer, sendBuffer.Length);
                                    mainIndex = 0;
                                }
                            }
                            if (mainIndex >= 0)
                            {
                                AssureSend(sendBuffer, mainIndex);
                                mainIndex = 0;
                            }
                        }
                        else
                        {
                            AssureSend(sendBuffer, mainIndex);
                        }
                    }
                }

                return totalDataLength;
            }

            private void AssureSend(byte[] buffer, int count)
            {
                int bytesSent = 0;
                int noOfChunks = 0;
                DateTime startTime;
                lock (send_mutex)
                {
                    while (bytesSent < count)
                    {
                        try
                        {
                            _isIdle = false;
                            noOfChunks++;
                            bytesSent += sock.Send(buffer, bytesSent, count - bytesSent, SocketFlags.None);
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                            {
                                continue;
                            }
                            throw;
                        }
                    }
                }
            }

            /// <summary> Reads the peer's address. First a cookie has to be sent which has to match my own cookie, otherwise
            /// the connection will be refused
            /// </summary>
            internal virtual bool readPeerAddress(System.Net.Sockets.Socket client_sock, ref Address peer_addr)
            {

                ConnectInfo info = null;
                byte[] buf;//, input_cookie = new byte[Enclosing_Instance.cookie.Length];
                int len = 0;
                bool connectingFirstTime = false;
                ProductVersion receivedVersion;
                if (sock != null)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "Before reading from socket");

                    // read the length of the address
                    byte[] lenBuff = new byte[4];
                    Util.Util.ReadInput(sock, lenBuff, 0, lenBuff.Length);
                    len = Util.Util.convertToInt32(lenBuff);

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.readPeerAddress()", "Address length = " + len);
                    // finally read the address itself
                    buf = new byte[len];
                    Util.Util.ReadInput(sock, buf, 0, len);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "before deserialization of adress");
                    object[] args = (object[])CompactBinaryFormatter.FromByteBuffer(buf, null);
                    peer_addr = args[0] as Address;
                    connectingFirstTime = (bool)args[1];
                    receivedVersion = (ProductVersion)args[2];//reading the productVersion
                    if((args[3]!=null)&&(!(((string)args[3]).Equals("Eval") )) && !env_name.Equals("Eval") && !((string)args[3]).Equals(env_name))
                    {
                        throw new VersionMismatchException("Environment does not match");
                    }

                    if (receivedVersion.IsValidVersion(receivedVersion.EditionID) == false)
                    {

                        NCacheLog.Warn("Cookie version is different");

                    }
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "after deserialization of adress");
                    updateLastAccessed();
                }
                return connectingFirstTime;
            }

            /// <summary> Send the cookie first, then the our port number. If the cookie doesn't match the receiver's cookie,
            /// the receiver will reject the connection and close it.
            /// </summary>
            internal virtual void sendLocalAddress(Address local_addr, bool connectingFirstTime)
            {
                byte[] buf;
                //Product Version is sent as a part of the object array; no version is to be sent explicitly
                ProductVersion currentVersion = ProductVersion.ProductInfo;
                
                if (local_addr == null)
                {
                    NCacheLog.Warn("local_addr is null");
                    throw new Exception("local address is null");
                }
                if (sock != null)
                {
                    try
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "b4 serializing...");
                        //Debugger.Launch();
                        object[] objArray = new object[] { local_addr, connectingFirstTime, currentVersion,env_name};
                        buf = CompactBinaryFormatter.ToByteBuffer(objArray, null);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after serializing...");
                        byte[] lenBuff;// write the length of the buffer
                        lenBuff = Util.Util.WriteInt32(buf.Length);
                        sock.Send(lenBuff);

                        // and finally write the buffer itself
                        sock.Send(buf);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after sending...");
                        updateLastAccessed();
                    }
                    catch (System.Exception t)
                    {
                        NCacheLog.Error("exception is " + t);
                        throw t;
                    }
                }
            }

            /// <summary> Reads the peer's address. First a cookie has to be sent which has to match my own cookie, otherwise
            /// the connection will be refused
            /// </summary>
            internal virtual ConnectInfo ReadConnectInfo(System.Net.Sockets.Socket client_sock)
            {
                ConnectInfo info = null;
                byte[] version, buf;//, input_cookie = new byte[Enclosing_Instance.cookie.Length];
                int len = 0;

                if (sock != null)
                {
                    version = new byte[Version.version_id.Length];
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "before reading from socket");
                    Util.Util.ReadInput(sock, version, 0, version.Length);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "after reading from socket");
                    if (Version.CompareTo(version) == false)
                    {

                        NCacheLog.Error("ConnectionTable.ReadConnectInfo()", "WARRING: Cookie version is different");
                        NCacheLog.Error("ConnectionTable.ReadConnectInfo()", "WARRING: Cookie sent by machine having Address [ " + peer_addr + " ] does not match own cookie.");
                        NCacheLog.Error("ConnectionTable.ReadConnectInfo()", "WARRING: Cluster formation is not allowed between different editions of NCache. Terminating connection!");
                        throw new VersionMismatchException("NCache version of machine " + peer_addr + " does not match with local installation. Cluster formation is not allowed between different editions of NCache.");
                    }

                    // read the length of the address
                    byte[] lenBuff = new byte[4];
                    Util.Util.ReadInput(sock, lenBuff, 0, lenBuff.Length);
                    len = Util.Util.convertToInt32(lenBuff);

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.readPeerAddress()", "Address length = " + len);
                    // finally read the address itself
                    buf = new byte[len];
                    Util.Util.ReadInput(sock, buf, 0, len);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "before deserialization of adress");
                    info = (ConnectInfo)CompactBinaryFormatter.FromByteBuffer(buf, null);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.connection.readpeerAdress", "after deserialization of adress");
                    updateLastAccessed();
                }
                return info;
            }

            /// <summary> Send the cookie first, then the our port number. If the cookie doesn't match the receiver's cookie,
            /// the receiver will reject the connection and close it.
            /// </summary>
            internal virtual void SendConnectInfo(ConnectInfo info)
            {
                byte[] buf;

                if (sock != null)
                {
                    try
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "b4 serializing...");
                        buf = CompactBinaryFormatter.ToByteBuffer(info, null);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after serializing...");
                        sock.Send(Version.version_id);

                        // write the length of the buffer
                        byte[] lenBuff;
                        lenBuff = Util.Util.WriteInt32(buf.Length);
                        sock.Send(lenBuff);

                        // and finally write the buffer itself
                        sock.Send(buf);
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after sending...");
                        updateLastAccessed();
                    }
                    catch (System.Exception t)
                    {
                        NCacheLog.Error("exception is " + t);
                        throw t;
                    }
                }
            }

            internal virtual System.String printCookie(byte[] c)
            {
                if (c == null)
                    return "";
                return new System.String(Global.ToCharArray(c));
            }

            //The receiver thread...
            //* Will be replaced by async method... as done in the client and server side of the communication.
            public virtual void Run()
            {
                Message msg = null;
                byte[] buf = null;
                int len = 0;
                while (handler != null)
                {
                    Stream stmIn = null;
                    BinaryReader msgReader = null;
                    try
                    {
                        if (sock == null)
                        {
                            NCacheLog.Error("input stream is null !");
                            //Console.WriteLine("Socket is Null");
                            break;
                        }
                        byte[] lenBuff = new byte[4];
                        buf = null;

                        Util.Util.ReadInput(sock, lenBuff, 0, lenBuff.Length);

                        len = Util.Util.convertToInt32(lenBuff);

                        stmIn = new ClusteredMemoryStream(len);
                        buf = receiveBuffer;
                        int totalRecevied = 0;
                        int totaldataToReceive = len;
                        HPTimeStats socketReceiveTimeStats = null;
                        if (enclosingInstance.enableMonitoring)
                        {
                            socketReceiveTimeStats = new HPTimeStats();
                            socketReceiveTimeStats.BeginSample();
                        }
                        DateTime dt = DateTime.Now;

                        while (totalRecevied < len)
                        {
                            int dataToReceive = buf.Length < totaldataToReceive ? buf.Length : totaldataToReceive;
                            int recLength = Util.Util.ReadInput(sock, buf, 0, dataToReceive);
                            stmIn.Write(buf, 0, recLength);
                            totaldataToReceive -= recLength;
                            totalRecevied += recLength;
                        }

                        DateTime now = DateTime.Now;

                        TimeSpan ts = now - dt;

                        if (ts.TotalMilliseconds > _worsRecvTime.TotalMilliseconds)
                            _worsRecvTime = ts;


                        if (socketReceiveTimeStats != null)
                        {
                            socketReceiveTimeStats.EndSample();

                            enclosingInstance.enclosingInstance.Stack.perfStatsColl.IncrementSocketReceiveTimeStats((long)socketReceiveTimeStats.Current);
                            enclosingInstance.enclosingInstance.Stack.perfStatsColl.IncrementSocketReceiveSizeStats((long)len);

                        }

                        enclosingInstance.publishBytesReceivedStats(len + 4);

                        if (totalRecevied == len)
                        {
                            stmIn.Position = 0;
                            byte[] quadBuffer = new byte[4];
                            stmIn.Read(quadBuffer, 0, quadBuffer.Length);
                            int noOfMessages = Util.Util.convertToInt32(quadBuffer, 0);
                            int messageBaseIndex = 4;
                            for (int msgCount = 0; msgCount < noOfMessages; msgCount++)
                            {
                                stmIn.Read(quadBuffer, 0, quadBuffer.Length);
                                int totalMessagelength = Util.Util.convertToInt32(quadBuffer, 0);
                                stmIn.Read(quadBuffer, 0, quadBuffer.Length);
                                int messageLength = Util.Util.convertToInt32(quadBuffer, 0);

                                msgReader = new BinaryReader(stmIn, new UTF8Encoding(true));
                                FlagsByte flags = new FlagsByte();
                                flags.DataByte = msgReader.ReadByte();

                                if (flags.AnyOn(FlagsByte.Flag.TRANS))
                                {
                                    Message tmpMsg = new Message();
                                    tmpMsg.DeserializeLocal(msgReader);
                                    msg = tmpMsg;
                                }
                                else
                                {
                                    msg = (Message)CompactBinaryFormatter.Deserialize(stmIn, null, false, null);
                                }

                                if (msg != null)
                                {
                                    int payLoadLength = totalMessagelength - messageLength - 4;
                                    if (payLoadLength > 0)
                                    {

                                        int noOfChunks = payLoadLength / LARGE_OBJECT_SIZE;
                                        noOfChunks += (payLoadLength - (noOfChunks * LARGE_OBJECT_SIZE)) != 0 ? 1 : 0;
                                        Array payload = new Array[noOfChunks];

                                        int nextChunk = 0;
                                        int nextChunkSize = 0;
                                        int startIndex = messageBaseIndex + 8 + messageLength;

                                        for (int i = 0; i < noOfChunks; i++)
                                        {
                                            nextChunkSize = payLoadLength - nextChunk;
                                            if (nextChunkSize > LARGE_OBJECT_SIZE)
                                                nextChunkSize = LARGE_OBJECT_SIZE;

                                            byte[] binaryChunk = new byte[nextChunkSize];
                                            //Buffer.BlockCopy(buf, startIndex, binaryChunk, 0, nextChunkSize);
                                            stmIn.Read(binaryChunk, 0, nextChunkSize);
                                            nextChunk += nextChunkSize;
                                            startIndex += nextChunkSize;

                                            payload.SetValue(binaryChunk, i);
                                        }

                                        msg.Payload = payload;
                                    }
                                    messageBaseIndex += (totalMessagelength + 4);
                                    ConnectionHeader hdr = msg.getHeader("ConnectionHeader") as ConnectionHeader;
                                    if (hdr != null)
                                    {
                                        switch (hdr.Type)
                                        {
                                            case ConnectionHeader.CLOSE_SILENT:

                                                if (NCacheLog.IsErrorEnabled) NCacheLog.CriticalInfo("Connection.Run", peer_addr + " connection being closed silently");
                                                this.self_close = true;
                                                handler = null;
                                                continue;

                                            case ConnectionHeader.LEAVE:
                                                //The node is leaving the cluster gracefully.
                                                leavingGracefully = true;
                                                if (NCacheLog.IsErrorEnabled) NCacheLog.CriticalInfo("Connection.Run", peer_addr.ToString() + " is leaving gracefully");
                                                handler = null;
                                                continue;

                                            case ConnectionHeader.GET_SECOND_ADDRESS_REQ:
                                                SendSecondaryAddressofPeer();
                                                continue;

                                            case ConnectionHeader.GET_SECOND_ADDRESS_RSP:
                                                lock (get_addr_sync)
                                                {
                                                    secondaryAddress = hdr.MySecondaryAddress;
                                                    Monitor.PulseAll(get_addr_sync);
                                                }
                                                continue;

                                            case ConnectionHeader.ARE_U_IN_INITIALIZATION_PHASE:
                                                try
                                                {
                                                    bool iMinInitializationPhase = !enclosingInstance.enclosingInstance.Stack.IsOperational;
                                                    SendInitializationPhaseRsp(iMinInitializationPhase);
                                                }
                                                catch (Exception e)
                                                {

                                                }
                                                break;

                                            case ConnectionHeader.INITIALIZATION_PHASE_RSP:
                                                lock (initializationPhase_mutex)
                                                {
                                                    inInitializationPhase = hdr.InitializationPhase;
                                                    Monitor.PulseAll(inInitializationPhase);
                                                }
                                                break;
                                        }
                                    }
                                }
                                msg.Src = peer_addr;

                                msg.MarkArrived();
                                Enclosing_Instance.receive(msg); // calls receiver.receiver(msg)
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        lock (send_mutex)
                        {
                            socket_error = true;
                            isConnected = false;
                        }
                        break;
                    }
                    catch (ThreadAbortException)
                    {
                        lock (send_mutex)
                        {
                            socket_error = true;
                            isConnected = false;
                        }
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        lock (send_mutex)
                        {
                            socket_error = true;
                            isConnected = false;
                        }
                        break;

                    }
                    catch (System.OutOfMemoryException memExc)
                    {
                        lock (send_mutex) { isConnected = false; }
                        NCacheLog.CriticalInfo("Connection.Run()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " memory exception " + memExc.ToString());
                        break; // continue;
                    }
                    catch (ExtSocketException sock_exp)
                    {
                        lock (send_mutex)
                        {
                            socket_error = true;
                            isConnected = false;
                        }
                        // peer closed connection
                        NCacheLog.Error("Connection.Run()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + sock_exp.Message);
                        break;
                    }
                    catch (System.IO.EndOfStreamException eof_ex)
                    {
                        lock (send_mutex) { isConnected = false; }
                        // peer closed connection
                        NCacheLog.Error("Connection.Run()", "data :" + len + Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + eof_ex);
                        break;
                    }
                    catch (System.Net.Sockets.SocketException io_ex)
                    {
                        lock (send_mutex)
                        {
                            socket_error = true;
                            isConnected = false;
                        }
                        NCacheLog.Error("Connection.Run()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + io_ex.Message);
                        break;
                    }
                    catch (System.ArgumentException ex)
                    {
                        lock (send_mutex) { isConnected = false; }
                        break;
                    }
                    catch (System.Exception e)
                    {
                        lock (send_mutex) { isConnected = false; }
                        NCacheLog.Error("Connection.Run()", Enclosing_Instance.local_addr + "-->" + peer_addr.ToString() + " exception is " + e);
                        break;
                    }
                    finally
                    {
                        if (stmIn != null) stmIn.Close();
                        if (msgReader != null) msgReader.Close();
                    }
                }

                handler = null;

                if (LeavingGracefully)
                {
                    try
                    {
                        enclosingInstance.notifyConnectionClosed(peer_addr);

                        enclosingInstance.remove(peer_addr, IsPrimary);
                    }
                    catch (ThreadAbortException) { }
                    catch (ThreadInterruptedException) { }
                }
            }

            public void HandleRequest(object state)
            {
                Enclosing_Instance.receive((Message)state);
            }

            public bool IsSelfClosing
            {
                get { return self_close; }
            }

            public bool LeavingGracefully
            {
                get { return leavingGracefully; }
            }

            public bool IsSocketError
            {
                get { return socket_error; }
            }

            public Address GetSecondaryAddressofPeer()
            {
                Connection.ConnectionHeader header = new ConnectionHeader(ConnectionHeader.GET_SECOND_ADDRESS_REQ);
                Message msg = new Message(peer_addr, null, new byte[0]);
                msg.putHeader("ConnectionHeader", header);
                lock (get_addr_sync)
                {
                    SendInternal(Util.Util.serializeMessage(msg));
                    Monitor.Wait(get_addr_sync);
                }
                return secondaryAddress;
            }
            public void SendSecondaryAddressofPeer()
            {
                Address secondaryAddress = null;
                Connection.ConnectionHeader header = new ConnectionHeader(ConnectionHeader.GET_SECOND_ADDRESS_RSP);
                header.MySecondaryAddress = enclosingInstance.local_addr_s;

                Message msg = new Message(peer_addr, null, new byte[0]);
                msg.putHeader("ConnectionHeader", header);
                NCacheLog.Error("Connection.SendSecondaryAddress", "secondaryAddr: " + header.MySecondaryAddress);
                SendInternal(Util.Util.serializeMessage(msg));

            }
            public override System.String ToString()
            {
                System.Text.StringBuilder ret = new System.Text.StringBuilder();

                if (sock == null)
                    ret.Append("<null socket>");
                else
                {
                    ret.Append("<" + this.peer_addr.ToString() + ">");
                }

                return ret.ToString();
            }


            internal virtual void closeSocket()
            {
                if (sock != null)
                {
                    try
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.closeSocket()", "client port local_port= " + ((IPEndPoint)sock.LocalEndPoint).Port + "client port remote_port= " + ((IPEndPoint)sock.RemoteEndPoint).Port);
#if NETCORE
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            sock.Shutdown(SocketShutdown.Both);
                        }
#endif
                        sock.Close(); // should actually close in/out (so we don't need to close them explicitly)
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.closeSocket()", "connection destroyed");
                    }
                    catch (System.Exception e)
                    {
                    }
                    sock = null;
                }
            }
            internal class ConnectionHeader : Header, ICompactSerializable
            {
                public const int CLOSE_SILENT = 1;
                public const int LEAVE = 2;
                public const int GET_SECOND_ADDRESS_REQ = 3;
                public const int GET_SECOND_ADDRESS_RSP = 4;
                public const int ARE_U_IN_INITIALIZATION_PHASE = 5;
                public const int INITIALIZATION_PHASE_RSP = 6;

                int _type;
                Address _secondaryAddress;
                bool initializationPhase;

                public ConnectionHeader(int type)
                {
                    _type = type;
                }
                public int Type
                {
                    get { return _type; }
                }
                public bool InitializationPhase
                {
                    get { return initializationPhase; }
                    set { initializationPhase = value; }
                }

                public Address MySecondaryAddress
                {
                    get { return _secondaryAddress; }
                    set { _secondaryAddress = value; }
                }

                public override string ToString()
                {
                    return "ConnectionHeader Type : " + _type;
                }

                #region ICompactSerializable Members

                public void Deserialize(CompactReader reader)
                {
                    _type = reader.ReadInt32();
                    _secondaryAddress = reader.ReadObject() as Address;
                    initializationPhase = reader.ReadBoolean();
                }

                public void Serialize(CompactWriter writer)
                {
                    writer.Write(_type);
                    writer.WriteObject(_secondaryAddress);
                    writer.Write(initializationPhase);
                }

                #endregion
            }
        }


        internal class Reaper : IThreadRunnable
        {
            private void InitBlock(ConnectionTable enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private ConnectionTable enclosingInstance;
            virtual public bool Running
            {
                get
                {
                    return t != null;
                }

            }
            public ConnectionTable Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            internal ThreadClass t = null;
            private string _cacheName;

            internal Reaper(ConnectionTable enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }

            public virtual void start()
            {
                if (Enclosing_Instance.conns_NIC_1.Count == 0)
                    return;
                if (t != null && !t.IsAlive)
                    t = null;
                if (t == null)
                {
                    t = new ThreadClass(new System.Threading.ThreadStart(this.Run), "ConnectionTable.ReaperThread");
                    t.IsBackground = true; // will allow us to terminate if all remaining threads are daemons
                    t.Start();
                }
            }

            public virtual void stop()
            {
                if (t != null)
                    t = null;
            }


            // Aobvoe functin re-writtnen
            public virtual void Run()
            {
                Connection value_Renamed;
                System.Collections.DictionaryEntry entry;
                long curr_time;
                ArrayList temp = new ArrayList();

                if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection reaper thread was started. Number of connections=" + Enclosing_Instance.conns_NIC_1.Count + ", reaper_interval=" + Enclosing_Instance.reaper_interval + ", conn_expire_time=" + Enclosing_Instance.conn_expire_time);

                while (Enclosing_Instance.conns_NIC_1.Count > 0 && t != null)
                {
                    // first sleep
                    Util.Util.sleep(Enclosing_Instance.reaper_interval);

                    if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("ConnectionTable.Reaper", "b4 lock conns.SyncRoot");
                    lock (Enclosing_Instance.conns_NIC_1.SyncRoot)
                    {
                        curr_time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
                        for (System.Collections.IEnumerator it = Enclosing_Instance.conns_NIC_1.GetEnumerator(); it.MoveNext();)
                        {
                            entry = (System.Collections.DictionaryEntry)it.Current;
                            value_Renamed = (Connection)entry.Value;

                            if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection is " + ((curr_time - value_Renamed.last_access) / 1000) + " seconds old (curr-time=" + curr_time + ", last_access=" + value_Renamed.last_access + ')');
                            if (value_Renamed.last_access + Enclosing_Instance.conn_expire_time < curr_time)
                            {
                                if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("connection " + value_Renamed + " has been idle for too long (conn_expire_time=" + Enclosing_Instance.conn_expire_time + "), will be removed");
                                value_Renamed.Destroy();
                                temp.Add(it.Current);
                            }
                        }


                        for (int i = 0; i < temp.Count; i++)
                        {
                            if (Enclosing_Instance.conns_NIC_1.Contains((Address)temp[i]))
                            {
                                Enclosing_Instance.conns_NIC_1.Remove((Address)temp[i]);
                                temp[i] = null;
                            }
                        }

                    }
                    if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("ConnectionTable.Reaper", "after lock conns.SyncRoot");
                }

                if (enclosingInstance.NCacheLog.IsInfoEnabled) enclosingInstance.NCacheLog.Info("reaper terminated");
                t = null;
            }
        }


    }
}
