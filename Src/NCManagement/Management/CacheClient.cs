// Copyright (c) 2018 Alachisoft
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
using System.IO;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;

using Microsoft.Win32;



using Alachisoft.NCache.Runtime.Exceptions;



using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management.ServiceControl;

namespace Alachisoft.NCache.Management
{
	/// <summary>
	/// Manages client side connection to caches.
	/// </summary>
	public sealed class CacheClient
	{
		CacheClient() {}

		/// <summary>
		/// Initialize a registered cache given by the ID.
		/// </summary>
		/// <param name="cacheId"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
		/// <returns>A reference to <see cref="Cache"/> object.</returns>
		public static Cache GetCacheInstance(string cacheId)
		{
			return GetCacheInstance(cacheId, TimeSpan.FromSeconds(30));
		}

		/// <summary>
		/// Initialize a registered cache given by the ID.
		/// </summary>
		/// <param name="cacheId"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
		/// <returns>A reference to <see cref="Cache"/> object.</returns>
		public static Cache GetCacheInstance(string cacheId, TimeSpan timeout)
		{
			if(cacheId == null) throw new ArgumentNullException("cacheId");
			try
			{
				CacheConfig data = CacheConfigManager.GetCacheConfig(cacheId);
				return GetCacheInstance(data, timeout);
			}
			catch(Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Initialize a registered cache given by the ID.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentNullException">data is a null reference (Nothing in Visual Basic).</exception>
		/// <returns>A reference to <see cref="Cache"/> object.</returns>
		public static Cache GetCacheInstance(CacheConfig data, TimeSpan timeout)
		{
			if(data == null) throw new ArgumentNullException("data");
			try
			{
				if(data == null) return null;
				if(data.UseInProc)
				{
					return CacheFactory.CreateFromPropertyString(data.PropertyString);
				}

				Cache cache = ConnectCacheInstance(data, timeout);
				return cache;
			}
			catch(Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Creates and returns an instance of NCache.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="timeout"></param>
		/// <returns>A reference to <see cref="Cache"/> object.</returns>
		private static Cache ConnectCacheInstance(CacheConfig data, TimeSpan timeout)
		{
            CacheService ncache;
            ncache = new NCacheRPCService(data.ServerName, (int)data.Port);
       
			try
			{
				ncache.UseTcp = data.UseTcp;
				ncache.ServerName = data.ServerName;
				ncache.Port = data.Port;
				if(ncache.ServerName == null  || 
					ncache.ServerName.Length < 1 ||
					ncache.ServerName.CompareTo(".") == 0 ||
					ncache.ServerName.CompareTo("localhost") == 0)
				{
					ncache.ServerName = Environment.MachineName;
				}

               
			}
			catch(Exception)
			{
				throw;
			}
			finally
			{
				ncache.Dispose();
			}
			return null;
		}
	}
}
