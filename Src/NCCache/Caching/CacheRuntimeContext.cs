// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Serialization;

namespace Alachisoft.NCache.Caching
{
    internal class CacheRuntimeContext : IDisposable
    {
        /// <summary> The one and only manager of the whole cache sytem. </summary>
        private Cache _cacheRoot;

        /// <summary> Logger used for NCache Logging. </summary>
        private ILogger _logger;

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        private CacheBase _cacheImpl;
        /// <summary>Manager for implementing expiration</summary>
        public ExpirationManager ExpiryMgr;
        /// <summary> scheduler for auto-expiration tasks. </summary>
        public TimeScheduler TimeSched;
        /// <summary> Asynchronous event processor. </summary>
        public AsyncProcessor AsyncProc;
        /// <summary> The performance statistics collector object. </summary>
        public StatisticCounter PerfStatsColl;
        /// <summary> Serialization context(actually name of the cache) used by the Compact framework.</summary>
        private string _serializationContext;
        /// <summary> Renders the cache to its client. </summary>
        private CacheRenderer _renderer;

  
        private bool _isStartedAsMirror = false;


        public CacheRuntimeContext()
        {
        }


        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public Cache CacheRoot
        {
            get { return _cacheRoot; }
            set { _cacheRoot = value; }
        }

        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public CacheBase CacheImpl
        {
            get { return _cacheImpl; }
            set { _cacheImpl = value; }
        }

        

        public bool IsStartedAsMirror
        {
            get { return _isStartedAsMirror; }
            set { _isStartedAsMirror = value; }
        }
      
        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public CacheBase CacheInternal
        {
            get { return CacheImpl.InternalCache; }
        }

        /// <summary> Gets Cache serialization context used by CompactSerialization Framework. </summary>
        public string SerializationContext
        {
            get
            {
                return _serializationContext;
            }
            set
            {
                _serializationContext = value;
            }
        }

        public CacheRenderer Render
        {
            get { return _renderer; }
            set { _renderer = value; }
        }


        public ILogger NCacheLog
        {
            get { return _logger; }
            set { _logger = value; }
        }

#if !CLIENT
        /// <summary> The one and only manager of the whole cache sytem. </summary>
        public bool IsClusteredImpl
        {
            get { return Util.CacheHelper.IsClusteredCache(CacheImpl); }
        }
#endif

   
        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (SerializationContext != null)
                {
                    CompactFormatterServices.UnregisterAllCustomCompactTypes(SerializationContext);
                }

                if (PerfStatsColl != null)
                {
                    PerfStatsColl.Dispose();
                    PerfStatsColl = null;
                }
                if (ExpiryMgr != null)
                {
                    ExpiryMgr.Dispose();
                    ExpiryMgr = null;
                }
                if (CacheImpl != null)
                {
                    CacheImpl.Dispose();
                    CacheImpl = null;
                }
                if (TimeSched != null)
                {
                    TimeSched.Dispose();
                    TimeSched = null;
                }
                if (AsyncProc != null)
                {
                    AsyncProc.Stop();
                    AsyncProc = null;
                }

                if (disposing) GC.SuppressFinalize(this);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
