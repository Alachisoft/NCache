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
#if !NETCORE
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
#endif
namespace Alachisoft.NCache.Common.Remoting
{
	/// <summary>
	/// Summary description for NCacheChannels.
	/// </summary>
	public class RemotingChannels
	{
#if !NETCORE
        /// <summary> The underlying TCP server channel. </summary>
        private IChannel _tcpServerChannel;
		/// <summary> The underlying TCP client channel. </summary>
		private IChannel _tcpClientChannel;
		/// <summary> The underlying HTTP server channel. </summary>
		private IChannel _httpServerChannel;
		/// <summary> The underlying HTTP client channel. </summary>
		private IChannel _httpClientChannel;
        /// <summary> The underlying IPO Server channel. </summary>
        private IChannel _ipcServerChannel;
        /// <summary> The underlying IPC client channel. </summary>
        private IChannel _ipcClientChannel;
#endif
		public RemotingChannels()
		{
			//
			// TODO: Add constructor logic here 
			//
		}
#if !NETCORE
        /// <summary> Returns the underlying TCP server channel. </summary>
        public IChannel TcpServerChannel { get { return _tcpServerChannel; } }
		/// <summary> Returns the underlying TCP client channel. </summary>
		public IChannel TcpClientChannel { get { return _tcpClientChannel; } }
		/// <summary> Returns the underlying HTTP server channel. </summary>
		public IChannel HttpServerChannel { get { return _httpServerChannel; } }
		/// <summary> Returns the underlying HTTP client channel. </summary>
		public IChannel HttpClientChannel { get { return _httpClientChannel; } }
#endif

#region	/                 --- TcpChannel ---           /

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterTcpChannels(string channelName, int port)
		{
			RegisterTcpServerChannel(String.Concat(channelName, ".s"), port);
			RegisterTcpClientChannel(String.Concat(channelName, ".c"));
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterTcpChannels(string channelName, string ip, int port)
        {
            RegisterTcpServerChannel(String.Concat(channelName, ".s"), ip, port);
            RegisterTcpClientChannel(String.Concat(channelName, ".c"));

            
        }


		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterTcpChannels()
		{
			UnregisterTcpServerChannel();
			UnregisterTcpClientChannel();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterTcpServerChannel(string channelName, int port)
		{
#if !NETCORE
			_tcpServerChannel = ChannelServices.GetChannel(channelName);
			if (_tcpServerChannel == null)
			{
				BinaryServerFormatterSinkProvider sprovider = new BinaryServerFormatterSinkProvider();
				sprovider.TypeFilterLevel = TypeFilterLevel.Full;
				_tcpServerChannel = new TcpServerChannel(channelName, port, sprovider);
				ChannelServices.RegisterChannel(_tcpServerChannel);
			}
#endif
		}

        public void RegisterTcpServerChannel(string channelName, string ip, int port)
        {
            try
            {
#if !NETCORE
                _tcpServerChannel = ChannelServices.GetChannel(channelName);
                if (_tcpServerChannel == null)
                {
                    IDictionary properties = new Hashtable();
                    properties["name"] = channelName;
                    {
                        properties["port"] = port;
                        properties["bindTo"] =ip ;
                    }
                    BinaryServerFormatterSinkProvider sprovider = new BinaryServerFormatterSinkProvider();
                    sprovider.TypeFilterLevel = TypeFilterLevel.Full;
                    _tcpServerChannel = new TcpServerChannel(properties, sprovider);
                    ChannelServices.RegisterChannel(_tcpServerChannel);
                }
#endif
            }
            catch (System.Net.Sockets.SocketException se)
            {
                switch (se.ErrorCode)
                {
                    // 10049 --> address not available.
                    case 10049:
                        throw new Exception("The address " + ip + " specified for NCacheServer.BindToIP is not valid");
                    default:
                        throw;
                }
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterTcpClientChannel(string channelName)
		{
#if !NETCORE
			_tcpClientChannel = ChannelServices.GetChannel(channelName);
			if (_tcpClientChannel == null)
			{
				BinaryClientFormatterSinkProvider cprovider = new BinaryClientFormatterSinkProvider();
				_tcpClientChannel = new TcpClientChannel(channelName, cprovider);
				ChannelServices.RegisterChannel(_tcpClientChannel);
			}
#endif
		}

		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterTcpServerChannel()
		{
#if !NETCORE
            if (_tcpServerChannel != null)
			{
				ChannelServices.UnregisterChannel(_tcpServerChannel);
				_tcpServerChannel = null;
			}
#endif
		}

		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterTcpClientChannel()
		{
#if !NETCORE
			if (_tcpClientChannel != null)
			{
				ChannelServices.UnregisterChannel(_tcpClientChannel);
				_tcpClientChannel = null;
			}
#endif
		}

#endregion

#region	/                 --- HttpChannel ---           /

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterHttpChannels(string channelName, int port)
		{
			RegisterHttpServerChannel(String.Concat(channelName, ".sh"), port);
			RegisterHttpClientChannel(String.Concat(channelName, ".ch"));
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterHttpChannels(string channelName, string ip, int port)
        {
            RegisterHttpServerChannel(String.Concat(channelName, ".sh"), ip, port);
            RegisterHttpClientChannel(String.Concat(channelName, ".ch"));
        }

		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterHttpChannels()
		{
			UnregisterHttpServerChannel();
			UnregisterHttpClientChannel();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterHttpServerChannel(string channelName, int port)
		{
 #if !NETCORE
			_httpServerChannel = ChannelServices.GetChannel(channelName);
			if (_httpServerChannel == null)
			{
				BinaryServerFormatterSinkProvider sprovider = new BinaryServerFormatterSinkProvider();
				sprovider.TypeFilterLevel = TypeFilterLevel.Full;
				_httpServerChannel = new HttpServerChannel(channelName, port, sprovider);
				ChannelServices.RegisterChannel(_httpServerChannel);
			}
#endif
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterHttpServerChannel(string channelName, string ip, int port)
        {
            try
            {
 #if !NETCORE
                _httpServerChannel = ChannelServices.GetChannel(channelName);
                if (_httpServerChannel == null)
                {
                    IDictionary properties = new Hashtable();
                    properties["name"] = channelName;
                    {
                        properties["port"] = port;
                        properties["bindTo"] = ip;
                    }
                    BinaryServerFormatterSinkProvider sprovider = new BinaryServerFormatterSinkProvider();
                    sprovider.TypeFilterLevel = TypeFilterLevel.Full;
                    _httpServerChannel = new HttpServerChannel(properties, sprovider);
                    ChannelServices.RegisterChannel(_httpServerChannel);
                }
#endif
            }
            catch (System.Net.Sockets.SocketException se)
            {
                switch (se.ErrorCode)
                {
                    // 10049 --> address not available.
                    case 10049:
                        throw new Exception("The address " + ip + " specified for NCacheServer.BindToIP is not valid");
                    default:
                        throw;
                }
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="port"></param>
		public void RegisterHttpClientChannel(string channelName)
		{
#if !NETCORE
			_httpClientChannel = ChannelServices.GetChannel(channelName);
			if (_httpClientChannel == null)
			{
				BinaryClientFormatterSinkProvider cprovider = new BinaryClientFormatterSinkProvider();
				_httpClientChannel = new HttpClientChannel(channelName, cprovider);
				ChannelServices.RegisterChannel(_httpClientChannel);
			}
#endif
		}

		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterHttpServerChannel()
		{
#if !NETCORE
            if (_httpServerChannel != null)
			{
				ChannelServices.UnregisterChannel(_httpServerChannel);
				_httpServerChannel = null;
			}
#endif
		}

		/// <summary>
		/// Stop this service.
		/// </summary>
		public void UnregisterHttpClientChannel()
		{
#if !NETCORE
			if (_httpClientChannel != null)
			{
				ChannelServices.UnregisterChannel(_httpClientChannel);
				_httpClientChannel = null;
			}
#endif
		}

#endregion

#region	/                 --- IPCChannel ---           /

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterIPCChannels(string channelName, string portName)
        {
            RegisterIPCServerChannel(String.Concat(channelName, ".s"), portName);
            RegisterIPCClientChannel(String.Concat(channelName, ".c"));
            AppUtil.LogEvent("IPC channel registered at port: " + portName, System.Diagnostics.EventLogEntryType.Information);
        }    
         
        /// <summary>
        /// Stop this service.
        /// </summary>
        public void UnregisterIPCChannels()
        {
            UnregisterTcpServerChannel();
            UnregisterTcpClientChannel();
        }

        /// <summary>
        /// Registers IPC server channel.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterIPCServerChannel(string channelName, string portName)
        {
#if !NETCORE
            _ipcServerChannel = ChannelServices.GetChannel(channelName);
            if (_ipcServerChannel == null)
            {
                BinaryServerFormatterSinkProvider sprovider = new BinaryServerFormatterSinkProvider();
                sprovider.TypeFilterLevel = TypeFilterLevel.Full;
                _ipcServerChannel = new IpcServerChannel(channelName,portName, sprovider); ;
                ChannelServices.RegisterChannel(_ipcServerChannel);
            }
#endif
        }

        /// <summary>
        /// Registers IPC Client Channel.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="port"></param>
        public void RegisterIPCClientChannel(string channelName)
        {
#if !NETCORE
            _ipcClientChannel = ChannelServices.GetChannel(channelName);
            if (_ipcClientChannel == null)
            {
                BinaryClientFormatterSinkProvider cprovider = new BinaryClientFormatterSinkProvider();
                _ipcClientChannel = new IpcClientChannel(channelName, cprovider);
                ChannelServices.RegisterChannel(_ipcClientChannel);
            }
#endif
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        public void UnregisterIPCServerChannel()
        {
#if !NETCORE
            if (_ipcServerChannel != null)
            {
                ChannelServices.UnregisterChannel(_ipcServerChannel);
                _ipcServerChannel = null;
            }
#endif
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        public void UnregisterIPCClientChannel()
        {
#if !NETCORE
            if (_ipcClientChannel != null)
            {
                ChannelServices.UnregisterChannel(_ipcClientChannel);
                _ipcClientChannel = null;
            }
#endif
        }

#endregion
	}
}
