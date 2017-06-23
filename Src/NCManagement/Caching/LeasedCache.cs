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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Diagnostics;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching
{
	/// <summary>
	/// Summary description for LeasedCache.
	/// </summary>
	internal class LeasedCache : Cache
	{
		/// <summary>
		/// Sponsor used to extend lifetime of cache.
		/// </summary>
		private class LeasedCacheSponsor : ISponsor 
		{
			/// <summary>
			/// Requests a sponsoring client to renew the lease for the specified object.
			/// </summary>
			/// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
			/// <returns>The additional lease time for the specified object.</returns>
			public TimeSpan Renewal(ILease lease)
			{
				return TimeSpan.FromMinutes(10);
			}
		}

		/// <summary>
		/// The ISponsor object used to control the lifetime of cache.
		/// </summary>
		private static ISponsor		_sponsor = new LeasedCacheSponsor();

		/// <summary>
		/// Default constructor.
		/// </summary>
		internal LeasedCache()
		{
		}

		/// <summary>
		/// Overloaded constructor.
		/// </summary>
		/// <param name="configString"></param>
		internal LeasedCache(string configString):base(configString)
		{

		}

		#region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>
        /// </remarks>
        private void Dispose(bool disposing)
        {
            ILease lease = (ILease)RemotingServices.GetLifetimeService(this);
            if (lease != null)
            {
                lease.Unregister(_sponsor);
            }
            if (disposing) GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			try
			{
				Dispose(true);
			}
			finally
			{
				base.Dispose();
			}
		}

		#endregion

		
		/// <summary>
		/// Start the cache functionality.
		/// </summary>
        protected override void Start(CacheRenderer renderer, bool twoPhaseInitialization)
		{
            base.Start(renderer, twoPhaseInitialization);
		}

		/// <summary>
		/// Stop the internal working of the cache.
		/// </summary>
        public override void Stop()
		{
			base.Stop();
		}

		/// <summary>
		/// Start the cache functionality.
		/// </summary>
		public void StartInstance(CacheRenderer renderer, bool twoPhaseInitialization)
		{
            Start(renderer, twoPhaseInitialization);
		}

        public void StartInstancePhase2()
        {
            base.StartPhase2();
        }
		/// <summary>
		/// Stop the internal working of the cache.
		/// </summary>
        public void StopInstance()
		{
			Stop();
		}
	}
}
