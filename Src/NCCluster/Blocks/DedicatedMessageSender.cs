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
using System;
using System.Collections;
using System.Threading;
using Alachisoft.NGroups.Stack;
using Alachisoft.NGroups.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NGroups.Blocks
{
    internal class DedicatedMessageSender : ThreadClass, IDisposable
    {
        private Alachisoft.NCache.Common.DataStructures.Queue mq;
        private ConnectionTable.Connection peerConnection;
        private ConnectionTable connectionTable;
        int id;
        private object sync_lock;

        
        ILogger _ncacheLog;

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        int sendBufferSize = 1024 * 1024;
        byte[] sendBuffer;
        long waitTimeout = 0;
        bool isNagglingEnabled = true;
        int nagglingSize = 500 * 1024;

        PerfStatsCollector perfStatsCollector;


        public DedicatedMessageSender(Alachisoft.NCache.Common.DataStructures.Queue mq, ConnectionTable.Connection connection, object syncLockObj, ILogger NCacheLog, bool onPrimaryNIC, bool doNaggling, int naglingSize)

        {
            this.mq = mq;
            this.peerConnection = connection;
            if (connection != null)
            {
                string primary = connection.IsPrimary ? "p" : "s";
                string primaryNIC = onPrimaryNIC ? "p" : "s";
                Name = "DmSender - " + connection.peer_addr.ToString() + " - " + primary + primaryNIC;

            }

            perfStatsCollector = peerConnection.Enclosing_Instance.enclosingInstance.Stack.perfStatsColl;

            IsBackground = true;
            sync_lock = syncLockObj;
            _ncacheLog = NCacheLog;
            isNagglingEnabled = doNaggling;
            nagglingSize = naglingSize;
            if (nagglingSize + 8 > sendBufferSize) sendBufferSize = nagglingSize + 8;
            sendBuffer = new byte[sendBufferSize];
        }

        public void UpdateConnection(ConnectionTable.Connection newCon)
        {
            peerConnection = newCon;
        }

        /// <summary>Removes events from mq and calls handler.down(evt) </summary>
        override public void Run()
        {
            try
            {
                ArrayList msgList = new ArrayList();
                byte[] tmpBuffer = null;
                int totalMsgSize = 4;
                int noOfMsgs = 0;
                int offset = 8;
                bool resendMessage = false;
                ArrayList msgsTobeSent = new ArrayList();
                while (!mq.Closed)
                {
                    try
                    {
                        if (resendMessage)
                        {
                            if (tmpBuffer != null && tmpBuffer.Length > 0)
                            {
                                peerConnection.send(tmpBuffer, null, totalMsgSize + 4);

                                if (perfStatsCollector != null) perfStatsCollector.IncrementNagglingMessageStats(noOfMsgs);

                            }
                            resendMessage = false;
                            continue;
                        }
                        msgsTobeSent.Clear();
                        lock (sync_lock)
                        {
                            tmpBuffer = sendBuffer;
                            totalMsgSize = 4;
                            noOfMsgs = 0;
                            offset = 8;
                            while (true)
                            {
                                BinaryMessage bMsg = (BinaryMessage)mq.remove();
                                
                                if (bMsg != null)
                                {

                                    if (!peerConnection.IsPrimary) msgsTobeSent.Add(bMsg);

                                    noOfMsgs++;

                                    totalMsgSize += bMsg.Size;

                                    if (totalMsgSize + 8 > sendBuffer.Length)
                                    {
                                        byte[] bigbuffer = new byte[totalMsgSize + 8];
                                        Buffer.BlockCopy(tmpBuffer, 0, bigbuffer, 0, totalMsgSize - bMsg.Size);
                                        tmpBuffer = bigbuffer;
                                    }

                                    Buffer.BlockCopy(bMsg.Buffer, 0, tmpBuffer, offset, bMsg.Buffer.Length);
                                    offset += bMsg.Buffer.Length;
                                    if (bMsg.UserPayLoad != null)
                                    {
                                        byte[] buf = null;
                                        for (int i = 0; i < bMsg.UserPayLoad.Length; i++)
                                        {
                                            buf = bMsg.UserPayLoad.GetValue(i) as byte[];
                                            Buffer.BlockCopy(buf, 0, tmpBuffer, offset, buf.Length);
                                            offset += buf.Length;
                                        }
                                    }
                                }
                                bMsg = null;
                                bool success;
                                bMsg = mq.peek(waitTimeout, out success) as BinaryMessage;
                                if ((!isNagglingEnabled || bMsg == null || ((bMsg.Size + totalMsgSize + 8) > nagglingSize))) break;

                            }
                        }

                        
                        byte[] bTotalLength = Util.Util.WriteInt32(totalMsgSize);
                        Buffer.BlockCopy(bTotalLength, 0, tmpBuffer, 0, bTotalLength.Length);
                        byte[] bNoOfMsgs = Util.Util.WriteInt32(noOfMsgs);
                        Buffer.BlockCopy(bNoOfMsgs, 0, tmpBuffer, 4, bNoOfMsgs.Length);
                        peerConnection.send(tmpBuffer, null, totalMsgSize + 4);

                        if (perfStatsCollector != null) perfStatsCollector.IncrementNagglingMessageStats(noOfMsgs);

                    }
                    catch (ExtSocketException e)
                    {
                        NCacheLog.Error(Name, e.ToString());

                        if (peerConnection.IsPrimary)
                        {

                            if (peerConnection.LeavingGracefully)
                            {
                                NCacheLog.Error("DmSender.Run",   peerConnection.peer_addr + " left gracefully");
                                break;
                            }
                            
                            
                                NCacheLog.Error("DMSender.Run",   "Connection broken with " + peerConnection.peer_addr + ". node left abruptly");
                                ConnectionTable.Connection connection = peerConnection.Enclosing_Instance.Reconnect(peerConnection.peer_addr);
                                if (connection != null)
                                {
                                    
                                    Thread.Sleep(3000);
                                    resendMessage = true;
                                    continue;
                                }
                                else
                                {
                                    NCacheLog.Error("DMSender.Run",   Name + ". Failed to re-establish connection with " + peerConnection.peer_addr);
                                    break;
                                }

                            
                        }
                        else
                        {
                            NCacheLog.Error("DmSender.Run",   "secondary connection broken; peer_addr : " + peerConnection.peer_addr);
                            try
                            {
                                foreach (BinaryMessage bMsg in msgsTobeSent)
                                {
                                    try
                                    {
                                        if (bMsg != null && mq != null && !mq.Closed)
                                            mq.add(bMsg);
                                    }
                                    catch (Exception ex)
                                    {
                                        NCacheLog.Error("DmSender.Run",   "an error occurred while requing the messages. " + ex.ToString());
                                    }
                                }
                            }
                            catch (Exception) { }
                        }

                        break;
                    }
                    catch (QueueClosedException e)
                    {
                       
                        break;
                    }
                    catch (ThreadInterruptedException e)
                    {
                       
                        break;
                    }
                    catch (ThreadAbortException) {  }
                    catch (System.Exception e)
                    {
                        
                        NCacheLog.Error(Name + "",   Name + " exception is " + e.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
               
            }

            catch (ThreadAbortException) {  }

            catch (Exception ex)
            {
                NCacheLog.Error(Name + "",  "exception=" + ex.ToString());
                
            }
            try
            {
                peerConnection.Enclosing_Instance.notifyConnectionClosed(peerConnection.peer_addr);

                peerConnection.Enclosing_Instance.remove(peerConnection.peer_addr, peerConnection.IsPrimary);

            }
            catch (Exception e)
            {
                
            }

        }

        #region IDisposable Members

        public void Dispose()
        {

        }

        #endregion
    }
}
