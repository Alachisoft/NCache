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
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Web.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Reflection;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configures the <see cref="T:MemcachedClient"/>. This class cannot be inherited.
	/// </summary>
	public sealed class MemcachedClientSection : ConfigurationSection, IMemcachedClientConfiguration
	{
		/// <summary>
		/// Returns a collection of Memcached servers which can be used by the client.
		/// </summary>
		[ConfigurationProperty("servers", IsRequired = true)]
		public EndPointElementCollection Servers
		{
			get { return (EndPointElementCollection)base["servers"]; }
		}

		/// <summary>
		/// Gets or sets the configuration of the socket pool.
		/// </summary>
		[ConfigurationProperty("socketPool", IsRequired = false)]
		public SocketPoolElement SocketPool
		{
			get { return (SocketPoolElement)base["socketPool"]; }
			set { base["socketPool"] = value; }
		}

		/// <summary>
		/// Gets or sets the configuration of the authenticator.
		/// </summary>
		[ConfigurationProperty("authentication", IsRequired = false)]
		public AuthenticationElement Authentication
		{
			get { return (AuthenticationElement)base["authentication"]; }
			set { base["authentication"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedNodeLocator"/> which will be used to assign items to Memcached nodes.
		/// </summary>
		[ConfigurationProperty("locator", IsRequired = false)]
		public ProviderElement<IMemcachedNodeLocator> NodeLocator
		{
			get { return (ProviderElement<IMemcachedNodeLocator>)base["locator"]; }
			set { base["locator"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IMemcachedKeyTransformer"/> which will be used to convert item keys for Memcached.
		/// </summary>
		[ConfigurationProperty("keyTransformer", IsRequired = false)]
		public ProviderElement<IMemcachedKeyTransformer> KeyTransformer
		{
			get { return (ProviderElement<IMemcachedKeyTransformer>)base["keyTransformer"]; }
			set { base["keyTransformer"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> which will be used serialzie or deserialize items.
		/// </summary>
		[ConfigurationProperty("transcoder", IsRequired = false)]
		public ProviderElement<ITranscoder> Transcoder
		{
			get { return (ProviderElement<ITranscoder>)base["transcoder"]; }
			set { base["transcoder"] = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Enyim.Caching.Memcached.IPerformanceMonitor"/> which will be used monitor the performance of the client.
		/// </summary>
		[ConfigurationProperty("performanceMonitor", IsRequired = false)]
		public ProviderElement<IPerformanceMonitor> PerformanceMonitor
		{
			get { return (ProviderElement<IPerformanceMonitor>)base["performanceMonitor"]; }
			set { base["performanceMonitor"] = value; }
		}

		/// <summary>
		/// Called after deserialization.
		/// </summary>
		protected override void PostDeserialize()
		{
			WebContext hostingContext = base.EvaluationContext.HostingContext as WebContext;

			if (hostingContext != null && hostingContext.ApplicationLevel == WebApplicationLevel.BelowApplication)
			{
				throw new InvalidOperationException("The " + this.SectionInformation.SectionName + " section cannot be defined below the application level.");
			}
		}

		/// <summary>
		/// Gets or sets the type of the communication between client and server.
		/// </summary>
		[ConfigurationProperty("protocol", IsRequired = false, DefaultValue = MemcachedProtocol.Binary)]
		public MemcachedProtocol Protocol
		{
			get { return (MemcachedProtocol)base["protocol"]; }
			set { base["protocol"] = value; }
		}

		#region [ IMemcachedClientConfiguration]
        

		IList<IPEndPoint> IMemcachedClientConfiguration.Servers
		{
			get { return this.Servers.ToIPEndPointCollection(); }
		}

		ISocketPoolConfiguration IMemcachedClientConfiguration.SocketPool
		{
			get { return this.SocketPool; }
		}

		IMemcachedKeyTransformer IMemcachedClientConfiguration.CreateKeyTransformer()
		{
			return this.KeyTransformer.CreateInstance() ?? new DefaultKeyTransformer();
		}

		IMemcachedNodeLocator IMemcachedClientConfiguration.CreateNodeLocator()
		{
			return this.NodeLocator.CreateInstance() ?? new DefaultNodeLocator();
		}

		ITranscoder IMemcachedClientConfiguration.CreateTranscoder()
		{
			return this.Transcoder.CreateInstance() ?? new DefaultTranscoder();
		}

		IAuthenticationConfiguration IMemcachedClientConfiguration.Authentication
		{
			get { return this.Authentication; }
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
			return this.PerformanceMonitor.CreateInstance();
		}
        
        //string IMemcachedClientConfiguration.CacheName
        //{
        //    get { return this.CacheName.ToString(); }
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
