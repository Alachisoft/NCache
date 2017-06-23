// Copyright (c) 2017 Alachisoft
// 
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
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Reflection;
using Enyim.Caching.Memcached.Protocol.Binary;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configuration class
	/// </summary>
	public class MemcachedClientConfiguration : IMemcachedClientConfiguration
	{
		// these are lazy initialized in the getters
		private Type nodeLocator;
		private ITranscoder transcoder;
		private IMemcachedKeyTransformer keyTransformer;
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MemcachedClientConfiguration"/> class.
		/// </summary>
		public MemcachedClientConfiguration()
		{
			this.Servers = new List<IPEndPoint>();
			this.SocketPool = new SocketPoolConfiguration();
			this.Authentication = new AuthenticationConfiguration();

			this.Protocol = MemcachedProtocol.Binary;
		}

		/// <summary>
		/// Adds a new server to the pool.
		/// </summary>
		/// <param name="address">The address and the port of the server in the format 'host:port'.</param>
		public void AddServer(string address)
		{
			this.Servers.Add(ConfigurationHelper.ResolveToEndPoint(address));
		}

		/// <summary>
		/// Adds a new server to the pool.
		/// </summary>
		/// <param name="address">The host name or IP address of the server.</param>
		/// <param name="port">The port number of the memcached instance.</param>
		public void AddServer(string host, int port)
		{
			this.Servers.Add(ConfigurationHelper.ResolveToEndPoint(host, port));
		}

		/// <summary>
		/// Gets a list of <see cref="T:IPEndPoint"/> each representing a Memcached server in the pool.
		/// </summary>
		public IList<IPEndPoint> Servers { get; private set; }

         /// <summary>
		/// Gets the configuration of the socket pool.
		/// </summary>
		public ISocketPoolConfiguration SocketPool { get; private set; }

		/// <summary>
		/// Gets the authentication settings.
		/// </summary>
		public IAuthenticationConfiguration Authentication { get; private set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		public IMemcachedKeyTransformer KeyTransformer
		{
			get { return this.keyTransformer ?? (this.keyTransformer = new DefaultKeyTransformer()); }
			set { this.keyTransformer = value; }
		}
        
		/// <summary>
		/// Gets or sets the Type of the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		/// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
		public Type NodeLocator
		{
			get { return this.nodeLocator; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(IMemcachedNodeLocator));
				this.nodeLocator = value;
			}
		}

		/// <summary>
		/// Gets or sets the NodeLocatorFactory instance which will be used to create a new IMemcachedNodeLocator instances.
		/// </summary>
		/// <remarks>If both <see cref="M:NodeLocator"/> and  <see cref="M:NodeLocatorFactory"/> are assigned then the latter takes precedence.</remarks>
		public IProviderFactory<IMemcachedNodeLocator> NodeLocatorFactory { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialize or deserialize items.
		/// </summary>
		public ITranscoder Transcoder
		{
			get { return this.transcoder ?? (this.transcoder = new DefaultTranscoder()); }
			set { this.transcoder = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IPerformanceMonitor"/> instance which will be used monitor the performance of the client.
		/// </summary>
		public IPerformanceMonitor PerformanceMonitor { get; set; }

		/// <summary>
		/// Gets or sets the type of the communication between client and server.
		/// </summary>
		public MemcachedProtocol Protocol { get; set; }
       

		#region [ interface                     ]



		IList<System.Net.IPEndPoint> IMemcachedClientConfiguration.Servers
		{
			get { return this.Servers; }
		}

		ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
		{
			get { return this.SocketPool; }
		}

		IAuthenticationConfiguration IMemcachedClientConfiguration.Authentication
		{
			get { return this.Authentication; }
		}
        
		IMemcachedKeyTransformer IMemcachedClientConfiguration.CreateKeyTransformer()
		{
			return this.KeyTransformer;
		}

		IMemcachedNodeLocator IMemcachedClientConfiguration.CreateNodeLocator()
		{
			var f = this.NodeLocatorFactory;
			if (f != null) return f.Create();

			return this.NodeLocator == null
					? new DefaultNodeLocator()
					: (IMemcachedNodeLocator)FastActivator.Create(this.NodeLocator);
		}

		ITranscoder IMemcachedClientConfiguration.CreateTranscoder()
		{
			return this.Transcoder;
		}

        MemcachedProtocol IMemcachedClientConfiguration.CreateProtocol()
		{
			switch (this.Protocol)
			{
                case MemcachedProtocol.Text: return MemcachedProtocol.Text;
                case MemcachedProtocol.Binary: return MemcachedProtocol.Binary;
			}

			throw new ArgumentOutOfRangeException("Unknown protocol: " + (int)this.Protocol);
		}

		IPerformanceMonitor IMemcachedClientConfiguration.CreatePerformanceMonitor()
		{
			return this.PerformanceMonitor;
		}


        //string IMemcachedClientConfiguration.CacheName
        //{
        //    get { return this.CacheName; }
        //}
		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
