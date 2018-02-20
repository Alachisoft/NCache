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
// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $
using Alachisoft.NGroups.Blocks;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Net;
using System;
using System.Collections;
using System.Threading;

namespace Alachisoft.NGroups.Protocols
{
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
#else
                    _thread.Interrupt();
#endif
                _thread = null;
            }
            if (_statusCheckingThread != null)
            {
#if !NETCORE
                _statusCheckingThread.Abort();
#else
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
                            msg.putHeader(HeaderType.KEEP_ALIVE, new TCPHearBeat(TCPHearBeat.ARE_YOU_ALIVE));

                            if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "sending status request to " + _currentSuspect);


                            _enclosingInstance.sendUnicastMessage(msg, false, msg.Payload, Priority.High);
                            _statusReceived = null;

                            //wait for the result or timeout occurs first;
                            Monitor.Wait(_status_mutex, _statusTimeout);

                            if (_statusReceived != null)
                            {
                                TCPHearBeat status = _statusReceived as TCPHearBeat;

                                if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.CheckStatus", "received status " + status + " from " + _currentSuspect);

                                if (status.Type == TCPHearBeat.I_AM_NOT_DEAD)
                                    nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_ALIVE);
                                else if (status.Type == TCPHearBeat.I_AM_LEAVING)
                                    nodeStatus = new NodeStatus(_currentSuspect, NodeStatus.IS_LEAVING);
                                else if (status.Type == TCPHearBeat.I_AM_STARTING)
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
            msg.putHeader(HeaderType.KEEP_ALIVE, new TCPHearBeat(TCPHearBeat.SEND_HEART_BEAT));
            msg.Dests = idleMembers.Clone() as ArrayList;
            _enclosingInstance.sendMulticastMessage(msg, false, Priority.High);

        }

        private bool CheckConnected(Address member)
        {
            bool isConnected = false;
            ConnectionTable ct = _enclosingInstance.ct;
            Connection con;
            con = ct.GetPrimaryConnection(member, false);
            if (con != null)
            {
                isConnected = con.IsConnected;
            }
            return isConnected;
        }

        public void ReceivedHeartBeat(Address sender, TCPHearBeat hrtBeat)
        {
            Message rspMsg;
            switch (hrtBeat.Type)
            {

                case TCPHearBeat.HEART_BEAT:
                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "received heartbeat from ->:" + sender.ToString());

                    lock (_idleConnections.SyncRoot)
                    {
                        _idleConnections.Remove(sender);
                    }
                    break;

                case TCPHearBeat.SEND_HEART_BEAT:

                    rspMsg = new Message(sender, null, new byte[0]);
                    rspMsg.putHeader(HeaderType.KEEP_ALIVE, new TCPHearBeat(TCPHearBeat.HEART_BEAT));
                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "seding heartbeat to ->:" + sender.ToString());

                    _enclosingInstance.sendUnicastMessage(rspMsg, false, rspMsg.Payload, Priority.High);
                    break;

                case TCPHearBeat.ARE_YOU_ALIVE:

                    rspMsg = new Message(sender, null, new byte[0]);



                    TCPHearBeat rsphrtBeat = (_enclosingInstance.isClosing || _enclosingInstance._leaving) ? new TCPHearBeat(TCPHearBeat.I_AM_LEAVING) : new TCPHearBeat(TCPHearBeat.I_AM_NOT_DEAD);
                    rsphrtBeat = _enclosingInstance.isStarting ? new TCPHearBeat(TCPHearBeat.I_AM_STARTING) : rsphrtBeat;
                    rspMsg.putHeader(HeaderType.KEEP_ALIVE, rsphrtBeat);
                    if (_enclosingInstance.Stack.NCacheLog.IsInfoEnabled) _enclosingInstance.Stack.NCacheLog.Info("ConnectionKeepAlive.ReceivedHeartBeat", "seding status" + rsphrtBeat + " to ->:" + sender.ToString());

                    _enclosingInstance.sendUnicastMessage(rspMsg, false, rspMsg.Payload, Priority.High);
                    break;

                case TCPHearBeat.I_AM_STARTING:
                case TCPHearBeat.I_AM_LEAVING:
                case TCPHearBeat.I_AM_NOT_DEAD:


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
}
