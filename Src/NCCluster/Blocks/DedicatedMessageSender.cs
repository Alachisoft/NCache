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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NGroups.Blocks
{
    internal class DedicatedMessageSender : ThreadClass, IDisposable
    {
        private Alachisoft.NCache.Common.DataStructures.Queue mq;
        private Connection peerConnection;
        private ConnectionTable connectionTable;
        int id;
        private object sync_lock;
        ILogger _ncacheLog;
        int sendBufferSize = 1024 * 1024;
        byte[] sendBuffer;
        long waitTimeout = 0;
        PerfStatsCollector perfStatsCollector;

        public DedicatedMessageSender(Alachisoft.NCache.Common.DataStructures.Queue mq, Connection connection, object syncLockObj, ILogger NCacheLog, bool onPrimaryNIC)
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
            sendBuffer = new byte[sendBufferSize];
        }

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public void UpdateConnection(Connection newCon)
        {
            peerConnection = newCon;
        }

        /// <summary>Removes events from mq and calls handler.down(evt) </summary>
        override public void Run()
        {
            bool connectionCloseNotified = false;
            try
            {
                ArrayList msgList = new ArrayList();
                byte[] tmpBuffer = null;
                int totalMsgSize = 4;
                int noOfMsgs = 0;
                int offset = 8;
                bool resendMessage = false;
                ArrayList msgsTobeSent = new ArrayList();
                ClusteredMemoryStream stream = null;
                while (!mq.Closed)
                {
                    try
                    {
                        if (resendMessage)
                        {
                            if (stream!= null && stream.Length > 0)
                            {
                                peerConnection.send(stream.GetInternalBuffer(), null, totalMsgSize + 4);


                            }
                            resendMessage = false;
                            continue;
                        }
                        msgsTobeSent.Clear();
                        stream = new ClusteredMemoryStream();

                        lock (sync_lock)
                        {
                            tmpBuffer = sendBuffer;
                            totalMsgSize = 4;
                            noOfMsgs = 0;
                            offset = 8;
                            
                            stream.Seek(8, System.IO.SeekOrigin.Begin);

                            BinaryMessage bMsg = (BinaryMessage)mq.remove();
                            if (bMsg != null)
                            {

                                if (!peerConnection.IsPrimary) msgsTobeSent.Add(bMsg);

                                noOfMsgs++;
                                totalMsgSize += bMsg.Size;

                                foreach (byte[] buffer in bMsg.Buffer)
                                {
                                    stream.Write(buffer, 0, buffer.Length);
                                }
                                if (bMsg.UserPayLoad != null)
                                {
                                    byte[] buf = null;
                                    for (int i = 0; i < bMsg.UserPayLoad.Length; i++)
                                    {
                                        buf = bMsg.UserPayLoad.GetValue(i) as byte[];
                                        stream.Write(buf, 0, buf.Length);
                                        offset += buf.Length;
                                    }
                                }
                            }
                            bMsg = null;
                            
                        }

                        byte[] bTotalLength = Util.Util.WriteInt32(totalMsgSize);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        stream.Write(bTotalLength, 0, bTotalLength.Length);
                        byte[] bNoOfMsgs = Util.Util.WriteInt32(noOfMsgs);
                        stream.Write(bNoOfMsgs, 0, bNoOfMsgs.Length);
                        peerConnection.send(stream.GetInternalBuffer(), null, totalMsgSize + 4);
                        stream = null;

                    }
                    catch (ExtSocketException e)
                    {
                        connectionCloseNotified = false;
                        NCacheLog.Error(Name, e.ToString());

                        if (peerConnection.IsPrimary)
                        {

                            if (peerConnection.LeavingGracefully)
                            {
                                NCacheLog.Error("DmSender.Run", peerConnection.peer_addr + " left gracefully");
                                break;
                            }
                                NCacheLog.Error("DMSender.Run", "Connection broken with " + peerConnection.peer_addr + ". node left abruptly");
                                Connection connection = peerConnection.Enclosing_Instance.Reconnect(peerConnection.peer_addr, out connectionCloseNotified);

                                if (connection != null)
                                {
                                    Thread.Sleep(3000);
                                    resendMessage = true;
                                    continue;
                                }
                                else
                                {
                                    NCacheLog.Error("DMSender.Run", Name + ". Failed to re-establish connection with " + peerConnection.peer_addr);
                                    break;
                                }
                        }
                        else
                        {
                            NCacheLog.Error("DmSender.Run", "secondary connection broken; peer_addr : " + peerConnection.peer_addr);
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
                                        NCacheLog.Error("DmSender.Run", "an error occurred while requing the messages. " + ex.ToString());
                                    }
                                }
                            }
                            catch (Exception) { }
                        }

                        break;
                    }
                    catch (QueueClosedException e)
                    {
                        connectionCloseNotified = false;
                        break;
                    }
                    catch (ThreadInterruptedException e)
                    {
                        connectionCloseNotified = false;
                        break;
                    }
                    catch (ThreadAbortException) 
                    { 
                        break; 
                    }
                    catch (System.Exception e)
                    {
                        connectionCloseNotified = false;
                        NCacheLog.Error(Name + "", Name + " exception is " + e.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException){ }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                connectionCloseNotified = false;
                NCacheLog.Error(Name + "", "exception=" + ex.ToString());
            }
            try
            {
                if (!connectionCloseNotified)
                    peerConnection.Enclosing_Instance.notifyConnectionClosed(peerConnection.peer_addr);
                else
                    NCacheLog.CriticalInfo("DmSender.Run", "no need to notify about connection close");

                peerConnection.Enclosing_Instance.remove(peerConnection.peer_addr, peerConnection.IsPrimary);

            }
            catch (Exception e)
            {
                //who cares...
            }

        }

        #region IDisposable Members

        public void Dispose()
        {

        }

        #endregion
    }
}