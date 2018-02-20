// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// $Id: GroupRequest.java,v 1.8 2004/09/05 04:54:22 ovidiuf Exp $
using System;
using System.Collections;
#if NET40
using System.Collections.Concurrent;
#endif
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
using Alachisoft.NGroups.Protocols;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;

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
    class ConnectionTable //: IThreadRunnable
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

        internal System.Collections.Hashtable conns_NIC_1 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection

        private System.Collections.Hashtable secondayrConns_NIC_1 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private System.Collections.Hashtable conns_NIC_2 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private System.Collections.Hashtable secondayrConns_NIC_2 = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection

        private System.Collections.Hashtable dedicatedSenders = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable()); // keys: Addresses (peer address), values: Connection
        private Receiver receiver = null;
        private System.Net.Sockets.TcpListener srv_sock1 = null;

        private System.Net.Sockets.TcpListener srv_sock2 = null;


        private System.Net.IPAddress bind_addr1 = null;

        private System.Net.IPAddress bind_addr2 = null;


        internal Address local_addr = null; // bind_addr + port of srv_sock
        internal Address local_addr_s = null; // bind_addr + port of Secondary srv_sock.

        internal int srv_port = 7800;
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
        internal long reaper_interval = 60000; // reap unused conns once a minute
        internal long conn_expire_time = 300000; // connections can be idle for 5 minutes before they are reaped
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
        internal bool enableMonitoring;
        
        bool useDedicatedSender = true;
        object con_reestablish_sync = new object();
        ArrayList _nodeRejoiningList;

        private int _retries;
        private int _retryInterval;

        private int _idGenerator = 0;
        
        


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

        public ConnectionTable(Receiver r, System.Net.IPAddress bind_addr1, System.Net.IPAddress bind_addr2, int srv_port, int port_range, ILogger NCacheLog, int retries, int retryInterval, bool isInproc)
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

        public ConnectionTable(Receiver r, System.Net.IPAddress bind_addr, int srv_port, long reaper_interval, long conn_expire_time, ILogger NCacheLog)
        {
            setReceiver(r);
            this.bind_addr1 = bind_addr;
            this.srv_port = srv_port;
            this.reaper_interval = reaper_interval;
            this.conn_expire_time = conn_expire_time;
            this._ncacheLog = NCacheLog;
            start();
        }


        public virtual void setReceiver(Receiver r)
        {
            receiver = r;
        }


        public virtual void addConnectionListener(ConnectionListener l)
        {
            if (l != null && !conn_listeners.Contains(l))
                conn_listeners.Add(l);
        }


        public virtual void removeConnectionListener(ConnectionListener l)
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
                        NCacheLog.Error("ConnectionTable.makeConnection",   "member :" + memeber + " Exception:" + e.ToString());
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
                    NCacheLog.Error("ConnectionTable.makeConnection",   "destroying connection with member : Exception:" + e.ToString());
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
                        NCacheLog.Error("ConnectionTable.makeConnection",   "an error occurred while establishing secondary connection. Exception:" + e.ToString());
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
                    if (stopped) return;

                    Connection con = GetConnection(member, null, true, useDualConnection, true);

                    if (con == null)
                    {
                        NCacheLog.Error("ConnectionTable.MakeConnectionAsync",   "could not establish connection with " + member);
                        failedNodes.Add(member);
                    }
                    else
                    {
                        con.IsPartOfCluster = true;
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.MakeConnectionAsync",   "established connection with " + member);
                    }
                }
            }
            catch (Exception e)
            {
                NCacheLog.Error("ConnectionTable.MakeConnectionAsync",   " Exception:" + e.ToString());
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
                            int queueCount = dmSenderMgr.QueueMessage(msg, userPayload,priority);

                            enclosingInstance.Stack.perfStatsColl.IncrementTcpDownQueueCountStats(queueCount);

                            return bytesSent;
                        }
                    }
                    return bytesSent;
                }
            }
            catch (System.Net.Sockets.SocketException sock_ex)
            {
                if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.GetConnection",   sock_ex.Message);
                for (int i = 0; i < conn_listeners.Count; i++)
                    ((ConnectionListener)conn_listeners[i]).couldnotConnectTo(dest);

                return bytesSent;
            }
            catch (ThreadAbortException) { return bytesSent; }
            catch (ThreadInterruptedException) { return bytesSent; }
            catch (System.Exception ex)
            {
                if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.GetConnection",   "connection to " + dest + " could not be established: " + ex);
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
                        int queueCount = dmSenderMgr.QueueMessage(msg, userPayload,priority);

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
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send",   local_addr + " re-establishing connection with " + dest);

                    conn = ReEstablishConnection(dest);
                    if (conn != null)
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send",   local_addr + " re-established connection successfully with " + dest);

                        try
                        {
                            bytesSent = conn.send(msg, userPayload);
                            return bytesSent;
                        }
                        catch (Exception e)
                        {
                            NCacheLog.Error("ConnectionTable.send",   "send failed after reconnect " + e.ToString());
                        }
                    }
                    else
                        NCacheLog.Error("ConnectionTable.send",   local_addr + " failed to re-establish connection  with " + dest);
                }
                else
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.send",   local_addr + " need not to re-establish connection with " + dest);

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Ct.send",   "sending message to " + dest + " failed (ex=" + ex.GetType().FullName + "); removing from connection table");
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

                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Connect",   "Opening socket connection with " + ipRemoteEndPoint.ToString());
                sock.Connect(ipRemoteEndPoint);

            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10049) //"Requested address is not valid in its context
                {
                    //A call to bind to a local ip is failed, therefore we dont bind.
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, send_buf_size);
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, recv_buf_size);

                    //we do not bind an IP address.
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Connect",   "Opening socket connection with " + ipRemoteEndPoint.ToString());
                    sock.Connect(ipRemoteEndPoint);
                }
                else
                    throw;
            }

            object size = sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            size = sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            int lport = 0;
            if (!sock.Connected)
                NCacheLog.Error("Connection.getConnection()",   "can not be connected");
            else
            {
                lport = ((IPEndPoint)sock.LocalEndPoint).Port;
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CONNECTED at local port = " + lport);
                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   "client port local_port= " + ((IPEndPoint)sock.LocalEndPoint).Port + "client port remote_port= " + ((IPEndPoint)sock.RemoteEndPoint).Port);
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
                            NCacheLog.Error("ConnectionTable.ConnectToPeerOnSeconary",   "failed to connect with " + con_with_NIC_1.peer_addr + " on second IP " + secondaryAddres);
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

            con = getConnection(dest, null, reEstablish, true, true,true);

            return con;
        }

        protected virtual Connection GetConnection(Address dest, Address primaryAddress, bool reEstablish, bool getDualConnection, bool withFirstNIC)
        {
            Connection con;
            con = getConnection(dest, primaryAddress, reEstablish, true, withFirstNIC,true);
            if (con != null && getDualConnection && reEstablish)
            {
                getConnection(dest, primaryAddress, reEstablish, false, withFirstNIC,true);
            }
            return con;
        }


        /// <summary>Try to obtain correct Connection (or create one if not yet existent) </summary>
        protected virtual Connection getConnection(Address dest, Address primaryAddress, bool reEstablishCon, bool isPrimary, bool withFirstNIC,bool connectingFirstTime)
        {
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

                if ((conn == null ||!connectingFirstTime) && reEstablishCon)
                {

                    try
                    {
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   "No Connetion was found found with " + dest.ToString());
                        if (local_addr == null) return null; //cluster being stopped.

                        sock = Connect(dest, withFirstNIC);
                        conn = new Connection(this, sock, primaryAddress, this.NCacheLog, isPrimary, _retries, _retryInterval);
                        conn.MemManager = MemManager;
                        conn.IamInitiater = true;
                        ConnectInfo conInfo = null;
                        try
                        {
                            byte connectStatus = connectingFirstTime ? ConnectInfo.CONNECT_FIRST_TIME : ConnectInfo.RECONNECTING;
                            
                            conn.sendLocalAddress(local_addr,connectingFirstTime);
                            conn.readPeerAddress(sock,ref peer_addr);
                            if (((Address)local_addr).CompareTo((Address)dest) > 0)
                            {
                                conInfo = new ConnectInfo(connectStatus, GetConnectionId());
                                if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection",   dest + " I should send connect_info");
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
                            NCacheLog.Error("ConnectionTable.getConnection()",   e.Message);
                            conn.DestroySilent();
                            return null;
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection",   "b4 lock conns.SyncRoot");
                        try
                        {
                            conn_syn_lock.AcquireWriterLock(Timeout.Infinite);
                            if (isPrimary)
                            {
                                if (withFirstNIC)
                                {
                                    if (conns_NIC_1.ContainsKey(dest))
                                    {
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   "connection is already in the table");
                                        Connection tmpConn = (Connection)conns_NIC_1[dest];
                                       
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection",   "table_con id :" + tmpConn.ConInfo.Id + " new_con id :" + conn.ConInfo.Id);
                                        if (conn.ConInfo.Id < tmpConn.ConInfo.Id)
                                        {
                                            conn.Destroy();
                                            return tmpConn;
                                        }
                                        else
                                        {
                                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   dest + "--->connection present in the table is terminated");
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
                                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   "connection is already in the table");
                                        Connection tmpConn = (Connection)conns_NIC_1[dest];
                                        if (conn.ConInfo.Id < tmpConn.ConInfo.Id)
                                        {
                                            conn.Destroy();
                                            return tmpConn;
                                        }
                                        else
                                        {
                                            NCacheLog.Warn("ConnectionTable.getConnection()",   dest + "connection present in the table is terminated");
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
                            conn_syn_lock.ReleaseWriterLock();
                        }
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection",   "after lock conns.SyncRoot");
                        
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.getConnection()",   "connection is now working");
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
                    if(NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ReEstablishConnection",   "already re-established connection with " + addr);
                    return con;
                }
                con = getConnection(addr, null, true, true, true, false); 


            }
            catch (Exception ex)
            {
                con = null;
                NCacheLog.Error("ConnectionTable.ReEstablishConnection",   "failed to re-establish connection with " + addr + " " + ex.ToString());
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
                local_addr = new Address(bind_addr1, ((System.Net.IPEndPoint)srv_sock1.LocalEndpoint).Port);
            else
                local_addr = new Address(((IPEndPoint)srv_sock1.LocalEndpoint).Address, ((System.Net.IPEndPoint)srv_sock1.LocalEndpoint).Port);

            if (srv_sock2 != null)
            {
                local_addr_s = new Address(bind_addr2, ((System.Net.IPEndPoint)srv_sock2.LocalEndpoint).Port);
            }


            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("server socket created on " + local_addr);

            
            enableMonitoring = ServiceConfiguration.EnableDebuggingCounters;
            useDualConnection = ServiceConfiguration.EnableDualSocket;

            //Roland Kurmann 4/7/2003, build new thread group
            //Roland Kurmann 4/7/2003, put in thread_group
            acceptor1 = new Thread(new ThreadStart(this.RunPrimary));
            acceptor1.Name = "ConnectionTable.AcceptorThread_p";
            acceptor1.IsBackground = true;
            acceptor1.Start();

            NCacheLog.CriticalInfo("ConnectionTable.Start", "operating parameters -> [bind_addr :" + local_addr + " ; dual_socket: " + useDualConnection + " ;  ");

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
            System.Net.Sockets.TcpListener tmp;
            if (disconThread != null)
            {
                //Flush: Buffer Appender can clear all Logs as reported by this thread
                NCacheLog.Flush();
#if !NETCORE
                disconThread.Abort();
#else
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
                    tmp.Stop();
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
                    tmp.Stop();
                }
                catch (System.Exception)
                {
                }
            }

            local_addr = null;

            //2. Stop dedicated senders
            StopDedicatedSenders();

            // 3. then close the connections

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.stop",   "b4 lock conns.SyncRoot");
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
                conn_syn_lock.ReleaseWriterLock();
            }
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.stop",   "after lock conns.SyncRoot");
        }


        /// <summary>Remove <code>addr</code>from connection table. This is typically triggered when a member is suspected.</summary>
        /// 

        public virtual void remove(Address addr, bool isPrimary)
        {
            Connection conn;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.remove",   "b4 lock conns.SyncRoot");
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
                conn_syn_lock.ReleaseWriterLock();
            }
            RemoveDedicatedMessageSender(addr);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.remove",   "after lock conns.SyncRoot");
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
                NCacheLog.Error("ConnectionTable.Reconnect",   "node name is NULL");
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
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Reconnect",   node.ToString() + " is part of node rejoining list");

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

                            if (NCacheLog.IsErrorEnabled) NCacheLog.Error("ConnectionTable.Reconnect",   "Can not establish connection with " + node + " after " + _retries + " retries");

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
                        if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Reconnect",   node.ToString() + " is not part of the node rejoining list");
                        notifyConnectionClosed(node);
                        connectionCloseNotified = true;

                    }
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ConnectionTable.Reconnect",   "An error occurred while reconnecting with " + node + " Error :" + e.ToString());
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
                NCacheLog.Error("ConnectionTable.InformAboutReconnection",   e.ToString());
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
            TcpListener listener = objArr[0] as TcpListener;
            bool isPrimaryListener = (bool)objArr[1];

            System.Net.Sockets.Socket client_sock;
            Connection conn = null;
            Address peer_addr = null;

            while (listener != null)
            {
                try
                {
                    client_sock = listener.AcceptSocket();
                    int cport = ((IPEndPoint)client_sock.RemoteEndPoint).Port;

                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "CONNECTION ACCPETED Remote port = " + cport);
                    client_sock.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                   
                    client_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, send_buf_size);
                    client_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, recv_buf_size);

                    object size = client_sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
                    size = client_sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
                    // create new thread and add to conn table
                    conn = new Connection(this, client_sock, null, _ncacheLog, true,  _retries, _retryInterval); // will call receive(msg)

                    // get peer's address
                    bool connectingFirstTime = conn.readPeerAddress(client_sock, ref peer_addr);
                    conn.sendLocalAddress(local_addr,connectingFirstTime);
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

                        conn_syn_lock.ReleaseWriterLock();
                    }
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.run", "after lock conns.SyncRoot");

                    if (isPrimary && isPrimaryListener)

                        notifyConnectionOpened(peer_addr);



                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.Run()", "connection working now");
                }
                catch (VersionMismatchException ex)
                {
                    continue;
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

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ToString",   "b4 lock conns.SyncRoot");
            try
            {
                conn_syn_lock.AcquireReaderLock(Timeout.Infinite);

                ret.Append("connections (" + conns_NIC_1.Count + "):\n");
                for (System.Collections.IEnumerator e = conns_NIC_1.Keys.GetEnumerator(); e.MoveNext(); )
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
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.ToString",   "b4 lock conns.SyncRoot");
            ret.Append('\n');
            return ret.ToString();
        }

        public bool ConnectionExist(Address member)
        {
            return conns_NIC_1 != null ? conns_NIC_1.Contains(member) : false;
        }
        /// <summary>Finds first available port starting at start_port and returns server socket. Sets srv_port </summary>
        protected internal virtual System.Net.Sockets.TcpListener createServerSocket(IPAddress bind_addr, int start_port)
        {
            System.Net.Sockets.TcpListener ret = null;
            //We will try to start on a two  ports
            //	while (true) 
            for (int i = 1; i <= port_range; i++) // W 
            {

                try
                {
                    if (bind_addr == null)
                    {
                        System.Net.Sockets.TcpListener temp_tcpListener;
                        temp_tcpListener = new System.Net.Sockets.TcpListener(start_port);
                        temp_tcpListener.Start();
                        ret = temp_tcpListener;
                    }
                    else
                    {
                        System.Net.Sockets.TcpListener temp_tcpListener2;
                        temp_tcpListener2 = new System.Net.Sockets.TcpListener(new System.Net.IPEndPoint(bind_addr, start_port));
                        temp_tcpListener2.Start();
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
               
            if (ret == null) NCacheLog.Error("ConnectionTable.createServerSocket",   "binding failed " + bind_addr == null ? "null" : bind_addr.ToString() + " is not valid");
            return ret;
        }


        internal virtual void notifyConnectionOpened(Address peer)
        {
            if (peer == null)
                return;
            for (int i = 0; i < conn_listeners.Count; i++)
                ((ConnectionListener)conn_listeners[i]).connectionOpened(peer);
        }

        internal virtual void notifyConnectionClosed(Address peer)
        {
            NCacheLog.CriticalInfo("ConnectionTable.notifyConnectionClosed", peer.ToString() + " connection close notification");
            if (peer == null)
                return;
            for (int i = 0; i < conn_listeners.Count; i++)
                ((ConnectionListener)conn_listeners[i]).connectionClosed(peer);
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
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("ConnectionTable.addConnection",   "Connection added to the table");
        }





       


    }
}
