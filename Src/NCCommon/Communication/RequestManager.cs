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
using System.Collections;
using Alachisoft.NCache.Common.Communication.Exceptions;
namespace Alachisoft.NCache.Common.Communication
{
    public class RequestManager :IChannelEventListener, IDisposable
    {
        IChannel _channel;
        Hashtable _requests = new Hashtable();
        object _lock = new object();
        long _lastRequestId;
        bool _resendRequestOnChannelDisconnect = true;
        private int _requestTimeout = 90 *1000; //default is ninety second
        private IChannelDisconnected _channelDisconnectedListener;


        public RequestManager(IChannel chnnel, IChannelDisconnected channelDisconnectedListener)
        {
            if (chnnel == null)
                throw new ArgumentNullException("channel");

            _channel = chnnel;
            _channel.RegisterEventListener(this);
            _channelDisconnectedListener = channelDisconnectedListener;
        }

        public int RequestTimedout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = value; }
        }

        public object SendRequest(IRequest request)
        {
            object response = null;

            request.RequestId = GenerateRequestId();
            bool lockReacquired = false;
            RequestResponsePair reqRespPair = new RequestResponsePair();

            lock (_lock)
            {
                reqRespPair.Request = request;

                if (!_requests.Contains(request.RequestId))
                {
                    _requests.Add(request.RequestId, reqRespPair);
                }
            }

        
            try
            {
                lock (reqRespPair)
                {
                    _channel.SendMessage(request);
                    reqRespPair.RequestSentOverChannel = true;
                    lockReacquired = System.Threading.Monitor.Wait(reqRespPair, _requestTimeout);
                }
            }
            catch (ChannelException e)
            {
                throw;
            }
            finally
            {
                lock (_lock)
                {
                    _requests.Remove(request.RequestId);
                }
            }


            if (!lockReacquired)
                throw new Runtime.Exceptions.TimeoutException("Request has timedout. Did not receive response from " + _channel.Server);

            if (reqRespPair.ChannelException != null)
                throw reqRespPair.ChannelException;

            response = reqRespPair.Response;

            return response;
        }

        private long GenerateRequestId()
        {
            lock (this)
            {
                long requestId = ++_lastRequestId;
                if(requestId <0)
                {
                    _lastRequestId = 0;
                    requestId = 0;
                }
                return requestId;
            }
        }

        #region  /                      --- IChannelEventListener's Implementation---               /

        public void ReceiveResponse(IResponse response)
        {
            IResponse protoResponse = response;
            RequestResponsePair reqResponsePair = _requests[protoResponse.RequestId] as RequestResponsePair;
  
            lock (reqResponsePair)
            {
                if (reqResponsePair != null)
                {
                    reqResponsePair.Response = protoResponse;
                    System.Threading.Monitor.Pulse(reqResponsePair);
                }
            }
        }

        public void ChannelDisconnected(string serverIp, string reason)
        {
            Hashtable requestClone = null;
            lock (_lock)
            {
                requestClone = _requests.Clone() as Hashtable;
            }
            IDictionaryEnumerator ide = requestClone.GetEnumerator();

            while (ide.MoveNext())
            {
                RequestResponsePair reqRspPair = ide.Value as RequestResponsePair;

                if (!reqRspPair.RequestSentOverChannel) continue;

                lock (reqRspPair)
                {
                    if (_resendRequestOnChannelDisconnect)
                    {
                        //resend the request when channel is disconnected
                        try
                        {
                            if (_channel != null) _channel.SendMessage(reqRspPair.Request);
                        }
                        catch (ChannelException ce)
                        {
                            lock (reqRspPair)
                            {
                                reqRspPair.ChannelException = ce;
                                System.Threading.Monitor.PulseAll(reqRspPair);
                            }
                        }
                    }
                    else
                    {
                        lock (reqRspPair)
                        {
                            reqRspPair.ChannelException = new ChannelException(reason);
                            System.Threading.Monitor.PulseAll(reqRspPair);
                        }
                    }

                   if(_channelDisconnectedListener !=null)
                        _channelDisconnectedListener.ChannelDisconnected(serverIp, reason);
                }
            }

        }
        public void ChannelError(object error)
        {
            Hashtable requestClone = null;
            lock (_lock)
            {
                requestClone = _requests.Clone() as Hashtable;
            }
            IDictionaryEnumerator ide = requestClone.GetEnumerator();

            while (ide.MoveNext())
            {
                RequestResponsePair reqRspPair = ide.Value as RequestResponsePair;

                if (!reqRspPair.RequestSentOverChannel) continue;

                lock (reqRspPair)
                {
                    reqRspPair.Response = error;
                    System.Threading.Monitor.PulseAll(reqRspPair);
                }
            }
        }
        
        #endregion

        private void Dispose(bool gracefull)
        {
            try
            {
                lock (_lock)
                {
                    if(_requests != null)
                        _requests.Clear();
                    if (_channel != null)
                    {
                        _channel.Disconnect();
                        _channel = null;
                    }
                }
            }
            catch (Exception e)
            {
            }
            if(gracefull)
                GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
        }
     
        ~RequestManager()
        {
            Dispose(false);
        }
    }
}
