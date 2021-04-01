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
using System.Text;
using System.Web.SessionState;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime;
namespace Alachisoft.NCache.Web.SessionStateManagement
{
	[CLSCompliant(false)]
    public interface ISessionCache
    {        
        /// <summary>
        /// prefix of the primary cache for local webserver.
        /// </summary>
        string PrimaryPrefix { get;}

        /// <summary>
        /// Gets The Cache Id
        /// </summary>
        string GetCacheId { get; }

        void Add(string sessionId, string key, object value);
        void Insert(string sessionId, string key, CacheItem item, string group);
        void Insert(string sessionId, string key, object value, bool enableRetry);
       
        object Get(string sessionId, string key, string group, string subGroup);
        bool Contains(string sessionId, string key);

        #region Overloads with locking support
		[CLSCompliant(false)]
        object Get(string sessionId, string key, ref LockHandle lockHandle, bool acquireLock, bool enableRetry);
        object Remove(string sessionId, string key, LockHandle lockHandle, bool enableRetry);

        void Insert(string sessionId, string key, CacheItem item, LockHandle lockHandle, bool releaseLock, bool enableRetry);
        void Insert(string sessionId, string key, CacheItem item, bool enableRetry);

        void Unlock(string sessionId, string key);
        #endregion
        void Add(string sessionId, string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority);
        void Insert(string sessionId, string key, object value,  DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority);

        object Remove(string sessionId, string key, bool enableRetry);
        object Get(string sessionId, string key, bool enableRetry);
        object Get(string key);
        object Remove(string key);
        void InitializeCache(string cache);
        void Dispose();

        IEnumerator GetEnumerator();

        string CurrentSessionCache 
        { get; set;}

     
        bool IsSessionCookieless //Raiynair
        { get; set;}
        /// <summary>
        /// This method is used incase the sessionId is not available. This operation is done on primary cache.
        /// e.g during initialization of SSP SessionID is not yet known and then SetApplicationId operation on cache is performed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="usePrimaryCache"></param>
        /// <returns></returns>
        void Add(string key, object value);
        void Insert(string key, object value);
        bool Contains(string key);
    }
}
