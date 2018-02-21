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
using System.Diagnostics;
using System.Threading.Tasks;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using System.Threading;

namespace Alachisoft.NCache.SignalR
{
    public class NCacheProvider : ICacheProvider
    {
        private Cache _cache = null;
        private TraceSource _trace;
        private Action<int, NCacheMessage> messageHandler = null;
        private String eventKey = null;
        
        public async Task ConnectAsync(string cacheName, TraceSource trace)
        {
            try
            {
                _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheName);               
            }
            catch (Exception ex)
            {
                if (_cache != null)
                {
                    _cache.Dispose();
                    _cache = null;
                }
                throw new InvalidOperationException("failed to initialize cache");
            }            
            _cache.CacheStopped+=OnCacheStop;
            
            _trace = trace;
        }

        public async Task SubscribeAsync(string _eventKey, Action<int, NCacheMessage> OnMessage)
        {
            _trace.TraceInformation("subscribing to key: " + _eventKey);

            if (_cache != null)
            {
                this.eventKey = _eventKey;
                _cache.CustomEvent += this.OnCustomEvent;
                this.messageHandler = OnMessage;
            }
        }
        
        public void Close()
        {
            _trace.TraceInformation("Closing " + eventKey);

            if (_cache != null)
            {
                _cache.CustomEvent -= OnCustomEvent;
                _cache.CacheStopped -= OnCacheStop;

                _cache.Dispose();
            }
        }
        public void Dispose()
        {
            if (_cache != null)
            {
                this.eventKey = string.Empty;
                this.messageHandler = null;

                _cache.CustomEvent -= this.OnCustomEvent;
                _cache.Dispose();
            }
        }

        public Task PublishAsync(string key, byte[] messageArguments)
        {
            if (_cache == null)
            {
                throw new InvalidOperationException("cache has not been initialized");
            }

            return Task.Run(() => { try { _cache.RaiseCustomEvent(key, messageArguments); } catch (Exception) { } });        
        }
               
        public event Action<Exception> CacheStopped;        
        
        private void OnCacheStop(String cacheName)
        {
            _trace.TraceWarning("Cache "+cacheName+" Stopped");
            if (CacheStopped != null)
            {              
                var handler = CacheStopped;
                if (_cache != null)
                {
                    _cache.Dispose();
                    handler(new InvalidOperationException("cache " + cacheName + " stopped."));
                }
            }
        }

        private void OnCustomEvent(object notifId, object data) 
        {
            String eventKey = notifId as String;
            if(eventKey.Equals(this.eventKey) && this.messageHandler!=null)
            {
                messageHandler(0,NCacheMessage.FromBytes(data as byte[],this._trace));
            }
        }


        public ulong GetUniqueID()
        {                         
            var itemVersion = _cache.Insert(eventKey,new CacheItem(new byte[1]));
            return itemVersion.Version;            
        }
    }
}
