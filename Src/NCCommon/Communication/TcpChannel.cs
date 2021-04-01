//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Common.Communication.Exceptions;
using System.IO;

namespace Alachisoft.NCache.Common.Communication
{
    public class TcpChannel: IChannel
    {
        const int DATA_SIZE_BUFFER_LENGTH = 10; 
        
        IConnection _connection;
        string _serverIP;
        string _bindIP;
        int _port;
        byte[] _sizeBuffer = new byte[DATA_SIZE_BUFFER_LENGTH];
        IChannelFormatter _formatter;
        IChannelEventListener _eventListener;
        Thread _receiverThread;
        ITraceProvider _traceProvider;
        private string _name;

        public TcpChannel(string serverIP, int port,string bindingIP,ITraceProvider traceProvider)
        {
            if (string.IsNullOrEmpty(serverIP))
                throw new ArgumentNullException("serverIP");

            _serverIP = serverIP;
            _port = port;
            _bindIP = bindingIP;
            _traceProvider = traceProvider;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
       
        public bool Connect()
        {
            lock (this)
            {
                if (_formatter == null)
                {
                    throw new Exception("Channel formatter is not specified");
                }

                if (_eventListener == null)
                {
                    throw new Exception("There is no channel event listener specified");
                }

                try
                {
                    if (_connection == null)
                    {
                        _connection = new TcpConnection();

                        if (!string.IsNullOrEmpty(_bindIP))
                            _connection.Bind(_bindIP);

                        _connection.Connect(_serverIP, _port);

                        _receiverThread = new Thread(new ThreadStart(Run));
                        _receiverThread.IsBackground = true;
                        _receiverThread.Start();
                        return true;
                    }
                }
                catch (ConnectionException ce)
                {
                    if (_traceProvider != null)
                    {
                        _traceProvider.TraceError(Name + ".Connect", ce.ToString());
                    }
                    throw new ChannelException(ce.Message, ce);
                }
            }
            return false;
        }

        public void Disconnect()
        {
            lock (this)
            {
                if (_connection != null)
                {
                    _connection.Disconnect();
                    if (_receiverThread != null && _receiverThread.IsAlive)
                        if (!_receiverThread.Join(500))
#if !NETCORE
                            _receiverThread.Abort();
#elif NETCORE
                            _receiverThread.Interrupt();
#endif
                    _connection = null;
                }
            }
        }

        public bool SendMessage(object message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            //first serialize the message using channel formatter
            byte[] serailizedMessage = _formatter.Serialize(message);
            byte[] msgLength = UTF8Encoding.UTF8.GetBytes(serailizedMessage.Length.ToString());
            //message is written in a specific order as expected by Socket server
            //"MNG" tag for all management commands in discarding buffer
            byte[] mngTag = Encoding.ASCII.GetBytes("MNG");

            MemoryStream stream = new MemoryStream();
            stream.Position = 0;
            stream.Write(mngTag, 0, mngTag.Length);
            stream.Position = 20; //skip discarding buffer
            stream.Write(msgLength, 0, msgLength.Length);
            stream.Position = 30;
            stream.Write(serailizedMessage, 0, serailizedMessage.Length);

            byte[] finalBuffer = stream.ToArray();
            stream.Close();

            try
            {
                if (EnsureConnected())
                {
                    try
                    {
                        _connection.Send(finalBuffer, 0, finalBuffer.Length);
                        return true;
                    }
                    catch (ConnectionException)
                    {
                        if (EnsureConnected())
                        {
                            _connection.Send(finalBuffer, 0, finalBuffer.Length);
                            return true;
                        }
                    }

                }
            }
            catch (Exception e)
            {
                throw new ChannelException(e.Message, e);
            }

            return false;
        }

        private bool EnsureConnected()
        {
            lock (this)
            {
                if (_connection != null && !_connection.IsConnected)
                {
                    Disconnect();
                    Connect();
                }
            }

            return _connection.IsConnected;
        }

        public void RegisterEventListener(IChannelEventListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener");

            _eventListener = listener;
        }

        public void UnRegisterEventListener(IChannelEventListener listener)
        {
            if(listener != null && listener.Equals(_eventListener))
                _eventListener = null;
        }

        public IChannelFormatter Formatter
        {
            get
            {
                return _formatter;
            }
            set
            {
                _formatter = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void Run()
        {
            try
            {
                while (true)
                {
                    try
                    {

                        //receive data size for the response
                        if (_connection != null)
                        {
                            _connection.Receive(_sizeBuffer, DATA_SIZE_BUFFER_LENGTH);

                            int rspLength = Convert.ToInt32(UTF8Encoding.UTF8.GetString(_sizeBuffer, 0, _sizeBuffer.Length));

                            if (rspLength > 0)
                            {
                                byte[] dataBuffer = new byte[rspLength];
                                _connection.Receive(dataBuffer, rspLength);

                                //deserialize the message
                                IResponse response = null;
                                if (_formatter != null)
                                    response = _formatter.Deserialize(dataBuffer) as IResponse;

                                if (_eventListener != null)
                                    _eventListener.ReceiveResponse(response);
                            }

                        }
                        else
                        {
                            break;
                        }

                    }
                    catch (ThreadAbortException) { break; }
                    catch (ThreadInterruptedException) { break; }
                    catch (ConnectionException ce)
                    {
                        if (_traceProvider != null)
                        {
                            _traceProvider.TraceError(Name + ".Run", ce.ToString());
                        }
                        if (_eventListener != null) _eventListener.ChannelDisconnected(_serverIP, ce.Message);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (_traceProvider != null)
                        {
                            _traceProvider.TraceError(Name + ".Run", e.ToString());
                        }
                        object error = null;

                        if (_formatter != null)
                            error = _formatter.GetErrorResponse(e);

                        if (_eventListener != null)
                            _eventListener.ChannelError(error);
                    }
                }
            }
            catch (ThreadAbortException ex)
            { 
                
            }
            catch (ThreadInterruptedException e)
            {

            }
        }


        public string Server
        {
            get {
                
                if (_serverIP != null) return _serverIP;

                return "";
            }
        }
    }
}
