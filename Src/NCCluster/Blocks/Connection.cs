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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Sockets;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Serialization.Formatters;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Alachisoft.NGroups.Blocks
{

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

        private int _retries;
        private int _retryInterval;

        bool isMember;
        public bool markedClose;
        private ConnectInfo conInfo;
        private bool iaminitiater;
        private TimeSpan _worsRecvTime = new TimeSpan(0, 0, 0);
        private TimeSpan _worsSendTime = new TimeSpan(0, 0, 0);


        internal Connection(ConnectionTable enclosingInstance, System.Net.Sockets.Socket s, Address peer_addr, ILogger NCacheLog, bool isPrimary, int retries, int retryInterval)

        {
            InitBlock(enclosingInstance);
            sock = s;
            this.peer_addr = peer_addr;

            this._retries = retries;
            this._retryInterval = retryInterval;

            this._ncacheLog = NCacheLog;

            _isPrimary = isPrimary;

            receiveBuffer = new byte[receiveBufferSize];

            _socSendArgs = new SocketAsyncEventArgs();
            _socSendArgs.UserToken = new SendContext();
            _socSendArgs.Completed += OnCompleteAsyncSend;
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
            #region New Async mechanism

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("connection was created to " + peer_addr);
            #endregion

            #region Old sync mechanism

            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("connection was created to " + peer_addr);
            if (handler == null)
            {

                // Roland Kurmann 4/7/2003, put in thread_group
                handler = new ThreadClass(new System.Threading.ThreadStart(this.Run), "ConnectionTable.Connection.HandlerThread");
                handler.IsBackground = true;
                handler.Start();
            }

            #endregion
        }

        #region Async mechanism

        #region Constants

        private const int BufferHeader = sizeof(int);

        #endregion

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
#else
                        handler.Interrupt();
#endif
                }
                catch (Exception) { }
            }
            handler = null;
            if (inStream != null) inStream.Close();
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
                {
                    lock (send_mutex)
                    {
                        socket_error = true;
                        isConnected = false;
                    }
                    throw new ExtSocketException("Connection is closed");
                }
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
            #region Old Sync Mechanism
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
            #endregion
        }

        /// <summary> Reads the peer's address. First a cookie has to be sent which has to match my own cookie, otherwise
        /// the connection will be refused
        /// </summary>
        internal virtual bool readPeerAddress(System.Net.Sockets.Socket client_sock, ref Address peer_addr)
        {
            //Address peer_addr = null;
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
                    object[] objArray = new object[] { local_addr, connectingFirstTime, currentVersion };
                    buf = CompactBinaryFormatter.ToByteBuffer(objArray, null);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after serializing...");

                    byte[] lenBuff;// write the length of the buffer
                    lenBuff = Util.Util.WriteInt32(buf.Length);
                    sock.Send(lenBuff);

                    // and finally write the buffer itself
                    sock.Send(buf);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after sending...");
                    //out_Renamed.Flush(); // needed ?
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
                //sock.Receive(lenBuff, 4 , SocketFlags.None);
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

                    // write the version
                    sock.Send(Version.version_id);

                    // write the length of the buffer
                    byte[] lenBuff;
                    lenBuff = Util.Util.WriteInt32(buf.Length);
                    sock.Send(lenBuff);

                    // and finally write the buffer itself
                    sock.Send(buf);
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.sendLocaladress", "after sending...");
                    //out_Renamed.Flush(); // needed ?
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

                                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.Run", "connection being closed silently");
                                            this.self_close = true;
                                            handler = null;
                                            continue;

                                        case ConnectionHeader.LEAVE:
                                            //The node is leaving the cluster gracefully.
                                            leavingGracefully = true;
                                            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.Run", peer_addr.ToString() + " is leaving gracefully");
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
                enclosingInstance.notifyConnectionClosed(peer_addr);

                enclosingInstance.remove(peer_addr, IsPrimary);

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
                    sock.Close(); // should actually close in/out (so we don't need to close them explicitly)
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("Connection.closeSocket()", "connection destroyed");
                }
                catch (System.Exception e)
                {
                    //log.Error("Connection.Close()", e.Message);
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
}
