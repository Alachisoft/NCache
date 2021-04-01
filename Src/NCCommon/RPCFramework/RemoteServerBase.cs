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
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.Net;
using System;
using System.Net;

namespace Alachisoft.NCache.Common.RPCFramework
{
    public abstract class RemoteServerBase : IDisposable
    {
        protected RequestManager _requestManager;
        protected string _server;
        protected int _port;
        protected string _bindIP;
        protected bool initialized;
        protected IChannelDisconnected _channelDisconnected;

        public RemoteServerBase(string server, int port)
            : this(server, port, null)
        {

        }

        public RemoteServerBase(string server, int port, string bindIp)
        {
            if (server == null) throw new ArgumentNullException("server");
            IPAddress address = DnsCache.Resolve(server);

            if (address == null) throw new Exception("Failed to resolve server address to IP. Server =" + server);
            _server = address.ToString();
            _port = port;
            _bindIP = bindIp;
        }

        protected abstract IChannelFormatter GetChannelFormatter();

        protected virtual bool InitializeInternal()
        {
            return true;
        }


        public int RequestTimedout
        {
            get { return _requestManager.RequestTimedout; }
            set { _requestManager.RequestTimedout = value; }
        }

        public bool Initialized
        {
            get { return initialized; }
        }

        public void Initialize(ITraceProvider traceProvider)
        {
            TcpChannel channel = new TcpChannel(_server, _port, _bindIP, traceProvider);
            channel.Formatter = GetChannelFormatter();
            RequestManager requestManager = new RequestManager(channel, _channelDisconnected);
            channel.Connect();
            _requestManager = requestManager;

            try
            {
                initialized = InitializeInternal();
            }
            catch (Exception)
            {
                channel.Disconnect();
                _requestManager = null;
                throw;
            }

            if (!initialized)
            {
                channel.Disconnect();
                _requestManager = null;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_requestManager != null)
                    _requestManager.Dispose();
            }
            catch (Exception) { }
        }

        public void OnChannelDisconnected(IChannelDisconnected channelDisconnection)
        {
            _channelDisconnected = channelDisconnection;
        }
    }

}
